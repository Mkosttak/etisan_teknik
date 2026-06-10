using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;

namespace TeknikServisAPI.Services
{
    public interface IBackgroundJobService
    {
        Task CheckPendingFaultsAsync();
        Task CheckLowStockItemsAsync();
        Task CleanupOldLogsAsync();
        Task CleanupExpiredRefreshTokensAsync();
    }

    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(ApplicationDbContext context, ILogger<BackgroundJobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 24 saatten uzun bekleyen arızaları kontrol eder
        /// </summary>
        public async Task CheckPendingFaultsAsync()
        {
            try
            {
                _logger.LogInformation("Checking pending faults...");

                var pendingFaults = await _context.Faults
                    .Where(f => f.Status == "Created" && 
                           f.CreatedAt < DateTime.UtcNow.AddHours(-24))
                    .ToListAsync();

                if (pendingFaults.Any())
                {
                    _logger.LogWarning(
                        "Found {Count} faults pending for more than 24 hours", 
                        pendingFaults.Count
                    );

                    // Burada email/SMS bildirimi gönderilebilir
                    foreach (var fault in pendingFaults)
                    {
                        _logger.LogWarning(
                            "Fault #{FaultId} for device {SerialNumber} is pending for {Hours} hours",
                            fault.Id,
                            fault.DeviceSerialNumber ?? "N/A",
                            (DateTime.UtcNow - fault.CreatedAt).TotalHours
                        );
                    }
                }
                else
                {
                    _logger.LogInformation("No pending faults found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending faults");
                throw;
            }
        }

        /// <summary>
        /// Düşük stok seviyesindeki ürünleri kontrol eder
        /// </summary>
        public async Task CheckLowStockItemsAsync()
        {
            try
            {
                _logger.LogInformation("Checking low stock items...");

                var lowStockItems = await _context.StockItems
                    .Where(s => s.IsActive && s.CurrentStock <= s.MinimumStock)
                    .ToListAsync();

                if (lowStockItems.Any())
                {
                    _logger.LogWarning(
                        "Found {Count} items with low stock", 
                        lowStockItems.Count
                    );

                    foreach (var item in lowStockItems)
                    {
                        _logger.LogWarning(
                            "Stock item '{ItemName}' is low: Current={Current}, Minimum={Minimum}",
                            item.ItemName,
                            item.CurrentStock,
                            item.MinimumStock
                        );
                    }
                }
                else
                {
                    _logger.LogInformation("All stock items are at acceptable levels");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking low stock items");
                throw;
            }
        }

        /// <summary>
        /// 90 günden eski logları temizler
        /// </summary>
        public async Task CleanupOldLogsAsync()
        {
            try
            {
                _logger.LogInformation("Cleaning up old logs...");

                // Eski fault history kayıtlarını temizle (opsiyonel)
                var oldHistories = await _context.FaultHistories
                    .Where(h => h.CreatedAt < DateTime.UtcNow.AddDays(-90))
                    .ToListAsync();

                if (oldHistories.Any())
                {
                    _logger.LogInformation(
                        "Removing {Count} old history records", 
                        oldHistories.Count
                    );
                    
                    // Gerçek uygulamada dikkatli olun, bu kayıtları silmek yerine arşivleyebilirsiniz
                    // _context.FaultHistories.RemoveRange(oldHistories);
                    // await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old logs");
                throw;
            }
        }

        /// <summary>
        /// Süresi dolmuş, kullanılmış veya iptal edilmiş refresh token'ları temizler
        /// Güvenlik ve performans için önemli - token tablosunun büyümesini önler
        /// </summary>
        public async Task CleanupExpiredRefreshTokensAsync()
        {
            try
            {
                _logger.LogInformation("Cleaning up expired refresh tokens...");

                // 30 günden eski expired, used veya revoked token'ları sil
                var cutoffDate = DateTime.UtcNow.AddDays(-30);
                
                var expiredTokens = await _context.RefreshTokens
                    .Where(t => t.ExpiresAt < cutoffDate || 
                               (t.IsUsed && t.CreatedAt < cutoffDate) ||
                               (t.IsRevoked && t.RevokedAt < cutoffDate))
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _logger.LogInformation(
                        "Removing {Count} expired/used/revoked refresh tokens", 
                        expiredTokens.Count
                    );
                    
                    _context.RefreshTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully cleaned up {Count} tokens", expiredTokens.Count);
                }
                else
                {
                    _logger.LogInformation("No expired refresh tokens to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired refresh tokens");
                throw;
            }
        }

    }
}
