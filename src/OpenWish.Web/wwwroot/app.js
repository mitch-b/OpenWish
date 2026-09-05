window.openWishScrollToElement = function (id) {
    const element = document.getElementById(id);
    if (!element) {
        throw new Error(`Unable to scroll to missing element: ${id}`);
    }

    element.scrollIntoView();
};
