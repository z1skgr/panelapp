using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class EditOfferMaterialViewModel
    {
        public int OfferMaterialID { get; set; }

        public int OfferID { get; set; }

        [Range(0.01, 999999, ErrorMessage = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.")]
        public decimal Quantity { get; set; }

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DiscountPercent { get; set; }

        [Range(0, 9999999, ErrorMessage = "Η τιμή δεν μπορεί να είναι αρνητική.")]
        public decimal UnitPrice { get; set; }

        public bool IsManualPrice { get; set; }

        [StringLength(500)]
        public string? ManualPriceReason { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}