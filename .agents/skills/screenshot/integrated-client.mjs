import assert from "node:assert/strict";
import { writeFile } from "node:fs/promises";
import { chromium } from "playwright";

const wish = "https://wish.test:8443";
const atelier = "https://atelier.test:8443";
const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
const page = await context.newPage();
const assertions = [];
let consentCaptured = false;

async function json(url, method = "GET", data) {
  const response = await context.request.fetch(url, { method, data });
  assert.ok(response.ok(), `${method} ${new URL(url).pathname}: ${response.status()} ${await response.text()}`);
  return response.status() === 204 ? null : response.json();
}

async function status() {
  return json(`${atelier}/api/integrations/openwish/status?refresh=1`);
}

async function items(listId) {
  return json(`${wish}/api/wishlists/${listId}/items`);
}

async function connect(selectedList) {
  await page.goto(`${atelier}/api/integrations/openwish/connect?returnTo=wishlist`);
  await page.locator("#consent-approve").waitFor();
  for (const checkbox of await page.locator('input[name="wishlist"]').all()) await checkbox.uncheck();
  await page.locator(`input[name="wishlist"][value="${selectedList}"]`).check();
  await page.getByLabel("Start a new list for these ideas").check();
  await page.getByLabel("New list name").fill("Atelier private default");
  if (!consentCaptured) {
    await page.screenshot({ path: "/evidence/integrated-client-https.png" });
    consentCaptured = true;
  }
  await page.locator("#consent-approve").click();
  await page.waitForURL(`${atelier}/wishlist?openwish=connected`);
}

try {
  for (let attempt = 0; attempt < 120; attempt++) {
    const readiness = await context.request.get(wish);
    if (readiness.ok()) break;
    assert.ok([502, 503].includes(readiness.status()), `Unexpected readiness response: ${readiness.status()}`);
    assert.notEqual(attempt, 119, "OpenWish never became ready behind the proxy");
    await page.waitForTimeout(500);
  }
  console.log("Verifying actual-client consent and Strict-cookie callback...");
  assert.ok((await context.request.post(`${wish}/auth/dev-login?persona=owner`)).ok());
  await json(`${atelier}/api/auth/dev-login`, "POST");
  const cookie = (await context.cookies(atelier)).find(value => value.name === "connect.sid");
  assert.ok(cookie?.secure && cookie.sameSite === "Strict", "Actual production session must be Secure and SameSite=Strict");
  const existing = await json(`${wish}/api/wishlists`, "POST", { name: "Atelier existing list", isPrivate: true });
  const callbacks = [];
  page.on("request", request => {
    if (request.url().startsWith(`${atelier}/api/integrations/openwish/callback`)) callbacks.push(request);
  });
  await connect(existing.publicId);
  let state = await status();
  assert.ok(state.connected);
  const defaultList = state.wishlists.find(list => list.isDefault);
  assert.equal(defaultList.name, "Atelier private default");
  assert.notEqual(defaultList.id, existing.publicId);
  assert.ok(callbacks.some(request => new URL(request.url()).searchParams.get("bounce") === "1"),
    "Strict-cookie cross-site callback must complete its same-site bounce");
  assertions.push("Actual Atelier PKCE/state callback succeeds over HTTPS with Secure/Strict cookies and a same-site bounce.");
  assertions.push("Creating a private list alongside an existing selection makes the new private list the default.");

  const piece = await json(`${atelier}/api/records`, "POST", {
    collection: "wishlist",
    data: { name: "Real-client wool coat", brand: "Synthetic Studio", priority: "High", targetPrice: 125,
      imageUrl: "https://atelier.test:8443/private.png", sourceUrl: "https://example.com/coat" }
  });
  const send = () => json(`${atelier}/api/integrations/openwish/pieces/${piece.id}`, "PUT", {});
  await send();
  state = await status();
  let link = state.links[piece.id];
  assert.equal(link.wishlistId, defaultList.id);
  assert.ok(link.openWishUrl.startsWith(`${wish}/`), "Public URLs must retain the HTTPS proxy origin");
  let item = (await items(defaultList.id)).find(value => value.publicId === link.itemId);
  assert.equal(item.name, "Real-client wool coat");
  assert.equal(item.priority, 1);
  assert.ok(!item.imageUrl, "Private companion images must not be sent");
  await send();
  assert.equal((await items(defaultList.id)).filter(value => value.publicId === link.itemId).length, 1);
  assertions.push("Real-client send uses the private default, returns public HTTPS URLs, filters private images, and is idempotent.");

  await json(`${atelier}/api/records/${piece.id}`, "PUT", { data: { ...piece, name: "Real-client edited coat", priority: "" } });
  for (let attempt = 0; attempt < 100; attempt++) {
    item = (await items(defaultList.id)).find(value => value.publicId === link.itemId);
    if (item?.name === "Real-client edited coat" && !item.priority) break;
    await page.waitForTimeout(100);
  }
  assert.equal(item.name, "Real-client edited coat");
  assert.ok(!item.priority);
  await json(`${atelier}/api/integrations/openwish/pieces/${piece.id}`, "PUT", { wishlistId: existing.publicId });
  state = await status();
  link = state.links[piece.id];
  assert.equal(link.wishlistId, existing.publicId);
  assert.ok(!(await items(defaultList.id)).some(value => value.publicId === link.itemId));
  assert.ok((await items(existing.publicId)).some(value => value.publicId === link.itemId));
  assertions.push("Companion edits sync automatically, including clearing priority, and moving preserves the single linked item.");

  await page.goto(`${wish}/wishlists/${existing.publicId}`);
  const deleteDialog = page.getByRole("dialog", { name: "Delete item" });
  for (let attempt = 0; attempt < 100 && !await deleteDialog.isVisible(); attempt++) {
    await page.getByRole("button", { name: "Delete Real-client edited coat" }).click();
    await page.waitForTimeout(100);
  }
  await deleteDialog.getByRole("button", { name: "Delete item", exact: true }).click();
  await deleteDialog.waitFor({ state: "hidden" });
  await json(`${atelier}/api/records/${piece.id}`, "PUT", { data: { ...piece, name: "Owner-deleted idea stays deleted" } });
  await status();
  assert.equal((await items(existing.publicId)).length, 0);
  await send();
  state = await status();
  assert.ok(state.links[piece.id]);
  await json(`${atelier}/api/integrations/openwish/pieces/${piece.id}`, "DELETE");
  assert.equal((await items(defaultList.id)).length, 0);
  assertions.push("Owner deletion is not silently recreated by an edit; explicit resend and remove remain available.");

  await page.goto(`${wish}/Account/Manage/ConnectedApps`);
  await page.getByRole("link", { name: "Disconnect" }).click();
  await page.getByRole("button", { name: "Disconnect The Atelier" }).click();
  state = await status();
  assert.ok(state.needsReconnect && !state.connected);
  await connect(existing.publicId);
  assert.ok((await status()).connected);
  assertions.push("OpenWish settings disconnect produces the real client's reconnect state, and fresh consent restores access.");
  await writeFile("/evidence/integrated-client-result.json", JSON.stringify({ passed: true, assertions }, null, 2));
  console.log(JSON.stringify({ passed: true, assertions }, null, 2));
} finally {
  await browser.close();
}
