using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class PanelMaterial
    {
        public int PanelMaterialID { get; set; }
        public int PanelID { get; set; }
        public int MaterialID { get; set; }
        public int? SupplierID { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsManualPrice { get; set; }
        public string? ManualPriceReason { get; set; }
        public int? AddedByUserID { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime LastModifiedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}