using panelapp.Constants;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace panelapp.Models
{
    public class Offer
    {
        public int OfferID { get; set; }

        [Required]
        [StringLength(50)]
        public string OfferCode { get; set; } = string.Empty;

        public int? CustomerID { get; set; }

        [Required]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = OfferStatuses.Draft;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LaborCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProfitAmount { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public DateTime? SentDate { get; set; }

        public DateTime? AcceptedDate { get; set; }

        public DateTime? ConvertedDate { get; set; }

        public int? PanelID { get; set; }

        public Customer? Customer { get; set; }

        public Panel? Panel { get; set; }

        public ICollection<OfferMaterial> OfferMaterials { get; set; } = new List<OfferMaterial>();

        public ICollection<OfferCabinet> OfferCabinets { get; set; } = new List<OfferCabinet>();
        public ICollection<OfferExtraItem> OfferExtraItems { get; set; } = new List<OfferExtraItem>();

        [NotMapped]
        public decimal MaterialsNetTotal =>
            OfferMaterials?.Sum(x => x.LineNetTotal) ?? 0;

        [NotMapped]
        public decimal CabinetsNetTotal =>
            OfferCabinets?.Sum(x => x.LineNetTotal) ?? 0;

        [NotMapped]
        public decimal ExtraItemsNetTotal =>
            OfferExtraItems?.Sum(x => x.LineNetTotal) ?? 0;

        [NotMapped]
        public decimal FinalOfferTotal =>
            MaterialsNetTotal + CabinetsNetTotal + ExtraItemsNetTotal + LaborCost + ProfitAmount;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }

    }
}