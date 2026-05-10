namespace panelapp.ViewModels
{
    public class MaterialSupplierGroupViewModel
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;

        public List<MaterialListRow> Materials { get; set; } = new();

        public int CurrentMaterialPage { get; set; }
        public int TotalMaterialPages { get; set; }
        public int TotalMaterialCount { get; set; }

        public bool SupplierActive { get; set; }

        public List<CabinetListRow> Cabinets { get; set; } = new();

        public int TotalCabinetCount { get; set; }

        public int CurrentCabinetPage { get; set; }

        public int TotalCabinetPages { get; set; }

        public int TotalCatalogCount => TotalMaterialCount + TotalCabinetCount;
    }
}