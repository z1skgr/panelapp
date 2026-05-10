(function () {
    const storageKey = "app-theme";
    const body = document.body;
    const toggleBtn = document.getElementById("themeToggleBtn");
    const toggleIcon = document.getElementById("themeToggleIcon");

    function applyTheme(theme) {
        const isDark = theme === "dark";

        body.classList.toggle("dark-mode", isDark);

        if (toggleIcon) {
            toggleIcon.className = isDark
                ? "bi bi-sun"
                : "bi bi-moon-stars";
        }

        if (toggleBtn) {
            toggleBtn.title = isDark
                ? "Αλλαγή σε light mode"
                : "Αλλαγή σε dark mode";
        }
    }

    function getAutoTheme() {
        const hour = new Date().getHours();
        return hour >= 7 && hour < 20 ? "light" : "dark";
    }

    const savedTheme = localStorage.getItem(storageKey);

    applyTheme(savedTheme || getAutoTheme());

    if (toggleBtn) {
        toggleBtn.addEventListener("click", function () {
            const currentTheme = body.classList.contains("dark-mode")
                ? "dark"
                : "light";

            const nextTheme = currentTheme === "dark"
                ? "light"
                : "dark";

            localStorage.setItem(storageKey, nextTheme);

            applyTheme(nextTheme);
        });
    }
})();

document.addEventListener("DOMContentLoaded", function () {
    const alerts = document.querySelectorAll(".auto-dismiss-alert");

    alerts.forEach(function (alertElement) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alertElement);
            bsAlert.close();
        }, 4500);
    });
});

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el);
    });
});