namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiCatalogLineDraftViewModel
    {
        public string? SupplierName { get; set; }
        public string CodeOrDescription { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
    }
}