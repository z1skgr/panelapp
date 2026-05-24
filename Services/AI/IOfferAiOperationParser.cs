using panelapp.Models.AI;

namespace panelapp.Services.AI
{
    public interface IOfferAiOperationParser
    {
        Task<OfferAiOperation> ParseAsync(
            string message,
            CancellationToken cancellationToken = default);
    }
}