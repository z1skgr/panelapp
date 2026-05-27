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

    if (!form.classList.contains("js-confirm-form")) {
        return;
    }

    // Αν έχει ήδη επιβεβαιωθεί -> άστο να συνεχίσει
    if (form.dataset.confirmed === "true") {
        return;
    }

    // Σταματάμε το submit
    e.preventDefault();
    e.stopImmediatePropagation();

    showConfirm({
        title: form.dataset.confirmTitle || "Επιβεβαίωση",
        message: form.dataset.confirmMessage || "Είσαι σίγουρος;",
        confirmText: form.dataset.confirmButton || "Επιβεβαίωση",

        onConfirm: function () {
            form.dataset.confirmed = "true";

            const hiddenSubmit = document.createElement("button");
            hiddenSubmit.type = "submit";
            hiddenSubmit.hidden = true;

            form.appendChild(hiddenSubmit);
            hiddenSubmit.click();
            hiddenSubmit.remove();
        }
    });
});