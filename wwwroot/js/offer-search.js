
    document.addEventListener("DOMContentLoaded", function () {
            const searchInput = document.getElementById("offersSearchInput");
    const statusSelect = document.getElementById("offersStatusSelect");

    if (!searchInput || !statusSelect) {
                return;
            }

    if (sessionStorage.getItem("offersSearchFocused") === "true") {
        searchInput.focus();
    searchInput.setSelectionRange(
    searchInput.value.length,
    searchInput.value.length
    );

    sessionStorage.removeItem("offersSearchFocused");
            }

    let timer = null;

    searchInput.addEventListener("input", function () {
        clearTimeout(timer);

    timer = setTimeout(function () {
        sessionStorage.setItem("offersSearchFocused", "true");

    const params = new URLSearchParams(window.location.search);

    if (searchInput.value.trim()) {
        params.set("search", searchInput.value.trim());
                    } else {
        params.delete("search");
                    }

    params.delete("page");

    window.location.href = window.location.pathname + "?" + params.toString();
                }, 450);
            });

    statusSelect.addEventListener("change", function () {
                const params = new URLSearchParams(window.location.search);

    if (statusSelect.value) {
        params.set("status", statusSelect.value);
                } else {
        params.delete("status");
                }

    params.delete("page");

    window.location.href = window.location.pathname + "?" + params.toString();
            });
        });