using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class Panel
    {
        public int PanelID { get; set; }
        public string PanelCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int? CustomerID { get; set; }
        public Customer? Customer { get; set; }

        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public List<PanelMaterial> PanelMaterials { get; set; } = new();

        public List<PanelCabinet> PanelCabinets { get; set; } = new();

        public List<PanelExtraItem> PanelExtraItems { get; set; } = new();

        public decimal LaborCost { get; set; }

        public decimal ProfitAmount { get; set; }

        public int? SourceOfferID { get; set; }

        public Offer? SourceOffer { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedDate { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}