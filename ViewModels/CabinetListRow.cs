namespace panelapp.ViewModels
{
    public class MaterialListRow
    {
        public int MaterialID { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public bool Active { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }
}