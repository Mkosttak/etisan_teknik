using System.ComponentModel.DataAnnotations;

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// Talep ile stok kalemi arasındaki ilişkiyi ve miktarı temsil eder
    /// </summary>
    public class RequestStockItem
    {
        public int Id { get; set; }
        
        [Required]
        public int RequestId { get; set; }
        
        [Required]
        public int StockItemId { get; set; }
        
        /// <summary>
        /// Talep edilen miktar
        /// </summary>
        [Required]
        public int Quantity { get; set; }
        
        /// <summary>
        /// Talep oluşturulduğunda stoktan düşüldü mü?
        /// </summary>
        public bool IsDeducted { get; set; } = false;
        
        /// <summary>
        /// Bu ürün iptal edildi mi? (Yönetim tarafından ret edildi)
        /// </summary>
        public bool IsCancelled { get; set; } = false;

        /// <summary>
        /// Bu ürün kargoya verildi mi? (Kısmi gönderim takibi için)
        /// </summary>
        public bool IsShipped { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Request Request { get; set; } = null!;
        public StockItem StockItem { get; set; } = null!;
    }
}

