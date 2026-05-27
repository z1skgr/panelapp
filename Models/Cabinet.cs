using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace panelapp.Models
{
    public class Cabinet
    {
        public int CabinetID { get; set; }

        [Required]
        [StringLength(100)]
        public string CabinetCode { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Unit { get; set; } = "pcs";

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPrice { get; set; }

        public int SupplierID { get; set; }

        public Supplier? Supplier { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}