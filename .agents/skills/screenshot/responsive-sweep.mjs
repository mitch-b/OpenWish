// Responsive regression sweep: visits the core journeys as each persona at phone
// and desktop widths (light and dark) and fails on layout, error and
// accessibility regressions that are easy to introduce while polishing UI.
//
// Used by openwish-smoke.mjs, and runnable on its own against any development
// instance that has /auth/dev-login and /auth/dev-seed enabled:
//   node responsive-sweep.mjs http://127.0.0.1:5000 [outputDirectory]
import { chromium } from "playwright";
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

export const sweepViewports = [
  { name: "mobile", width: 390, height: 844 },
  { name: "desktop", width: 1366, height: 900 }
];

export function sweepRoutes(manifest) {
  return {
    owner: [
      "/",
      "/wishlists",
      `/wishlists/${manifest.wishlistPublicId}`,
      `/wishlists/${manifest.privateWishlistPublicId}`,
      "/wishlists/new",
      "/add",
      "/events",
      `/events/${manifest.eventPublicId}`,
      "/events/new",
      "/friends",
      "/gift-plan",
      "/whats-new",
      "/Account/Manage",
      "/Account/Manage/GiftProfile"
    ],
    friend: [
      "/",
      `/wishlists/${manifest.wishlistPublicId}`,
      `/wishlists/${manifest.friendWishlistPublicId}`,
      `/events/${manifest.eventPublicId}`,
      "/gift-plan"
    ],
    guest: ["/", "/events"],
    anonymous: ["/Account/Login", "/Account/Register", "/Account/ForgotPassword"]
  };
}

const ignoredConsoleErrors = [
  // Chromium reports these for aborted navigations and cancelled fetches while
  // the sweep moves quickly between pages; they are not application faults.
  /net::ERR_ABORTED/i,
  /Failed to load resource: net::ERR_/i
];

async function settle(page) {
  try {
    await page.waitForLoadState("networkidle", { timeout: 8_000 });
  } catch {
    // Long-lived connections can keep the network busy; layout checks below
    // still run against the rendered page.
  }
  await page.evaluate(() => document.fonts?.ready);
}

async function inspectPage(page, viewport) {
  return page.evaluate(({ isMobile }) => {
    const isVisible = element => {
      const style = getComputedStyle(element);
      if (style.visibility === "hidden" || style.display === "none" || Number(style.opacity) === 0) {
        return false;
      }
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    };
    const describe = element => {
      const text = (element.innerText || element.getAttribute("aria-label") || element.id || element.className || "")
        .toString().trim().replace(/\s+/g, " ").slice(0, 60);
      return `<${element.tagName.toLowerCase()}> "${text}"`;
    };
    const accessibleName = element => {
      const labelledBy = element.getAttribute("aria-labelledby");
      const labelledText = labelledBy
        ? labelledBy.split(/\s+/).map(id => document.getElementById(id)?.textContent ?? "").join(" ")
        : "";
      const imageAlt = [...element.querySelectorAll("img[alt]")].map(image => image.alt).join(" ");
      return [
        element.getAttribute("aria-label"),
        labelledText,
        element.innerText,
        element.getAttribute("title"),
        imageAlt,
        element.tagName === "INPUT" ? element.value : ""
      ].filter(Boolean).join(" ").trim();
    };

    const problems = [];
    const viewportWidth = document.documentElement.clientWidth;
    const contentWidth = document.documentElement.scrollWidth;
    if (contentWidth > viewportWidth) {
      problems.push(`page overflows horizontally (${contentWidth}px content in ${viewportWidth}px)`);
    }

    if (document.querySelector("#blazor-error-ui") && isVisible(document.querySelector("#blazor-error-ui"))) {
      problems.push("the Blazor error banner is visible");
    }

    const controls = [...document.querySelectorAll(
      "a[href], button, [role='button'], input[type='submit'], input[type='button']"
    )].filter(isVisible);
    for (const control of controls) {
      if (!accessibleName(control)) {
        problems.push(`control has no accessible name: ${describe(control)}`);
      }
    }

    if (isMobile) {
      const main = document.querySelector("main") ?? document.body;
      const actions = [...main.querySelectorAll("button, .btn, [role='button'], input[type='submit']")]
        .filter(isVisible)
        .filter(element => !element.closest("[data-sweep-allow-compact]"))
        .filter(element => !element.matches(".btn-link, .btn-close") || element.matches(".btn"));
      for (const action of actions) {
        const rect = action.getBoundingClientRect();
        if (rect.height + 0.01 < 44 && rect.width + 0.01 < 44) {
          problems.push(`touch target is ${Math.round(rect.width)}x${Math.round(rect.height)}px: ${describe(action)}`);
        } else if (rect.height + 0.01 < 32) {
          problems.push(`touch target is only ${Math.round(rect.height)}px tall: ${describe(action)}`);
        }
      }

      for (const element of main.querySelectorAll("*")) {
        if (!isVisible(element) ||
            element.closest(".visually-hidden, .visually-hidden-focusable, .sr-only, [data-sweep-allow-overflow], pre, code, table, .table-responsive")) {
          continue;
        }
        const rect = element.getBoundingClientRect();
        if (rect.right > viewportWidth + 1 && getComputedStyle(element).position !== "fixed") {
          const scroller = element.closest("[style*='overflow'], .overflow-auto, .overflow-x-auto");
          if (!scroller) {
            problems.push(`element extends past the viewport (${Math.round(rect.right)}px): ${describe(element)}`);
            break;
          }
        }
      }
    }

    return [...new Set(problems)];
  }, { isMobile: viewport.width < 576 });
}

export async function verifyResponsiveSweep(browser, { baseUrl, manifest, outputDirectory, themes = ["light", "dark"] }) {
  const routes = sweepRoutes(manifest);
  const failures = [];
  let checkedPages = 0;

  for (const [persona, personaRoutes] of Object.entries(routes)) {
    for (const theme of themes) {
      for (const viewport of sweepViewports) {
        if (theme === "dark" && viewport.name === "desktop") {
          continue;
        }

        const context = await browser.newContext({
          viewport: { width: viewport.width, height: viewport.height },
          colorScheme: theme,
          hasTouch: viewport.name === "mobile",
          isMobile: viewport.name === "mobile"
        });
        await context.addInitScript(selectedTheme => {
          try {
            localStorage.setItem("theme", selectedTheme);
          } catch {
            // Storage can be unavailable before the first navigation.
          }
        }, theme);
        if (persona !== "anonymous") {
          const response = await context.request.post(`${baseUrl}/auth/dev-login?persona=${persona}`);
          if (!response.ok()) {
            await context.close();
            throw new Error(`Responsive sweep could not sign in as ${persona}: ${response.status()}.`);
          }
        }

        const page = await context.newPage();
        const consoleErrors = [];
        const serverErrors = [];
        page.on("console", message => {
          if (message.type() === "error" && !ignoredConsoleErrors.some(pattern => pattern.test(message.text()))) {
            consoleErrors.push(message.text());
          }
        });
        page.on("pageerror", error => consoleErrors.push(error.message));
        page.on("response", response => {
          if (response.status() >= 500) {
            serverErrors.push(`${response.status()} ${new URL(response.url()).pathname}`);
          }
        });

        try {
          for (const route of personaRoutes) {
            consoleErrors.length = 0;
            serverErrors.length = 0;
            const label = `${persona} ${route} @ ${viewport.name}/${theme}`;
            const response = await page.goto(`${baseUrl}${route}`, { waitUntil: "domcontentloaded" });
            if (!response?.ok()) {
              failures.push(`${label}: returned ${response?.status() ?? "no response"}`);
              continue;
            }
            await settle(page);
            const problems = await inspectPage(page, viewport);
            problems.push(...consoleErrors.map(message => `console error: ${message.slice(0, 200)}`));
            problems.push(...serverErrors.map(message => `server error: ${message}`));
            checkedPages++;
            if (problems.length) {
              failures.push(`${label}:\n    - ${problems.join("\n    - ")}`);
              if (outputDirectory) {
                const fileName = `sweep-failure-${persona}-${viewport.name}-${theme}-${route.replace(/[^a-z0-9]+/gi, "-")}.png`;
                await page.screenshot({ path: path.join(outputDirectory, fileName), fullPage: true });
              }
            }
          }
        } finally {
          await context.close();
        }
      }
    }
  }

  if (failures.length) {
    throw new Error(`Responsive sweep found ${failures.length} page(s) with problems:\n${failures.join("\n")}`);
  }

  return {
    scenario: "responsive-sweep",
    assertions: [
      `${checkedPages} persona, route, viewport and theme combinations rendered without horizontal overflow`,
      "every visible link and button has an accessible name",
      "phone actions in the main content meet the 44px touch target (or are deliberately compact)",
      "no console errors, page errors, server errors or Blazor error banners"
    ]
  };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const baseUrl = process.argv[2] ?? process.env.OPENWISH_BASE_URL ?? "http://127.0.0.1:5000";
  const outputDirectory = process.argv[3] ?? process.env.OPENWISH_EVIDENCE_DIR;
  if (outputDirectory) {
    await fs.mkdir(outputDirectory, { recursive: true });
  }
  const browser = await chromium.launch();
  try {
    const seedContext = await browser.newContext();
    await seedContext.request.post(`${baseUrl}/auth/dev-login?persona=owner`);
    const seed = await seedContext.request.post(`${baseUrl}/auth/dev-seed`);
    if (!seed.ok()) {
      throw new Error(`Development seed failed with ${seed.status()}.`);
    }
    const manifest = await seed.json();
    await seedContext.close();
    const result = await verifyResponsiveSweep(browser, { baseUrl, manifest, outputDirectory });
    console.log(JSON.stringify(result, null, 2));
  } finally {
    await browser.close();
  }
}
