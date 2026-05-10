using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class EditPanelCabinetViewModel
    {
        public int PanelCabinetID { get; set; }

        public int PanelID { get; set; }

        [Range(0.01, 999999)]
        public decimal Quantity { get; set; }

        [Range(0, 9999999)]
        public decimal UnitPrice { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        public bool IsManualPrice { get; set; }

        [StringLength(500)]
        public string? ManualPriceReason { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}