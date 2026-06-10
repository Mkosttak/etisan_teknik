using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        
        public int StockItemId { get; set; }
        
        [MaxLength(50)]
        public string MovementType { get; set; } = "In"; // In, Out, Adjustment
        
        public int Quantity { get; set; }
        
        public int? UserId { get; set; }
        
        public string? Reference { get; set; } // Sipariş no, irsaliye no, vb.
        
        [MaxLength(500)]
        public string? Reason { get; set; } // Hareket sebebi

        [MaxLength(200)]
        public string? InstitutionName { get; set; } // İlgili firma/kurum adı
        
        [MaxLength(50)]
        public string? RelatedEntityType { get; set; } // Request, Fault, ServiceForm, etc.
        
        public int? RelatedEntityId { get; set; } // İlgili entity'nin ID'si
        
        public DateTime MovementDate { get; set; } = DateTime.UtcNow; // Hareket tarihi
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public StockItem StockItem { get; set; } = null!;
        public User? User { get; set; }
    }
}









