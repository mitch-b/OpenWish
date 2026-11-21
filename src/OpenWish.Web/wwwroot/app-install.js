(() => {
    const key = "openwish-install-dismissed";
    const standalone = matchMedia("(display-mode: standalone)");
    const ios = /iPad|iPhone|iPod/.test(navigator.userAgent) ||
        (navigator.platform === "MacIntel" && navigator.maxTouchPoints > 1);
    const mobile = ios || /Android/.test(navigator.userAgent) || navigator.userAgentData?.mobile;
    let offer;
    let dismissed = false;
    let installed = false;
    let failed = false;

    function wasDismissed() {
        try {
            return dismissed || localStorage.getItem(key) === "true";
        } catch {
            return dismissed;
        }
    }

    function dismiss() {
        dismissed = true;
        try {
            localStorage.setItem(key, "true");
        } catch {
            // Some browser privacy modes deny storage; retain the choice on this page.
        }
    }

    function render() {
        const appMode = standalone.matches || navigator.standalone === true || installed;
        const account = document.querySelector('[data-app-install="account"]');
        for (const panel of document.querySelectorAll("[data-app-install]")) {
            if (panel !== account) {
                panel.hidden = !!account || !mobile || appMode || wasDismissed();
            }
            panel.querySelector("[data-install-copy]").hidden = appMode;
            panel.querySelector("[data-install-installed]").hidden = !appMode;
            panel.querySelector("[data-install-native]").hidden = appMode || !offer;
            panel.querySelector("[data-install-manual]").hidden = appMode || !!offer;
            panel.querySelector("[data-install-ios]").hidden = !ios;
            panel.querySelector("[data-install-browser]").hidden = ios;
            panel.querySelector("[data-install-error]").hidden = !failed || appMode;
        }
    }

    window.openWishInitializeAppInstall = render;
    window.addEventListener("beforeinstallprompt", event => {
        event.preventDefault();
        offer = event;
        failed = false;
        render();
    });
    window.addEventListener("appinstalled", () => {
        installed = true;
        offer = undefined;
        dismiss();
        render();
    });
    window.addEventListener("storage", event => {
        if (event.key === key || event.key === null) render();
    });
    window.addEventListener("pageshow", render);
    standalone.addEventListener("change", render);

    document.addEventListener("click", async event => {
        if (!(event.target instanceof Element)) return;
        if (event.target.closest("[data-install-dismiss]")) {
            dismiss();
            render();
        } else if (offer && event.target.closest("[data-install-native]")) {
            const prompt = offer;
            offer = undefined;
            try {
                await prompt.prompt();
                const { outcome } = await prompt.userChoice;
                if (outcome === "dismissed" || outcome === "accepted") dismiss();
            } catch {
                failed = true;
            }
            render();
        }
    });
})();
