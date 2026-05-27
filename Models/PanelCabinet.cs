using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace panelapp.Models
{
    public class PanelCabinet
    {
        public int PanelCabinetID { get; set; }

        public int PanelID { get; set; }

        public int CabinetID { get; set; }

        public int? SupplierID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        public bool IsManualPrice { get; set; }

        [StringLength(500)]
        public string? ManualPriceReason { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        public Panel? Panel { get; set; }

        public Cabinet? Cabinet { get; set; }

        public Supplier? Supplier { get; set; }

        [NotMapped]
        public decimal DiscountedUnitPrice =>
            UnitPrice * (1 - DiscountPercent / 100m);

        [NotMapped]
        public decimal OriginalTotalPrice =>
            Quantity * UnitPrice;

        [NotMapped]
        public decimal DiscountAmount =>
            OriginalTotalPrice - LineNetTotal;

        [NotMapped]
        public decimal LineNetTotal =>
            Quantity * DiscountedUnitPrice;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}