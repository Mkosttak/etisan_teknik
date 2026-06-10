using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// WorkItem yorumu
    /// </summary>
    public class WorkItemComment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İlgili WorkItem ID</summary>
        public int WorkItemId { get; set; }

        /// <summary>Yazan kullanıcı ID</summary>
        public int UserId { get; set; }

        /// <summary>Yorum içeriği</summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation
        [ForeignKey("WorkItemId")]
        public WorkItem WorkItem { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
