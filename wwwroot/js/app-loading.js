document.addEventListener("submit", function (e) {

    const form = e.target;

    const submitBtn = form.querySelector(
        'button[type="submit"]:not(.no-loading)'
    );

    if (!submitBtn) return;

    if (submitBtn.classList.contains("btn-loading"))
        return;

    submitBtn.classList.add("btn-loading");

    submitBtn.disabled = true;
});