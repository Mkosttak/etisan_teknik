using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;

        private const string DashboardStatsCacheKey = "dashboard:stats";
        private const string DashboardActivitiesCacheKeyPrefix = "dashboard:activities:";
        private static readonly TimeSpan DashboardStatsCacheDuration = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan DashboardActivitiesCacheDuration = TimeSpan.FromSeconds(30);
        private const int MaxActivitiesCount = 50;

        public DashboardService(ApplicationDbContext context, IMemoryCache cache, ILogger<DashboardService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            if (_cache.TryGetValue(DashboardStatsCacheKey, out DashboardStatsDto? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }
            try
            {
                // Veritabanı bağlantı kontrolü
                if (!await _context.Database.CanConnectAsync())
                {
                    return new DashboardStatsDto();
                }

                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var last6Months = now.AddMonths(-6);

                // Temel istatistikler
                var totalDevices = await _context.Devices.CountAsync();
                var totalFaults = await _context.Faults.CountAsync();

                // Bu ay tamamlanan arızalar
                var completedThisMonth = await _context.Faults
                    .CountAsync(f => (f.Status == FaultStatuses.Delivered || f.Status == FaultStatuses.SentToCustomer) && 
                               f.CompletedAt.HasValue && 
                               f.CompletedAt.Value >= startOfMonth);

                var totalFaultsThisMonth = await _context.Faults
                    .CountAsync(f => f.CreatedAt >= startOfMonth);

                // Ortalama tamir süresi (tamamlanan arızalar için)
                var completedFaultsData = await _context.Faults
                    .Where(f => (f.Status == FaultStatuses.Delivered || f.Status == FaultStatuses.SentToCustomer) && f.CompletedAt.HasValue)
                    .Select(f => new { f.CreatedAt, f.CompletedAt })
                    .ToListAsync();

                var avgRepairTime = completedFaultsData.Any()
                    ? completedFaultsData.Average(f => (f.CompletedAt!.Value - f.CreatedAt).TotalDays)
                    : 0;

                // Durum dağılımı - önce veritabanından çek, sonra label'ları uygula
                var statusDistributionRaw = await _context.Faults
                    .GroupBy(f => f.Status)
                    .Select(g => new 
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();
                
                var statusDistribution = statusDistributionRaw.Select(g => new FaultStatusDistribution
                {
                    Status = g.Status,
                    StatusLabel = GetStatusLabel(g.Status),
                    Count = g.Count,
                    Percentage = totalFaults > 0 ? (double)g.Count / totalFaults * 100 : 0
                }).ToList();

                // Son 6 ay trend - basitleştirilmiş sorgu
                var faultsLast6Months = await _context.Faults
                    .Where(f => f.CreatedAt >= last6Months)
                    .ToListAsync();
                
                var monthlyTrends = faultsLast6Months
                    .GroupBy(f => new { f.CreatedAt.Year, f.CreatedAt.Month })
                    .Select(g => new MonthlyFaultTrend
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        TotalFaults = g.Count(),
                        CompletedFaults = g.Count(f => f.Status == FaultStatuses.Delivered || f.Status == FaultStatuses.SentToCustomer),
                        PendingFaults = g.Count(f => f.Status != FaultStatuses.Delivered && f.Status != FaultStatuses.SentToCustomer && f.Status != FaultStatuses.Cancelled)
                    })
                    .OrderBy(m => m.Month)
                    .ToList();

                // Üniversite istatistikleri (Top 10) - basitleştirilmiş sorgu
                var universityDevices = await _context.Devices
                    .Where(d => !string.IsNullOrEmpty(d.University))
                    .ToListAsync();
                
                // Üniversite başına aktif arıza sayısını ayrı bir sorgu ile al
                var faultsByUniversity = await _context.Faults
                    .Where(f => !string.IsNullOrEmpty(f.InstitutionName) && 
                                f.Status != FaultStatuses.Delivered && f.Status != FaultStatuses.SentToCustomer && f.Status != FaultStatuses.Cancelled)
                    .GroupBy(f => f.InstitutionName)
                    .Select(g => new { University = g.Key!, Count = g.Count() })
                    .ToDictionaryAsync(x => x.University, x => x.Count);
                
                var universityStats = universityDevices
                    .GroupBy(d => d.University)
                    .Select(g => new UniversityStats
                    {
                        University = g.Key ?? "Bilinmiyor",
                        TotalDevices = g.Count(),
                        ActiveFaults = (g.Key != null && faultsByUniversity.ContainsKey(g.Key)) 
                            ? faultsByUniversity[g.Key] 
                            : 0
                    })
                    .OrderByDescending(u => u.TotalDevices)
                    .Take(10)
                    .ToList();

                // Bekleyen Kargoları Hesapla (Requests ve Faults'u adres bazlı grupla)
                var pendingRequests = await _context.Requests
                    .Where(r => r.Status == RequestStatuses.Pending || r.Status == RequestStatuses.InProgress)
                    .Select(r => new { r.ShippingCity, r.ShippingDistrict, r.ShippingAddressLine, r.ContactPerson })
                    .ToListAsync();

                var pendingFaults = await _context.Faults
                    .Where(f => f.Status == FaultStatuses.ShippingToCustomer)
                    .Select(f => new { f.ShippingCity, f.ShippingDistrict, f.ShippingAddressLine, f.ContactPerson })
                    .ToListAsync();

                var allPendingItems = pendingRequests.Concat(pendingFaults).ToList();

                // Adres ve Kişiye göre grupla (Unique durak sayısı)
                var distinctPackages = allPendingItems
                    .GroupBy(x => new {
                        City = x.ShippingCity?.Trim().ToLower(),
                        District = x.ShippingDistrict?.Trim().ToLower(),
                        Address = x.ShippingAddressLine?.Trim().ToLower(),
                        Person = x.ContactPerson?.Trim().ToLower()
                    })
                    .ToList();

                var totalPackages = distinctPackages.Count;

                var ankaraDeliveries = distinctPackages
                    .Count(g => g.Key.City != null && g.Key.City.Contains("ankara"));

                // Verifone'a gönderilecek cihaz sayısı
                var verifoneShipments = await _context.Faults
                    .CountAsync(f => f.Status == FaultStatuses.ShippingToManufacturer);

                var stats = new DashboardStatsDto
                {
                    TotalDevices = totalDevices,
                    TotalDevicesInRepair = await _context.Devices
                        .CountAsync(d => d.Status == DeviceStatuses.InRepair),
                    
                    TotalDevicesAtManufacturer = await _context.Devices
                        .CountAsync(d => d.Status == DeviceStatuses.AtManufacturer),
                    
                    TotalPendingFaults = await _context.Faults
                        .CountAsync(f => f.Status == FaultStatuses.Pending || f.Status == FaultStatuses.InProgress || 
                                   f.Status == FaultStatuses.Created || f.Status == FaultStatuses.ReceivedByTechnician ||
                                   f.Status == FaultStatuses.ShippingToManufacturer || f.Status == FaultStatuses.SentToService ||
                                   f.Status == FaultStatuses.ShippingToCustomer),
                    
                    TotalActiveCustomers = 0,
                    
                    
                    TotalPendingShipments = totalPackages,
                    TotalPendingPackages = totalPackages,
                    PendingAnkaraDeliveries = ankaraDeliveries,
                    PendingVerifoneShipments = verifoneShipments,
                    
                    LowStockItemsCount = await _context.StockItems
                        .CountAsync(s => s.CurrentStock <= s.MinimumStock && s.IsActive),
                    
                    TotalRequests = await _context.Requests
                        .CountAsync(r => r.Status == RequestStatuses.Pending || r.Status == RequestStatuses.InProgress),

                    // Yeni istatistikler
                    TotalCompletedFaultsThisMonth = completedThisMonth,
                    TotalFaultsThisMonth = totalFaultsThisMonth,
                    AverageRepairTimeInDays = Math.Round(avgRepairTime, 1),
                    FaultStatusDistribution = statusDistribution,
                    MonthlyFaultTrends = monthlyTrends,
                    TopUniversities = universityStats
                };

                _cache.Set(DashboardStatsCacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DashboardStatsCacheDuration
                });

                return stats;
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama ve boş stats döndür
                _logger.LogError(ex, "Dashboard stats error");
                return new DashboardStatsDto();
            }
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10)
        {
            count = Math.Clamp(count, 1, MaxActivitiesCount);
            var cacheKey = $"{DashboardActivitiesCacheKeyPrefix}{count}";
            if (_cache.TryGetValue(cacheKey, out List<RecentActivityDto>? cachedActivities))
            {
                return cachedActivities ?? new List<RecentActivityDto>();
            }
            var activities = new List<RecentActivityDto>();

            // Son arıza kayıtları
            var faults = await _context.Faults
                .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                .Take(count)
                .ToListAsync();
            
            var recentFaults = faults.Select(f => new RecentActivityDto
            {
                Id = f.Id,
                Type = "fault",
                Title = $"Arıza: {f.DeviceSerialNumber ?? "Bilinmiyor"}",
                Description = f.Description ?? "",
                Status = f.Status,
                CreatedAt = f.UpdatedAt ?? f.CreatedAt,
                Icon = "tool"
            }).ToList();

            // Son talepler
            var recentRequests = await _context.Requests
                .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                .Take(count)
                .Select(r => new RecentActivityDto
                {
                    Id = r.Id,
                    Type = "request",
                    Title = "Talep",
                    Description = r.Description ?? "",
                    Status = r.Status,
                    CreatedAt = r.UpdatedAt ?? r.CreatedAt,
                    Icon = "file-text"
                })
                .ToListAsync();

            // Son kargolar
            var recentShipments = await _context.Shipments
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .Select(s => new RecentActivityDto
                {
                    Id = s.Id,
                    Type = "shipment",
                    Title = $"Kargo: {s.TrackingNumber ?? "Takip No Yok"}",
                    Description = s.CargoCompany ?? s.Notes ?? "",
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    Icon = "car"
                })
                .ToListAsync();

            activities.AddRange(recentFaults);
            activities.AddRange(recentRequests);
            activities.AddRange(recentShipments);

            var result = activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DashboardActivitiesCacheDuration
            });

            return result;
        }

        private static string GetStatusLabel(string status)
        {
            return status switch
            {
                FaultStatuses.Created => "Oluşturuldu",
                FaultStatuses.ReceivedByTechnician => "Teknikde",
                FaultStatuses.ShippingToManufacturer => "Verifone'a Gönderilecek",
                FaultStatuses.SentToService => "Verifone'a Gönderildi",
                FaultStatuses.ShippingToCustomer => "Müşteriye Gönderilecek",
                FaultStatuses.SentToCustomer => "Müşteriye Gönderildi",
                FaultStatuses.Delivered => "Teslim Edildi",
                FaultStatuses.Cancelled => "İptal Edildi",
                _ => status
            };
        }
    }
}
