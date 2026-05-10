using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class EditPanelMaterialAdminViewModel
    {
        public int PanelMaterialID { get; set; }
        public int PanelID { get; set; }

        public string PanelCode { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        [Range(0.01, 999999)]
        public decimal Quantity { get; set; }
        [Range(0.01, 99999999)]
        public decimal UnitPrice { get; set; }
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        public bool IsManualPrice { get; set; }
        public string? ManualPriceReason { get; set; }
    }
}