using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsAsync(int userId, bool unreadOnly = false, int count = 20);
        Task<int> GetUnreadCountAsync(int userId, string? userRole = null);
        Task<Notification> CreateNotificationAsync(string title, string message, string type, string? link = null, int? relatedId = null, string? relatedType = null, int? userId = null, string? targetRole = null);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
    }

    public class NotificationDto : Notification
    {
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int userId, bool unreadOnly = false, int count = 20)
        {
            var user = await _context.Users.FindAsync(userId);
            string? userRole = user?.Role;

            // 1. Kullanıcıya özel VEYA Rolüne özel bildirimleri çek
            var roleLower = userRole?.ToLower();
            
            var query = _context.Notifications.Where(n => 
                (n.UserId == userId) || 
                (roleLower != null && n.TargetRole != null && n.TargetRole.ToLower() == roleLower) ||
                (n.UserId == null && n.TargetRole == null) // Herkese açık
            );

            // 2. Okunma bilgisini (Receipt) joinle
            var notificationsWithReceipts = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Select(n => new 
                { 
                    Notification = n,
                    Receipt = _context.NotificationReceipts.FirstOrDefault(r => r.NotificationId == n.Id && r.UserId == userId)
                })
                .ToListAsync();

            // 3. DTO'ya dönüştür
            var result = notificationsWithReceipts.Select(x => new NotificationDto
            {
                Id = x.Notification.Id,
                Title = x.Notification.Title,
                Message = x.Notification.Message,
                Type = x.Notification.Type,
                Link = x.Notification.Link,
                RelatedId = x.Notification.RelatedId,
                RelatedType = x.Notification.RelatedType,
                UserId = x.Notification.UserId,
                TargetRole = x.Notification.TargetRole,
                CreatedAt = x.Notification.CreatedAt,
                IsRead = x.Receipt != null && x.Receipt.IsRead,
                ReadAt = x.Receipt?.ReadAt
            });

            if (unreadOnly)
            {
                result = result.Where(n => !n.IsRead);
            }

            return result.ToList();
        }

        public async Task<int> GetUnreadCountAsync(int userId, string? userRole = null)
        {
            if (userRole == null)
            {
                var user = await _context.Users.FindAsync(userId);
                userRole = user?.Role;
            }

            // Toplam ilgili bildirim sayısı
            var roleLower = userRole?.ToLower();
            
            var relevantNotificationsQuery = _context.Notifications.Where(n => 
                (n.UserId == userId) || 
                (roleLower != null && n.TargetRole != null && n.TargetRole.ToLower() == roleLower) ||
                (n.UserId == null && n.TargetRole == null)
            );

            // Okunmuş (Receipt var) bildirim ID'leri
            var readNotificationIds = _context.NotificationReceipts
                .Where(r => r.UserId == userId && r.IsRead)
                .Select(r => r.NotificationId);

            // Okunmamış = İlgili - Okunmuş
            return await relevantNotificationsQuery
                .Where(n => !readNotificationIds.Contains(n.Id))
                .CountAsync();
        }

        public async Task<Notification> CreateNotificationAsync(string title, string message, string type, string? link = null, int? relatedId = null, string? relatedType = null, int? userId = null, string? targetRole = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Link = link,
                RelatedId = relatedId,
                RelatedType = relatedType,
                UserId = userId,
                TargetRole = targetRole,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var receipt = await _context.NotificationReceipts
                .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId);

            if (receipt == null)
            {
                receipt = new NotificationReceipt
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                };
                _context.NotificationReceipts.Add(receipt);
            }
            else if (!receipt.IsRead)
            {
                receipt.IsRead = true;
                receipt.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            string? userRole = user?.Role;

            // Okunmamış ilgili bildirimleri bul
            // Bu sorgu biraz karmaşık olabilir, o yüzden önce tüm ilgili bildirim ID'lerini alalım
            // Sonra receipt olmayanlara receipt ekleyelim

            var roleLower = userRole?.ToLower();

            var relevantNotifications = await _context.Notifications
                .Where(n => 
                    (n.UserId == userId) || 
                    (roleLower != null && n.TargetRole != null && n.TargetRole.ToLower() == roleLower) ||
                    (n.UserId == null && n.TargetRole == null)
                )
                .Select(n => n.Id)
                .ToListAsync();

            var existingReceipts = await _context.NotificationReceipts
                .Where(r => r.UserId == userId && relevantNotifications.Contains(r.NotificationId))
                .ToListAsync();

            // 1. Receipt'i olup okunmamış olanları güncelle
            foreach (var receipt in existingReceipts.Where(r => !r.IsRead))
            {
                receipt.IsRead = true;
                receipt.ReadAt = DateTime.UtcNow;
            }

            // 2. Receipt'i hiç olmayanlar için yeni oluştur
            var readNotificationIds = existingReceipts.Select(r => r.NotificationId).ToList();
            var missingReceiptIds = relevantNotifications.Except(readNotificationIds);

            foreach (var notifId in missingReceiptIds)
            {
                _context.NotificationReceipts.Add(new NotificationReceipt
                {
                    NotificationId = notifId,
                    UserId = userId,
                    IsRead = true,
                    ReadAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
