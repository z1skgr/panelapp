
    const supplierSelect = document.getElementById('supplierSelect');
    const materialSearch = document.getElementById('materialSearch');
    const materialSelect = document.getElementById('materialSelect');
    const materialMessage = document.getElementById('materialMessage');

    if (!supplierSelect || !materialSearch || !materialSelect || !materialMessage) {
        console.warn("Material filter elements not found.");
    } else {
        let debounceTimer;

        function resetMaterialDropdown(message) {
            materialSelect.innerHTML = '<option value="">-- Επιλογή Υλικού --</option>';
            materialSelect.value = "";
            materialSelect.disabled = true;
            materialMessage.innerText = message || "";
        }

        async function loadMaterials() {
            const supplierId = supplierSelect.value;
            const term = materialSearch.value.trim();

            materialSelect.innerHTML = '<option value="">-- Επιλογή Υλικού --</option>';
            materialSelect.value = "";

            if (!supplierId) {
                resetMaterialDropdown("Επίλεξε προμηθευτή για να δεις υλικά.");
                return;
            }

            materialSelect.disabled = true;
            materialMessage.innerText = "Φόρτωση...";

            const url = `/Panels/SearchMaterials?supplierId=${encodeURIComponent(supplierId)}&term=${encodeURIComponent(term)}`;

            try {
                const response = await fetch(url);
                const data = await response.json();

                materialSelect.innerHTML = '<option value="">-- Επιλογή Υλικού --</option>';

                const items = Array.isArray(data.items) ? data.items : [];

                if (data.needsSearch) {
                    materialSelect.disabled = true;
                    materialMessage.innerText = data.message || "Μεγάλος αριθμός υλικών. Κάνε αναζήτηση.";
                    return;
                }

                if (items.length === 0) {
                    materialSelect.disabled = false;
                    materialMessage.innerText = data.message || "Δεν βρέθηκαν υλικά.";
                    return;
                }

                for (const item of items) {
                    const option = document.createElement('option');
                    option.value = item.value;
                    option.text = item.text;
                    materialSelect.appendChild(option);
                }

                materialSelect.disabled = false;

                if (data.message) {
                    materialMessage.innerText = data.message;
                } else {
                    materialMessage.innerText = term
                        ? `Βρέθηκαν ${items.length} υλικά με βάση το φίλτρο.`
                        : `Βρέθηκαν ${items.length} υλικά.`;
                }
            } catch (error) {
                console.error('Error loading materials:', error);
                resetMaterialDropdown("Σφάλμα φόρτωσης υλικών.");
            }
        }

        supplierSelect.addEventListener('change', function () {
            materialSearch.value = "";
            resetMaterialDropdown("Φόρτωση υλικών...");
            loadMaterials();
        });

        materialSearch.addEventListener('input', function () {
            clearTimeout(debounceTimer);

            if (!supplierSelect.value) {
                resetMaterialDropdown("Επίλεξε προμηθευτή για να δεις υλικά.");
                return;
            }

            debounceTimer = setTimeout(loadMaterials, 250);
        });

        document.addEventListener('DOMContentLoaded', function () {
            if (supplierSelect.value) {
                loadMaterials();
            } else {
                resetMaterialDropdown("Επίλεξε προμηθευτή για να δεις υλικά.");
            }
        });
    }
