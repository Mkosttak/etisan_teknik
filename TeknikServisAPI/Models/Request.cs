using System.ComponentModel.DataAnnotations;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Models
{
    public class Request
    {
        public int Id { get; set; }
        
        // Cihaz bilgileri (manuel giriş - opsiyonel)
        [MaxLength(100)]
        public string? DeviceSerialNumber { get; set; }
        
        // Talep açıklaması (opsiyonel)
        public string? Description { get; set; }
        
        // Firma bilgileri
        [MaxLength(200)]
        public string? InstitutionName { get; set; }
        
        [MaxLength(200)]
        public string? InstitutionOtherName { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = RequestStatuses.Pending; // Pending, InProgress, Completed, Cancelled
        
        // İletişim bilgileri
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        
        [MaxLength(20)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(100)]
        public string? ContactEmail { get; set; }
        
        // Gönderilecek adres bilgileri
        [MaxLength(1000)]
        public string? ShippingAddressLine { get; set; }
        
        [MaxLength(100)]
        public string? ShippingCity { get; set; }
        
        [MaxLength(100)]
        public string? ShippingDistrict { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        // Oluşturan kullanıcı
        public int? CreatedByUserId { get; set; }
        
        // Acil talep işareti
        public bool IsUrgent { get; set; } = false;
        
        /// <summary>
        /// Stoğumuzda olmayan, ekstra talep edilen ürünler (JSON formatında)
        /// Örnek: "2x Kablo, 1x Adaptör"
        /// </summary>
        public string? ExtraItems { get; set; }

        /// <summary>
        /// Gönderilen ekstra ürünler (JSON/Virgüllü formatta)
        /// </summary>
        public string? ShippedExtraItems { get; set; }
        
        // Navigation properties
        /// <summary>
        /// Bu talep için stoğumuzdan gönderilecek ürünler
        /// </summary>
        public ICollection<RequestStockItem> RequestStockItems { get; set; } = new List<RequestStockItem>();
        
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }
    }
}


