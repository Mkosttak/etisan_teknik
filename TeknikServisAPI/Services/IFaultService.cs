/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IFaultService.cs
 * TİP: Service Interface - Arıza Yönetimi
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Arıza (Fault) CRUD, durum takibi, geçmiş yönetimi
 * SORUMLULUKLAR: Arıza lifecycle, status updates, history, comments
 * IMPLEMENTATION: FaultService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public interface IFaultService
    {
        Task<List<FaultDto>> GetAllFaultsAsync(int? userId = null, string? userRole = null);
        Task<PagedResult<FaultDto>> GetPagedFaultsAsync(FaultFilterParams filterParams, int? userId = null, string? userRole = null);
        Task<FaultDto?> GetFaultByIdAsync(int id, int? userId = null, string? userRole = null);
        Task<FaultDto> CreateFaultAsync(CreateFaultDto dto, int createdByUserId);
        Task<FaultDto?> UpdateFaultAsync(int id, UpdateFaultDto dto, int updatedByUserId, string? userRole = null);
        Task<bool> DeleteFaultAsync(int id, int? userId = null, string? userRole = null);
        Task<List<FaultHistoryDto>> GetFaultHistoryAsync(int faultId);
        Task<FaultDto?> UpdateFaultStatusAsync(int id, string status, string? notes, string? errorCode, int userId, string? userRole = null);
        Task<FaultHistoryDto> AddFaultCommentAsync(int id, string notes, int userId, string? userRole = null);
        Task UpdateBackupDeviceRequestIdAsync(int faultId, int requestId);
    }
}





