
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
                        showTemporaryMessage("Η προσφορά αντιγράφηκε στο πρόχειρο.", "success");
                    }
                    catch {
                        showTemporaryMessage("Δεν ήταν δυνατή η αντιγραφή.", "danger");
                    }
                });
            });
        }

        function showTemporaryMessage(message, type) {
            const alert = document.createElement("div");

            alert.className = `alert alert-${type} js-alert mt-3`;
            alert.textContent = message;

            const container = document.querySelector(".container-fluid");
            container.insertBefore(alert, container.children[1]);

            setTimeout(() => {
                alert.classList.add("fade");

                setTimeout(() => {
                    alert.remove();
                }, 600);

            }, 3500);
        }
    });
