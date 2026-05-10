
    document.addEventListener("DOMContentLoaded", function () {
        const popup = document.getElementById("aiAssistantPopup");
    const toggle = document.getElementById("aiPopupToggle");
    const close = document.getElementById("aiPopupClose");

    if (!popup || !toggle || !close) return;

        toggle.addEventListener("click", () => {
        popup.classList.toggle("is-open");
        });

        close.addEventListener("click", () => {
        popup.classList.remove("is-open");
        });

    const alreadyShown = localStorage.getItem("aiAssistantShown");

    if (!alreadyShown) {
        setTimeout(() => {
            popup.classList.add("is-open");
        }, 900);

    localStorage.setItem("aiAssistantShown", "true");
        }
    });