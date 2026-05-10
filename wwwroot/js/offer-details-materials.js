
    const supplierSelect = document.getElementById("supplierSelect");
    const materialSearchInput = document.getElementById("materialSearchInput");
    const materialSelect = document.getElementById("materialSelect");
    const materialSearchMessage = document.getElementById("materialSearchMessage");

    let searchTimer = null;

    function clearMaterials(message) {
        materialSelect.innerHTML = "";
        const option = document.createElement("option");
        option.value = "";
        option.textContent = message;
        materialSelect.appendChild(option);
        materialSearchMessage.textContent = message;
    }

    async function loadMaterials() {
        const supplierId = supplierSelect.value;
        const term = materialSearchInput.value;

        if (!supplierId) {
            clearMaterials("Επίλεξε προμηθευτή για να φορτωθούν τα υλικά.");
            return;
        }

        const url =
            `/Offers/SearchMaterials?supplierId=${encodeURIComponent(supplierId)}&term=${encodeURIComponent(term)}`;

        const response = await fetch(url);
        const result = await response.json();

        materialSelect.innerHTML = "";

        if (!result.items || result.items.length === 0) {
            const option = document.createElement("option");
            option.value = "";
            option.textContent = result.message || "Δεν βρέθηκαν υλικά.";
            materialSelect.appendChild(option);
            materialSearchMessage.textContent = result.message || "Δεν βρέθηκαν υλικά.";
            return;
        }

        const emptyOption = document.createElement("option");
        emptyOption.value = "";
        emptyOption.textContent = "-- Επιλογή Υλικού --";
        materialSelect.appendChild(emptyOption);

        result.items.forEach(item => {
            const option = document.createElement("option");
            option.value = item.value;
            option.textContent = item.text;
            materialSelect.appendChild(option);
        });

        materialSearchMessage.textContent = result.message;
    }

    supplierSelect.addEventListener("change", loadMaterials);

    materialSearchInput.addEventListener("input", function () {
        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {
            loadMaterials();
        }, 350);
    });


    