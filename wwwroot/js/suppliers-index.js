(() => {
    const searchForm = document.getElementById('suppliersSearchForm');
    const searchInput = document.getElementById('searchTerm');
    const statusFilter = document.getElementById('statusFilter');

    const pageSizeForm = document.getElementById('suppliersPageSizeForm');
    const pageSizeSelect = document.getElementById('pageSizeSelect');

    let debounceTimer;

    const shouldRefocus = sessionStorage.getItem('suppliersSearchFocus') === 'true';

    if (shouldRefocus && searchInput) {
        searchInput.focus();

        const length = searchInput.value.length;
        searchInput.setSelectionRange(length, length);

        sessionStorage.removeItem('suppliersSearchFocus');
    }

    if (searchForm && searchInput) {
        searchInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);

            debounceTimer = setTimeout(() => {
                sessionStorage.setItem('suppliersSearchFocus', 'true');
                searchForm.submit();
            }, 750);
        });
    }

    if (searchForm && statusFilter) {
        statusFilter.addEventListener('change', () => {
            searchForm.submit();
        });
    }

    if (pageSizeForm && pageSizeSelect) {
        pageSizeSelect.addEventListener('change', () => {
            pageSizeForm.submit();
        });
    }
})();