namespace panelapp.Services
{
    public class MaterialImportResult
    {
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }

        public List<string> Messages { get; set; } = new();

        public bool Success { get; set; } = true;
    }
}