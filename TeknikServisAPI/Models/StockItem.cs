using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class StockItem
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ItemName { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? ItemCode { get; set; }
        
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public int CurrentStock { get; set; } = 0;
        
        public int MinimumStock { get; set; } = 0;
        
        [MaxLength(50)]
        public string? Unit { get; set; } = "adet";
        
        public decimal? UnitPrice { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    }
}
