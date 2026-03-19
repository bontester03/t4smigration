(function () {
    function onReady(callback) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", callback);
            return;
        }

        callback();
    }

    onReady(function () {
        var sidebarToggle = document.getElementById("sidebarToggle");
        if (sidebarToggle) {
            sidebarToggle.addEventListener("click", function (event) {
                event.preventDefault();
                document.body.classList.toggle("sidenav-toggled");
                document.body.classList.toggle("sidebar-open");
            });
        }

        if (window.feather && typeof window.feather.replace === "function") {
            window.feather.replace();
        }
    });
})();
