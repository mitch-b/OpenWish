// Buttons with data-copy-text write the clipboard inside the browser's own click so Safari accepts it.
window.openWishLastCopy = Promise.resolve(false);
document.addEventListener("click", function (event) {
    const button = event.target instanceof Element ? event.target.closest("[data-copy-text]") : null;
    if (button && !button.disabled) {
        const feedback = document.getElementById(button.getAttribute("data-copy-feedback") ?? "");
        window.openWishLastCopy = window.openWishCopyText(button.getAttribute("data-copy-text") ?? "").then(() => {
            if (feedback) feedback.textContent = "Address copied. Paste it into your assistant.";
            return true;
        }, () => {
            if (feedback) feedback.textContent = "Could not copy. Select the address and copy it manually.";
            return false;
        });
    }
});

window.openWishLastCopyResult = function () {
    return window.openWishLastCopy;
};

function updateIntegrationPicker(picker, reconcileDefault = false) {
    const search = picker.querySelector("[data-picker-search]").value.trim().toLocaleLowerCase();
    const selectedOnly = picker.querySelector("[data-picker-selected]").checked;
    const options = [...picker.querySelectorAll("[data-picker-option]")];
    const selected = options.filter(option => option.querySelector('input[name="wishlist"]').checked);
    let visible = 0;
    for (const option of options) {
        option.hidden = !option.dataset.pickerName.toLocaleLowerCase().includes(search) ||
            (selectedOnly && !option.querySelector('input[name="wishlist"]').checked);
        if (!option.hidden) visible += 1;
    }
    picker.querySelector("[data-picker-status]").textContent =
        `${selected.length} selected of ${options.length} lists. ${visible} shown.`;
    picker.querySelector("[data-picker-empty]").hidden = visible !== 0;

    // Search never disables checkboxes: hidden selections must still be posted.
    const form = picker.closest("form");
    const defaultSelect = form?.querySelector('select[name="defaultWishlist"]');
    if (defaultSelect) {
        const allowAllWishlists = form.querySelector('input[name="allowAllWishlists"]')?.checked === true;
        const selectedIds = new Set(selected.map(option => option.querySelector('input[name="wishlist"]').value));
        if (reconcileDefault && !allowAllWishlists && !selectedIds.has(defaultSelect.value)) {
            defaultSelect.value = selectedIds.values().next().value ?? "";
        }
        for (const option of defaultSelect.options) {
            // Keep a submitted invalid default visible when rendering a validation error.
            option.disabled = !allowAllWishlists && !selectedIds.has(option.value) && option.value !== defaultSelect.value;
        }
    }
}

window.openWishInitializeIntegrationPickers = function () {
    for (const picker of document.querySelectorAll("[data-integration-picker]")) {
        picker.querySelector("[data-picker-tools]").hidden = false;
        updateIntegrationPicker(picker);
    }
};

document.addEventListener("DOMContentLoaded", window.openWishInitializeIntegrationPickers);
document.addEventListener("keydown", event => {
    if (event.key === "Enter" && event.target instanceof Element && event.target.matches("[data-picker-search]")) {
        event.preventDefault();
    }
});
for (const eventName of ["input", "change"]) {
    document.addEventListener(eventName, event => {
        if (!(event.target instanceof Element)) return;
        const permissionsChanged = event.target.matches('input[name="wishlist"], input[name="allowAllWishlists"]');
        const picker = event.target.closest("[data-integration-picker]") ??
            (event.target.matches('input[name="allowAllWishlists"]')
                ? event.target.closest("form")?.querySelector("[data-integration-picker]") : null);
        if (picker) updateIntegrationPicker(picker, permissionsChanged);
    });
}

window.openWishCopyText = async function (text) {
    if (navigator.clipboard?.writeText) {
        await navigator.clipboard.writeText(text);
        return;
    }

    // Self-hosted instances on plain HTTP have no async clipboard API.
    const previouslyFocused = document.activeElement;
    const field = document.createElement("textarea");
    field.value = text;
    field.setAttribute("readonly", "");
    field.style.position = "fixed";
    field.style.opacity = "0";
    document.body.appendChild(field);
    try {
        field.focus();
        field.select();
        if (!document.execCommand("copy")) {
            throw new Error("Unable to copy text.");
        }
    } finally {
        field.remove();
        if (previouslyFocused instanceof HTMLElement) {
            previouslyFocused.focus({ preventScroll: true });
        }
    }
};

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
const openWishInertElements = new Map();

function makeDialogBackgroundInert(dialog) {
    const inertElements = [];
    let currentElement = dialog;

    while (currentElement.parentElement && currentElement.parentElement !== document.body) {
        const parentElement = currentElement.parentElement;
        for (const sibling of parentElement.children) {
            if (!(sibling instanceof HTMLElement) ||
                sibling === currentElement ||
                sibling.hasAttribute("data-dialog-background-allowed")) {
                continue;
            }

            const inertState = openWishInertElements.get(sibling);
            if (inertState) {
                inertState.count += 1;
            } else {
                openWishInertElements.set(sibling, {
                    count: 1,
                    wasInert: sibling.inert
                });
                sibling.inert = true;
            }
            inertElements.push(sibling);
        }
        currentElement = parentElement;
    }

    return inertElements;
}

function restoreDialogBackground(inertElements) {
    for (const element of inertElements) {
        const inertState = openWishInertElements.get(element);
        if (!inertState) {
            continue;
        }

        inertState.count -= 1;
        if (inertState.count === 0) {
            element.inert = inertState.wasInert;
            openWishInertElements.delete(element);
        }
    }
}

function releaseDialogState(state) {
    state.dialog.removeEventListener("keydown", state.handleKeyDown);
    restoreDialogBackground(state.inertElements);
}

window.openWishActivateDialog = function (id, returnFocusId, monitorOpenerRemoval = false) {
    const dialog = document.getElementById(id);
    if (!(dialog instanceof HTMLElement)) {
        throw new Error(`Unable to activate missing dialog: ${id}`);
    }

    const existingState = openWishDialogs.get(id);
    if (existingState?.dialog.isConnected) {
        return;
    }
    if (existingState) {
        releaseDialogState(existingState);
        openWishDialogs.delete(id);
    }

    const returnFocusElement = returnFocusId
        ? document.getElementById(returnFocusId)
        : null;
    const previouslyFocused = returnFocusElement instanceof HTMLElement
        ? returnFocusElement
        : document.activeElement instanceof HTMLElement
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
    const inertElements = makeDialogBackgroundInert(dialog);
    openWishDialogs.set(id, {
        dialog,
        handleKeyDown,
        inertElements,
        previouslyFocused,
        monitorOpenerRemoval
    });
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

    releaseDialogState(state);
    openWishDialogs.delete(id);
    if (openWishDialogs.size === 0) {
        document.body.classList.remove("dialog-open");
    }
    if (state.previouslyFocused?.isConnected && !state.previouslyFocused.inert) {
        state.previouslyFocused.focus({ preventScroll: true });
    }

    const focusMainContent = () => {
        const fallback = document.querySelector("main h1, main h2, main h3, main");
        if (fallback instanceof HTMLElement) {
            if (!fallback.hasAttribute("tabindex")) {
                fallback.tabIndex = -1;
            }
            fallback.focus({ preventScroll: true });
        }
    };
    if (!state.previouslyFocused?.isConnected) {
        focusMainContent();
        return;
    }
    if (!state.monitorOpenerRemoval) {
        return;
    }

    const openerObserver = new MutationObserver(() => {
        if (!state.previouslyFocused?.isConnected) {
            openerObserver.disconnect();
            focusMainContent();
        }
    });
    openerObserver.observe(document.body, { childList: true, subtree: true });
    setTimeout(() => openerObserver.disconnect(), 2000);
};

const syncNavigationDisclosure = navigationToggle => {
    navigationToggle.setAttribute("aria-expanded", navigationToggle.checked ? "true" : "false");
};

document.addEventListener("change", event => {
    if (event.target instanceof HTMLInputElement &&
        event.target.matches(".navbar-toggler")) {
        syncNavigationDisclosure(event.target);
    }
});

document.addEventListener("keydown", event => {
    if (event.target instanceof Element &&
        event.target.closest("#wishlistTabs [role='tab']") &&
        ["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) {
        event.preventDefault();
    }
});

document.addEventListener("click", event => {
    if (!(event.target instanceof Element) ||
        !event.target.closest(".nav-scrollable a, .nav-scrollable button")) {
        return;
    }

    const navigationToggle = document.querySelector(".navbar-toggler");
    if (navigationToggle instanceof HTMLInputElement) {
        navigationToggle.checked = false;
        syncNavigationDisclosure(navigationToggle);
    }
});

// Mirrors ProductLink.ContainsWebAddress so pasted text that contains a product link can be imported.
const productLinkPattern = /https?:\/\/[^\s<>"'`]+|(?:^|[^\w@.\/-])www\.[a-z0-9-]+(?:\.[a-z0-9-]+)+/i;
const maxProductLinkPasteLength = 4096;
const productLinkPasteListeners = new Map();

const textHasProductLink = text =>
    typeof text === "string" &&
    text.length > 0 &&
    text.length <= maxProductLinkPasteLength &&
    productLinkPattern.test(text);

window.openWishTextHasProductLink = textHasProductLink;

window.openWishListenForProductLinkPaste = function (key, dotNetReference) {
    productLinkPasteListeners.set(key, dotNetReference);
};

window.openWishStopListeningForProductLinkPaste = function (key) {
    productLinkPasteListeners.delete(key);
};

const isEditableTarget = target =>
    target instanceof Element &&
    target.closest("input, textarea, select, [contenteditable]:not([contenteditable='false'])") !== null;

// A link pasted into a product link field is imported right away; keep the browser from also inserting
// the shared text so the field shows only the cleaned link.
document.addEventListener("paste", event => {
    if (!(event.target instanceof Element) || !event.target.closest("[data-product-link-input]")) {
        return;
    }

    if (textHasProductLink(event.clipboardData?.getData("text") ?? "")) {
        event.preventDefault();
    }
}, true);

// On pages that opt in, pasting a product link outside a text field starts adding it as a new item.
document.addEventListener("paste", event => {
    if (event.defaultPrevented ||
        productLinkPasteListeners.size === 0 ||
        openWishDialogs.size > 0 ||
        isEditableTarget(event.target)) {
        return;
    }

    const text = event.clipboardData?.getData("text") ?? "";
    if (!textHasProductLink(text)) {
        return;
    }

    const listener = [...productLinkPasteListeners.values()].at(-1);
    event.preventDefault();
    listener.invokeMethodAsync("ImportPastedProductLink", text)
        .catch(error => console.error("Unable to add the pasted product link.", error));
});
window.openWishInitializePasswordToggles = function () {
    for (const button of document.querySelectorAll("[data-password-toggle]")) {
        button.hidden = false;
    }
};

document.addEventListener("DOMContentLoaded", window.openWishInitializePasswordToggles);
document.addEventListener("click", event => {
    const button = event.target instanceof Element ? event.target.closest("[data-password-toggle]") : null;
    if (!button) return;
    const show = button.getAttribute("aria-pressed") !== "true";
    for (const id of button.getAttribute("aria-controls").split(" ")) {
        const input = document.getElementById(id);
        if (input instanceof HTMLInputElement) input.type = show ? "text" : "password";
    }
    button.setAttribute("aria-pressed", String(show));
    button.textContent = show ? "Hide passwords" : "Show passwords";
});
globalThis.openWishInitializeGuestAccess = function () {
    const form = document.querySelector('form[data-guest-access]');
    const token = new URLSearchParams(location.hash.slice(1)).get('access');
    if (location.pathname === '/guest' && token) {
        history.replaceState(null, '', location.pathname + location.search);
        if (form && /^[0-9A-F]{64}$/.test(token)) {
            const button = form.querySelector('button');
            form.hidden = false;
            form.addEventListener('submit', () => {
                form.querySelector('input[name="token"]').value = token;
                button.disabled = true;
                button.textContent = 'Opening private invitation...';
            }, { once: true });
        }
    }
    const name = document.getElementById('guest-match-name');
    if (name && new URLSearchParams(location.search).get('opened') === 'true') {
        name.focus();
    }
    const eventDate = document.querySelector('time[data-guest-event-date]');
    if (eventDate) {
        eventDate.textContent = new Intl.DateTimeFormat(undefined, { dateStyle: 'long', timeStyle: 'short' })
            .format(new Date(eventDate.dateTime));
    }
};
