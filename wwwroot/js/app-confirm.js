window.showConfirm = function ({
    title = "Επιβεβαίωση",
    message = "Είσαι σίγουρος;",
    confirmText = "Επιβεβαίωση",
    onConfirm
}) {
    const modalEl = document.getElementById("appConfirmModal");
    const titleEl = document.getElementById("appConfirmModalTitle");
    const messageEl = document.getElementById("appConfirmModalMessage");
    const okBtn = document.getElementById("appConfirmModalOk");

    if (!modalEl || !titleEl || !messageEl || !okBtn) return;

    titleEl.textContent = title;
    messageEl.textContent = message;
    okBtn.textContent = confirmText;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    okBtn.onclick = () => {
        modal.hide();

        if (typeof onConfirm === "function") {
            onConfirm();
        }
    };

    modal.show();
};

document.addEventListener("submit", function (e) {
    const form = e.target;

    if (!form.classList.contains("js-confirm-form")) return;

    e.preventDefault();

    showConfirm({
        title: form.dataset.confirmTitle || "Επιβεβαίωση",
        message: form.dataset.confirmMessage || "Είσαι σίγουρος;",
        confirmText: form.dataset.confirmButton || "Επιβεβαίωση",
        onConfirm: function () {
            form.classList.remove("js-confirm-form");
            form.submit();
        }
    });
});