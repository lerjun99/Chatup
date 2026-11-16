// ChatUp service worker: makes the app installable (PWA), keeps static files fast and available,
// shows an offline page when there's no connection, and handles notification clicks / Web Push.
//
// ChatUp is a Blazor Server app: pages are live over SignalR, so they can't work offline. Only the static shell
// (css, js, fonts, icons) is cached; the live connection (/_blazor), the hubs and API calls are never touched.

const VERSION = "chatup-v1";
const STATIC_CACHE = `${VERSION}-static`;
const OFFLINE_URL = "/offline.html";

const PRECACHE = [
    OFFLINE_URL,
    "/css/offline.css",
    "/js/offline.js",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/manifest.webmanifest"
];

self.addEventListener("install", (event) => {
    event.waitUntil((async () => {
        const cache = await caches.open(STATIC_CACHE);
        await cache.addAll(PRECACHE);
        await self.skipWaiting();
    })());
});

self.addEventListener("activate", (event) => {
    event.waitUntil((async () => {
        // Drop caches of older versions
        const keys = await caches.keys();
        await Promise.all(keys.filter(k => !k.startsWith(VERSION)).map(k => caches.delete(k)));
        await self.clients.claim();
    })());
});

const isStaticAsset = (url) =>
    /\.(?:css|js|woff2?|ttf|otf|eot|svg|png|jpe?g|gif|webp|ico)$/i.test(url.pathname) &&
    !url.pathname.startsWith("/_blazor");

self.addEventListener("fetch", (event) => {
    const request = event.request;
    if (request.method !== "GET") return;

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;            // API, CDN: straight to the network
    if (url.pathname.startsWith("/_blazor")) return;             // live Blazor circuit
    if (url.pathname.includes("hub")) return;                    // SignalR hubs

    // Pages: always the network (they must be fresh); the offline page when it can't be reached
    if (request.mode === "navigate") {
        event.respondWith((async () => {
            try {
                return await fetch(request);
            } catch {
                const cache = await caches.open(STATIC_CACHE);
                return (await cache.match(OFFLINE_URL)) || Response.error();
            }
        })());
        return;
    }

    // Static files: serve from cache at once, refresh it in the background (stale-while-revalidate)
    if (isStaticAsset(url)) {
        event.respondWith((async () => {
            const cache = await caches.open(STATIC_CACHE);
            const cached = await cache.match(request);
            const network = fetch(request)
                .then((response) => {
                    if (response && response.ok && response.type === "basic") cache.put(request, response.clone());
                    return response;
                })
                .catch(() => cached);
            return cached || network;
        })());
    }
});

// ---------- Notifications ----------
self.addEventListener("notificationclick", (event) => {
    event.notification.close();
    const url = new URL((event.notification.data && event.notification.data.url) || "/", self.location.origin).href;

    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });

        // Reuse an open ChatUp window (browser tab or installed app) if there is one
        for (const client of windows) {
            if (new URL(client.url).origin === self.location.origin) {
                await client.focus();
                if ("navigate" in client) await client.navigate(url);
                return;
            }
        }
        await self.clients.openWindow(url);
    })());
});

// Web Push (sent by the server while ChatUp is closed) lands here once a push service is configured
self.addEventListener("push", (event) => {
    if (!event.data) return;
    let payload = {};
    try { payload = event.data.json(); } catch { payload = { title: "ChatUp", body: event.data.text() }; }

    event.waitUntil(self.registration.showNotification(payload.title || "ChatUp", {
        body: payload.body || "",
        icon: "/icons/icon-192.png",
        badge: "/icons/icon-96.png",
        tag: payload.tag,
        data: { url: payload.url || "/" }
    }));
});
