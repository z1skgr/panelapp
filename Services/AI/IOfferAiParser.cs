using panelapp.ViewModels.AiOffers;

namespace panelapp.Services.AI
{
    public interface IOfferAiParser
    {
        Task<OfferAiDraftViewModel> ParseAsync(string prompt, CancellationToken cancellationToken = default);
    }
}