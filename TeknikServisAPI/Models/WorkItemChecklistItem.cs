using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// WorkItem checklist maddesi
    /// </summary>
    public class WorkItemChecklistItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İlgili WorkItem ID</summary>
        public int WorkItemId { get; set; }

        /// <summary>Madde başlığı</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Tamamlandı mı?</summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>Sıralama</summary>
        public int Order { get; set; } = 0;

        /// <summary>Tamamlanma tarihi</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>Tamamlayan kullanıcı ID</summary>
        public int? CompletedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("WorkItemId")]
        public WorkItem WorkItem { get; set; } = null!;

        [ForeignKey("CompletedByUserId")]
        public User? CompletedByUser { get; set; }
    }
}
