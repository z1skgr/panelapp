using panelapp.ViewModels;

namespace ZL_panelapp.ViewModels
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
    }
}