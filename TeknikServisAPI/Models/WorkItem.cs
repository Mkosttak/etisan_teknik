using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// Ana iş birimi - Kanban kartı
    /// Talepler, Planlı İşler ve Arıza Sonrası İşler tek varlıkta birleşir
    /// </summary>
    public class WorkItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İş başlığı</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Detaylı açıklama</summary>
        public string? Description { get; set; }

        /// <summary>İş türü (Request, PlannedWork, IncidentFollowUp)</summary>
        public WorkItemKind Kind { get; set; } = WorkItemKind.Request;

        /// <summary>Kanban sütunu / durum</summary>
        public WorkItemStage Stage { get; set; } = WorkItemStage.Backlog;

        /// <summary>Öncelik seviyesi</summary>
        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

        // ============ Kurum Bilgileri ============

        /// <summary>Kurum adı</summary>
        [MaxLength(200)]
        public string? InstitutionName { get; set; }

        /// <summary>Diğer kurum adı (DİĞER seçildiğinde)</summary>
        [MaxLength(200)]
        public string? InstitutionOtherName { get; set; }

        // ============ İletişim Bilgileri ============

        /// <summary>İletişim kişisi</summary>
        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        /// <summary>Telefon numarası</summary>
        [MaxLength(20)]
        public string? ContactPhone { get; set; }

        /// <summary>E-posta adresi</summary>
        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        // ============ Adres Bilgileri ============

        /// <summary>Adres satırı</summary>
        [MaxLength(1000)]
        public string? ShippingAddressLine { get; set; }

        /// <summary>Şehir</summary>
        [MaxLength(100)]
        public string? ShippingCity { get; set; }

        /// <summary>İlçe</summary>
        [MaxLength(100)]
        public string? ShippingDistrict { get; set; }

        /// <summary>Posta kodu</summary>
        [MaxLength(10)]
        public string? ShippingPostalCode { get; set; }

        // ============ Planlama Bilgileri ============

        /// <summary>Planlanan başlangıç tarihi</summary>
        public DateTime? PlannedStartAt { get; set; }

        /// <summary>Planlanan bitiş tarihi</summary>
        public DateTime? PlannedEndAt { get; set; }

        /// <summary>Son tarih (deadline)</summary>
        public DateTime? DueAt { get; set; }

        // ============ İlişkiler ============

        /// <summary>İlgili cihaz ID</summary>
        public int? DeviceId { get; set; }

        /// <summary>Kaynak arıza ID</summary>
        public int? SourceFaultId { get; set; }

        /// <summary>Eski Request ID (migration için, idempotent)</summary>
        public int? LegacyRequestId { get; set; }

        /// <summary>İlgili Servis Formu ID (eğer parça değişimi ise)</summary>
        public int? RelatedServiceFormId { get; set; }

        /// <summary>Oluşturan kullanıcı ID</summary>
        public int? CreatedByUserId { get; set; }

        // ============ Zaman Damgaları ============

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        // ============ Concurrency Control ============

        /// <summary>Optimistic concurrency için row version</summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ============ Navigation Properties ============

        [ForeignKey("DeviceId")]
        public Device? Device { get; set; }

        [ForeignKey("SourceFaultId")]
        public Fault? SourceFault { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }

        /// <summary>Etiketler (many-to-many)</summary>
        public ICollection<WorkItemTag> Tags { get; set; } = new List<WorkItemTag>();

        /// <summary>Yorumlar</summary>
        public ICollection<WorkItemComment> Comments { get; set; } = new List<WorkItemComment>();

        /// <summary>Ekler (dosyalar)</summary>
        public ICollection<WorkItemAttachment> Attachments { get; set; } = new List<WorkItemAttachment>();

        /// <summary>Checklist maddeleri</summary>
        public ICollection<WorkItemChecklistItem> ChecklistItems { get; set; } = new List<WorkItemChecklistItem>();

        /// <summary>Değişiklik geçmişi</summary>
        public ICollection<WorkItemHistory> History { get; set; } = new List<WorkItemHistory>();
    }
}
