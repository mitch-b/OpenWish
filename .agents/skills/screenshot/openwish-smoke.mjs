import { chromium } from "playwright";
import fs from "node:fs/promises";
import path from "node:path";

const baseUrl = process.env.OPENWISH_BASE_URL ?? "http://web:8080";
const outputDirectory = process.env.OPENWISH_EVIDENCE_DIR ?? "/evidence";
const scenarios = [
  { name: "desktop", viewport: { width: 1440, height: 1000 } },
  { name: "mobile", viewport: { width: 390, height: 844 }, isMobile: true }
];

async function waitUntilReady(request) {
  let lastError;

  for (let attempt = 1; attempt <= 60; attempt += 1) {
    try {
      const response = await request.get(`${baseUrl}/alive`);
      if (response.ok()) {
        return;
      }
      lastError = new Error(`Health check returned ${response.status()}.`);
    } catch (error) {
      lastError = error;
    }

    await new Promise(resolve => setTimeout(resolve, 2000));
  }

  throw lastError ?? new Error("OpenWish did not become ready.");
}

await fs.mkdir(outputDirectory, { recursive: true });

const browser = await chromium.launch();
const results = [];

try {
  const readinessContext = await browser.newContext();
  await waitUntilReady(readinessContext.request);
  await readinessContext.close();

  for (const scenario of scenarios) {
    const browserErrors = [];
    const failedResponses = [];
    const context = await browser.newContext({
      viewport: scenario.viewport,
      isMobile: scenario.isMobile ?? false
    });
    const page = await context.newPage();

    page.on("console", message => {
      if (message.type() === "error") {
        browserErrors.push(message.text());
      }
    });
    page.on("pageerror", error => browserErrors.push(error.message));
    page.on("response", response => {
      if (response.status() >= 400) {
        failedResponses.push(`${response.status()} ${response.url()}`);
      }
    });

    const loginResponse = await context.request.post(`${baseUrl}/auth/dev-login?persona=owner`);
    if (!loginResponse.ok()) {
      throw new Error(`Development login failed with ${loginResponse.status()}.`);
    }

    const userResponse = await context.request.get(`${baseUrl}/api/account/user`);
    if (!userResponse.ok()) {
      throw new Error(`Authenticated user check failed with ${userResponse.status()}.`);
    }
    const user = await userResponse.json();
    if (user.email !== "playwright-owner@openwish.local") {
      throw new Error(`Unexpected verification user: ${JSON.stringify(user)}`);
    }

    let authorizationStatus;
    if (scenario.name === "desktop") {
      const createResponse = await context.request.post(`${baseUrl}/api/wishlists`, {
        data: {
          name: "Private verification wishlist",
          isPrivate: true
        }
      });
      if (createResponse.status() !== 201) {
        throw new Error(`Wishlist creation failed with ${createResponse.status()}.`);
      }
      const wishlist = await createResponse.json();

      const guestContext = await browser.newContext();
      const guestLoginResponse = await guestContext.request.post(`${baseUrl}/auth/dev-login?persona=guest`);
      if (!guestLoginResponse.ok()) {
        throw new Error(`Guest login failed with ${guestLoginResponse.status()}.`);
      }

      const forbiddenDelete = await guestContext.request.delete(`${baseUrl}/api/wishlists/${wishlist.publicId}`);
      authorizationStatus = forbiddenDelete.status();
      await guestContext.close();

      if (authorizationStatus !== 403) {
        throw new Error(`Unauthorized wishlist deletion returned ${authorizationStatus}, expected 403.`);
      }
    }

    await page.goto(baseUrl, { waitUntil: "domcontentloaded" });
    await page.getByRole("heading", { name: "Welcome Back!" }).waitFor();
    await page.getByRole("link", { name: "Create Wishlist" }).waitFor();
    await page.screenshot({
      path: path.join(outputDirectory, `openwish-home-${scenario.name}.png`),
      fullPage: true
    });

    await page.goto(`${baseUrl}/whats-new`, { waitUntil: "domcontentloaded" });
    await page.getByRole("heading", { name: "What's new" }).waitFor();
    await page.getByText("Version 0.1.0").waitFor();
    await page.getByText("Sustainable improvements").waitFor();

    if (browserErrors.length > 0) {
      throw new Error(`Browser errors: ${browserErrors.join(" | ")}`);
    }
    if (failedResponses.length > 0) {
      throw new Error(`Failed responses: ${failedResponses.join(" | ")}`);
    }

    results.push({
      scenario: scenario.name,
      loginStatus: loginResponse.status(),
      userStatus: userResponse.status(),
      authorizationStatus,
      homeHeading: "Welcome Back!",
      releaseVersion: "0.1.0",
      screenshot: `openwish-home-${scenario.name}.png`
    });

    await context.close();
  }

  await fs.writeFile(
    path.join(outputDirectory, "openwish-e2e-result.json"),
    `${JSON.stringify({ passed: true, baseUrl, scenarios: results }, null, 2)}\n`
  );
} catch (error) {
  await fs.writeFile(
    path.join(outputDirectory, "openwish-e2e-result.json"),
    `${JSON.stringify({ passed: false, baseUrl, error: error.message, scenarios: results }, null, 2)}\n`
  );
  throw error;
} finally {
  await browser.close();
}
