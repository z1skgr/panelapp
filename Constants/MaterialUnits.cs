namespace panelapp.Constants
{
    public static class MaterialUnits
    {
        public const string Pcs = "pcs";
        public const string Meters = "meters";

        public static readonly string[] Allowed = { Pcs, Meters };

        public static bool IsAllowed(string? value)
        {
            return NormalizeUnit(value) != null;
        }

        public static string? NormalizeUnit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value
                .Trim()
                .ToLowerInvariant()
                .Replace(".", "")
                .Replace(",", "")
                .Replace(" ", "");

            return normalized switch
            {
                // PCS
                "pcs" => Pcs,
                "pc" => Pcs,
                "piece" => Pcs,
                "pieces" => Pcs,

                "τεμ" => Pcs,
                "τεμαχιο" => Pcs,
                "τεμάχιο" => Pcs,
                "τεμαχια" => Pcs,
                "τεμάχια" => Pcs,

                "τμχ" => Pcs,
                "tmx" => Pcs,

                // METERS
                "m" => Meters,
                "mt" => Meters,
                "mtr" => Meters,

                "meter" => Meters,
                "meters" => Meters,
                "metre" => Meters,
                "metres" => Meters,

                "μετρο" => Meters,
                "μέτρο" => Meters,
                "μετρα" => Meters,
                "μέτρα" => Meters,

                "μ" => Meters,

                _ => null
            };
        }

        public static string ToDisplayName(string? unit)
        {
            return NormalizeUnit(unit) switch
            {
                Pcs => "Τεμάχια",
                Meters => "Μέτρα",
                _ => "-"
            };
        }
    }
}