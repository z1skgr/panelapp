namespace panelapp.ViewModels
{
    public class SupplierDiscountsModalViewModel
    {
        public string SourceType { get; set; } = "";
        public int SourceId { get; set; }

        public List<SupplierDiscountInfoViewModel> Suppliers { get; set; } = new();
    }
}
