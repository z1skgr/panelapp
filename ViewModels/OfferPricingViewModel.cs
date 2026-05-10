using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class OfferPricingViewModel
    {
        public int OfferID { get; set; }

        public string OfferCode { get; set; } = string.Empty;

        [Range(0, 9999999, ErrorMessage = "Τα εργατικά δεν μπορούν να είναι αρνητικά.")]
        public decimal LaborCost { get; set; }

        [Range(0, 9999999, ErrorMessage = "Το κέρδος δεν μπορεί να είναι αρνητικό.")]
        public decimal ProfitAmount { get; set; }

        public string RowVersion { get; set; } = string.Empty;
    }
}