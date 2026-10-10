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
