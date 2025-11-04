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