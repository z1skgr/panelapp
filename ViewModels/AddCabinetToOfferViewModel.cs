using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class AddCabinetToOfferViewModel
    {
        public int OfferID { get; set; }

        public int? SupplierID { get; set; }

        [Required(ErrorMessage = "Πρέπει να επιλέξεις ερμάριο.")]
        public int CabinetID { get; set; }

        [Range(0.01, 999999, ErrorMessage = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DiscountPercent { get; set; } = 0;

        public List<SelectListItem> Suppliers { get; set; } = new();
    }
}