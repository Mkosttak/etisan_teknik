using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class FaultHistory
    {
        public int Id { get; set; }
        
        public int FaultId { get; set; }
        
        public int UserId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Created, StatusChanged, Assigned, Completed, etc.
        
        public string? Notes { get; set; }
        
        [MaxLength(50)]
        public string? OldValue { get; set; }
        
        [MaxLength(50)]
        public string? NewValue { get; set; }
        
        [MaxLength(10)]
        [NotMapped] // Migration yapılana kadar NotMapped (Hata kodu Notes içinde saklanıyor)
        public string? ErrorCode { get; set; }
 // Hata kodu (000, 001, 002, vb.)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Fault Fault { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}





