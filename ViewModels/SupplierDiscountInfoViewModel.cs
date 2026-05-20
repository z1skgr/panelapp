namespace panelapp.ViewModels
{
    public class SupplierDiscountInfoViewModel
    {
        public int SupplierID { get; set; }

        public string SupplierName { get; set; } = string.Empty;

        public decimal DefaultDiscountPercent { get; set; }
    }
}