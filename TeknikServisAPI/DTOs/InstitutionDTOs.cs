namespace TeknikServisAPI.DTOs
{
    public class InstitutionListDto
    {
        public string Name { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int FaultCount { get; set; }
        public int ServiceFormCount { get; set; }
        public int WorkItemCount { get; set; }
        public int StockMovementCount { get; set; }
    }

    public class InstitutionSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        
        // Request istatistikleri
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        
        // Fault istatistikleri
        public int TotalFaults { get; set; }
        public int PendingFaults { get; set; }
        public int InProgressFaults { get; set; }
        public int CompletedFaults { get; set; }
        
        // ServiceForm istatistikleri
        public int TotalServiceForms { get; set; }
        public int DraftServiceForms { get; set; }
        public int SubmittedServiceForms { get; set; }
        
        // WorkItem istatistikleri
        public int TotalWorkItems { get; set; }
        public int BacklogWorkItems { get; set; }
        public int DoneWorkItems { get; set; }
        
        // Stok istatistikleri
        public int TotalStockMovements { get; set; }
        public int UniqueItemsUsed { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public class InstitutionStockUsageDto
    {
        public int StockItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int TotalQuantityUsed { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public int UsageCount { get; set; } // Kaç kere kullanıldı
    }
}
