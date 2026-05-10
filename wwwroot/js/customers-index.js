(() => {
    const searchForm = document.getElementById("customersSearchForm");
    const searchInput = document.getElementById("searchTerm");

    const shouldRefocus = sessionStorage.getItem('customersSearchFocus') === 'true';

    if (shouldRefocus && searchInput) {
        searchInput.focus();

        const length = searchInput.value.length;
        searchInput.setSelectionRange(length, length);

        sessionStorage.removeItem('customersSearchFocus');
    }

    const optionsForm = document.getElementById("customerOptionsForm");
    const customerPageSize = document.getElementById("customerPageSize");
    const panelsPerCustomer = document.getElementById("panelsPerCustomer");

    let debounceTimer;

    if (searchForm && searchInput) {
        searchInput.addEventListener("input", () => {
            clearTimeout(debounceTimer);

            debounceTimer = setTimeout(() => {
                sessionStorage.setItem('customersSearchFocus', 'true');
                searchForm.submit();
            }, 750);
        });
    }

    if (optionsForm && customerPageSize) {
        customerPageSize.addEventListener("change", () => optionsForm.submit());
    }

    if (optionsForm && panelsPerCustomer) {
        panelsPerCustomer.addEventListener("change", () => optionsForm.submit());
    }
})();