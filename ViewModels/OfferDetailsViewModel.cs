using panelapp.Models;

namespace panelapp.ViewModels
{
    public class OfferDetailsViewModel
    {
        public Offer Offer { get; set; } = new();

        public AddMaterialToOfferViewModel AddMaterialForm { get; set; } = new();

        public OfferPricingViewModel PricingForm { get; set; } = new();

        public decimal MaterialsNetTotal =>
            Offer.OfferMaterials?.Sum(x => x.LineNetTotal) ?? 0;

        public decimal CabinetsNetTotal =>
            Offer.OfferCabinets?.Sum(x => x.LineNetTotal) ?? 0;

        public decimal ExtraItemsNetTotal =>
            Offer.OfferExtraItems?.Sum(x => x.LineNetTotal) ?? 0;

        public decimal FinalOfferTotal =>
            MaterialsNetTotal
            + CabinetsNetTotal
            + ExtraItemsNetTotal
            + Offer.LaborCost
            + Offer.ProfitAmount;

        public AddCabinetToOfferViewModel AddCabinetForm { get; set; } = new();

        public AddOfferExtraItemViewModel AddExtraItemForm { get; set; } = new();
    }
}