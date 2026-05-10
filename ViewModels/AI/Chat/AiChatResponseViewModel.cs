namespace panelapp.ViewModels.AI.Chat
{
    public class AiChatResponseViewModel
    {
        public string Message { get; set; } = string.Empty;

        public string ResponseType { get; set; } = "chat";

        public bool RequiresConfirmation { get; set; }

        public string? ActionUrl { get; set; }

        public string? ActionLabel { get; set; }

        public string? SerializedDraft { get; set; }
    }
}