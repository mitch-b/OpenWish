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
