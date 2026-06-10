namespace TeknikServisAPI.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalDevices { get; set; }
        public int TotalDevicesInRepair { get; set; }
        public int TotalDevicesAtManufacturer { get; set; }
        public int TotalPendingFaults { get; set; }
        public int TotalActiveCustomers { get; set; }
        public int TotalPendingShipments { get; set; }
        public int TotalPendingPackages { get; set; }
        public int PendingAnkaraDeliveries { get; set; }
        public int PendingVerifoneShipments { get; set; }
        public int LowStockItemsCount { get; set; }
        public int TotalRequests { get; set; }
        
        // Yeni istatistikler
        public int TotalCompletedFaultsThisMonth { get; set; }
        public int TotalFaultsThisMonth { get; set; }
        public double AverageRepairTimeInDays { get; set; }
        public List<FaultStatusDistribution> FaultStatusDistribution { get; set; } = new();
        public List<MonthlyFaultTrend> MonthlyFaultTrends { get; set; } = new();
        public List<UniversityStats> TopUniversities { get; set; } = new();
    }

    public class FaultStatusDistribution
    {
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class MonthlyFaultTrend
    {
        public string Month { get; set; } = string.Empty;
        public int TotalFaults { get; set; }
        public int CompletedFaults { get; set; }
        public int PendingFaults { get; set; }
    }

    public class UniversityStats
    {
        public string University { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int ActiveFaults { get; set; }
    }

    public class RecentActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Icon { get; set; } = string.Empty;
    }
}





