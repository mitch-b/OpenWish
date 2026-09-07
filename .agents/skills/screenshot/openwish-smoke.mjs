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
  await assertNoEmptySpinnerStatuses(page, route);

  visitedRoutes.push(route);
  return response;
}

async function assertNoEmptySpinnerStatuses(page, route) {
  const emptyStatuses = await page.locator('.spinner-border[role="status"]').evaluateAll(elements =>
    elements
      .filter(element =>
        !element.getAttribute("aria-label")?.trim() &&
        !element.textContent?.trim())
      .map(element => element.outerHTML)
  );

  if (emptyStatuses.length > 0) {
    throw new Error(`${route} exposed empty spinner statuses: ${emptyStatuses.join(" | ")}`);
  }
}

async function screenshot(page, fileName) {
  await page.evaluate(() => window.scrollTo({ top: 0, left: 0, behavior: "instant" }));
  await page.waitForTimeout(500);
  await assertDesktopSidebarContinuity(page);
  await page.screenshot({
    path: path.join(walkthroughDirectory, fileName),
    fullPage: true
  });
}

async function assertDesktopSidebarContinuity(page) {
  const dimensions = await page.evaluate(() => {
    if (window.innerWidth <= 640) {
      return null;
    }

    const shell = document.querySelector(".page");
    const sidebar = document.querySelector(".sidebar");
    if (!shell || !sidebar) {
      throw new Error("The application shell is missing its desktop sidebar.");
    }

    return {
      shellHeight: shell.getBoundingClientRect().height,
      sidebarHeight: sidebar.getBoundingClientRect().height
    };
  });

  if (dimensions && dimensions.sidebarHeight + 1 < dimensions.shellHeight) {
    throw new Error(
      `Desktop sidebar ended at ${dimensions.sidebarHeight}px before the ${dimensions.shellHeight}px application shell.`
    );
  }
}

async function assertResponsiveWidths(page, viewports) {
  for (const viewport of viewports) {
    await page.setViewportSize(viewport);
    await page.waitForTimeout(100);

    const dimensions = await page.evaluate(() => ({
      viewportWidth: window.innerWidth,
      pageWidth: document.documentElement.scrollWidth
    }));
    if (dimensions.pageWidth > dimensions.viewportWidth) {
      throw new Error(
        `Page overflowed horizontally at ${viewport.width}x${viewport.height}: ` +
        `${dimensions.pageWidth}px content in a ${dimensions.viewportWidth}px viewport.`
      );
    }
  }
}

async function verifyExternalLogin(browser, results) {
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true
  });
  const page = await context.newPage();
  const visitedRoutes = [];

  await page.route("https://accounts.google.com/**", route => route.abort());
  await visit(page, "/Account/Login", "Single sign-on", visitedRoutes);

  const externalLoginForm = page.locator("form.external-login-form");
  if (await externalLoginForm.getAttribute("data-enhance") !== "false") {
    throw new Error("External login must use a full browser navigation for the OAuth handoff.");
  }

  const signInButton = page.getByRole("button", { name: "Continue with Google" });
  await signInButton.waitFor({ state: "visible" });
  await screenshot(page, "login-mobile.png");
  const postRequest = page.waitForRequest(request =>
    request.method() === "POST" &&
    new URL(request.url()).pathname === "/Account/PerformExternalLogin"
  );

  const [request] = await Promise.all([
    postRequest,
    signInButton.click({ noWaitAfter: true })
  ]);
  if (!request.postData()?.includes("provider=Google")) {
    throw new Error("Google sign-in did not submit the selected provider.");
  }

  results.push({ scenario: "mobile-external-login", visitedRoutes });
  await context.close();
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
  const productLink = page.getByRole("link", {
    name: "View Noise-Cancelling Headphones product (opens in a new tab)"
  });
  await productLink.waitFor({ state: "visible" });
  if (await productLink.getAttribute("target") !== "_blank" ||
      await productLink.getAttribute("rel") !== "noopener noreferrer") {
    throw new Error("Product links must safely open in a new tab.");
  }
  await page.getByRole("button", { name: "List View" }).click();
  await page.getByRole("link", {
    name: "View Noise-Cancelling Headphones product (opens in a new tab)"
  }).waitFor({ state: "visible" });
  await page.getByRole("button", { name: "Grid View" }).click();
  await screenshot(page, "wishlist-details.png");

  await visit(page, "/wishlists/new", "Create a Wishlist", visitedRoutes);
  await page.waitForTimeout(2000);
  if (await page.evaluate(() => document.activeElement?.id) !== "name") {
    throw new Error("The wishlist title field did not retain focus after interactivity started.");
  }
  await page.getByRole("button", { name: "Choose an icon" }).click();
  await screenshot(page, "create-wishlist.png");
  await page.getByRole("button", { name: "Wrapped gift" }).click();
  await page.locator("#name").fill("Emoji Test Wishlist");
  await page.getByRole("button", { name: "Create Wishlist" }).click();
  await page.waitForURL(`${baseUrl}/wishlists`);
  const createdWishlistsResponse = await context.request.get(`${baseUrl}/api/wishlists`);
  const createdWishlists = await createdWishlistsResponse.json();
  const emojiWishlist = createdWishlists.find(wishlist => wishlist.name === "Emoji Test Wishlist");
  if (emojiWishlist?.icon !== "🎁") {
    throw new Error("The selected wishlist emoji was not persisted.");
  }
  await visit(page, `/wishlists/${manifest.wishlistPublicId}/manage`, "Manage Wishlist", visitedRoutes);
  await assertVisible(page, "Who can see this?");
  await visit(page, `/wishlists/${manifest.wishlistPublicId}/items/new`, "Add Item to Wishlist", visitedRoutes);
  await page.waitForTimeout(2000);
  if (await page.evaluate(() => document.activeElement?.getAttribute("placeholder")) !== "Paste a product URL") {
    throw new Error("The product URL field did not retain focus after interactivity started.");
  }

  await visit(page, "/events", "Plan gift exchanges", visitedRoutes);
  await assertVisible(page, "Holiday Gift Exchange");
  await page.getByRole("button", { name: "Actions for Holiday Gift Exchange" })
    .waitFor({ state: "visible" });
  await screenshot(page, "events.png");

  await visit(page, `/events/${manifest.eventPublicId}`, "Holiday Gift Exchange", visitedRoutes);
  await assertVisible(page, "Your Secret Santa match");
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "Suggested Budget");
  await assertVisible(page, "TaylorDemo");
  await screenshot(page, "event-details.png");

  await visit(page, "/events/new", "Create a Secret Santa", visitedRoutes);
  await page.waitForTimeout(2000);
  if (await page.evaluate(() => document.activeElement?.id) !== "name") {
    throw new Error("The event name field did not retain focus after interactivity started.");
  }
  const secretSantaOption = page.getByRole("button", { name: /Secret Santa/ });
  if (!(await secretSantaOption.getAttribute("class"))?.includes("event-type-option-selected")) {
    throw new Error("Secret Santa was not the default event type.");
  }
  await page.locator("#name").fill("Neighborhood Secret Santa");
  await page.getByRole("button", { name: "Create and invite people" }).click();
  await page.waitForURL(/\/events\/[^/]+(?:#secret-santa-setup)?$/);
  await assertVisible(page, "Finish your Secret Santa setup");
  await assertVisible(page, "Invite your group");
  await assertVisible(page, "Add your wishlist");
  await assertVisible(page, "Draw names");
  await screenshot(page, "secret-santa-setup.png");
  await assertResponsiveWidths(page, [
    { width: 320, height: 568 },
    { width: 768, height: 500 },
    { width: 1024, height: 600 }
  ]);
  await page.setViewportSize({ width: 390, height: 700 });
  await screenshot(page, "secret-santa-setup-mobile.png");
  await page.setViewportSize({ width: 1440, height: 1000 });

  await page.getByRole("button", { name: "Invite people" }).first().click();
  await assertVisible(page, "Paste as many addresses as you need");
  await page.locator("#emailInput").fill("one@example.com, two@example.com");
  await assertVisible(page, "Send 2 invitations");
  await screenshot(page, "invitation-dialog.png");
  await page.getByRole("button", { name: "Cancel" }).click();

  await visit(page, `/events/${manifest.eventPublicId}/manage`, "Manage Event", visitedRoutes);
  await assertVisible(page, "Participants");
  await screenshot(page, "event-management.png");

  await visit(page, "/friends", "Connect with friends", visitedRoutes);
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "CaseyDemo");
  await assertVisible(page, "TaylorDemo");
  await screenshot(page, "friends.png");

  await page.locator(".notification-bell").click();
  await assertVisible(page, "Event invitation");
  await assertVisible(page, "Wishlist activity");
  await screenshot(page, "notifications.png");
  const notificationCountBeforeDelete = await page.locator(".notification-item").count();
  const notificationToDelete = page.locator(".notification-item").first();
  await notificationToDelete.getByRole("button", { name: "Delete notification" }).click();
  await page.getByRole("button", { name: "Delete", exact: true }).click();
  await page.waitForFunction(
    expectedCount => document.querySelectorAll(".notification-item").length === expectedCount,
    notificationCountBeforeDelete - 1
  );
  const notificationsAfterDeleteResponse = await context.request.get(
    `${baseUrl}/api/notifications?includeRead=true`
  );
  const notificationsAfterDelete = await notificationsAfterDeleteResponse.json();
  if (notificationsAfterDelete.length !== notifications.length - 1 ||
      notificationsAfterDelete.some(notification => notification.publicId === notificationPublicId)) {
    throw new Error("Deleted notification remained available from the API.");
  }
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

  await visit(page, `/events/${manifest.eventPublicId}`, "Your Secret Santa match", visitedRoutes);
  await assertVisible(page, "Assignments are ready");
  await screenshot(page, "event-details-dark.png");

  await visit(page, "/whats-new", "What's new", visitedRoutes);
  await assertVisible(page, "Version 0.1.2");
  await assertVisible(page, "Clearer loading updates");

  await visit(page, "/Account/Manage", "Profile", visitedRoutes);
  const username = await page.locator("#username").inputValue();
  if (username !== "AlexDemo") {
    throw new Error(`Profile displayed unexpected username '${username}'.`);
  }

  await visit(page, "/events", "Neighborhood Secret Santa", visitedRoutes);
  const createdEventCard = page.locator(".event-card").filter({ hasText: "Neighborhood Secret Santa" });
  await createdEventCard.getByRole("button", { name: "Actions for Neighborhood Secret Santa" }).click();
  await createdEventCard.getByRole("button", { name: "Delete" }).click();
  await page.getByRole("button", { name: "Continue" }).click();
  await page.getByRole("button", { name: "Delete Event" }).click();
  await createdEventCard.waitFor({ state: "detached" });

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
      "accessible product links",
      "event details and gift assignment",
      "friends and pending requests",
      "notifications",
      "accessible loading updates",
      "theme persistence",
      "release history",
      "account profile"
    ]
  });
  await context.close();
  return { notificationPublicId };
}

async function verifyDevelopmentLoginJourney(browser, results) {
  const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];

  const invite = encodeURIComponent("invited@example.com|inviter-id");
  await visit(page, `/Account/Register?invite=${invite}`, "Invitations are tied to the address", visitedRoutes);
  await assertVisible(page, "invited@example.com");
  const invitedEmailInput = page.locator('input[name="Input.Email"]');
  if (await invitedEmailInput.getAttribute("type") !== "hidden") {
    throw new Error("Invited registration exposed an editable email field.");
  }
  if (await page.evaluate(() => document.activeElement?.id) !== "Input.Password") {
    throw new Error("Invited registration did not focus the first editable field.");
  }
  await screenshot(page, "invited-registration.png");
  await assertResponsiveWidths(page, [
    { width: 320, height: 568 },
    { width: 768, height: 600 },
    { width: 1024, height: 700 }
  ]);
  await page.setViewportSize({ width: 390, height: 844 });
  await screenshot(page, "invited-registration-mobile.png");
  await page.setViewportSize({ width: 1280, height: 900 });

  await visit(page, "/Account/Login", "Local demo accounts", visitedRoutes);
  if (await page.evaluate(() => document.activeElement?.id) !== "Input.Email") {
    throw new Error("Login did not focus the first editable field.");
  }
  await screenshot(page, "login.png");
  await page.getByRole("button", { name: "Sign in as AlexDemo (organizer)" }).click();
  await assertVisible(page, "AlexDemo");

  if (diagnostics.browserErrors.length > 0) {
    throw new Error(`Development login browser errors: ${diagnostics.browserErrors.join(" | ")}`);
  }
  if (diagnostics.failedResponses.length > 0) {
    throw new Error(`Development login failed responses: ${diagnostics.failedResponses.join(" | ")}`);
  }

  results.push({
    scenario: "development-login",
    visitedRoutes
  });
  await context.close();
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
  await visit(page, `/events/${manifest.eventPublicId}`, "Accept your invitation to join", visitedRoutes);
  if (await page.getByText("You're in the Secret Santa.").isVisible()) {
    throw new Error("Pending invitee was incorrectly shown accepted-participant guidance.");
  }
  await page.getByRole("link", { name: "Review invitation" }).click();
  await assertVisible(page, "You're almost in!");
  await assertVisible(page, "Accept invite");
  await page.getByRole("button", { name: "Accept invite" }).click();
  await assertVisible(page, "Continue and add my wishlist");

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

  const activityResponse = await context.request.get(`${baseUrl}/api/activities/friends`);
  if (!activityResponse.ok()) {
    throw new Error(`Friend activity request returned ${activityResponse.status()}.`);
  }
  const activities = await activityResponse.json();
  if (!activities.some(activity => activity.publicId === "demo-wishlist-activity")) {
    throw new Error("Visible friend wishlist activity was omitted.");
  }
  if (activities.some(activity => activity.publicId === "demo-private-wishlist-activity")) {
    throw new Error("Private wishlist activity was disclosed to a friend.");
  }

  await visit(page, "/events", "Holiday Gift Exchange", visitedRoutes);
  if (await page.getByRole("button", { name: "Actions for Holiday Gift Exchange" }).count() !== 0) {
    throw new Error("A non-owner received an event actions menu.");
  }

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
  await assertVisible(page, "You're shopping for");
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
  await page.locator(".navbar-toggler").check({ force: true });
  await page.getByRole("link", { name: "Wishlists", exact: true }).waitFor({ state: "visible" });
  await screenshot(page, "navigation-mobile.png");
  await page.locator(".navbar-toggler").uncheck({ force: true });

  await visit(page, `/wishlists/${manifest.wishlistPublicId}`, "Family Gift Ideas", visitedRoutes);
  await assertVisible(page, "Noise-Cancelling Headphones");
  await screenshot(page, "wishlist-mobile.png");

  await visit(page, `/events/${manifest.eventPublicId}`, "Your Secret Santa match", visitedRoutes);
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "View JordanDemo's wishlist");
  await screenshot(page, "secret-santa-mobile.png");

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

  await verifyDevelopmentLoginJourney(browser, results);
  await verifyExternalLogin(browser, results);
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
