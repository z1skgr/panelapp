using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class SupplierContactPersonInputViewModel
    {
        [Required(ErrorMessage = "Το όνομα υπευθύνου είναι υποχρεωτικό.")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^\d*$", ErrorMessage = "Το τηλέφωνο πρέπει να περιέχει μόνο αριθμούς.")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
        [StringLength(200)]
        public string? Email { get; set; }
    }
}