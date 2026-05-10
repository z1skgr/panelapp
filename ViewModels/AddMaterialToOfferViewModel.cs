using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class AddMaterialToOfferViewModel
    {
        public int OfferID { get; set; }

        public string OfferCode { get; set; } = string.Empty;

        public int? SupplierID { get; set; }

        public string MaterialSearch { get; set; } = string.Empty;

        [Display(Name = "Υλικό")]
        public int MaterialID { get; set; }

        [Range(0.01, 999999, ErrorMessage = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DiscountPercent { get; set; } = 0;

        public List<SelectListItem> Suppliers { get; set; } = new();

        public List<SelectListItem> Materials { get; set; } = new();
    }
}