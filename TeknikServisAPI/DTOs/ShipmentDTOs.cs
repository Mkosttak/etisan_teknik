namespace TeknikServisAPI.DTOs
{
    public class ShipmentDto
    {
        public int Id { get; set; }
        public string ShipmentType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? CargoCompany { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public List<ShipmentItemDto> Items { get; set; } = new();
    }

    public class CreateShipmentDto
    {
        public string ShipmentType { get; set; } = "ToCustomer";
        public string? TrackingNumber { get; set; }
        public string? CargoCompany { get; set; }
        public string? Notes { get; set; }
        public List<CreateShipmentItemDto> Items { get; set; } = new();
    }

    public class UpdateShipmentDto
    {
        public string? Status { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CargoCompany { get; set; }
        public string? Notes { get; set; }
    }

    public class ShipmentItemDto
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public int? DeviceId { get; set; }
        public int? StockItemId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class CreateShipmentItemDto
    {
        public string ItemType { get; set; } = "Device";
        public int? DeviceId { get; set; }
        public int? StockItemId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string? SerialNumber { get; set; }
    }
}





