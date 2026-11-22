import { readFile, writeFile } from "node:fs/promises";
import { chromium, devices } from "playwright";

// The browser shares the app container's network namespace; loopback enables Cache Storage.
const origin = "http://localhost:8080";
const mode = process.env.BENCHMARK_MODE;
const sampleCount = Number(process.env.BENCHMARK_SAMPLES || 5);
const profiles = {
  desktop: { browser: { viewport: { width: 1280, height: 900 } } },
  mobile: {
    browser: devices["Pixel 7"],
    cpu: 4,
    network: { offline: false, latency: 100, downloadThroughput: 1_250_000, uploadThroughput: 250_000 },
  },
};
const requestedProfiles = (process.env.BENCHMARK_PROFILES || "desktop,mobile").split(",");
if (!["Auto", "Server", "WebAssembly"].includes(mode) ||
    !Number.isSafeInteger(sampleCount) || sampleCount < 1 ||
    requestedProfiles.some(profile => !Object.hasOwn(profiles, profile))) {
  throw new Error("Invalid benchmark mode, sample count, or profile");
}

const inventory = JSON.parse(await readFile("/results/components.json", "utf8"));
const routes = inventory.filter(row => row.boundary === "Auto" && row.route);
const layoutKeys = inventory.filter(row => row.boundary === "Auto" && !row.route).map(row => row.key);
const browser = await chromium.launch();
const samples = [];
let fixtures;

async function waitForApp() {
  const client = await browser.newContext();
  try {
    let last;
    for (let attempt = 0; attempt < 45; attempt++) {
      try {
        const response = await client.request.get(`${origin}/alive`, { timeout: 2000 });
        if (response.ok()) return;
        last = new Error(`Health endpoint returned ${response.status()}`);
      } catch (error) {
        last = error;
      }
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
    throw new Error("Isolated web app did not start", { cause: last });
  } finally {
    await client.close();
  }
}

async function authenticated(persona, options = {}) {
  const context = await browser.newContext({ ...options, serviceWorkers: "block" });
  try {
    const response = await context.request.post(`${origin}/auth/dev-login?persona=${persona}`);
    if (!response.ok()) throw new Error(`Login for ${persona}: ${response.status()}`);
    return context;
  } catch (error) {
    await context.close();
    throw error;
  }
}

async function fixtureCode() {
  const context = await authenticated("friend2");
  try {
    const response = await context.request.post(`${origin}/api/friends/join/code`);
    if (!response.ok()) throw new Error(`Friend join code: ${response.status()}`);
    const { code } = await response.json();
    if (!code) throw new Error("Friend join code response was empty");
    return code;
  } finally {
    await context.close();
  }
}

function visitFor({ key, route }, code) {
  const path = route.replace("{EventId}", fixtures.eventPublicId)
    .replace("{WishlistId}", fixtures.wishlistPublicId).replace("{Code}", code);
  if (path.includes("{")) throw new Error(`Add a fixture for ${route}`);
  if (key === "Pages/Events/AcceptInvite") {
    return { path: `${path}?email=playwright-guest%40openwish.local`, persona: "guest" };
  }
  if (key === "Pages/Social/FriendJoin") return { path, persona: "guest" };
  return { path, persona: "owner" };
}

async function newPage(context, profile) {
  const page = await context.newPage();
  if (profile.network) {
    const cdp = await context.newCDPSession(page);
    await cdp.send("Network.emulateNetworkConditions", profile.network);
    await cdp.send("Emulation.setCPUThrottlingRate", { rate: profile.cpu });
  }
  await page.addInitScript(() => {
    const probes = Object.create(null);
    window.openWishBench = { probes };
    const record = node => {
      if (!(node instanceof Element)) return;
      for (const probe of [node, ...node.querySelectorAll("[data-render-benchmark]")]) {
        const key = probe.dataset.renderBenchmark;
        if (key && !probes[key]) {
          probes[key] = { readyMs: performance.now(), renderer: probe.dataset.renderer };
        }
      }
    };
    new MutationObserver(records => {
      for (const recordEntry of records) for (const node of recordEntry.addedNodes) record(node);
    }).observe(document, { childList: true, subtree: true });
  });
  return page;
}

async function navigate(page, path) {
  const response = await page.goto(`${origin}${path}`, { waitUntil: "domcontentloaded", timeout: 60_000 });
  if (response?.status() !== 200 || new URL(page.url()).pathname !== new URL(path, origin).pathname) {
    throw new Error(`${path}: unexpected navigation ${response?.status()} to ${new URL(page.url()).pathname}`);
  }
}

async function prime(page, path, key) {
  const expected = mode === "Server" ? "Server" : "WebAssembly";
  // Auto can start Server first while it populates the WASM cache. Do not call that warm WASM.
  for (let attempt = 0; attempt < 6; attempt++) {
    await navigate(page, path);
    await page.waitForFunction(key => window.openWishBench.probes[key], key, { timeout: 45_000 });
    if (await page.evaluate(key => window.openWishBench.probes[key].renderer, key) === expected) return;
    await page.waitForLoadState("networkidle", { timeout: 45_000 });
    await page.waitForTimeout(500);
  }
  const cache = await page.evaluate(async () => ({
    secure: isSecureContext, keys: window.caches && await caches.keys(),
    wasmLoads: performance.getEntriesByType("resource").filter(entry => entry.name.includes(".wasm")).length,
  }));
  throw new Error(`Could not prime ${mode} on ${path} for ${expected}: ${JSON.stringify(cache)}`);
}

async function measure(page, path, keys, cache) {
  await navigate(page, path);
  await page.waitForFunction(keys => keys.every(key => window.openWishBench.probes[key]), keys, { timeout: 45_000 });
  const data = await page.evaluate(async keys => {
    const results = {};
    for (const key of keys) {
      const probe = document.querySelector(`[data-render-benchmark="${key}"]`);
      const actionMs = [];
      for (let i = 0; i < 3; i++) {
        const previous = probe.dataset.probeCount;
        actionMs.push(await new Promise((resolve, reject) => {
          const started = performance.now();
          const timeout = setTimeout(() => {
            observer.disconnect();
            reject(new Error(`${key}: renderer did not handle probe click`));
          }, 10_000);
          const observer = new MutationObserver(() => {
            if (probe.dataset.probeCount === previous) return;
            clearTimeout(timeout);
            observer.disconnect();
            resolve(performance.now() - started);
          });
          observer.observe(probe, { attributes: true, attributeFilter: ["data-probe-count"] });
          probe.click();
        }));
      }
      results[key] = { ...window.openWishBench.probes[key], actionMs };
    }
    const navigation = performance.getEntriesByType("navigation")[0];
    const paint = performance.getEntriesByName("first-contentful-paint")[0];
    return { probes: results, ttfbMs: navigation.responseStart, fcpMs: paint?.startTime ?? null };
  }, keys);

  await page.waitForLoadState("networkidle", { timeout: 45_000 });
  data.frameworkBytes = await page.evaluate(() => performance.getEntriesByType("resource")
    .filter(resource => new URL(resource.name).pathname.startsWith("/_framework/"))
    .reduce((sum, resource) => sum + resource.transferSize, 0));
  const expected = mode === "Auto" ? (cache === "cold" ? "Server" : "WebAssembly") : mode;
  for (const [key, { renderer }] of Object.entries(data.probes)) {
    if (renderer !== expected && !(mode === "Auto" && cache === "cold" && renderer === "WebAssembly")) {
      throw new Error(`${path} ${key} (${mode}, ${cache}): observed ${renderer}, expected ${expected}`);
    }
  }
  return data;
}

try {
  await waitForApp();
  const seed = await authenticated("owner");
  try {
    const response = await seed.request.post(`${origin}/auth/dev-seed`);
    if (!response.ok()) throw new Error(`Isolated fixture seed: ${response.status()}`);
    fixtures = await response.json();
    if (!fixtures.eventPublicId || !fixtures.wishlistPublicId) throw new Error("Missing benchmark fixtures");
  } finally {
    await seed.close();
  }
  for (const name of requestedProfiles) {
    const profile = profiles[name];
    // Warm the process and DB, not the measured browser contexts.
    const warmup = await authenticated("owner", profile.browser);
    try {
      const page = await newPage(warmup, profile);
      await prime(page, "/", "Pages/Home");
    } finally {
      await warmup.close();
    }
    for (let sample = 0; sample < sampleCount; sample++) {
      const code = await fixtureCode();
      const warmContexts = new Map();
      try {
        for (const { key, route } of routes) {
          const { path, persona } = visitFor({ key, route }, code);
          // Measure the two layout roots once on the dashboard, not once per route.
          const keys = key === "Pages/Home" ? [key, ...layoutKeys] : [key];
          for (const cache of sample % 2 === 0 ? ["cold", "warm"] : ["warm", "cold"]) {
            let context;
            let page;
            try {
              if (cache === "warm") {
                if (!warmContexts.has(persona)) {
                  context = await authenticated(persona, profile.browser);
                  warmContexts.set(persona, context);
                  const primer = await newPage(context, profile);
                  await prime(primer, "/", "Pages/Home");
                  await primer.close();
                }
                context = warmContexts.get(persona);
              } else {
                context = await authenticated(persona, profile.browser);
              }
              page = await newPage(context, profile);
              const measurement = await measure(page, path, keys, cache);
              samples.push({ profile: name, cache, sample, route, ...measurement });
            } catch (error) {
              throw new Error(`${mode}/${name}/${cache}/${key}: ${error.message}`, { cause: error });
            } finally {
              await page?.close();
              if (cache === "cold") await context?.close();
            }
          }
        }
      } finally {
        await Promise.all([...warmContexts.values()].map(context => context.close()));
      }
      console.log(`${mode}/${name}: ${sample + 1}/${sampleCount} rounds`);
      await writeFile(`/results/${mode}.json`, JSON.stringify({
        mode, browser: browser.version(), profiles, samples,
      }, null, 2));
    }
  }
} finally {
  await browser.close();
}
