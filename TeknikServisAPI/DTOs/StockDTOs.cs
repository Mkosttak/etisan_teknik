namespace TeknikServisAPI.DTOs
{
    public class StockItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
        public bool IsActive { get; set; }
        public bool IsLowStock => CurrentStock <= MinimumStock;
    }

    public class CreateStockItemDto
    {
        public string ItemName { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public int CurrentStock { get; set; } = 0;
        public int MinimumStock { get; set; } = 0;
    }

    public class UpdateStockItemDto
    {
        public string? ItemName { get; set; }
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public int? CurrentStock { get; set; }
        public int? MinimumStock { get; set; }
        public bool? IsActive { get; set; }
    }

    public class StockMovementDto
    {
        public int Id { get; set; }
        public int StockItemId { get; set; }
        public string StockItemName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Reference { get; set; }
        public string? InstitutionName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateStockMovementDto
    {
        public int StockItemId { get; set; }
        public string MovementType { get; set; } = "In";
        public int Quantity { get; set; }
        public string? Reference { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Notes { get; set; }
    }
}




