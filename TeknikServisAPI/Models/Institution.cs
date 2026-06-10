using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    public class Institution
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Adres bilgileri (ileride doldurulacak)
        public string? AddressLine { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(100)]
        public string? District { get; set; }
        
        [MaxLength(10)]
        public string? PostalCode { get; set; }
        
        // İletişim bilgileri (ileride doldurulacak)
        [MaxLength(100)]
        public string? ContactPerson { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        public string? Email { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
