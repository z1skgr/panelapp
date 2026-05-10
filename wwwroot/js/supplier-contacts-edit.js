document.addEventListener("DOMContentLoaded", function () {
            const contacts = [];

    const nameInput = document.getElementById("contactName");
    const phoneInput = document.getElementById("contactPhone");
    const addBtn = document.getElementById("addContactBtn");
    const table = document.getElementById("newContactsTable");
    const wrapper = document.getElementById("newContactsWrapper");
    const hidden = document.getElementById("hiddenNewContacts");
    const errorBox = document.getElementById("contactError");
    const emailInput = document.getElementById("contactEmail");

    function escapeHtml(value) {
                return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
                    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
            }

    function isValidEmail(value) {
                return value.includes("@@") && value.includes(".");
            }

    function renderContacts() {
        table.innerHTML = "";
    hidden.innerHTML = "";

                contacts.forEach((contact, index) => {
        table.insertAdjacentHTML("beforeend", `
                        <tr>
                            <td>${escapeHtml(contact.fullName)}</td>
                            <td>${contact.phone ? escapeHtml(contact.phone) : "-"}</td>
                            <td>${contact.email ? escapeHtml(contact.email) : "-"}</td>
                            <td class="text-end">
                                <button type="button"
                                        class="btn btn-sm btn-outline-danger"
                                        data-index="${index}">
                                    Αφαίρεση
                                </button>
                            </td>
                        </tr>
                    `);

    hidden.insertAdjacentHTML("beforeend", `
    <input type="hidden" name="NewContactPersons[${index}].FullName" value="${escapeHtml(contact.fullName)}">
        <input type="hidden" name="NewContactPersons[${index}].Phone" value="${escapeHtml(contact.phone || "")}">
        <input type="hidden" name="NewContactPersons[${index}].Email" value="${escapeHtml(contact.email || "")}">
        `);
                });

        wrapper.classList.toggle("d-none", contacts.length === 0);
            }

        addBtn.addEventListener("click", function () {
            errorBox.classList.add("d-none");
        errorBox.textContent = "";


        const fullName = nameInput.value.trim();
        const phone = phoneInput.value.trim();
        const email = emailInput.value.trim();

        if (!fullName) {
            errorBox.textContent = "Το όνομα υπευθύνου είναι υποχρεωτικό.";
        errorBox.classList.remove("d-none");
        return;
                }

        if (phone && !/^[0-9]+$/.test(phone)) {
            errorBox.textContent = "Το τηλέφωνο πρέπει να περιέχει μόνο αριθμούς.";
        errorBox.classList.remove("d-none");
        return;
                }

        if (email && !isValidEmail(email)) {
            errorBox.textContent = "Το email δεν είναι έγκυρο.";
        errorBox.classList.remove("d-none");
        return;
                }


        contacts.push({fullName, phone, email});

        nameInput.value = "";
        phoneInput.value = "";
        emailInput.value = "";
        nameInput.focus();

        renderContacts();
            });

        table.addEventListener("click", function (e) {
                const button = e.target.closest("button[data-index]");
        if (!button) return;

        contacts.splice(parseInt(button.dataset.index, 10), 1);
        renderContacts();
            });
        });
