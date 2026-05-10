namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiExtraItemDraftViewModel
    {
        public string? ItemCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "pcs";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
    }
}