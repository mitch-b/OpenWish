// Theme management utilities
window.theme = (function () {
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
        document.documentElement.dataset.theme = t;
        document.documentElement.classList.toggle('dark', t === 'dark');
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

    return {
        getPreferred: getPreferred,
        apply: apply,
        toggle: toggle
    };
})();

// Ensure theme applied if early inline script failed or host was re-rendered without dataset
try {
    if (!document.documentElement.dataset.theme) {
        window.theme.apply(window.theme.getPreferred());
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