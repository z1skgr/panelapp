using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class SupplierContactPerson
    {
        public int SupplierContactPersonID { get; set; }

        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Το όνομα υπευθύνου είναι υποχρεωτικό.")]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^\d*$", ErrorMessage = "Το τηλέφωνο πρέπει να περιέχει μόνο αριθμούς.")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
        [StringLength(200)]
        public string? Email { get; set; }

        public bool Active { get; set; } = true;

        public Supplier? Supplier { get; set; }


    }
}