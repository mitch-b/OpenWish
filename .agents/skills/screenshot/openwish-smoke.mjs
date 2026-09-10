import { chromium } from "playwright";
import fs from "node:fs/promises";
import path from "node:path";

const baseUrl = process.env.OPENWISH_BASE_URL ?? "http://web:8080";
const evidenceDirectory = process.env.OPENWISH_EVIDENCE_DIR ?? "/evidence";
const walkthroughDirectory = process.env.OPENWISH_WALKTHROUGH_DIR ?? evidenceDirectory;
const releaseVersion = process.env.OPENWISH_RELEASE_VERSION;
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

async function assertMinimumTouchTarget(locator, description) {
  const bounds = await locator.boundingBox();
  if (!bounds || bounds.width < 44 || bounds.height < 44) {
    throw new Error(
      `${description} measured ${bounds?.width ?? 0}x${bounds?.height ?? 0}px; expected at least 44x44px.`
    );
  }
}

async function verifyExternalLogin(browser, results) {
  for (const isMobile of [false, true]) {
    await verifyExternalLoginHandoff(browser, results, isMobile);
  }
}

async function verifyExternalLoginHandoff(browser, results, isMobile) {
  const context = await browser.newContext({
    viewport: isMobile ? { width: 390, height: 844 } : { width: 1440, height: 1000 },
    isMobile
  });
  const page = await context.newPage();
  const diagnostics = monitorPage(page);
  const visitedRoutes = [];
  let handoffTimer;

  try {
    // Playwright routes do not intercept subsequent requests in a redirect chain.
    // Intercept at the protocol level so the real 302 runs without contacting Google.
    const session = await context.newCDPSession(page);
    await session.send("Fetch.enable", {
      patterns: [{ urlPattern: "https://accounts.google.com/*", requestStage: "Request" }]
    });
    const authorizationRequest = new Promise((resolve, reject) => {
      session.on("Fetch.requestPaused", event => {
        const isAuthorization = event.resourceType === "Document" &&
          new URL(event.request.url).pathname === "/o/oauth2/v2/auth";
        session.send("Fetch.fulfillRequest", {
          requestId: event.requestId,
          responseCode: isAuthorization ? 200 : 204,
          responseHeaders: [{ name: "Content-Type", value: "text/html" }],
          body: isAuthorization
            ? Buffer.from("<h1>Google authorization boundary</h1>").toString("base64")
            : ""
        }).then(() => {
          if (isAuthorization) {
            resolve(event.request);
          }
        }, reject);
      });
    });
    const returnUrl = isMobile ? "/wishlists" : "/";
    await visit(page, `/Account/Login?ReturnUrl=${encodeURIComponent(returnUrl)}`, "Single sign-on", visitedRoutes);

    const externalLoginForm = page.locator("form.external-login-form");
    if (await externalLoginForm.getAttribute("data-enhance") !== "false") {
      throw new Error("External login must use a full browser navigation for the OAuth handoff.");
    }
    if (await externalLoginForm.getAttribute("action") !== "/Account/PerformExternalLogin") {
      throw new Error("External login must post to the root-relative challenge endpoint.");
    }

    const signInButton = page.getByRole("button", { name: "Continue with Google" });
    await signInButton.waitFor({ state: "visible" });
    if (isMobile) {
      await screenshot(page, "login-mobile.png");
    }
    const challengeResponse = page.waitForResponse(response =>
      response.request().method() === "POST" &&
      new URL(response.url()).pathname === "/Account/PerformExternalLogin"
    );

    const handoffTimeout = new Promise((_, reject) => {
      handoffTimer = setTimeout(() => reject(new Error(
        `Google authorization handoff timed out at ${page.url()}. Browser diagnostics: ${[
          ...diagnostics.browserErrors, ...diagnostics.failedResponses
        ].join(" | ") || "none"}`
      )), 10000);
    });
    const [response, authorization] = await Promise.race([
      Promise.all([
        challengeResponse,
        authorizationRequest,
        page.getByRole("heading", { name: "Google authorization boundary", exact: true }).waitFor(),
        signInButton.click({ noWaitAfter: true })
      ]),
      handoffTimeout
    ]);
    const postData = response.request().postData();
    if (postData === null) {
      throw new Error("Google sign-in challenge POST had no form body.");
    }
    const formData = new URLSearchParams(postData);
    if (formData.get("provider") !== "Google" || formData.get("ReturnUrl") !== returnUrl) {
      throw new Error("Google sign-in did not preserve the provider and return destination.");
    }
    if (response.status() !== 302) {
      throw new Error(`Google sign-in challenge returned ${response.status()} instead of a redirect.`);
    }
    const authorizationUrl = new URL(authorization.url);
    if (authorizationUrl.origin !== "https://accounts.google.com" ||
        authorizationUrl.pathname !== "/o/oauth2/v2/auth" ||
        authorizationUrl.searchParams.get("redirect_uri") !== `${baseUrl}/signin-google` ||
        authorizationUrl.searchParams.get("response_type") !== "code" ||
        !authorizationUrl.searchParams.get("state") ||
        page.url() !== authorizationUrl.href ||
        response.headers()["location"] !== authorizationUrl.href) {
      throw new Error("Google sign-in did not navigate to the expected OAuth authorization URL.");
    }
    if (diagnostics.browserErrors.length > 0 || diagnostics.failedResponses.length > 0) {
      throw new Error(`External login browser failures: ${[
        ...diagnostics.browserErrors, ...diagnostics.failedResponses
      ].join(" | ")}`);
    }

    results.push({
      scenario: isMobile ? "mobile-external-login" : "desktop-external-login",
      visitedRoutes,
      challengeStatus: response.status(),
      authorizationOrigin: authorizationUrl.origin,
      redirectUri: authorizationUrl.searchParams.get("redirect_uri")
    });
  } finally {
    clearTimeout(handoffTimer);
    await context.close();
  }
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
  const wishlistSearch = page.getByRole("searchbox", { name: "Search wishlists" });
  if (await wishlistSearch.getAttribute("aria-controls") !== "wishlist-results") {
    throw new Error("Wishlist discovery search does not identify its results.");
  }
  await wishlistSearch.fill("Private");
  await page.getByRole("status").filter({ hasText: "1 wishlist found." }).waitFor({ state: "attached" });
  const clearWishlistSearch = page.getByRole("button", { name: "Clear wishlist search" });
  await clearWishlistSearch.click();
  await assertVisible(page, "Family Gift Ideas");
  await screenshot(page, "wishlists.png");

  await page.getByRole("tab", { name: "Friends' Wishlists" }).click();
  await assertVisible(page, "Jordan's Favorites");

  await visit(page, `/wishlists/${manifest.wishlistPublicId}`, "Family Gift Ideas", visitedRoutes);
  await assertVisible(page, "Noise-Cancelling Headphones");
  await assertVisible(page, "Cast-Iron Dutch Oven");
  await assertVisible(page, "National Park Pass");
  await assertVisible(page, "$249.99");
  await assertVisible(page, "3");
  const itemSearch = page.getByRole("searchbox", { name: "Search wishlist items" });
  if (await itemSearch.getAttribute("aria-controls") !== "wishlist-items") {
    throw new Error("Wishlist search does not identify the item results it filters.");
  }
  await itemSearch.fill("Dutch Oven");
  await page.getByRole("status").filter({ hasText: "1 of 3 wishlist items shown." })
    .waitFor({ state: "attached" });
  await page.getByRole("button", { name: "Clear wishlist item search" }).click();

  const filtersButton = page.getByRole("button", { name: "Filters" });
  await filtersButton.click();
  await page.getByRole("button", { name: "Filters", expanded: true })
    .waitFor({ state: "visible" });
  const highPriorityFilter = page.getByRole("button", { name: "High" });
  await highPriorityFilter.click();
  await page.getByRole("button", { name: "High", pressed: true })
    .waitFor({ state: "visible" });
  const priceSort = page.getByRole("button", { name: "Price" });
  await priceSort.click();
  await page.getByRole("button", { name: "Price", pressed: true })
    .waitFor({ state: "visible" });
  await page.getByRole("button", { name: "Clear all" }).click();

  const productLink = page.getByRole("link", {
    name: "View Noise-Cancelling Headphones product (opens in a new tab)"
  });
  await productLink.waitFor({ state: "visible" });
  if (await productLink.getAttribute("target") !== "_blank" ||
      await productLink.getAttribute("rel") !== "noopener noreferrer") {
    throw new Error("Product links must safely open in a new tab.");
  }
  const listView = page.getByRole("button", { name: "List view" });
  await listView.click();
  await page.getByRole("button", { name: "List view", pressed: true })
    .waitFor({ state: "visible" });
  await page.getByRole("link", {
    name: "View Noise-Cancelling Headphones product (opens in a new tab)"
  }).waitFor({ state: "visible" });
  const gridView = page.getByRole("button", { name: "Grid view" });
  await gridView.click();
  await page.getByRole("button", { name: "Grid view", pressed: true })
    .waitFor({ state: "visible" });
  await screenshot(page, "wishlist-details.png");
  const addItemButton = page.getByRole("button", { name: "Add item" }).first();
  await addItemButton.click();
  const itemDialog = page.getByRole("dialog", { name: "Add item" });
  await itemDialog.waitFor({ state: "visible" });
  if (!(await addItemButton.evaluate(element => element.closest("[inert]") !== null))) {
    throw new Error("The item dialog did not make background content inert.");
  }
  const modalProductUrl = itemDialog.getByLabel("Product URL");
  if (!(await modalProductUrl.evaluate(element => element === document.activeElement))) {
    throw new Error("The item dialog did not initially focus the product URL field.");
  }
  if (await modalProductUrl.getAttribute("aria-describedby") !== "product-url-import-modal-help") {
    throw new Error("The item dialog product URL is not connected to its help text.");
  }
  if (!(await itemDialog.getByRole("button", { name: "Import" }).isDisabled())) {
    throw new Error("The item dialog allows an empty product URL import.");
  }
  await modalProductUrl.fill("https://example.com/gift");
  await itemDialog.getByRole("button", { name: "Import" }).click({ trial: true });
  if (await itemDialog.getByText("Importing product details...").isVisible()) {
    throw new Error("Typing a product URL incorrectly displayed an import-in-progress state.");
  }
  await itemDialog.getByRole("button", { name: "Close" }).focus();
  await page.keyboard.press("Shift+Tab");
  if (!(await itemDialog.getByRole("button", { name: "Add item", exact: true })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Keyboard focus did not wrap within the item dialog.");
  }
  await screenshot(page, "wishlist-item-dialog.png");
  await page.keyboard.press("Escape");
  await itemDialog.waitFor({ state: "detached" });
  if (!(await addItemButton.evaluate(element => element === document.activeElement))) {
    throw new Error("Closing the item dialog did not restore focus to its opener.");
  }

  await visit(page, "/wishlists/new", "Create a Wishlist", visitedRoutes);
  await page.waitForTimeout(2000);
  if (await page.evaluate(() => document.activeElement?.id) !== "name") {
    throw new Error("The wishlist title field did not retain focus after interactivity started.");
  }
  const allFriendsVisibility = page.getByRole("radio", { name: /^All friends/ });
  const privateVisibility = page.getByRole("radio", { name: /^Private/ });
  if (!(await allFriendsVisibility.isChecked())) {
    throw new Error("All friends was not the default wishlist visibility.");
  }
  await privateVisibility.focus();
  const privateVisibilityFocus = await privateVisibility.evaluate(element => {
    const option = element.closest(".visibility-option");
    const styles = option ? getComputedStyle(option) : null;
    return styles ? { outlineStyle: styles.outlineStyle, outlineWidth: styles.outlineWidth } : null;
  });
  if (!privateVisibilityFocus ||
      privateVisibilityFocus.outlineStyle === "none" ||
      privateVisibilityFocus.outlineWidth === "0px") {
    throw new Error("The focused wishlist visibility choice had no visible focus indicator.");
  }
  await page.keyboard.press("Space");
  if (!(await privateVisibility.isChecked()) || await allFriendsVisibility.isChecked()) {
    throw new Error("Wishlist visibility choices were not mutually exclusive.");
  }
  await page.getByRole("button", { name: "Choose an icon" }).click();
  await screenshot(page, "create-wishlist.png");
  await page.getByRole("button", { name: "Wrapped gift" }).click();
  await allFriendsVisibility.focus();
  await page.keyboard.press("Space");
  await page.locator("#name").fill("Emoji Test Wishlist");
  await page.getByRole("button", { name: "Create wishlist" }).click();
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
  const productUrl = page.getByLabel("Product URL");
  if (await page.evaluate(() => document.activeElement?.id) !== "product-url-import") {
    throw new Error("The product URL field did not retain focus after interactivity started.");
  }
  if (await productUrl.getAttribute("aria-describedby") !== "product-url-import-help") {
    throw new Error("The product URL field is not connected to its help text.");
  }
  if (!(await page.getByRole("button", { name: "Import" }).isDisabled())) {
    throw new Error("The item form allows an empty product URL import.");
  }
  await productUrl.fill("https://example.com/gift");
  await page.getByRole("button", { name: "Import" }).click({ trial: true });
  await page.getByLabel("Name").fill("Travel Mug");
  await page.getByRole("button", { name: "Add item", exact: true }).click();
  await page.waitForURL(`${baseUrl}/wishlists/${manifest.wishlistPublicId}`);
  await assertVisible(page, "Travel Mug");
  await screenshot(page, "added-wishlist-item.png");

  await visit(page, "/events", "Plan gift exchanges", visitedRoutes);
  await assertVisible(page, "Holiday Gift Exchange");
  await page.getByRole("button", { name: "Actions for Holiday Gift Exchange" })
    .waitFor({ state: "visible" });
  await page.getByRole("link", { name: /Open event.*Holiday Gift Exchange/ })
    .waitFor({ state: "visible" });
  await screenshot(page, "events.png");

  await visit(page, `/events/${manifest.eventPublicId}`, "Holiday Gift Exchange", visitedRoutes);
  await assertVisible(page, "Your Secret Santa match");
  await assertVisible(page, "JordanDemo");
  await assertVisible(page, "Suggested Budget");
  await assertVisible(page, "TaylorDemo");
  const refreshReservedItems = page.getByRole("button", { name: "Refresh" });
  await refreshReservedItems.click();
  await page.getByRole("status").filter({ hasText: "Reserved items refreshed. 0 items found." })
    .waitFor({ state: "attached" });
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
  const setupSteps = page.locator(".secret-santa-setup");
  await assertMinimumTouchTarget(
    setupSteps.getByRole("link", { name: "Edit" }),
    "Mobile Secret Santa edit action"
  );
  await assertMinimumTouchTarget(
    setupSteps.getByRole("link", { name: "Invite people" }),
    "Mobile Secret Santa invitation action"
  );
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
  const friendInvites = page.getByLabel("Email addresses");
  if (await friendInvites.getAttribute("aria-describedby") !== "emailInvitesHelp") {
    throw new Error("Friend invitation guidance is not connected to its field.");
  }
  const sendInvitations = page.getByRole("button", { name: "Send invitations" });
  if (!(await sendInvitations.isDisabled())) {
    throw new Error("Friend invitations can be submitted without an email address.");
  }
  const acceptTaylorRequest = page.getByRole("button", {
    name: "Accept friend request from TaylorDemo"
  });
  await acceptTaylorRequest.click();
  await page.getByRole("status").filter({ hasText: "TaylorDemo is now your friend." })
    .waitFor({ state: "attached" });
  await screenshot(page, "friends.png");
  await page.waitForTimeout(2000);
  await friendInvites.fill("new-friend@example.com");
  await sendInvitations.click({ trial: true });

  const notificationBell = page.locator(".notification-bell");
  await notificationBell.click();
  await page.getByRole("button", { name: /Notifications/, expanded: true })
    .waitFor({ state: "visible" });
  const notificationDialog = page.getByRole("dialog", { name: "Notifications" });
  await notificationDialog.waitFor({ state: "visible" });
  if (!(await page.locator(".content").evaluate(element => element.inert)) ||
      !(await page.locator(".sidebar").evaluate(element => element.inert))) {
    throw new Error("The modal notification panel did not make background content inert.");
  }
  if (!(await notificationDialog.getByRole("button", { name: "Mark all as read" })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Notification flyout did not focus its first useful action.");
  }
  await assertVisible(page, "Event invitation");
  await assertVisible(page, "Wishlist activity");
  await screenshot(page, "notifications.png");
  await notificationDialog.getByRole("button", { name: "Mark all as read" }).click();
  await page.getByRole("status").filter({ hasText: "All notifications marked as read." })
    .waitFor({ state: "attached" });
  const closeNotifications = page.getByRole("button", { name: "Close notifications" });
  await page.waitForFunction(() =>
    document.activeElement?.id === "notification-close-button"
  );
  const notificationCountBeforeDelete = await page.locator(".notification-item").count();
  const notificationToDelete = page.locator(".notification-item").first();
  const deleteNotification = notificationToDelete.getByRole("button", { name: "Delete notification" });
  await deleteNotification.click();
  const deleteDialog = page.getByRole("dialog", { name: "Delete notification" });
  await deleteDialog.waitFor({ state: "visible" });
  if (!(await notificationDialog.evaluate(element => element.closest("[inert]") !== null))) {
    throw new Error("The notification panel remained interactive behind its delete dialog.");
  }
  await assertVisible(page, "This cannot be undone.");
  if (!(await deleteDialog.getByRole("button", { name: "Keep notification" })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Notification deletion did not focus its safe action.");
  }
  await screenshot(page, "notification-delete-dialog.png");
  await page.keyboard.press("Escape");
  await deleteDialog.waitFor({ state: "detached" });
  if (!(await deleteNotification.evaluate(element => element === document.activeElement))) {
    throw new Error("Closing notification deletion did not restore focus.");
  }
  await deleteNotification.click();
  await page.getByRole("button", { name: "Delete", exact: true }).click();
  await page.waitForFunction(
    expectedCount => document.querySelectorAll(".notification-item").length === expectedCount,
    notificationCountBeforeDelete - 1
  );
  if (!(await closeNotifications.evaluate(element => element === document.activeElement))) {
    throw new Error("Successful notification deletion did not keep focus in the open panel.");
  }
  const notificationsAfterDeleteResponse = await context.request.get(
    `${baseUrl}/api/notifications?includeRead=true`
  );
  const notificationsAfterDelete = await notificationsAfterDeleteResponse.json();
  if (notificationsAfterDelete.length !== notifications.length - 1 ||
      notificationsAfterDelete.some(notification => notification.publicId === notificationPublicId)) {
    throw new Error("Deleted notification remained available from the API.");
  }
  await page.keyboard.press("Escape");
  await notificationDialog.waitFor({ state: "detached" });
  if (await notificationBell.getAttribute("aria-expanded") !== "false" ||
      !(await notificationBell.evaluate(element => element === document.activeElement))) {
    throw new Error("Closing notifications did not collapse the disclosure and restore focus.");
  }
  if (await page.locator(".content").evaluate(element => element.inert) ||
      await page.locator(".sidebar").evaluate(element => element.inert)) {
    throw new Error("Closing notifications left background content inert.");
  }

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
  if (!releaseVersion) {
    throw new Error("OPENWISH_RELEASE_VERSION must be set for release verification.");
  }
  await assertVisible(page, `Version ${releaseVersion}`);
  await assertVisible(page, "Comfortable mobile controls");

  await visit(page, "/Account/Manage", "Profile", visitedRoutes);
  const username = await page.locator("#username").inputValue();
  if (username !== "AlexDemo") {
    throw new Error(`Profile displayed unexpected username '${username}'.`);
  }

  await visit(page, "/events", "Neighborhood Secret Santa", visitedRoutes);
  const createdEventCard = page.locator(".event-card").filter({ hasText: "Neighborhood Secret Santa" });
  const createdEventActions = createdEventCard.getByRole("button", { name: "Actions for Neighborhood Secret Santa" });
  await createdEventActions.click();
  await createdEventCard.getByRole("button", { name: "Delete" }).click();
  const eventDeleteDialog = page.getByRole("dialog", { name: "Delete Event" });
  await eventDeleteDialog.waitFor({ state: "visible" });
  const cancelEventDeletion = eventDeleteDialog.getByRole("button", { name: "Cancel" });
  if (!(await cancelEventDeletion.evaluate(element => element === document.activeElement))) {
    throw new Error("The shared dialog did not focus its safe action.");
  }
  if (!(await createdEventActions.evaluate(element => element.closest("[inert]") !== null))) {
    throw new Error("The shared dialog did not make background content inert.");
  }
  await eventDeleteDialog.getByRole("button", { name: "Continue" }).click();
  await eventDeleteDialog.getByRole("button", { name: "Delete Event" }).waitFor({ state: "visible" });
  await screenshot(page, "event-delete-dialog.png");
  await page.keyboard.press("Escape");
  await eventDeleteDialog.waitFor({ state: "detached" });
  if (!(await createdEventActions.evaluate(element => element === document.activeElement))) {
    throw new Error("The shared dialog did not restore focus to its opener.");
  }
  await createdEventActions.click();
  await createdEventCard.getByRole("button", { name: "Delete" }).click();
  await page.getByRole("button", { name: "Continue" }).click();
  await page.getByRole("button", { name: "Delete Event" }).click();
  await createdEventCard.waitFor({ state: "detached" });
  await page.waitForFunction(() => document.activeElement?.matches("main h1, main h2, main h3, main"));

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
      "accessible notification updates and deletion",
      "immediate wishlist discovery",
      "friend invitation validation",
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
  const dutchOvenRow = page.locator("tr").filter({ hasText: "Cast-Iron Dutch Oven" });
  await dutchOvenRow.getByRole("button", { name: "Show" }).click();
  const giftCoordination = page.getByRole("region", {
    name: "Gift coordination for Cast-Iron Dutch Oven"
  });
  const dutchOvenComposer = giftCoordination.getByLabel("Add a comment");
  const dutchOvenComposerId = await dutchOvenComposer.getAttribute("id");
  const parkPassRow = page.locator("tr").filter({ hasText: "National Park Pass" });
  await parkPassRow.getByRole("button", { name: "Show" }).click();
  const parkPassCoordination = page.getByRole("region", {
    name: "Gift coordination for National Park Pass"
  });
  const parkPassComposerId = await parkPassCoordination.getByLabel("Add a comment").getAttribute("id");
  if (!dutchOvenComposerId?.startsWith("commentText-") ||
      !parkPassComposerId?.startsWith("commentText-") ||
      dutchOvenComposerId === parkPassComposerId) {
    throw new Error(
      `Wishlist comment composers did not have item-specific IDs: ${dutchOvenComposerId}, ${parkPassComposerId}`
    );
  }
  await giftCoordination.getByRole("button", { name: "Reserve this item" }).click();
  await giftCoordination.getByRole("status").filter({
    hasText: "Item reserved. Other shoppers can see that it is taken."
  }).waitFor({ state: "attached" });
  const cancelReservation = giftCoordination.getByRole("button", { name: "Cancel reservation" });
  await cancelReservation.click();
  const reservationDialog = giftCoordination.getByRole("alertdialog", { name: "Release this reservation?" });
  await reservationDialog.waitFor({ state: "visible" });
  if (!(await reservationDialog.getByRole("button", { name: "Keep reservation" })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Reservation cancellation did not focus its safe action.");
  }
  await screenshot(page, "reservation-cancel-confirmation.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await assertMinimumTouchTarget(
    reservationDialog.getByRole("button", { name: "Keep reservation" }),
    "Mobile keep-reservation action"
  );
  await assertMinimumTouchTarget(
    reservationDialog.getByRole("button", { name: "Release reservation" }),
    "Mobile release-reservation action"
  );
  await screenshot(page, "reservation-cancel-confirmation-mobile.png");
  await page.setViewportSize({ width: 1280, height: 900 });
  await reservationDialog.getByRole("button", { name: "Keep reservation" }).click();
  await reservationDialog.waitFor({ state: "detached" });
  if (!(await cancelReservation.evaluate(element => element === document.activeElement))) {
    throw new Error("Keeping a reservation did not restore focus to the cancellation trigger.");
  }
  await cancelReservation.click();
  await giftCoordination.getByRole("button", { name: "Release reservation" }).click();
  await giftCoordination.getByRole("status").filter({ hasText: "Reservation released." })
    .waitFor({ state: "attached" });
  if (!(await giftCoordination.getByRole("button", { name: "Reserve this item" })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Releasing a reservation did not focus the available reservation action.");
  }

  const commentText = `Verification comment ${Date.now()}`;
  await giftCoordination.getByLabel("Add a comment").fill(commentText);
  await giftCoordination.getByRole("button", { name: "Add comment" }).click();
  await giftCoordination.getByRole("status").filter({ hasText: "Comment added." })
    .waitFor({ state: "attached" });
  const addedComment = giftCoordination.locator(".comment-item").filter({ hasText: commentText });
  const deleteComment = addedComment.getByRole("button", { name: "Delete comment by TaylorDemo" });
  await deleteComment.click();
  const commentDialog = giftCoordination.getByRole("alertdialog", { name: "Delete this comment?" });
  if (!(await commentDialog.getByRole("button", { name: "Keep comment" })
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Comment deletion did not focus its safe action.");
  }
  await screenshot(page, "comment-delete-confirmation.png");
  await page.setViewportSize({ width: 390, height: 844 });
  await assertMinimumTouchTarget(
    commentDialog.getByRole("button", { name: "Keep comment" }),
    "Mobile keep-comment action"
  );
  await assertMinimumTouchTarget(
    commentDialog.getByRole("button", { name: "Delete comment", exact: true }),
    "Mobile delete-comment action"
  );
  await screenshot(page, "comment-delete-confirmation-mobile.png");
  await page.setViewportSize({ width: 1280, height: 900 });
  await commentDialog.getByRole("button", { name: "Keep comment" }).click();
  await commentDialog.waitFor({ state: "detached" });
  if (!(await deleteComment.evaluate(element => element === document.activeElement))) {
    throw new Error("Keeping a comment did not restore focus to its deletion trigger.");
  }
  await deleteComment.click();
  await giftCoordination.getByRole("button", { name: "Delete comment", exact: true }).click();
  await giftCoordination.getByRole("status").filter({ hasText: "Comment deleted." })
    .waitFor({ state: "attached" });
  if (!(await giftCoordination.getByLabel("Add a comment")
    .evaluate(element => element === document.activeElement))) {
    throw new Error("Deleting a comment did not focus the surviving comment composer.");
  }

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
  const mobileAddItem = page.getByRole("button", { name: "Add item", exact: true });
  const mobileViewToggle = page.getByRole("group", { name: "Wishlist view" });
  const [addItemPosition, controlsOverlap] = await Promise.all([
    mobileAddItem.evaluate(element => getComputedStyle(element).position),
    page.evaluate(() => {
      const addItem = document.querySelector("#add-wishlist-item")?.getBoundingClientRect();
      const viewToggle = document.querySelector(".view-toggle")?.getBoundingClientRect();
      if (!addItem || !viewToggle) {
        throw new Error("Mobile wishlist actions were not rendered.");
      }

      return !(
        addItem.right <= viewToggle.left ||
        addItem.left >= viewToggle.right ||
        addItem.bottom <= viewToggle.top ||
        addItem.top >= viewToggle.bottom
      );
    })
  ]);
  if (addItemPosition === "fixed" || controlsOverlap) {
    throw new Error("The mobile add-item action obscured the wishlist view controls.");
  }
  await assertVisible(page, "Add item");
  await assertMinimumTouchTarget(mobileAddItem, "Mobile add-item action");
  const mobileAddItemBounds = await mobileAddItem.boundingBox();
  if (!mobileAddItemBounds || mobileAddItemBounds.width < 250) {
    throw new Error("The mobile add-item action did not span the available content width.");
  }
  await assertMinimumTouchTarget(
    mobileViewToggle.getByRole("button", { name: "Grid view" }),
    "Mobile grid-view action"
  );
  await assertMinimumTouchTarget(
    mobileViewToggle.getByRole("button", { name: "List view" }),
    "Mobile list-view action"
  );
  await page.getByRole("button", { name: "Filters" }).click();
  await assertMinimumTouchTarget(
    page.getByRole("button", { name: "High" }),
    "Mobile priority filter"
  );
  await assertMinimumTouchTarget(
    page.getByRole("button", { name: "Price" }),
    "Mobile sort action"
  );
  await page.getByRole("button", { name: "Filters" }).click();
  await assertMinimumTouchTarget(
    page.getByRole("button", { name: "Edit Noise-Cancelling Headphones" }),
    "Mobile grid item edit action"
  );
  await mobileViewToggle.getByRole("button", { name: "List view" }).click();
  await assertMinimumTouchTarget(
    page.getByRole("button", { name: "Edit Noise-Cancelling Headphones" }),
    "Mobile list item edit action"
  );
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
