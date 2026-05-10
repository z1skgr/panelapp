(() => {
    document.addEventListener("DOMContentLoaded", () => {
        if (typeof TomSelect === "undefined") return;

        document.querySelectorAll("select.searchable-select").forEach(select => {
            if (select.tomselect) return;

            new TomSelect(select, {
                create: false,
                allowEmptyOption: true,
                maxOptions: parseInt(select.dataset.maxOptions || "40"),
                dropdownParent: "body",
                placeholder: select.dataset.placeholder || "Αναζήτηση...",
                sortField: {
                    field: "text",
                    direction: "asc"
                },
                render: {
                    no_results: () => '<div class="no-results px-2 py-1">Δεν βρέθηκαν αποτελέσματα</div>'
                }
            });
        });
    });
})();