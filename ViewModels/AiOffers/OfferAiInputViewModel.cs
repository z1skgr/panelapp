using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiInputViewModel
    {
        [Required(ErrorMessage = "Η περιγραφή είναι υποχρεωτική.")]
        [StringLength(8000)]
        public string Prompt { get; set; } = string.Empty;
    }
}