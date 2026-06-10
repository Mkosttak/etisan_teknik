using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// WorkItem etiketi - Many-to-Many ilişki için ara tablo
    /// </summary>
    public class WorkItemTag
    {
        [Key]
        public int Id { get; set; }

        /// <summary>İlgili WorkItem ID</summary>
        public int WorkItemId { get; set; }

        /// <summary>Etiket adı</summary>
        [Required]
        [MaxLength(50)]
        public string TagName { get; set; } = string.Empty;

        /// <summary>Etiket rengi (hex)</summary>
        [MaxLength(7)]
        public string? Color { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("WorkItemId")]
        public WorkItem WorkItem { get; set; } = null!;
    }
}
