import { chromium } from "playwright";
import fs from "node:fs/promises";
import path from "node:path";

const baseUrl = process.env.OPENWISH_BASE_URL ?? "http://web:8080";
const evidenceDirectory = process.env.OPENWISH_EVIDENCE_DIR ?? "/evidence";
const walkthroughDirectory = process.env.OPENWISH_WALKTHROUGH_DIR ?? evidenceDirectory;
const ownerEmail = "playwright-owner@openwish.local";
const guestEmail = "playwright-guest@openwish.local";
const friendEmail = "playwright-friend@openwish.local";
const expectedManifest = {
  wishlistPublicId: "demo-family-gift-ideas",
  privateWishlistPublicId: "demo-private-ideas",
  friendWishlistPublicId: "demo-jordan-favorites",
  eventPublicId: "demo-holiday-gift-exchange"
};

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

async function login(context, persona, expectedEmail) {
  const response = await context.request.post(`${baseUrl}/auth/dev-login?persona=${persona}`);
  if (!response.ok()) {
    throw new Error(`${persona} development login failed with ${response.status()}.`);
  }

  const userResponse = await context.request.get(`${baseUrl}/api/account/user`);
  if (!userResponse.ok()) {
    throw new Error(`${persona} account check failed with ${userResponse.status()}.`);
  }

  const user = await userResponse.json();
  if (user.email !== expectedEmail) {
    throw new Error(`Unexpected ${persona} user: ${JSON.stringify(user)}`);
  }

  return response.status();
}

function monitorPage(page) {
  const browserErrors = [];
  const failedResponses = [];

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

  return { browserErrors, failedResponses };
}

async function assertVisible(page, text) {
  await page.getByText(text, { exact: false }).first().waitFor({ state: "visible" });
}

async function visit(page, route, expectedText, visitedRoutes) {
  const response = await page.goto(`${baseUrl}${route}`, { waitUntil: "domcontentloaded" });
  if (!response?.ok()) {
    throw new Error(`${route} returned ${response?.status() ?? "no response"}.`);
  }

  await assertVisible(page, expectedText);
  const blazorError = page.locator("#blazor-error-ui");
  if (await blazorError.isVisible()) {
    throw new Error(`Blazor error UI was visible on ${route}.`);
  }

  visitedRoutes.push(route);
  return response;
}

async function screenshot(page, fileName) {
  await page.screenshot({
    path: path.join(walkthroughDirectory, fileName),
    fullPage: true
  });
}

async function verifyOwnerJourney(browser, manifest, results) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];
  const loginStatus = await login(context, "owner", ownerEmail);
  const homeResponse = await visit(page, "/", "Welcome Back!", visitedRoutes);
  const contentSecurityPolicy = homeResponse.headers()["content-security-policy"];
  if (!contentSecurityPolicy?.includes("frame-ancestors 'none'")) {
    throw new Error("The home page did not include the expected Content-Security-Policy.");
  }
  if (homeResponse.headers()["x-content-type-options"] !== "nosniff") {
    throw new Error("The home page did not include X-Content-Type-Options: nosniff.");
  }

  const notificationsResponse = await context.request.get(`${baseUrl}/api/notifications?includeRead=true`);
  if (!notificationsResponse.ok()) {
    throw new Error(`Owner notifications returned ${notificationsResponse.status()}.`);
  }
  const notifications = await notificationsResponse.json();
  const notificationPublicId = notifications[0]?.publicId;
  if (!notificationPublicId) {
    throw new Error("Owner security checks require a seeded notification.");
  }

  const itemsResponse = await context.request.get(
    `${baseUrl}/api/wishlists/${manifest.wishlistPublicId}/items`
  );
  const ownerItems = await itemsResponse.json();
  if (ownerItems.some(item => item.reservations?.length > 0)) {
    throw new Error("Wishlist owner received reservation details.");
  }

  const unsafeItem = { ...ownerItems[0], url: "javascript:alert(1)" };
  const unsafeItemResponse = await context.request.put(
    `${baseUrl}/api/wishlists/${manifest.wishlistPublicId}/items/${unsafeItem.id}`,
    { data: unsafeItem }
  );
  if (unsafeItemResponse.status() !== 400) {
    throw new Error(`Unsafe wishlist URL returned ${unsafeItemResponse.status()}, expected 400.`);
  }

  const privateScrape = await context.request.post(`${baseUrl}/api/products/scrape`, {
    data: { productUrl: "http://127.0.0.1:8080/" }
  });
  if (privateScrape.status() !== 204) {
    throw new Error(`Private-network scrape returned ${privateScrape.status()}, expected 204.`);
  }

  await assertVisible(page, "Family Gift Ideas");
  await assertVisible(page, "Holiday Gift Exchange");
  await assertVisible(page, "Friend Requests");
  await screenshot(page, "home-dashboard.png");

  await visit(page, "/wishlists", "Manage your wishlists", visitedRoutes);
  await assertVisible(page, "Family Gift Ideas");
  await assertVisible(page, "Private Ideas");
  await screenshot(page, "wishlists.png");

  await page.getByRole("tab", { name: "Friends' Wishlists" }).click();
  await assertVisible(page, "Jordan's Favorites");

  await visit(page, `/wishlists/${manifest.wishlistPublicId}`, "Family Gift Ideas", visitedRoutes);
  await assertVisible(page, "Noise-Cancelling Headphones");
  await assertVisible(page, "Cast-Iron Dutch Oven");
  await assertVisible(page, "National Park Pass");
  await assertVisible(page, "$249.99");
  await assertVisible(page, "3");
  await screenshot(page, "wishlist-details.png");

  await visit(page, "/wishlists/new", "Create a Wishlist", visitedRoutes);
  await visit(page, `/wishlists/${manifest.wishlistPublicId}/manage`, "Manage Wishlist", visitedRoutes);
  await assertVisible(page, "Who Can See This?");
  await visit(page, `/wishlists/${manifest.wishlistPublicId}/items/new`, "Add Item to Wishlist", visitedRoutes);

  await visit(page, "/events", "Plan gift exchanges", visitedRoutes);
  await assertVisible(page, "Holiday Gift Exchange");
  await screenshot(page, "events.png");

  await visit(page, `/events/${manifest.eventPublicId}`, "Holiday Gift Exchange", visitedRoutes);
  await assertVisible(page, "Your Gift Exchange Match");
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "Suggested Budget");
  await assertVisible(page, "TaylorDemo");
  await screenshot(page, "event-details.png");

  await visit(page, "/events/new", "Create New Event", visitedRoutes);
  await visit(page, `/events/${manifest.eventPublicId}/manage`, "Manage Event", visitedRoutes);
  await assertVisible(page, "Manage Participants");

  await visit(page, "/friends", "Connect with friends", visitedRoutes);
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "TaylorDemo");
  await screenshot(page, "friends.png");

  await page.locator(".notification-bell").click();
  await assertVisible(page, "Event invitation");
  await assertVisible(page, "Wishlist activity");
  await screenshot(page, "notifications.png");
  await page.getByRole("button", { name: "Close notifications" }).click();

  await page.getByRole("checkbox", { name: "Toggle dark or light theme" }).evaluate(element => {
    element.checked = true;
    element.dispatchEvent(new Event("change", { bubbles: true }));
  });
  await page.waitForFunction(() => localStorage.getItem("theme") === "dark");
  const selectedTheme = await page.evaluate(() => localStorage.getItem("theme"));
  if (selectedTheme !== "dark") {
    throw new Error(`Theme toggle stored '${selectedTheme}' instead of 'dark'.`);
  }
  await page.reload({ waitUntil: "domcontentloaded" });
  const persistedTheme = await page.evaluate(() => document.documentElement.dataset.theme);
  if (persistedTheme !== "dark") {
    throw new Error(`Theme did not persist after reload; found '${persistedTheme}'.`);
  }

  await visit(page, "/whats-new", "What's new", visitedRoutes);
  await assertVisible(page, "Version 0.1.0");
  await assertVisible(page, "Sustainable improvements");

  await visit(page, "/Account/Manage", "Profile", visitedRoutes);
  const username = await page.locator("#username").inputValue();
  if (username !== "AlexDemo") {
    throw new Error(`Profile displayed unexpected username '${username}'.`);
  }

  if (diagnostics.browserErrors.length > 0) {
    throw new Error(`Owner browser errors: ${diagnostics.browserErrors.join(" | ")}`);
  }
  if (diagnostics.failedResponses.length > 0) {
    throw new Error(`Owner failed responses: ${diagnostics.failedResponses.join(" | ")}`);
  }

  results.push({
    scenario: "owner-desktop",
    loginStatus,
    visitedRoutes,
    assertions: [
      "dashboard data",
      "owned and friend wishlists",
      "wishlist items and pricing",
      "event details and gift assignment",
      "friends and pending requests",
      "notifications",
      "theme persistence",
      "release history",
      "account profile"
    ]
  });
  await context.close();
  return { notificationPublicId };
}

async function verifyGuestJourney(browser, manifest, securityFixture, results) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];
  const loginStatus = await login(context, "guest", guestEmail);

  const forbiddenSeed = await context.request.post(`${baseUrl}/auth/dev-seed`);
  if (forbiddenSeed.status() !== 403) {
    throw new Error(`Guest seed attempt returned ${forbiddenSeed.status()}, expected 403.`);
  }

  const forbiddenDelete = await context.request.delete(
    `${baseUrl}/api/wishlists/${manifest.privateWishlistPublicId}`
  );
  if (forbiddenDelete.status() !== 403) {
    throw new Error(`Cross-user wishlist deletion returned ${forbiddenDelete.status()}, expected 403.`);
  }

  const guestEventResponse = await context.request.get(
    `${baseUrl}/api/events/${manifest.eventPublicId}`
  );
  if (!guestEventResponse.ok()) {
    throw new Error(`Guest event lookup returned ${guestEventResponse.status()}.`);
  }
  const guestEvent = await guestEventResponse.json();

  const sharedWishlistResponse = await context.request.get(
    `${baseUrl}/api/wishlists/${manifest.wishlistPublicId}`
  );
  if (!sharedWishlistResponse.ok()) {
    throw new Error(`Shared wishlist lookup returned ${sharedWishlistResponse.status()}.`);
  }
  const sharedWishlist = await sharedWishlistResponse.json();
  if (sharedWishlist.event !== null) {
    throw new Error("Event metadata was disclosed through a wishlist to a non-member.");
  }

  const forbiddenEventUpdate = await context.request.put(
    `${baseUrl}/api/events/${manifest.eventPublicId}`,
    { data: { ...guestEvent, name: "Unauthorized update" } }
  );
  if (forbiddenEventUpdate.status() !== 403) {
    throw new Error(`Cross-user event update returned ${forbiddenEventUpdate.status()}, expected 403.`);
  }

  const forbiddenEventDelete = await context.request.delete(
    `${baseUrl}/api/events/${manifest.eventPublicId}`
  );
  if (forbiddenEventDelete.status() !== 403) {
    throw new Error(`Cross-user event deletion returned ${forbiddenEventDelete.status()}, expected 403.`);
  }

  for (const route of [
    `/api/events/${manifest.eventPublicId}/invitations`,
    `/api/events/${manifest.eventPublicId}/pairing-rules`,
    `/api/events/${manifest.eventPublicId}/wishlists`
  ]) {
    const response = await context.request.get(`${baseUrl}${route}`);
    if (response.status() !== 403) {
      throw new Error(`Unauthorized event metadata request to ${route} returned ${response.status()}.`);
    }
  }

  const crossUserNotification = await context.request.put(
    `${baseUrl}/api/notifications/${securityFixture.notificationPublicId}/read`
  );
  if (crossUserNotification.status() !== 404) {
    throw new Error(
      `Cross-user notification mutation returned ${crossUserNotification.status()}, expected 404.`
    );
  }

  const guestItemsResponse = await context.request.get(
    `${baseUrl}/api/wishlists/${manifest.wishlistPublicId}/items`
  );
  const guestItems = await guestItemsResponse.json();
  const anonymousReservation = guestItems.flatMap(item => item.reservations ?? [])
    .find(reservation => reservation.isAnonymous);
  if (!anonymousReservation ||
      anonymousReservation.userId !== "" ||
      anonymousReservation.user !== null) {
    throw new Error("Anonymous reservation disclosed the reserving user's identity.");
  }

  await visit(page, "/events", "Pending Invitations", visitedRoutes);
  await assertVisible(page, "Holiday Gift Exchange");
  await visit(
    page,
    `/events/${manifest.eventPublicId}/accept-invite?email=${encodeURIComponent(guestEmail)}`,
    "You're almost in!",
    visitedRoutes
  );
  await assertVisible(page, "Accept invite");

  await visit(page, `/wishlists/${manifest.wishlistPublicId}`, "Family Gift Ideas", visitedRoutes);
  await assertVisible(page, "Reserved");

  if (diagnostics.browserErrors.length > 0) {
    throw new Error(`Guest browser errors: ${diagnostics.browserErrors.join(" | ")}`);
  }
  if (diagnostics.failedResponses.length > 0) {
    throw new Error(`Guest failed responses: ${diagnostics.failedResponses.join(" | ")}`);
  }

  results.push({
    scenario: "guest-collaboration",
    loginStatus,
    seedAuthorizationStatus: forbiddenSeed.status(),
    deleteAuthorizationStatus: forbiddenDelete.status(),
    visitedRoutes
  });
  await context.close();
}

async function verifyFriendJourney(browser, manifest, results) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];
  const loginStatus = await login(context, "friend", friendEmail);

  for (const route of [
    `/api/events/${manifest.eventPublicId}`,
    `/api/events/${manifest.eventPublicId}/wishlists`
  ]) {
    const response = await context.request.get(`${baseUrl}${route}`);
    if (!response.ok()) {
      throw new Error(`Friend event data request to ${route} returned ${response.status()}.`);
    }

    const payload = await response.json();
    const wishlists = Array.isArray(payload) ? payload : payload.eventWishlists;
    if ((wishlists ?? []).some(wishlist => wishlist.owner?.email)) {
      throw new Error(`Event wishlist owner email was disclosed by ${route}.`);
    }
  }

  await visit(page, `/events/${manifest.eventPublicId}`, "Holiday Gift Exchange", visitedRoutes);
  await assertVisible(page, "You're Shopping For:");
  await assertVisible(page, "AlexDemo");
  await assertVisible(page, "My Reserved Items");
  await assertVisible(page, "Noise-Cancelling Headphones");

  if (diagnostics.browserErrors.length > 0) {
    throw new Error(`Friend browser errors: ${diagnostics.browserErrors.join(" | ")}`);
  }
  if (diagnostics.failedResponses.length > 0) {
    throw new Error(`Friend failed responses: ${diagnostics.failedResponses.join(" | ")}`);
  }

  results.push({ scenario: "friend-gift-exchange", loginStatus, visitedRoutes });
  await context.close();
}

async function verifyMobileJourney(browser, manifest, results) {
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true
  });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];
  const loginStatus = await login(context, "owner", ownerEmail);

  await visit(page, "/", "Welcome Back!", visitedRoutes);
  await assertVisible(page, "Family Gift Ideas");
  await screenshot(page, "home-mobile.png");

  await visit(page, `/wishlists/${manifest.wishlistPublicId}`, "Family Gift Ideas", visitedRoutes);
  await assertVisible(page, "Noise-Cancelling Headphones");
  await screenshot(page, "wishlist-mobile.png");

  if (diagnostics.browserErrors.length > 0) {
    throw new Error(`Mobile browser errors: ${diagnostics.browserErrors.join(" | ")}`);
  }
  if (diagnostics.failedResponses.length > 0) {
    throw new Error(`Mobile failed responses: ${diagnostics.failedResponses.join(" | ")}`);
  }

  results.push({ scenario: "owner-mobile", loginStatus, visitedRoutes });
  await context.close();
}

await fs.mkdir(evidenceDirectory, { recursive: true });
await fs.mkdir(walkthroughDirectory, { recursive: true });

const browser = await chromium.launch();
const results = [];

try {
  const readinessContext = await browser.newContext();
  await waitUntilReady(readinessContext.request);
  await readinessContext.close();

  const seedContext = await browser.newContext();
  await login(seedContext, "owner", ownerEmail);
  const seedResponse = await seedContext.request.post(`${baseUrl}/auth/dev-seed`);
  if (!seedResponse.ok()) {
    throw new Error(`Development data seed failed with ${seedResponse.status()}: ${await seedResponse.text()}`);
  }
  const manifest = await seedResponse.json();
  await seedContext.close();

  for (const [key, expectedValue] of Object.entries(expectedManifest)) {
    if (manifest[key] !== expectedValue) {
      throw new Error(`Seed manifest '${key}' was '${manifest[key]}', expected '${expectedValue}'.`);
    }
  }

  const securityFixture = await verifyOwnerJourney(browser, manifest, results);
  await verifyGuestJourney(browser, manifest, securityFixture, results);
  await verifyFriendJourney(browser, manifest, results);
  await verifyMobileJourney(browser, manifest, results);

  await fs.writeFile(
    path.join(evidenceDirectory, "openwish-e2e-result.json"),
    `${JSON.stringify({ passed: true, baseUrl, manifest, scenarios: results }, null, 2)}\n`
  );
} catch (error) {
  await fs.writeFile(
    path.join(evidenceDirectory, "openwish-e2e-result.json"),
    `${JSON.stringify({ passed: false, baseUrl, error: error.message, scenarios: results }, null, 2)}\n`
  );
  throw error;
} finally {
  await browser.close();
}
