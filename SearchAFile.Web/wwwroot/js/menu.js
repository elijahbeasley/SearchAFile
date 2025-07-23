document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("header-toggle"),
        nav = document.getElementById("nav-bar"),
        bodypd = document.getElementById("body-pd"),
        headerpd = document.getElementById("header"),
        divBody = document.getElementById("divBody"),
        footer = document.getElementById("footer");

    // 1) Collect all elements that transition
    const transElems = [nav, bodypd, headerpd, divBody, footer];

    // 2) Temporarily disable their transitions
    transElems.forEach(el => {
        if (el) el.style.transition = "none";
    });

    // 3) Apply the "large" classes immediately (no animation)
    if (getCookie("SearchAFile_menu") === "large") {
        nav?.classList.add("show-menu");
        bodypd?.classList.add("body-pd");
        headerpd?.classList.add("body-pd");
        divBody?.classList.add("height-100-show");
        footer?.classList.add("footer-show");
    }

    // 4) Force a reflow so browsers “commit” the no-transition state
    transElems.forEach(el => {
        if (el) el.getBoundingClientRect();
    });

    // 5) Re-enable transitions for future toggles
    transElems.forEach(el => {
        if (el) el.style.transition = "";
    });

    // 6) Wire up your toggle as before
    if (toggle && nav && bodypd && headerpd && divBody && footer) {
        toggle.addEventListener("click", () => {
            nav.classList.toggle("show-menu");
            bodypd.classList.toggle("body-pd");
            headerpd.classList.toggle("body-pd");
            divBody.classList.toggle("height-100-show");
            footer.classList.toggle("footer-show");

            const isLarge = nav.classList.contains("show-menu");
            setCookie("SearchAFile_menu", isLarge ? "large" : "small");
        });
    }

    // 7) And your link‐highlight logic
    const linkColor = document.querySelectorAll(".nav_link");
    linkColor.forEach(l => l.addEventListener("click", function () {
        linkColor.forEach(x => x.classList.remove("active"));
        this.classList.add("active");
    }));
});