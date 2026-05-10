
namespace panelapp.ViewModels.AiOffers
{
    public class OfferAiPreviewViewModel
    {
        public string OriginalPrompt { get; set; } = string.Empty;

        public OfferAiDraftViewModel Draft { get; set; } = new();

        public int? ResolvedCustomerID { get; set; }
        public string? ResolvedCustomerName { get; set; }

        public List<OfferAiResolvedCatalogLineViewModel> Materials { get; set; } = new();
        public List<OfferAiResolvedCatalogLineViewModel> Cabinets { get; set; } = new();
        public List<OfferAiExtraItemDraftViewModel> ExtraItems { get; set; } = new();
        public string SerializedDraft { get; set; } = string.Empty;
        public bool CanCreate =>
            ResolvedCustomerID.HasValue &&
            Materials.All(x => x.IsResolved) &&
            Cabinets.All(x => x.IsResolved);
    }
}