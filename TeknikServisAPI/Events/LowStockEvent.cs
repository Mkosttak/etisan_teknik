using MediatR;

namespace TeknikServisAPI.Events
{
    public class LowStockEvent : INotification
    {
        public int StockItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinimumStock { get; set; }
    }
}
