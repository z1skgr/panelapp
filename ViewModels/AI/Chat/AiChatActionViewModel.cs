namespace panelapp.ViewModels.AI.Chat
{
    public class AiChatActionViewModel
    {
        public string Label { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string? Payload { get; set; }
    }
}