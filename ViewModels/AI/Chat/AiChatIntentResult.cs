namespace panelapp.ViewModels.AI.Chat
{
    public class AiChatIntentResult
    {
        public string IntentType { get; set; } = "unknown";

        public string? OperationType { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}