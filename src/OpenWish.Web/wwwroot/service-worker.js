// Makes OpenWish installable and shows a friendly page when the device is offline.
// Only the offline page is stored: wishlists, events, and every API response always come from the server.
const OFFLINE_CACHE = "openwish-offline-v1";
const OFFLINE_URL = "offline.html";
const OFFLINE_ASSETS = [OFFLINE_URL, "icons/app/openwish-192.png"];

self.addEventListener("install", event => {
    event.waitUntil(caches.open(OFFLINE_CACHE).then(cache => cache.addAll(OFFLINE_ASSETS)));
    self.skipWaiting();
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(keys.filter(key => key !== OFFLINE_CACHE).map(key => caches.delete(key))))
            .then(() => self.clients.claim()));
});

self.addEventListener("fetch", event => {
    if (event.request.mode !== "navigate") {
        return;
    }

    event.respondWith(fetch(event.request).catch(() => caches.match(OFFLINE_URL)));
});
