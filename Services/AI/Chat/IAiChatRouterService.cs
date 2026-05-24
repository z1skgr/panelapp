using panelapp.ViewModels.AI.Chat;

namespace panelapp.Services.AI.Chat
{
    public interface IAiChatRouterService
    {
        Task<AiChatResponseViewModel> HandleAsync(
            AiChatRequestViewModel model,
            CancellationToken cancellationToken = default);
    }
}