using MediatR;

namespace TeknikServisAPI.Events
{
    public class RequestCompletedEvent : INotification
    {
        public int RequestId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
