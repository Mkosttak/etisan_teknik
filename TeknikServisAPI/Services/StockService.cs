using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Exceptions;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public StockService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<StockItemDto>> GetAllStockItemsAsync()
        {
            return await _context.StockItems
                .Where(s => s.IsActive)
                .Select(s => new StockItemDto
                {
                    Id = s.Id,
                    ItemName = s.ItemName,
                    SerialNumber = s.SerialNumber,
                    Description = s.Description,
                    CurrentStock = s.CurrentStock,
                    MinimumStock = s.MinimumStock,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<StockItemDto?> GetStockItemByIdAsync(int id)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null || !item.IsActive) return null;

            return new StockItemDto
            {
                Id = item.Id,
                ItemName = item.ItemName,
                SerialNumber = item.SerialNumber,
                Description = item.Description,
                CurrentStock = item.CurrentStock,
                MinimumStock = item.MinimumStock,
                IsActive = item.IsActive
            };
        }

        public async Task<List<StockItemDto>> GetLowStockItemsAsync()
        {
            return await _context.StockItems
                .Where(s => s.CurrentStock <= s.MinimumStock && s.IsActive)
                .Select(s => new StockItemDto
                {
                    Id = s.Id,
                    ItemName = s.ItemName,
                    SerialNumber = s.SerialNumber,
                    Description = s.Description,
                    CurrentStock = s.CurrentStock,
                    MinimumStock = s.MinimumStock,
                    IsActive = s.IsActive
                })
                .ToListAsync();
        }

        public async Task<StockItemDto> CreateStockItemAsync(CreateStockItemDto dto)
        {
            var item = new StockItem
            {
                ItemName = dto.ItemName,
                SerialNumber = dto.SerialNumber,
                Description = dto.Description,
                CurrentStock = dto.CurrentStock,
                MinimumStock = dto.MinimumStock,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockItems.Add(item);
            await _context.SaveChangesAsync();

            return (await GetStockItemByIdAsync(item.Id))!;
        }

        public async Task<StockItemDto?> UpdateStockItemAsync(int id, UpdateStockItemDto dto)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return null;

            if (!string.IsNullOrEmpty(dto.ItemName))
                item.ItemName = dto.ItemName;
            if (dto.SerialNumber != null)
                item.SerialNumber = dto.SerialNumber;
            if (dto.Description != null)
                item.Description = dto.Description;
            if (dto.CurrentStock.HasValue)
                item.CurrentStock = dto.CurrentStock.Value;
            if (dto.MinimumStock.HasValue)
                item.MinimumStock = dto.MinimumStock.Value;
            if (dto.IsActive.HasValue)
                item.IsActive = dto.IsActive.Value;

            item.UpdatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Kritik stok kontrolü
            if (item.IsActive && item.CurrentStock <= item.MinimumStock) 
            {
               await SendLowStockNotification(item);
            }

            return await GetStockItemByIdAsync(id);
        }

        public async Task<bool> DeleteStockItemAsync(int id)
        {
            var item = await _context.StockItems.FindAsync(id);
            if (item == null) return false;

            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<StockMovementDto>> GetStockMovementsAsync(int stockItemId)
        {
            return await _context.StockMovements
                .Include(m => m.StockItem)
                .Include(m => m.User)
                .Where(m => m.StockItemId == stockItemId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id,
                    StockItemId = m.StockItemId,
                    StockItemName = m.StockItem.ItemName,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    UserId = m.UserId,
                    UserName = m.User != null ? $"{m.User.FirstName} {m.User.LastName}" : null,
                    Reference = m.Reference,
                    InstitutionName = m.InstitutionName,
                    Notes = m.Notes ?? m.Reason,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<StockMovementDto> AddStockMovementAsync(CreateStockMovementDto dto, int userId)
        {
            var item = await _context.StockItems.FindAsync(dto.StockItemId);
            if (item == null)
                throw new NotFoundException("Stok ürünü bulunamadı");

            // Stok miktarını güncelle
            if (dto.MovementType == StockMovementTypes.In)
                item.CurrentStock += dto.Quantity;
            else if (dto.MovementType == StockMovementTypes.Out)
            {
                // Negatif stoklara izin ver
                item.CurrentStock -= dto.Quantity;
            }
            else if (dto.MovementType == StockMovementTypes.Adjustment)
                item.CurrentStock = dto.Quantity;

            var movement = new StockMovement
            {
                StockItemId = dto.StockItemId,
                MovementType = dto.MovementType,
                Quantity = dto.Quantity,
                UserId = userId,
                Reference = dto.Reference,
                InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName))
                    ? dto.InstitutionOtherName
                    : dto.InstitutionName,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            // Kritik stok kontrolü (Sadece çıkış veya düşüş olduysa)
            bool isDecrease = dto.MovementType == StockMovementTypes.Out || 
                             (dto.MovementType == StockMovementTypes.Adjustment && dto.Quantity <= item.CurrentStock) || // Adjustment: yeni miktar eskisinden azsa? Hayır, dto.Quantity direk yeni miktar oluyor adjustment ise. 
                             (dto.MovementType == StockMovementTypes.In && dto.Quantity < 0); // In negatif gelmez ama önlem.

            if (item.IsActive && item.CurrentStock <= item.MinimumStock)
            {
                // Her hareket sonrası kontrol et, çünkü kritik seviyeye yeni düşmüş olabilir
                await SendLowStockNotification(item);
            }

            return (await _context.StockMovements
                .Include(m => m.StockItem)
                .Include(m => m.User)
                .Where(m => m.Id == movement.Id)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id,
                    StockItemId = m.StockItemId,
                    StockItemName = m.StockItem.ItemName,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    UserId = m.UserId,
                    UserName = m.User != null ? $"{m.User.FirstName} {m.User.LastName}" : null,
                    Reference = m.Reference,
                    InstitutionName = m.InstitutionName,
                    Notes = m.Notes ?? m.Reason,
                    CreatedAt = m.CreatedAt
                })
                .FirstAsync())!;
        }

        private async Task SendLowStockNotification(StockItem item)
        {
            try
            {
                // Bu ürün için son 24 saatte gönderilmiş oknunmamış bildirim var mı? (Spam önleme)
                // Bu özellik şu an NotificationService'de yok, o yüzden basitçe gönderelim. 
                // İlerde spam kontrolü eklenebilir.

                await _notificationService.CreateNotificationAsync(
                    title: "Kritik Stok Seviyesi",
                    message: $"{item.ItemName} stok seviyesi kritik düzeyde! Mevcut: {item.CurrentStock}, Min: {item.MinimumStock}",
                    type: "Stock",
                    link: "/stock",
                    relatedId: item.Id,
                    relatedType: "StockItem",
                    userId: null,
                    targetRole: AppRoles.Teknik
                );
            }
            catch
            {
                // Bildirim hatası akışı bozmasın
            }
        }
    }
}




