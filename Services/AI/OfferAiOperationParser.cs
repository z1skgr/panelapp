using panelapp.Models.AI;
using System.Globalization;
using System.Text.RegularExpressions;

namespace panelapp.Services.AI
{
    public class OfferAiOperationParser : IOfferAiOperationParser
    {
        public Task<OfferAiOperation> ParseAsync(
            string message,
            CancellationToken cancellationToken = default)
        {
            var text = message.Trim();

            var operation = new OfferAiOperation
            {
                Notes = text
            };

            var lower = text.ToLowerInvariant();

            if (lower.Contains("ποσότητα") || lower.Contains("τεμάχια") || lower.Contains("τεμ"))
            {
                operation.OperationType = "update_quantity";
                operation.Quantity = ExtractQuantity(text);
                operation.TargetItem = ExtractTarget(text);
            }
            else if (lower.Contains("τιμή"))
            {
                operation.OperationType = "update_price";
                operation.UnitPrice = ExtractFirstDecimal(text);
                operation.TargetItem = ExtractTarget(text);
            }
            else if (lower.Contains("έκπτωση") || lower.Contains("εκπτωση"))
            {
                operation.OperationType = "update_discount";
                operation.DiscountPercent = ExtractFirstDecimal(text);
                operation.TargetItem = ExtractTarget(text);
            }
            else if (lower.Contains("βγάλε") || lower.Contains("αφαίρεσε") || lower.Contains("διαγραφή"))
            {
                operation.OperationType = "remove_item";
                operation.TargetItem = ExtractTarget(text);
            }
            else if (lower.Contains("εργατικά") || lower.Contains("εργατικα"))
            {
                operation.OperationType = "update_labor";
                operation.LaborCost = ExtractFirstDecimal(text);
            }
            else if (lower.Contains("κέρδος") || lower.Contains("κερδος"))
            {
                operation.OperationType = "update_profit";
                operation.ProfitAmount = ExtractFirstDecimal(text);
            }
            else
            {
                operation.OperationType = "unknown";
            }

            return Task.FromResult(operation);
        }

        private static decimal? ExtractFirstDecimal(string text)
        {
            var match = Regex.Match(text, @"\d+([,.]\d+)?");

            if (!match.Success)
                return null;

            var value = match.Value.Replace(",", ".");

            if (decimal.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var result))
            {
                return result;
            }

            return null;
        }

        private static string? ExtractTarget(string text)
        {
            var lower = text.ToLowerInvariant();

            var keywords = new[]
            {
                "ποσότητα",
                "τεμάχια",
                "τεμ",
                "τιμή",
                "έκπτωση",
                "εκπτωση",
                "βγάλε",
                "αφαίρεσε",
                "διαγραφή",
                "σε",
                "στο"
            };

            var cleaned = text;

            foreach (var keyword in keywords)
            {
                cleaned = Regex.Replace(
                    cleaned,
                    keyword,
                    "",
                    RegexOptions.IgnoreCase);
            }

            cleaned = cleaned
                .Replace("άλλαξε", "", StringComparison.OrdinalIgnoreCase)
                .Replace("ποσότητα", "", StringComparison.OrdinalIgnoreCase)
                .Replace("τεμ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("τεμάχια", "", StringComparison.OrdinalIgnoreCase)
                .Replace("σε", "", StringComparison.OrdinalIgnoreCase)
                .Replace("%", "")
                .Trim();

            cleaned = Regex.Replace(
                cleaned,
                @"\s+\d+([,.]\d+)?\s*(τεμ|τεμάχια)?$",
                "",
                RegexOptions.IgnoreCase);

            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

            return string.IsNullOrWhiteSpace(cleaned)
                ? null
                : cleaned;
        }


        private static decimal? ExtractQuantity(string text)
        {
            var patterns = new[]
            {
        @"σε\s+(\d+([,.]\d+)?)",
        @"(\d+([,.]\d+)?)\s*τεμ",
        @"(\d+([,.]\d+)?)\s*τεμάχ"
    };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(
                    text,
                    pattern,
                    RegexOptions.IgnoreCase);

                if (!match.Success)
                    continue;

                var value = match.Groups[1].Value.Replace(",", ".");

                if (decimal.TryParse(
                        value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}