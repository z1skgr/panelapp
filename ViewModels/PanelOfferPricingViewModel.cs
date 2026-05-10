using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class PanelOfferPricingViewModel
    {
        public int PanelID { get; set; }

        public string PanelCode { get; set; } = string.Empty;

        [Range(0, 9999999, ErrorMessage = "Τα εργατικά δεν μπορούν να είναι αρνητικά.")]
        [Display(Name = "Εργατικά")]
        public decimal LaborCost { get; set; }

        [Range(0, 9999999, ErrorMessage = "Το κέρδος δεν μπορεί να είναι αρνητικό.")]
        [Display(Name = "Κέρδος εταιρείας")]
        public decimal ProfitAmount { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}