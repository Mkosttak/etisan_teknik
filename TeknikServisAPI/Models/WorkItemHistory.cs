using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// WorkItem değişiklik geçmişi (Audit log)
    /// </summary>
    public class WorkItemHistory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İlgili WorkItem ID</summary>
        public int WorkItemId { get; set; }

        /// <summary>İşlemi yapan kullanıcı ID</summary>
        public int? UserId { get; set; }

        /// <summary>Yapılan işlem (Created, StageChanged, Updated, CommentAdded, AttachmentAdded, ChecklistUpdated)</summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Değişen alan adı</summary>
        [MaxLength(50)]
        public string? Field { get; set; }

        /// <summary>Eski değer</summary>
        [MaxLength(500)]
        public string? OldValue { get; set; }

        /// <summary>Yeni değer</summary>
        [MaxLength(500)]
        public string? NewValue { get; set; }

        /// <summary>Ek notlar</summary>
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("WorkItemId")]
        public WorkItem WorkItem { get; set; } = null!;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
