using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // Info, Success, Warning, Error
        
        [MaxLength(500)]
        public string? Link { get; set; }
        
        public int? RelatedId { get; set; }
        
        [MaxLength(50)]
        public string? RelatedType { get; set; } // Request, Fault, Shipment, etc.
        
        public int? UserId { get; set; } // null ve TargetRole null ise = herkese
        
        [MaxLength(50)]
        public string? TargetRole { get; set; } // "Teknik", "Admin" vb.
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public User? User { get; set; }
    }
}
