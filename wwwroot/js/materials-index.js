(() => {
    const searchForm = document.getElementById("materialsSearchForm");
    const searchInput = document.getElementById("searchTerm");
    const supplierSelect = document.getElementById("supplierId");
    const supplierFilterSelect = document.getElementById("supplierFilter");

    const optionsForm = document.getElementById("materialOptionsForm");
    const suppliersPerPage = document.getElementById("suppliersPerPage");
    const materialsPerSupplier = document.getElementById("materialsPerSupplier");

    const shouldRefocus = sessionStorage.getItem('materialsSearchFocus') === 'true';

    if (shouldRefocus && searchInput) {
        searchInput.focus();

        const length = searchInput.value.length;
        searchInput.setSelectionRange(length, length);

        sessionStorage.removeItem('materialsSearchFocus');
    }

    let debounceTimer;

    if (searchForm && searchInput) {
        searchInput.addEventListener("input", () => {
            clearTimeout(debounceTimer);

            debounceTimer = setTimeout(() => {
                sessionStorage.setItem('materialsSearchFocus', 'true');
                searchForm.submit();
            }, 750);
        });
    }

    if (searchForm && supplierSelect) {
        supplierSelect.addEventListener("change", () => searchForm.submit());
    }

    if (searchForm && supplierFilterSelect) {
        supplierFilterSelect.addEventListener("change", () => {
            if (supplierSelect) {
                supplierSelect.value = "";
            }

            searchForm.submit();
        });
    }

    if (optionsForm && suppliersPerPage) {
        suppliersPerPage.addEventListener("change", () => optionsForm.submit());
    }

    if (optionsForm && materialsPerSupplier) {
        materialsPerSupplier.addEventListener("change", () => optionsForm.submit());
    }
})();