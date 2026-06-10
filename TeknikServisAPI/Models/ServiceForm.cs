using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Models
{
    public class ServiceForm
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = ServiceFormStatuses.Draft; // DRAFT | SUBMITTED
        
        // Alan 1 - Genel Bilgiler
        public DateTime FormDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(200)]
        public string? InstitutionName { get; set; }
        public bool InstitutionIsOther { get; set; } = false;
        
        [MaxLength(200)]
        public string? InstitutionOtherName { get; set; }
        
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? ContactFirstName { get; set; }
        
        [MaxLength(100)]
        public string? ContactLastName { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        public string? Email { get; set; }
        
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        
        [MaxLength(50)]
        public string? ServiceScope { get; set; } // SozlesmeIci, SozlesmeDisi, Diger
        
        [MaxLength(500)]
        public string? ServiceScopeOther { get; set; }
        
        [MaxLength(50)]
        public string? ServiceType { get; set; } // ArizaGiderme, Bakim, Kesif, Diger
        
        [MaxLength(500)]
        public string? ServiceTypeOther { get; set; }
        
        // Alan 2 - Servis Detayları
        [Column(TypeName = "LONGTEXT")]
        public string? ReportedIssue { get; set; }
        
        [Column(TypeName = "LONGTEXT")]
        public string? WorkPerformed { get; set; }
        
        [Column(TypeName = "LONGTEXT")]
        public string? MaterialsUsed { get; set; }
        
        [MaxLength(50)]
        public string? IssueStatus { get; set; } // SorunGiderilmedi, SorunGiderildi, SorunaRastlanmadi, ParcaDegistirildi, ParcaDegistirilecek, Diger
        
        [MaxLength(500)]
        public string? IssueStatusOther { get; set; }
        
        // Alan 3 - Fotoğraflar (JSON array of base64 strings)
        [Column(TypeName = "LONGTEXT")]
        public string? Photos { get; set; }
        
        // Alan 4 - İmzalar
        [MaxLength(100)]
        public string? EtisanFirstName { get; set; }
        
        [MaxLength(100)]
        public string? EtisanLastName { get; set; }
        
        [Column(TypeName = "LONGTEXT")]
        public string? EtisanSignature { get; set; } // base64
        
        [MaxLength(100)]
        public string? AuthorityFirstName { get; set; }
        
        [MaxLength(100)]
        public string? AuthorityLastName { get; set; }
        
        [MaxLength(100)]
        public string? AuthorityEmail { get; set; }
        
        [MaxLength(20)]
        public string? AuthorityPhone { get; set; }
        
        [Column(TypeName = "LONGTEXT")]
        public string? AuthoritySignature { get; set; } // base64
        
        // Metadata
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        [MaxLength(500)]
        public string? PdfPath { get; set; }

        [Column(TypeName = "LONGTEXT")]
        public string? PartsToReplace { get; set; }
    }
}
