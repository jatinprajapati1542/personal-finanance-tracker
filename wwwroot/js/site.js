// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {

    const sidebar = document.getElementById("sidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarOverlay = document.getElementById("sidebarOverlay");

    if (!sidebar || !sidebarToggle || !sidebarOverlay) {
        return;
    }

    sidebarToggle.addEventListener("click", function () {

        sidebar.classList.toggle("show");
        sidebarOverlay.classList.toggle("show");

    });


    sidebarOverlay.addEventListener("click", function () {

        sidebar.classList.remove("show");
        sidebarOverlay.classList.remove("show");

    });

});

document.addEventListener("DOMContentLoaded", function () {

    const currentPath = window.location.pathname.toLowerCase();

    document.querySelectorAll(".sidebar-link").forEach(function (link) {

        const href = link.getAttribute("href");

        if (!href) return;

        const linkPath = new URL(href, window.location.origin)
            .pathname
            .toLowerCase();

        if (linkPath === currentPath) {
            link.classList.add("active");
        }

    });

});