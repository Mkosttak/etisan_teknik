using MediatR;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Events;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.EventHandlers
{
    public class RequestNotificationHandler : 
        INotificationHandler<RequestCreatedEvent>,
        INotificationHandler<RequestCompletedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<RequestNotificationHandler> _logger;

        public RequestNotificationHandler(
            INotificationService notificationService,
            ILogger<RequestNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Handle(RequestCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Yeni Malzeme Talebi",
                    message: $"{notification.InstitutionName} - {notification.DeviceSerialNumber}\n{notification.Description}",
                    type: "Request",
                    link: $"/requests/{notification.RequestId}",
                    relatedId: notification.RequestId,
                    relatedType: "Request",
                    userId: null,
                    targetRole: AppRoles.Teknik
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Talep bildirimi gönderilirken hata: {RequestId}", notification.RequestId);
            }
        }

        public async Task Handle(RequestCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Talep Tamamlandı",
                    message: $"{notification.InstitutionName} - {notification.Description}",
                    type: "Request",
                    link: $"/requests/{notification.RequestId}",
                    relatedId: notification.RequestId,
                    relatedType: "Request",
                    userId: null,
                    targetRole: AppRoles.Proje
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tamamlanma bildirimi gönderilirken hata: {RequestId}", notification.RequestId);
            }
        }
    }
}
