namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiResolvedCatalogLineViewModel
    {
        public string? SupplierName { get; set; }
        public string CodeOrDescription { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal DiscountPercent { get; set; }

        public int? ResolvedItemID { get; set; }
        public int? ResolvedSupplierID { get; set; }

        public string? ResolvedCode { get; set; }
        public string? ResolvedDescription { get; set; }
        public decimal? UnitPrice { get; set; }

        public bool IsResolved => ResolvedItemID.HasValue;

        public string? Message { get; set; }
    }
}