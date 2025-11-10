// Theme management utilities
window.theme = (function () {
    const _themeHandlers = new WeakMap();
    function getPreferred() {
        try {
            var t = localStorage.getItem('theme');
            if (!t) {
                t = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }
            return t;
        } catch (e) { return 'light'; }
    }

    function apply(t) {
        // Set dataset + html class
        document.documentElement.dataset.theme = t;
        document.documentElement.classList.toggle('dark', t === 'dark');
        // Also mirror a body class (some CSS targets body.dark for scrollbars)
        if (document.body) {
            document.body.classList.toggle('dark', t === 'dark');
        }
        try { localStorage.setItem('theme', t); } catch (e) { }
        // persist for server-side awareness (optional usage later)
        document.cookie = 'ow_theme=' + t + ';path=/;SameSite=Lax;max-age=31536000';
        try {
            window.dispatchEvent(new CustomEvent('theme:changed', { detail: { theme: t } }));
            if (console && console.debug) console.debug('[theme] applied', t);
        } catch (e) { /* no-op */ }
    }

    function toggle() {
        var current = document.documentElement.dataset.theme || getPreferred();
        var next = current === 'dark' ? 'light' : 'dark';
        apply(next);
        return next;
    }

    function getCurrent() {
        return document.documentElement.dataset.theme || getPreferred();
    }

    function registerChangeHandler(dotNetObj) {
        if (!dotNetObj) return;
        const handler = function (e) {
            try { dotNetObj.invokeMethodAsync('OnThemeChanged', e.detail.theme); } catch (err) { /* no-op */ }
        };
        window.addEventListener('theme:changed', handler);
        _themeHandlers.set(dotNetObj, handler);
    }

    function unregisterChangeHandler(dotNetObj) {
        if (!dotNetObj) return;
        const handler = _themeHandlers.get(dotNetObj);
        if (handler) {
            window.removeEventListener('theme:changed', handler);
            _themeHandlers.delete(dotNetObj);
        }
    }

    return {
        getPreferred: getPreferred,
        apply: apply,
        toggle: toggle,
        getCurrent: getCurrent,
        registerChangeHandler: registerChangeHandler,
        unregisterChangeHandler: unregisterChangeHandler
    };
})();

// Global wrapper functions for Blazor JSInterop compatibility
// Blazor's JS.InvokeAsync works better with top-level functions
window.themeGetCurrent = function() {
    return window.theme.getCurrent();
};

window.themeToggle = function() {
    return window.theme.toggle();
};

window.themeRegisterChangeHandler = function(dotNetObj) {
    return window.theme.registerChangeHandler(dotNetObj);
};

window.themeUnregisterChangeHandler = function(dotNetObj) {
    return window.theme.unregisterChangeHandler(dotNetObj);
};

// Ensure theme applied if early inline script failed or host was re-rendered without dataset
try {
    // Always ensure body class syncs with dataset on load (even if early script already set data-theme)
    if (!document.documentElement.dataset.theme) {
        window.theme.apply(window.theme.getPreferred());
    } else if (document.body) {
        document.body.classList.toggle('dark', document.documentElement.dataset.theme === 'dark');
    }
} catch (e) { /* no-op */ }

// Reapply theme on Blazor navigation events (for cases where root attributes are lost or new content overrides styles)
document.addEventListener('blazor:navigation-end', function () {
    try {
        var t = localStorage.getItem('theme') || window.theme.getPreferred();
        window.theme.apply(t);
    } catch (e) { /* no-op */ }
});

// History API hook as additional fallback for SPA-like transitions that may not emit blazor:navigation-end
(function () {
    var applyPersisted = function () {
        try {
            var t = localStorage.getItem('theme');
            if (t) window.theme.apply(t);
        } catch (e) { /* no-op */ }
    };
    ['pushState', 'replaceState'].forEach(function (m) {
        var orig = history[m];
        if (!orig) return;
        history[m] = function () {
            var r = orig.apply(this, arguments);
            applyPersisted();
            return r;
        };
    });
    window.addEventListener('popstate', applyPersisted);
})();

// MutationObserver to guard against accidental removal of data-theme
try {
    new MutationObserver(function (mutations) {
        if (!document.documentElement.dataset.theme) {
            window.theme.apply(window.theme.getPreferred());
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
} catch (e) { /* no-op */ }

// Simple diagnostic hook for manual testing (invoke from console): theme._debug()
window.theme._debug = function () {
    return {
        datasetTheme: document.documentElement.dataset.theme,
        classList: Array.from(document.documentElement.classList),
        stored: localStorage.getItem('theme')
    };
};

// Optional: respond to system changes if user hasn't explicitly chosen
try {
    var mq = window.matchMedia('(prefers-color-scheme: dark)');
    mq.addEventListener('change', function (e) {
        var explicit = localStorage.getItem('theme');
        if (!explicit) {
            window.theme.apply(e.matches ? 'dark' : 'light');
        }
    });
} catch (e) { /* no-op */ }