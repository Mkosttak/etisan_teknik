using System.ComponentModel.DataAnnotations;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Models
{
    public class Shipment
    {
        public int Id { get; set; }
        
        [MaxLength(50)]
        public string ShipmentType { get; set; } = ShipmentTypes.ToCustomer; // ToCustomer, ToManufacturer, FromManufacturer
        
        [MaxLength(50)]
        public string Status { get; set; } = ShipmentStatuses.Preparing; // Preparing, Shipped, Delivered, Cancelled
        
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }
        
        [MaxLength(100)]
        public string? CargoCompany { get; set; }
        
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ShippedAt { get; set; }
        
        public DateTime? DeliveredAt { get; set; }
        
        // Navigation properties
        public ICollection<ShipmentItem> Items { get; set; } = new List<ShipmentItem>();
    }
}




