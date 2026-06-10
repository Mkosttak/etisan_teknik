using System.ComponentModel.DataAnnotations;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Models
{
    public class Device
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? DeviceModel { get; set; }
        
        [MaxLength(200)]
        public string? University { get; set; }
       
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = DeviceStatuses.Available; // Available, InUse, UnderMaintenance, Retired
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}
