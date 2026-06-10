using System.ComponentModel.DataAnnotations;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.DTOs
{
    // ============ Response DTOs ============

    /// <summary>
    /// WorkItem detay response
    /// </summary>
    public class WorkItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;

        
        // Kurum
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        
        // İletişim
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        
        // Adres
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }
        
        // Planlama
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public DateTime? DueAt { get; set; }
        
        // İlişkiler
        public int? DeviceId { get; set; }
        public string? DeviceSerialNumber { get; set; }
        public int? SourceFaultId { get; set; }
        public int? LegacyRequestId { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public int? RelatedServiceFormId { get; set; }
        
        // Zaman
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Alt koleksiyonlar (sayılar)
        public int TagCount { get; set; }
        public int CommentCount { get; set; }
        public int AttachmentCount { get; set; }
        public int ChecklistItemCount { get; set; }
        public int ChecklistCompletedCount { get; set; }
        
        // Etiketler
        public List<WorkItemTagDto> Tags { get; set; } = new();
    }

    /// <summary>
    /// WorkItem liste için özet response
    /// </summary>
    public class WorkItemListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;

        public string? InstitutionName { get; set; }
        public string? ShippingCity { get; set; }
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ChecklistItemCount { get; set; }
        public int ChecklistCompletedCount { get; set; }
        public int? RelatedServiceFormId { get; set; }
        public List<string> TagNames { get; set; } = new();
    }

    // ============ Create/Update DTOs ============

    /// <summary>
    /// Yeni WorkItem oluşturma
    /// </summary>
    public class CreateWorkItemDto
    {
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string Kind { get; set; } = "Request"; // Request, PlannedWork
        
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
        
        public string Stage { get; set; } = "Backlog"; // Backlog, Done, Cancelled
        
        // Kurum
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        
        // İletişim
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        
        // Adres
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }
        
        // Planlama
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public DateTime? DueAt { get; set; }
        
        // İlişkiler
        public int? DeviceId { get; set; }
        public int? SourceFaultId { get; set; }
        
        // Etiketler
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// WorkItem güncelleme
    /// </summary>
    public class UpdateWorkItemDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }
        
        public string? Description { get; set; }
        public string? Kind { get; set; }
        public string? Priority { get; set; }
        public string? Stage { get; set; }
        

        
        // Kurum
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        
        // İletişim
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        
        // Adres
        public string? ShippingAddressLine { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingPostalCode { get; set; }
        
        // Planlama
        public DateTime? PlannedStartAt { get; set; }
        public DateTime? PlannedEndAt { get; set; }
        public DateTime? DueAt { get; set; }
        
        // İlişkiler
        public int? DeviceId { get; set; }
        
        // Etiketler
        public List<string>? Tags { get; set; }
        
        /// <summary>Concurrency control için</summary>
        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// Stage değiştirme
    /// </summary>
    public class UpdateStageDto
    {
        [Required]
        public string Stage { get; set; } = string.Empty; // Backlog, Done, Cancelled
        
        public string? Notes { get; set; }
    }



    // ============ Filter DTO ============

    /// <summary>
    /// Liste filtreleme parametreleri
    /// </summary>
    public class WorkItemFilterDto
    {
        public string? Stage { get; set; }
        public string? Kind { get; set; }
        public string? Priority { get; set; }

        public string? InstitutionName { get; set; }
        public string? ShippingCity { get; set; }
        public string? Tag { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Search { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        
        // Sorting
        public string SortBy { get; set; } = "CreatedAt";
        public bool SortDesc { get; set; } = true;
    }

    // ============ Tag DTO ============

    public class WorkItemTagDto
    {
        public int Id { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string? Color { get; set; }
    }

    public class CreateTagDto
    {
        [Required]
        [MaxLength(50)]
        public string TagName { get; set; } = string.Empty;
        
        [MaxLength(7)]
        public string? Color { get; set; }
    }

    // ============ Comment DTO ============

    public class WorkItemCommentDto
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }

    // ============ Attachment DTO ============

    public class WorkItemAttachmentDto
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public int? UploadedByUserId { get; set; }
        public string? UploadedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ============ Checklist DTO ============

    public class WorkItemChecklistItemDto
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Order { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CompletedByUserId { get; set; }
        public string? CompletedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateChecklistItemDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public int Order { get; set; } = 0;
    }

    public class UpdateChecklistItemDto
    {
        public string? Title { get; set; }
        public bool? IsCompleted { get; set; }
        public int? Order { get; set; }
    }

    // ============ History DTO ============

    public class WorkItemHistoryDto
    {
        public int Id { get; set; }
        public int WorkItemId { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ============ Kanban Board DTO ============

    /// <summary>
    /// Kanban board için stage bazlı gruplu response
    /// </summary>
    public class KanbanBoardDto
    {
        public List<WorkItemListDto> Backlog { get; set; } = new();
        public List<WorkItemListDto> Done { get; set; } = new();
        public List<WorkItemListDto> Cancelled { get; set; } = new();
        
        public int TotalCount { get; set; }
    }
}
