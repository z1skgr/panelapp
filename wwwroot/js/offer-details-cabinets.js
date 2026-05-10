
    const cabinetSupplierSelect = document.getElementById("cabinetSupplierSelect");
    const cabinetSearchInput = document.getElementById("cabinetSearchInput");
    const cabinetSelect = document.getElementById("cabinetSelect");
    const cabinetSearchMessage = document.getElementById("cabinetSearchMessage");

    let cabinetSearchTimer = null;

    function clearCabinets(message) {
        if (!cabinetSelect || !cabinetSearchMessage) return;

        cabinetSelect.innerHTML = "";

        const option = document.createElement("option");
        option.value = "";
        option.textContent = message;

        cabinetSelect.appendChild(option);
        cabinetSearchMessage.textContent = message;
    }

    async function loadCabinets() {
        const supplierId = cabinetSupplierSelect.value;
        const term = cabinetSearchInput.value;

        if (!supplierId) {
            clearCabinets("Επίλεξε προμηθευτή για να φορτωθούν τα ερμάρια.");
            return;
        }

        const url =
            `/Offers/SearchCabinets?supplierId=${encodeURIComponent(supplierId)}&term=${encodeURIComponent(term)}`;

        const response = await fetch(url);
        const result = await response.json();

        cabinetSelect.innerHTML = "";

        if (!result.items || result.items.length === 0) {
            const option = document.createElement("option");
            option.value = "";
            option.textContent = result.message || "Δεν βρέθηκαν ερμάρια.";

            cabinetSelect.appendChild(option);
            cabinetSearchMessage.textContent = result.message || "Δεν βρέθηκαν ερμάρια.";
            return;
        }

        const emptyOption = document.createElement("option");
        emptyOption.value = "";
        emptyOption.textContent = "-- Επιλογή Ερμαρίου --";
        cabinetSelect.appendChild(emptyOption);

        result.items.forEach(item => {
            const option = document.createElement("option");
            option.value = item.value;
            option.textContent = item.text;
            cabinetSelect.appendChild(option);
        });

        cabinetSearchMessage.textContent = result.message;
    }

    if (cabinetSupplierSelect && cabinetSearchInput) {
        cabinetSupplierSelect.addEventListener("change", loadCabinets);

        cabinetSearchInput.addEventListener("input", function () {
            clearTimeout(cabinetSearchTimer);

            cabinetSearchTimer = setTimeout(function () {
                loadCabinets();
            }, 350);
        });
    }
