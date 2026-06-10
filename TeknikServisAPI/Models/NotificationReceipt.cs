using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeknikServisAPI.Models
{
    public class NotificationReceipt
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }
        
        public int UserId { get; set; }
        
        public bool IsRead { get; set; } = true;
        
        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }
        
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
