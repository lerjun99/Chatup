// ChatUp PWA: registers the service worker and drives the "Install app" button.
(function () {
    let deferredPrompt = null;
    const listeners = new Set();

    const isStandalone = () =>
        window.matchMedia("(display-mode: standalone)").matches ||
        window.matchMedia("(display-mode: window-controls-overlay)").matches ||
        window.navigator.standalone === true;

    // iPhone/iPad Safari never fires beforeinstallprompt: installing is Share → "Add to Home Screen"
    const isIos = () =>
        /iphone|ipad|ipod/i.test(navigator.userAgent) ||
        (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);

    const state = () => ({
        canInstall: !!deferredPrompt,
        isStandalone: isStandalone(),
        isIos: isIos() && !isStandalone()
    });

    const notify = () => listeners.forEach(ref => {
        ref.invokeMethodAsync("OnInstallStateChanged", state()).catch(() => listeners.delete(ref));
    });

    // Chrome / Edge / Android: keep the prompt for our own button instead of the browser's mini-infobar
    window.addEventListener("beforeinstallprompt", (e) => {
        e.preventDefault();
        deferredPrompt = e;
        notify();
    });

    window.addEventListener("appinstalled", () => {
        deferredPrompt = null;
        notify();
    });

    if ("serviceWorker" in navigator) {
        window.addEventListener("load", () => {
            navigator.serviceWorker.register("/service-worker.js", { scope: "/" })
                .catch(err => console.warn("Service worker registration failed", err));
        });
    }

    window.chatupPwa = {
        subscribe: (dotNetRef) => { listeners.add(dotNetRef); return state(); },
        unsubscribe: (dotNetRef) => { listeners.delete(dotNetRef); },
        state,

        // "accepted" | "dismissed" | "unavailable"
        install: async () => {
            if (!deferredPrompt) return "unavailable";
            const prompt = deferredPrompt;
            deferredPrompt = null;
            prompt.prompt();
            const choice = await prompt.userChoice;
            notify();
            return choice.outcome;
        }
    };
})();
