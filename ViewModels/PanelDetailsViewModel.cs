using panelapp.Models;

namespace panelapp.ViewModels
{
    public class PanelDetailsViewModel
    {
        public Panel Panel { get; set; } = new Panel();
        public List<PanelMaterialRowViewModel> Materials { get; set; } = new();
        public AddMaterialToPanelViewModel AddMaterialForm { get; set; } = new();
        public decimal GrandTotal => Materials.Sum(x => x.TotalPrice);
        public decimal TotalWithoutDiscount => Materials.Sum(x => x.OriginalTotalPrice);
        public decimal TotalDiscount => Materials.Sum(x => x.DiscountAmount);
    }

    public class PanelMaterialRowViewModel
    {
        public int PanelMaterialID { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }


        public decimal DiscountedUnitPrice => UnitPrice * (1 - DiscountPercent / 100m);
        public decimal TotalPrice => Quantity * DiscountedUnitPrice;
        public decimal OriginalTotalPrice => Quantity * UnitPrice;
        public decimal DiscountAmount => OriginalTotalPrice - TotalPrice;
        public decimal NetValue => OriginalTotalPrice - DiscountAmount;


        public bool IsManualPrice { get; set; }
        public string? ManualPriceReason { get; set; }
    }
}