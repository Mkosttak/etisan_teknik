using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.DTOs
{
    /// <summary>
    /// Talep için stok kalemi bilgisi
    /// </summary>
    public class RequestStockItemDto
    {
        public int Id { get; set; } // RequestStockItem ID
        public int StockItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool IsDeducted { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsShipped { get; set; }
    }

    public class ShipRequestItemsDto
    {
        public List<int> RequestStockItemIds { get; set; } = new();
        public List<string> ExtraItems { get; set; } = new();
    }

    public class RequestDto
    {
        public int Id { get; set; }
        public string? DeviceSerialNumber { get; set; }
        public string? Description { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public bool IsUrgent { get; set; }
        public string? ExtraItems { get; set; }
        public string? ShippedExtraItems { get; set; }
        public List<RequestStockItemDto> StockItems { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }

    /// <summary>
    /// Talep oluşturma için stok kalemi bilgisi
    /// </summary>
    public class CreateRequestStockItemDto
    {
        public int StockItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateRequestDto
    {
        public string? DeviceSerialNumber { get; set; }
        public string? Description { get; set; }

        public string? InstitutionName { get; set; }

        public string? InstitutionOtherName { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactEmail { get; set; }

        public string? ShippingAddressLine { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingDistrict { get; set; }
        public bool IsUrgent { get; set; }
        
        /// <summary>
        /// Stoğumuzda olmayan ekstra ürünler (serbest metin)
        /// </summary>
        public string? ExtraItems { get; set; }
        
        /// <summary>
        /// Stoğumuzdan gönderilecek ürünler
        /// </summary>
        public List<CreateRequestStockItemDto> StockItems { get; set; } = new();
    }

    public class UpdateRequestDto
    {
        public string? DeviceSerialNumber { get; set; }
        public string? Description { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Status { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public bool? IsUrgent { get; set; }
        public string? ExtraItems { get; set; }
        
        /// <summary>
        /// Güncelleme sırasında stok kalemleri değiştirilebilir
        /// </summary>
        public List<CreateRequestStockItemDto>? StockItems { get; set; }
        
        /// <summary>
        /// İptal edilecek stok kalemi ID'leri (RequestStockItem Id'leri)
        /// </summary>
        public List<int>? CancelledStockItemIds { get; set; }
        
        /// <summary>
        /// İptal edilecek ekstra ürün indeksleri
        /// </summary>
        public List<int>? CancelledExtraItemIndexes { get; set; }
    }

    public class UpdateRequestStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}


