using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class OfferEditViewModel
    {
        public int OfferID { get; set; }

        public string OfferCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο πελάτης είναι υποχρεωτικός.")]
        public int? CustomerID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public List<SelectListItem> Customers { get; set; } = new();
    }
}