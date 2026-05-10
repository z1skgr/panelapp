namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiDraftViewModel
    {
        public string? CustomerName { get; set; }
        public string? Description { get; set; }

        public List<OfferAiCatalogLineDraftViewModel> Materials { get; set; } = new();
        public List<OfferAiCatalogLineDraftViewModel> Cabinets { get; set; } = new();
        public List<OfferAiExtraItemDraftViewModel> ExtraItems { get; set; } = new();

        public decimal LaborCost { get; set; }
        public decimal ProfitAmount { get; set; }
    }
}