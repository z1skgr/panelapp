namespace panelapp.ViewModels.AI.Chat
{
    public class AiChatRequestViewModel
    {
        public string Message { get; set; } = string.Empty;

        public string? ConversationId { get; set; }

        public string? ContextType { get; set; }

        public int? ContextEntityId { get; set; }
    }
}