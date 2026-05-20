using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class Supplier
    {
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Το όνομα προμηθευτή είναι υποχρεωτικό.")]
        [StringLength(100)]
        public string SupplierName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Μη έγκυρο email.")]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModifiedDate { get; set; } = DateTime.Now;


        public ICollection<Material> Materials { get; set; } = new List<Material>();

        public ICollection<SupplierContactPerson> ContactPersons { get; set; } = new List<SupplierContactPerson>();

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DefaultDiscountPercent { get; set; } = 0;

    }
}