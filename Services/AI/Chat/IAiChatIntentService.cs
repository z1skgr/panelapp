using panelapp.ViewModels.AI.Chat;

namespace panelapp.Services.AI.Chat
{
    public interface IAiChatIntentService
    {
        AiChatIntentResult DetectIntent(string message);
    }
}