using System.ComponentModel.DataAnnotations;

namespace panelapp.Models
{
    public class ActivityLog
    {
        public int ActivityLogID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityID { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(100)]
        public string? UserRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}