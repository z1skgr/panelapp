namespace panelapp.ViewModels
{
    public class CabinetListRow
    {
        public int CabinetID { get; set; }

        public string CabinetCode { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public decimal CurrentPrice { get; set; }

        public bool Active { get; set; }

        public int SupplierID { get; set; }

        public string SupplierName { get; set; } = string.Empty;
    }
}