using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace panelapp.Models
{
    public class OfferMaterial
    {
        public int OfferMaterialID { get; set; }

        public int OfferID { get; set; }

        public int MaterialID { get; set; }

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

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public Offer? Offer { get; set; }

        public Material? Material { get; set; }

        public Supplier? Supplier { get; set; }

        [NotMapped]
        public decimal OriginalTotalPrice =>
            Quantity * UnitPrice;

        [NotMapped]
        public decimal DiscountAmount =>
            Quantity * UnitPrice * (DiscountPercent / 100);

        [NotMapped]
        public decimal LineNetTotal =>
            (Quantity * UnitPrice) - DiscountAmount;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}