window.openWishScrollToElement = function (id) {
    const element = document.getElementById(id);
    if (!element) {
        throw new Error(`Unable to scroll to missing element: ${id}`);
    }

    element.scrollIntoView();
};

window.openWishFocusElement = function (id) {
    const element = document.getElementById(id);
    if (!(element instanceof HTMLElement)) {
        throw new Error(`Unable to focus missing element: ${id}`);
    }

    element.focus({ preventScroll: true });
};

const openWishDialogs = new Map();

window.openWishActivateDialog = function (id) {
    const dialog = document.getElementById(id);
    if (!(dialog instanceof HTMLElement)) {
        throw new Error(`Unable to activate missing dialog: ${id}`);
    }

    const existingState = openWishDialogs.get(id);
    if (existingState?.dialog.isConnected) {
        return;
    }
    if (existingState) {
        existingState.dialog.removeEventListener("keydown", existingState.handleKeyDown);
        openWishDialogs.delete(id);
    }

    const previouslyFocused = document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    const focusableSelector = [
        "button:not([disabled])",
        "input:not([disabled])",
        "select:not([disabled])",
        "textarea:not([disabled])",
        "a[href]",
        "[tabindex]:not([tabindex='-1'])"
    ].join(",");
    const handleKeyDown = event => {
        if (event.key === "Escape") {
            event.preventDefault();
            dialog.querySelector("[data-dialog-close]")?.click();
            return;
        }

        if (event.key !== "Tab") {
            return;
        }

        const focusable = [...dialog.querySelectorAll(focusableSelector)]
            .filter(element => element instanceof HTMLElement && element.offsetParent !== null);
        if (focusable.length === 0) {
            event.preventDefault();
            dialog.focus();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    };

    dialog.addEventListener("keydown", handleKeyDown);
    openWishDialogs.set(id, { dialog, handleKeyDown, previouslyFocused });
    document.body.classList.add("dialog-open");

    const firstFocusable = dialog.querySelector("[data-dialog-initial-focus]")
        ?? dialog.querySelector(focusableSelector);
    if (firstFocusable instanceof HTMLElement) {
        firstFocusable.focus({ preventScroll: true });
    } else {
        dialog.tabIndex = -1;
        dialog.focus({ preventScroll: true });
    }
};

window.openWishDeactivateDialog = function (id) {
    const state = openWishDialogs.get(id);
    if (!state) {
        return;
    }

    state.dialog.removeEventListener("keydown", state.handleKeyDown);
    openWishDialogs.delete(id);
    if (openWishDialogs.size === 0) {
        document.body.classList.remove("dialog-open");
    }
    state.previouslyFocused?.focus({ preventScroll: true });
};

document.addEventListener("click", event => {
    if (!(event.target instanceof Element) ||
        !event.target.closest(".nav-scrollable a, .nav-scrollable button")) {
        return;
    }

    const navigationToggle = document.querySelector(".navbar-toggler");
    if (navigationToggle instanceof HTMLInputElement) {
        navigationToggle.checked = false;
    }
});
