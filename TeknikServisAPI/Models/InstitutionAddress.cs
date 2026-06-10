using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    public class InstitutionAddress
    {
        public int Id { get; set; }

        [Required]
        public int InstitutionId { get; set; }

        [ForeignKey("InstitutionId")]
        public Institution? Institution { get; set; }

        [MaxLength(100)]
        public string? Label { get; set; } // "Merkez", "Hastane Kampüs" vb.

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? District { get; set; }

        public string? AddressLine { get; set; }

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
