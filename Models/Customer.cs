using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class Customer
    {
        public int CustomerID { get; set; }

        [Required(ErrorMessage = "Το όνομα πελάτη είναι υποχρεωτικό.")]
        [StringLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Το ΑΦΜ είναι υποχρεωτικό.")]
        [Display(Name = "ΑΦΜ")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "Το ΑΦΜ πρέπει να αποτελείται από 9 ψηφία.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Το ΑΦΜ πρέπει να αποτελείται από 9 ψηφία.")]
        public string VatNumber { get; set; } = string.Empty;

        [Display(Name = "Τηλέφωνο")]
        [StringLength(50)]
        [RegularExpression(@"^\d{0,10}$", ErrorMessage = "Το τηλέφωνο πρέπει να περιέχει μόνο αριθμούς και μέχρι 10 ψηφία.")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Το email δεν είναι έγκυρο.")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Display(Name = "Υπεύθυνος Επικοινωνίας")]
        [StringLength(200)]
        public string? ContactPerson { get; set; }

        [Display(Name = "Διεύθυνση")]
        [StringLength(300)]
        public string? Address { get; set; }

        [Display(Name = "Σχόλια")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public List<Panel> Panels { get; set; } = new();
    }
}