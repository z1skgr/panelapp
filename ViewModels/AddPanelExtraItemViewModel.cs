using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class AddPanelExtraItemViewModel
    {
        public int PanelID { get; set; }

        [StringLength(100)]
        public string? ItemCode { get; set; }

        [Required(ErrorMessage = "Η περιγραφή είναι υποχρεωτική.")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Unit { get; set; } = "pcs";

        [Range(0.01, 999999, ErrorMessage = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, 9999999, ErrorMessage = "Η τιμή δεν μπορεί να είναι αρνητική.")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DiscountPercent { get; set; } = 0;
    }
}