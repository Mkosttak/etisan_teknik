/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IDashboardService.cs
 * TİP: Service Interface - Dashboard İstatistikleri
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Ana sayfa istatistikleri ve son aktiviteler
 * SORUMLULUKLAR: Stats aggregation, recent activities
 * IMPLEMENTATION: DashboardService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Services
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10);
    }
}









