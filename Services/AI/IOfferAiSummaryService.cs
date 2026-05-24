namespace panelapp.Services.AI
{
    public interface IOfferAiSummaryService
    {
        Task<string> GenerateSummaryAsync(int offerId, CancellationToken cancellationToken = default);
    }
}