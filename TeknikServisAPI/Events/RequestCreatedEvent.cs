using MediatR;

namespace TeknikServisAPI.Events
{
    public class RequestCreatedEvent : INotification
    {
        public int RequestId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsUrgent { get; set; }
    }
}
