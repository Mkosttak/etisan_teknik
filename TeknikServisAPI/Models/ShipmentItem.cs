using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class ShipmentItem
    {
        public int Id { get; set; }
        
        public int ShipmentId { get; set; }
        
        [MaxLength(50)]
        public string ItemType { get; set; } = "Device"; // Device, Part, Accessory
        
        public int? DeviceId { get; set; }
        
        public int? StockItemId { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public int Quantity { get; set; } = 1;
        
        public string? SerialNumber { get; set; }
        
        // Navigation properties
        public Shipment Shipment { get; set; } = null!;
        public Device? Device { get; set; }
        public StockItem? StockItem { get; set; }
    }
}









