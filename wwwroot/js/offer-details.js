
    document.addEventListener("DOMContentLoaded", function () {

        const alerts = document.querySelectorAll(".auto-dismiss-alert, .alert-success, .alert-danger");

        alerts.forEach(alert => {
            setTimeout(() => {
                alert.classList.add("fade");

                setTimeout(() => {
                    alert.remove();
                }, 600);

            }, 3500);
        });

        const copyButtons = document.querySelectorAll("#copyOfferButton, #copyOfferButtonTop");
        const copyText = document.getElementById("offerCopyText");

        if (copyButtons.length && copyText) {
            copyButtons.forEach(copyButton => {
                copyButton.addEventListener("click", async function () {
                    const text = copyText.innerText.trim();

                    try {
                        await navigator.clipboard.writeText(text);
                        showTemporaryToast("Η προσφορά αντιγράφηκε στο πρόχειρο.", "success");
                    }
                    catch {
                        showTemporaryToast("Δεν ήταν δυνατή η αντιγραφή.", "danger");
                    }
                });
            });
        }

        function showTemporaryToast(message, type = "success") {

            const existing = document.querySelector(".temporary-toast");

            if (existing) {
                existing.remove();
            }

            const toast = document.createElement("div");

            toast.className = `temporary-toast ${type}`;

            toast.innerHTML = `
        <div class="temporary-toast-content">
            <i class="bi ${type === "success"
                    ? "bi-check-circle-fill"
                    : "bi-exclamation-triangle-fill"
                }"></i>

            <span>${message}</span>
        </div>
    `;

            document.body.appendChild(toast);

            requestAnimationFrame(() => {
                toast.classList.add("show");
            });

            setTimeout(() => {

                toast.classList.remove("show");

                setTimeout(() => {
                    toast.remove();
                }, 250);

            }, 2600);
        }



        
    });
