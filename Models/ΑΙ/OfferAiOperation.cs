namespace panelapp.Models.AI
{
    public class OfferAiOperation
    {
        public string OperationType { get; set; } = string.Empty;

        public string? TargetItem { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? LaborCost { get; set; }

        public decimal? ProfitAmount { get; set; }

        public string? Notes { get; set; }

        public string? SupplierName { get; set; }
    }
}