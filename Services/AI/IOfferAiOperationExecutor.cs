using panelapp.Models.AI;

namespace panelapp.Services.AI
{
    public interface IOfferAiOperationExecutor
    {
        Task<string> ExecuteAsync(
            int offerId,
            OfferAiOperation operation,
            CancellationToken cancellationToken = default);
    }
}