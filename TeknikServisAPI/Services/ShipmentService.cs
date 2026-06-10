using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly ApplicationDbContext _context;

        public ShipmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ShipmentDto>> GetAllShipmentsAsync()
        {
            return await _context.Shipments
                .Include(s => s.Items)
                .Select(s => new ShipmentDto
                {
                    Id = s.Id,
                    ShipmentType = s.ShipmentType,
                    Status = s.Status,
                    TrackingNumber = s.TrackingNumber,
                    CargoCompany = s.CargoCompany,
                    Notes = s.Notes,
                    CreatedAt = s.CreatedAt,
                    ShippedAt = s.ShippedAt,
                    DeliveredAt = s.DeliveredAt,
                    Items = s.Items.Select(i => new ShipmentItemDto
                    {
                        Id = i.Id,
                        ItemType = i.ItemType,
                        DeviceId = i.DeviceId,
                        StockItemId = i.StockItemId,
                        Description = i.Description,
                        Quantity = i.Quantity,
                        SerialNumber = i.SerialNumber
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<ShipmentDto?> GetShipmentByIdAsync(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null) return null;

            return new ShipmentDto
            {
                Id = shipment.Id,
                ShipmentType = shipment.ShipmentType,
                Status = shipment.Status,
                TrackingNumber = shipment.TrackingNumber,
                CargoCompany = shipment.CargoCompany,
                Notes = shipment.Notes,
                CreatedAt = shipment.CreatedAt,
                ShippedAt = shipment.ShippedAt,
                DeliveredAt = shipment.DeliveredAt,
                Items = shipment.Items.Select(i => new ShipmentItemDto
                {
                    Id = i.Id,
                    ItemType = i.ItemType,
                    DeviceId = i.DeviceId,
                    StockItemId = i.StockItemId,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    SerialNumber = i.SerialNumber
                }).ToList()
            };
        }

        public async Task<ShipmentDto> CreateShipmentAsync(CreateShipmentDto dto)
        {
            var shipment = new Shipment
            {
                ShipmentType = dto.ShipmentType,
                Status = ShipmentStatuses.Preparing,
                TrackingNumber = dto.TrackingNumber,
                CargoCompany = dto.CargoCompany,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            // Gönderim öğelerini ekle
            foreach (var itemDto in dto.Items)
            {
                var item = new ShipmentItem
                {
                    ShipmentId = shipment.Id,
                    ItemType = itemDto.ItemType,
                    DeviceId = itemDto.DeviceId,
                    StockItemId = itemDto.StockItemId,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    SerialNumber = itemDto.SerialNumber
                };
                _context.ShipmentItems.Add(item);
            }

            await _context.SaveChangesAsync();

            return (await GetShipmentByIdAsync(shipment.Id))!;
        }

        public async Task<ShipmentDto?> UpdateShipmentAsync(int id, UpdateShipmentDto dto)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return null;

            if (!string.IsNullOrEmpty(dto.Status))
            {
                shipment.Status = dto.Status;
                if (dto.Status == ShipmentStatuses.Shipped)
                    shipment.ShippedAt = DateTime.UtcNow;
                else if (dto.Status == ShipmentStatuses.Delivered)
                    shipment.DeliveredAt = DateTime.UtcNow;
            }
            if (dto.TrackingNumber != null)
                shipment.TrackingNumber = dto.TrackingNumber;
            if (dto.CargoCompany != null)
                shipment.CargoCompany = dto.CargoCompany;
            if (dto.Notes != null)
                shipment.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            return await GetShipmentByIdAsync(id);
        }

        public async Task<bool> DeleteShipmentAsync(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return false;

            shipment.Status = ShipmentStatuses.Cancelled;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ShipmentDto?> UpdateShipmentStatusAsync(int id, string status)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return null;

            shipment.Status = status;
            if (status == ShipmentStatuses.Shipped)
                shipment.ShippedAt = DateTime.UtcNow;
            else if (status == ShipmentStatuses.Delivered)
                shipment.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetShipmentByIdAsync(id);
        }
    }
}





