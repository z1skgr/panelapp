using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class SupplierCreateViewModel
    {
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

        public List<SupplierContactPersonInputViewModel> ContactPersons { get; set; } = new();
    }
}