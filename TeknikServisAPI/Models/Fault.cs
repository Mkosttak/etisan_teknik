using System.ComponentModel.DataAnnotations;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Models
{
    public class Fault
    {
        public int Id { get; set; }
        
        // Cihaz bilgileri (manuel giriş - artık Device tablosuna bağlı değil)
        [MaxLength(100)]
        public string? DeviceSerialNumber { get; set; }
        
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string Status { get; set; } = FaultStatuses.Pending; // Pending, InProgress, WaitingForParts, Completed, Cancelled
        
        // Firma bilgileri
        [MaxLength(200)]
        public string? InstitutionName { get; set; }
        
        [MaxLength(200)]
        public string? InstitutionOtherName { get; set; }
        
        public int? AssignedToUserId { get; set; }
        
        public int? SpareDeviceId { get; set; } // Müşteriye gönderilen yedek cihaz
        
        // Gönderilecek adres bilgileri
        [MaxLength(100)]
        public string? ShippingAddressTitle { get; set; }
        
        public string? ShippingAddressLine { get; set; }
        
        [MaxLength(100)]
        public string? ShippingCity { get; set; }
        
        [MaxLength(100)]
        public string? ShippingDistrict { get; set; }
        
        [MaxLength(10)]
        public string? ShippingPostalCode { get; set; }
        
        // İletişim bilgileri
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        
        [MaxLength(20)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(100)]
        public string? ContactEmail { get; set; }
        
        // Yedek cihaz talebi
        public bool SendBackupDevice { get; set; }
        public int? BackupDeviceRequestId { get; set; }
        
        // Oluşturan kullanıcı
        public int? CreatedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        
        // Navigation properties
        public User? AssignedToUser { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }
        public Device? SpareDevice { get; set; }
        public ICollection<FaultHistory> History { get; set; } = new List<FaultHistory>();

        public int? RelatedServiceFormId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("RelatedServiceFormId")]
        public ServiceForm? RelatedServiceForm { get; set; }
    }
}


