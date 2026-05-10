namespace panelapp.Helpers
{
    public static class OfferStatusBadgeHelper
    {
        public static string GetBadgeClass(string status)
        {
            return status switch
            {
                "Draft" => "bg-secondary",
                "Sent" => "bg-primary",
                "Accepted" => "bg-success",
                "Rejected" => "bg-danger",
                "Cancelled" => "bg-dark",
                "Converted" => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }

        public static string GetGreekLabel(string status)
        {
            return status switch
            {
                "Draft" => "Πρόχειρη",
                "Sent" => "Στάλθηκε",
                "Accepted" => "Αποδεκτή",
                "Rejected" => "Απορρίφθηκε",
                "Cancelled" => "Ακυρώθηκε",
                "Converted" => "Μετατράπηκε",
                _ => status
            };
        }
    }
}