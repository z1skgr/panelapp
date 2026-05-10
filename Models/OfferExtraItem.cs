using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace panelapp.Models
{
    public class OfferExtraItem
    {
        public int OfferExtraItemID { get; set; }

        public int OfferID { get; set; }

        [StringLength(100)]
        public string? ItemCode { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = "pcs";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public Offer? Offer { get; set; }

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