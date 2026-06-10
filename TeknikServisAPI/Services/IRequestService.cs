/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IRequestService.cs
 * TİP: Service Interface - Talep Yönetimi
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Talep (Request) CRUD ve iş akışı yönetimi
 * SORUMLULUKLAR: Talep oluşturma, güncelleme, durum değiştirme, sevkiyat
 * IMPLEMENTATION: RequestService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public interface IRequestService
    {
        Task<List<RequestDto>> GetAllRequestsAsync(int? userId = null, string? userRole = null);
        Task<PagedResult<RequestDto>> GetPagedRequestsAsync(int page, int pageSize, string? status = null, string? priority = null, int? userId = null, string? userRole = null);
        Task<RequestDto?> GetRequestByIdAsync(int id, int? userId = null, string? userRole = null);
        Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, int? createdByUserId = null);
        Task<RequestDto?> UpdateRequestAsync(int id, UpdateRequestDto dto, int? userId = null, string? userRole = null);
        Task<RequestDto?> UpdateRequestStatusAsync(int id, string status, int? userId = null, string? userRole = null);
        Task<RequestDto?> ShipRequestItemsAsync(int requestId, List<int> requestStockItemIds, List<string> extraItems, int? userId = null, string? userRole = null);
        Task<bool> DeleteRequestAsync(int id, int? userId = null, string? userRole = null);
    }
}


