using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// Refresh Token entity - JWT token yenileme mekanizması için
    /// Production-grade güvenlik: Token rotation, expiration, revocation desteği
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Benzersiz refresh token değeri (cryptographically secure random string)
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// Token'ın oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Token'ın son kullanma tarihi
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Token iptal edildi mi? (logout, security breach, vb.)
        /// </summary>
        public bool IsRevoked { get; set; } = false;
        
        /// <summary>
        /// Token iptal tarihi
        /// </summary>
        public DateTime? RevokedAt { get; set; }
        
        /// <summary>
        /// Yeni token ile değiştirildi mi? (token rotation)
        /// </summary>
        public bool IsUsed { get; set; } = false;
        
        /// <summary>
        /// Bu token'ı değiştiren yeni token ID'si (token rotation takibi)
        /// </summary>
        public int? ReplacedByTokenId { get; set; }
        
        /// <summary>
        /// Token'ın oluşturulduğu IP adresi
        /// </summary>
        [MaxLength(45)] // IPv6 max length
        public string? CreatedByIp { get; set; }
        
        /// <summary>
        /// Token'ın iptal edildiği IP adresi
        /// </summary>
        [MaxLength(45)]
        public string? RevokedByIp { get; set; }
        
        /// <summary>
        /// Token'ın ait olduğu kullanıcı
        /// </summary>
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        
        /// <summary>
        /// Token hala aktif mi kontrol et
        /// </summary>
        public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow < ExpiresAt;
    }
}
