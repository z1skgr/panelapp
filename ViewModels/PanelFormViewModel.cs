using Microsoft.AspNetCore.Mvc.Rendering;
using panelapp.Constants;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class PanelFormViewModel
    {
        public int? PanelID { get; set; }

        [Required(ErrorMessage = "Ο κωδικός πίνακα είναι υποχρεωτικός.")]
        [Display(Name = "Κωδικός Πίνακα")]
        public string PanelCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Η επιλογή πελάτη είναι υποχρεωτική.")]
        public int? CustomerID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Η κατάσταση είναι υποχρεωτική.")]
        [Display(Name = "Κατάσταση")]
        public string Status { get; set; } = PanelStatuses.UnderConstruction;

        public List<SelectListItem> Customers { get; set; } = new();
    }
}

