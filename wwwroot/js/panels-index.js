(() => {
    const searchForm = document.getElementById('panelsSearchForm');
    const searchInput = document.getElementById('searchTerm');

    const pageSizeSelect = document.getElementById('pageSizeSelect');
    const pageSizeForm = document.getElementById('pageSizeForm');

    const shouldRefocus = sessionStorage.getItem('panelsSearchFocus') === 'true';

    if (shouldRefocus && searchInput) {
        searchInput.focus();

        const length = searchInput.value.length;
        searchInput.setSelectionRange(length, length);

        sessionStorage.removeItem('panelsSearchFocus');
    }

    let debounceTimer;

    if (searchForm && searchInput) {
        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);

            debounceTimer = setTimeout(() => {
                sessionStorage.setItem('panelsSearchFocus', 'true');
                searchForm.submit();
            }, 750);
        });
    }

    if (pageSizeSelect && pageSizeForm) {
        pageSizeSelect.addEventListener('change', () => {
            pageSizeForm.submit();
        });
    }
})();