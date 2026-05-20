using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class SupplierEditViewModel
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

        public List<SupplierContactPersonEditItem> ExistingContactPersons { get; set; } = new();

        public List<SupplierContactPersonInputViewModel> NewContactPersons { get; set; } = new();

        [Range(0, 100, ErrorMessage = "Η έκπτωση πρέπει να είναι από 0 έως 100.")]
        public decimal DefaultDiscountPercent { get; set; }
    }

    public class SupplierContactPersonEditItem
    {
        public int SupplierContactPersonID { get; set; }

        [Required(ErrorMessage = "Το όνομα υπευθύνου είναι υποχρεωτικό.")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^\d*$", ErrorMessage = "Το τηλέφωνο πρέπει να περιέχει μόνο αριθμούς.")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
        [StringLength(200)]
        public string? Email { get; set; }

        public bool Active { get; set; } = true;
    }
}