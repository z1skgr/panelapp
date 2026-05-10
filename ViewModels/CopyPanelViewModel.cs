using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class CopyPanelViewModel
    {
        public int SourcePanelID { get; set; }

        public string SourcePanelCode { get; set; } = string.Empty;

        public string? SourceCustomerName { get; set; }

        public string SuggestedPanelCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ο πελάτης είναι υποχρεωτικός.")]
        [Display(Name = "Πελάτης")]
        public int? CustomerID { get; set; }

        [Display(Name = "Περιγραφή")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Αντιγραφή Υλικών")]
        public bool CopyMaterials { get; set; } = true;

        [Display(Name = "Αντιγραφή Εκπτώσεων")]
        public bool CopyDiscounts { get; set; } = true;

        [Display(Name = "Αντιγραφή Χειροκίνητων Τιμών")]
        public bool CopyManualPrices { get; set; } = true;

        public List<SelectListItem> Customers { get; set; } = new();
    }
}