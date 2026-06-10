/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IStockService.cs
 * TİP: Service Interface - Stok Yönetimi
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Stok takibi, giriş/çıkış hareketleri, düşük stok uyarıları
 * SORUMLULUKLAR: Stock CRUD, movements, low stock alerts
 * IMPLEMENTATION: StockService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Services
{
    public interface IStockService
    {
        Task<List<StockItemDto>> GetAllStockItemsAsync();
        Task<StockItemDto?> GetStockItemByIdAsync(int id);
        Task<List<StockItemDto>> GetLowStockItemsAsync();
        Task<StockItemDto> CreateStockItemAsync(CreateStockItemDto dto);
        Task<StockItemDto?> UpdateStockItemAsync(int id, UpdateStockItemDto dto);
        Task<bool> DeleteStockItemAsync(int id);
        Task<List<StockMovementDto>> GetStockMovementsAsync(int stockItemId);
        Task<StockMovementDto> AddStockMovementAsync(CreateStockMovementDto dto, int userId);
    }
}









