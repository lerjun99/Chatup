// ChatUp notifications: chime, desktop (system) notifications and the tab-title badge.
// Loaded as a file because the Content-Security-Policy blocks inline scripts.
(function () {
    let audioCtx = null;
    let swRegistration = null;
    const baseTitle = { value: null };

    // Browsers only allow sound after the user has interacted with the page once: unlock on the first gesture
    function unlockAudio() {
        try {
            audioCtx = audioCtx || new (window.AudioContext || window.webkitAudioContext)();
            if (audioCtx.state === "suspended") audioCtx.resume();
        } catch { /* no Web Audio */ }
    }
    ["pointerdown", "keydown", "touchstart"].forEach(e =>
        window.addEventListener(e, unlockAudio, { once: true, passive: true }));

    // A soft two-note chime, synthesised (no audio file to load or cache)
    function chime() {
        try {
            unlockAudio();
            if (!audioCtx || audioCtx.state !== "running") return false;

            const now = audioCtx.currentTime;
            const master = audioCtx.createGain();
            master.gain.value = 0.18;
            master.connect(audioCtx.destination);

            [[880, 0], [1318.5, 0.12]].forEach(([freq, at]) => {
                const osc = audioCtx.createOscillator();
                const gain = audioCtx.createGain();
                osc.type = "sine";
                osc.frequency.value = freq;
                gain.gain.setValueAtTime(0.0001, now + at);
                gain.gain.exponentialRampToValueAtTime(1, now + at + 0.015);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + at + 0.45);
                osc.connect(gain).connect(master);
                osc.start(now + at);
                osc.stop(now + at + 0.5);
            });
            return true;
        } catch {
            return false;
        }
    }

    async function getRegistration() {
        if (swRegistration || !("serviceWorker" in navigator)) return swRegistration;
        try {
            swRegistration = await navigator.serviceWorker.register("/service-worker.js", { scope: "/" });
            await navigator.serviceWorker.ready;
        } catch (e) {
            console.warn("Notification service worker not registered", e);
        }
        return swRegistration;
    }

    window.chatupNotify = {
        chime,

        isHidden: () => document.visibilityState === "hidden" || !document.hasFocus(),

        // "granted" | "denied" | "default" | "unsupported"
        permission: () => ("Notification" in window) ? Notification.permission : "unsupported",

        requestPermission: async () => {
            if (!("Notification" in window)) return "unsupported";
            const result = await Notification.requestPermission();
            if (result === "granted") await getRegistration();
            return result;
        },

        // A system notification; clicking it focuses ChatUp and opens the url
        show: async (title, body, url, tag) => {
            if (!("Notification" in window) || Notification.permission !== "granted") return false;

            const options = {
                body: body || "",
                icon: "/icons/icon-192.png",
                badge: "/icons/icon-96.png",
                tag: tag || undefined,
                renotify: !!tag,
                data: { url: url || "/" }
            };

            const reg = await getRegistration();
            if (reg && reg.showNotification) {
                await reg.showNotification(title, options);
                return true;
            }

            // Fallback without a service worker (works while the tab is open)
            const n = new Notification(title, options);
            n.onclick = () => { window.focus(); if (url) window.location.href = url; n.close(); };
            return true;
        },

        // "(3) Inbox - ChatUp" while there are unread notifications
        setTitleBadge: (count) => {
            if (baseTitle.value === null || !document.title.startsWith("(")) baseTitle.value = document.title.replace(/^\(\d+\+?\)\s*/, "");
            document.title = count > 0 ? `(${count > 99 ? "99+" : count}) ${baseTitle.value}` : baseTitle.value;
        }
    };

    // Already allowed earlier: register the worker up front so the first popup is instant
    if ("Notification" in window && Notification.permission === "granted") getRegistration();
})();
