/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IShipmentService.cs
 * TİP: Service Interface - Kargo/Sevkiyat Yönetimi
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Sevkiyat takibi, kargo durumu yönetimi
 * SORUMLULUKLAR: Shipment CRUD, status tracking
 * IMPLEMENTATION: ShipmentService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Services
{
    public interface IShipmentService
    {
        Task<List<ShipmentDto>> GetAllShipmentsAsync();
        Task<ShipmentDto?> GetShipmentByIdAsync(int id);
        Task<ShipmentDto> CreateShipmentAsync(CreateShipmentDto dto);
        Task<ShipmentDto?> UpdateShipmentAsync(int id, UpdateShipmentDto dto);
        Task<bool> DeleteShipmentAsync(int id);
        Task<ShipmentDto?> UpdateShipmentStatusAsync(int id, string status);
    }
}





