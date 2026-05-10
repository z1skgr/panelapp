namespace panelapp.Services.Results
{
    public class PanelCopyResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? NewPanelID { get; set; }

        public string NewPanelCode { get; set; } = string.Empty;
    }
}