namespace panelapp.ViewModels
{
    public class PanelExportRow
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal CatalogTotal => Quantity * UnitPrice;

        public decimal NetTotal => CatalogTotal * (1 - DiscountPercent / 100m);

        public decimal DiscountValue => CatalogTotal - NetTotal;
    }
}