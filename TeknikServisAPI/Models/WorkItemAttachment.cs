using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// WorkItem dosya eki
    /// </summary>
    public class WorkItemAttachment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İlgili WorkItem ID</summary>
        public int WorkItemId { get; set; }

        /// <summary>Orijinal dosya adı</summary>
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>Sunucudaki dosya yolu</summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>Dosya boyutu (bytes)</summary>
        public long FileSize { get; set; }

        /// <summary>MIME tipi</summary>
        [MaxLength(100)]
        public string? MimeType { get; set; }

        /// <summary>Yükleyen kullanıcı ID</summary>
        public int? UploadedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("WorkItemId")]
        public WorkItem WorkItem { get; set; } = null!;

        [ForeignKey("UploadedByUserId")]
        public User? UploadedByUser { get; set; }
    }
}
