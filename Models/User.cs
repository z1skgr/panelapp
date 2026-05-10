using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class User
    {
        [Required]
        public int UserID { get; set; }
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}