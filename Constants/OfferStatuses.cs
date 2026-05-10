namespace panelapp.Constants
{
    public static class OfferStatuses
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
        public const string Accepted = "Accepted";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
        public const string Converted = "Converted";

        public static readonly string[] All =
        {
            Draft,
            Sent,
            Accepted,
            Rejected,
            Cancelled,
            Converted
        };
    }
}
