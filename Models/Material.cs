using panelapp.Constants;
using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class Material
    {
        public int MaterialID { get; set; }

        [Required]
        [StringLength(100)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Η μονάδα είναι υποχρεωτική.")]
        [RegularExpression("^(pcs|meters)$", ErrorMessage = "Η μονάδα πρέπει να είναι pcs ή meters.")]
        public string Unit { get; set; } = MaterialUnits.Pcs;

        [Range(0, double.MaxValue)]
        public decimal CurrentPrice { get; set; }

        public DateTime PriceUpdatedDate { get; set; }
        public int SupplierID { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public Supplier? Supplier { get; set; }
    }
}