using MediatR;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Events;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.EventHandlers
{
    public class StockNotificationHandler : INotificationHandler<LowStockEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<StockNotificationHandler> _logger;

        public StockNotificationHandler(
            INotificationService notificationService,
            ILogger<StockNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(LowStockEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Kritik Stok Seviyesi",
                    message: $"{notification.ItemName} stok seviyesi kritik düzeyde! Mevcut: {notification.CurrentStock}, Min: {notification.MinimumStock}",
                    type: "Stock",
                    link: "/stock",
                    relatedId: notification.StockItemId,
                    relatedType: "StockItem",
                    userId: null,
                    targetRole: AppRoles.Teknik
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok bildirimi gönderilirken hata: {ItemId}", notification.StockItemId);
            }
        }
    }
}
