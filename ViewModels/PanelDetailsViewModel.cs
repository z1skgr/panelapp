using panelapp.Models;
using System.ComponentModel.DataAnnotations;

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

        public PanelOfferSummaryViewModel OfferSummary { get; set; } = new();
        public PanelOfferPricingViewModel OfferPricingForm { get; set; } = new();

        public AddCabinetToPanelViewModel AddCabinetForm { get; set; } = new();

        public AddPanelExtraItemViewModel AddExtraItemForm { get; set; } = new();

        public List<PanelCabinetRowViewModel> Cabinets { get; set; } = new();

        public List<PanelExtraItemRowViewModel> ExtraItems { get; set; } = new();

        public decimal CabinetsGrandTotal => Cabinets.Sum(x => x.TotalPrice);

        public decimal ExtraItemsGrandTotal => ExtraItems.Sum(x => x.TotalPrice);

        public List<SupplierDiscountInfoViewModel> SupplierDiscounts { get; set; } = new();



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

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }



    public class PanelCabinetRowViewModel
    {
        public int PanelCabinetID { get; set; }

        public string CabinetCode { get; set; } = string.Empty;

        public string CabinetDescription { get; set; } = string.Empty;

        public string SupplierName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public bool IsManualPrice { get; set; }

        public string? ManualPriceReason { get; set; }

        public decimal DiscountedUnitPrice => UnitPrice * (1 - DiscountPercent / 100m);

        public decimal TotalPrice => Quantity * DiscountedUnitPrice;

        public decimal OriginalTotalPrice => Quantity * UnitPrice;

        public decimal DiscountAmount => OriginalTotalPrice - TotalPrice;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    }


    public class PanelExtraItemRowViewModel
    {
        public int PanelExtraItemID { get; set; }

        public string? ItemCode { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Unit { get; set; } = "pcs";

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountedUnitPrice => UnitPrice * (1 - DiscountPercent / 100m);

        public decimal TotalPrice => Quantity * DiscountedUnitPrice;

        public decimal OriginalTotalPrice => Quantity * UnitPrice;

        public decimal DiscountAmount => OriginalTotalPrice - TotalPrice;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}