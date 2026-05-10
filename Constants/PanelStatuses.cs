namespace panelapp.Constants
{
    public static class PanelStatuses
    {
        public const string UnderConstruction = "Under Construction";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] Allowed =
        {
            UnderConstruction,
            Completed,
            Cancelled
        };

        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return UnderConstruction;
            }

            var normalized = value.Trim();

            if (string.Equals(normalized, UnderConstruction, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "UnderConstruction", StringComparison.OrdinalIgnoreCase))
            {
                return UnderConstruction;
            }

            if (string.Equals(normalized, Completed, StringComparison.OrdinalIgnoreCase))
            {
                return Completed;
            }

            if (string.Equals(normalized, Cancelled, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "Canceled", StringComparison.OrdinalIgnoreCase))
            {
                return Cancelled;
            }

            return UnderConstruction;
        }

        public static bool IsAllowed(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = Normalize(value);
            return Allowed.Contains(normalized);
        }

        public static string ToDisplayLabel(string? value)
        {
            var normalized = Normalize(value);

            return normalized switch
            {
                UnderConstruction => "Υπό Κατασκευή",
                Completed => "Ολοκληρωμένος",
                Cancelled => "Ακυρωμένος",
                _ => normalized
            };
        }
    }
}