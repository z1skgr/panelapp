namespace panelapp.ViewModels
{
    public class HomeDashboardViewModel
    {
        public int TotalPanels { get; set; }
        public int UnderConstructionPanels { get; set; }
        public int CompletedPanels { get; set; }
        public int CancelledPanels { get; set; }

        public int TotalMaterials { get; set; }
        public int ActiveMaterials { get; set; }

        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public int TotalSuppliers { get; set; }

        public int TotalCustomers { get; set; }

        public decimal CompletionRate { get; set; }
        public decimal UnderConstructionRate { get; set; }
        public string TopCustomerName { get; set; } = "-";
        public int TopCustomerPanelCount { get; set; }
        public int PanelsThisMonth { get; set; }
        public DateTime? LastPanelUpdate { get; set; }

        public List<RecentPanelRow> RecentPanels { get; set; } = new();
        public List<ActivityFeedRow> ActivityFeed { get; set; } = new();

        public string ChartDataJson { get; set; } = "[]";


        public int TotalOffers { get; set; }
        public int DraftOffers { get; set; }
        public int SentOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int ConvertedOffers { get; set; }
        public int RejectedOffers { get; set; }

        public int OffersThisMonth { get; set; }

        public decimal EstimatedAcceptedOfferValue { get; set; }
        public decimal EstimatedConvertedOfferValue { get; set; }
        public decimal EstimatedOfferProfit { get; set; }

        public int TotalCabinets { get; set; }
        public int ActiveCabinets { get; set; }

        public decimal OfferAcceptanceRate { get; set; }

        public decimal OfferConversionRate { get; set; }
    }

    public class RecentPanelRow
    {
        public int PanelID { get; set; }
        public string PanelCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastModifiedDate { get; set; }
    }

    public class ActivityFeedRow
    {
        public int? PanelID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "bi-clock-history";
        public string BadgeClass { get; set; } = "bg-secondary";
        public DateTime CreatedAt { get; set; }

        public string? EntityType { get; set; }
        public int? EntityID { get; set; }
    }
}