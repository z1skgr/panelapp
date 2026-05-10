namespace panelapp.ViewModels
{
    public class MaterialIndexViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int? SupplierFilterId { get; set; }

        public int SupplierPage { get; set; }
        public int TotalSupplierPages { get; set; }
        public int TotalSupplierCount { get; set; }

        public int SuppliersPerPage { get; set; }
        public int MaterialsPerSupplier { get; set; }

        public List<panelapp.Models.Supplier> SupplierOptions { get; set; } = new();
        public List<MaterialSupplierGroupViewModel> Groups { get; set; } = new();

        public string SupplierFilter { get; set; } = "all";

        public int? ExpandedSupplierId { get; set; }

        public int CabinetsPerSupplier { get; set; } = 100;
    }
}