namespace panelapp.Services.AI.Chat.Helpers
{
    public static class AiMaterialSearchHelper
    {
        public static string NormalizeMaterialText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("_", "")
                .Trim()
                .ToUpperInvariant();
        }

        public static string ExtractMaterialSearchTerm(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            var text = message.Trim();

            var removeWords = new[]
            {
                "βρες υλικό", "βρες υλικο",
                "ψάξε υλικό", "ψαξε υλικο",
                "αναζήτηση υλικού", "αναζητηση υλικου",
                "αναζήτησε υλικό", "αναζητησε υλικο",
                "material search", "material",
                "υλικό", "υλικο"
            };

            foreach (var word in removeWords)
            {
                text = text.Replace(word, "", StringComparison.OrdinalIgnoreCase);
            }

            return text.Trim();
        }
    }
}