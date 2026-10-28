(() => {
    const toggle = document.querySelector(".sidebar-toggle");
    const sidebar = document.getElementById("appSidebar");

    if (!toggle || !sidebar) return;

    toggle.addEventListener("click", () => {
        const isOpen = sidebar.classList.toggle("sidebar-open");
        toggle.setAttribute("aria-expanded", String(isOpen));
    });

    document.addEventListener("click", (event) => {
        if (window.innerWidth >= 992 || !sidebar.classList.contains("sidebar-open")) {
            return;
        }

        if (!sidebar.contains(event.target) && !toggle.contains(event.target)) {
            sidebar.classList.remove("sidebar-open");
            toggle.setAttribute("aria-expanded", "false");
        }
    });
})();

// Write your JavaScript code.
