using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.DTOs
{
    public class FaultDto
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string? DeviceSerialNumber { get; set; }
        public string? Description { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public int? SpareDeviceId { get; set; }
        public string? SpareDeviceSerialNumber { get; set; }
        public string? ShippingAddressTitle { get; set; }
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public bool SendBackupDevice { get; set; }
        public int? BackupDeviceRequestId { get; set; }
        public string? BackupDeviceRequestStatus { get; set; } // Talep durumu (Pending, InProgress, Completed, Cancelled)
        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public int? RelatedServiceFormId { get; set; }
    }

    public class CreateFaultDto
    {
        // Cihaz bilgileri (manuel giriş - sadece seri numarası)
        public string? DeviceSerialNumber { get; set; }
        
        public string? Description { get; set; }

        public string? InstitutionName { get; set; }

        public string? InstitutionOtherName { get; set; }
        public string? ShippingAddressTitle { get; set; }

        public string? ShippingAddressLine { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingDistrict { get; set; }

        public string? ShippingPostalCode { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactEmail { get; set; }
        public bool SendBackupDevice { get; set; }
        public bool CreateDraftServiceForm { get; set; }
        public int? RelatedServiceFormId { get; set; }
    }

    public class UpdateFaultDto
    {
        // Cihaz bilgileri (manuel giriş - sadece seri numarası)
        public string? DeviceSerialNumber { get; set; }
        
        // Arıza bilgileri
        public string? Description { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Status { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? SpareDeviceId { get; set; }
        public bool? SendBackupDevice { get; set; }
        public bool? CreateDraftServiceForm { get; set; }
        
        // Adres ve iletişim bilgileri (Request oluşturmak için)
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
    }

    public class FaultHistoryDto
    {
        public int Id { get; set; }
        public int FaultId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateFaultStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? ErrorCode { get; set; } // Hata kodu (Verifone gönderimi için)
        public bool CreateDraftServiceForm { get; set; } // Taslak servis formu oluştur
    }

    public class AddFaultCommentDto
    {
        public string Notes { get; set; } = string.Empty;
    }
}





