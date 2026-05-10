using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class EditPanelExtraItemViewModel
    {
        public int PanelExtraItemID { get; set; }

        public int PanelID { get; set; }

        [StringLength(100)]
        public string? ItemCode { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Unit { get; set; } = "pcs";

        [Range(0.01, 999999)]
        public decimal Quantity { get; set; }

        [Range(0, 9999999)]
        public decimal UnitPrice { get; set; }

        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}