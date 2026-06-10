/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IInstitutionService.cs
 * TİP: Service Interface - Kurum Bazlı Veri Toplama
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Kuruma ait tüm verileri (talep, arıza, form, vb.) toplama
 * SORUMLULUKLAR: Institution-based data aggregation
 * IMPLEMENTATION: InstitutionService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Services
{
    public interface IInstitutionService
    {
        Task<InstitutionSummaryDto> GetSummaryAsync(string institutionName);
        Task<List<RequestDto>> GetRequestsAsync(string institutionName, int page = 1, int pageSize = 50);
        Task<List<FaultDto>> GetFaultsAsync(string institutionName, int page = 1, int pageSize = 50);
        Task<List<ServiceFormDto>> GetServiceFormsAsync(string institutionName, int page = 1, int pageSize = 50);
        Task<List<WorkItemDto>> GetWorkItemsAsync(string institutionName, int page = 1, int pageSize = 50);
        Task<List<StockMovementDto>> GetStockMovementsAsync(string institutionName, int page = 1, int pageSize = 50);
    }
}
