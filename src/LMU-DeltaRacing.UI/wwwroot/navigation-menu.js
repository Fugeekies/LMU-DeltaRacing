// Closes a menu of the navigation bar once one of its pages is chosen. Menus open on hover or
// keyboard focus; after a click the focus would stay on the chosen link and keep the menu open
// over the new page. Delegated from the document, so it works on the bar rendered by Blazor.
document.addEventListener("click", (event) => {
    "use strict";

    const link = event.target instanceof Element ? event.target.closest(".nav-menu a, .nav-group-toggle") : null;
    if (link) {
        link.blur();
    }
});
