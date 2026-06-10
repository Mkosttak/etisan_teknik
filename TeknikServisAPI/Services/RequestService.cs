using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MediatR;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Events;
using TeknikServisAPI.Exceptions;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class RequestService : IRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RequestService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IMediator _mediator;

        public RequestService(
            ApplicationDbContext context, 
            ILogger<RequestService> logger, 
            INotificationService notificationService, 
            IAuditLogService auditLogService,
            IMediator mediator)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
            _mediator = mediator;
        }

        public async Task<List<RequestDto>> GetAllRequestsAsync(int? userId = null, string? userRole = null)
        {
            var query = _context.Requests
                .AsNoTracking()
                .Include(r => r.RequestStockItems)
                    .ThenInclude(rsi => rsi.StockItem)
                .AsQueryable();

            // SahaElemani rolü için sadece kendi kayıtlarını göster
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                query = query.Where(r => r.CreatedByUserId == userId.Value);
            }

            return await query
                .OrderBy(r => r.Status == RequestStatuses.Cancelled ? 4 :
                              r.Status == RequestStatuses.Completed ? 3 :
                              r.IsUrgent ? 1 : 2)
                .ThenByDescending(r => r.CreatedAt)
                .Select(r => new RequestDto
                {
                    Id = r.Id,
                    DeviceSerialNumber = r.DeviceSerialNumber,
                    Description = r.Description,
                    InstitutionName = (r.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(r.InstitutionOtherName)) ? r.InstitutionOtherName : r.InstitutionName,
                    InstitutionOtherName = r.InstitutionOtherName,
                    Status = r.Status,
                    ContactPerson = r.ContactPerson,
                    ContactPhone = r.ContactPhone,
                    ContactEmail = r.ContactEmail,
                    ShippingAddressLine = r.ShippingAddressLine,
                    ShippingCity = r.ShippingCity,
                    ShippingDistrict = r.ShippingDistrict,
                    IsUrgent = r.IsUrgent,
                    ExtraItems = r.ExtraItems,
                    ShippedExtraItems = r.ShippedExtraItems,
                    StockItems = r.RequestStockItems.Select(rsi => new RequestStockItemDto
                    {
                        Id = rsi.Id,
                        StockItemId = rsi.StockItemId,
                        ItemName = rsi.StockItem.ItemName,
                        Quantity = rsi.Quantity,
                        IsDeducted = rsi.IsDeducted,
                        IsCancelled = rsi.IsCancelled,
                        IsShipped = rsi.IsShipped
                    }).ToList(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CompletedAt = r.CompletedAt,
                    CreatedByUserId = r.CreatedByUserId
                })
                .ToListAsync();
        }

        /// <summary>
        /// Sayfalanmış talep listesi döndürür (server-side pagination)
        /// ⭐ PERFORMANCE: Client-side filtreleme yerine server-side pagination kullanın
        /// </summary>
        public async Task<PagedResult<RequestDto>> GetPagedRequestsAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? priority = null,
            int? userId = null, 
            string? userRole = null)
        {
            // ⭐ SECURITY: Max page size kontrolü (DoS koruması)
            const int maxPageSize = 100;
            pageSize = Math.Min(pageSize, maxPageSize);
            page = Math.Max(1, page);

            var query = _context.Requests
                .AsNoTracking()
                .Include(r => r.RequestStockItems)
                    .ThenInclude(rsi => rsi.StockItem)
                .AsQueryable();

            // SahaElemani rolü için sadece kendi kayıtlarını göster
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                query = query.Where(r => r.CreatedByUserId == userId.Value);
            }

            // Filtreler
            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            if (!string.IsNullOrEmpty(priority))
            {
                if (priority.Equals("urgent", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.IsUrgent);
                else if (priority.Equals("normal", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => !r.IsUrgent);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Status == RequestStatuses.Cancelled ? 4 :
                              r.Status == RequestStatuses.Completed ? 3 :
                              r.IsUrgent ? 1 : 2)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RequestDto
                {
                    Id = r.Id,
                    DeviceSerialNumber = r.DeviceSerialNumber,
                    Description = r.Description,
                    InstitutionName = (r.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(r.InstitutionOtherName)) ? r.InstitutionOtherName : r.InstitutionName,
                    InstitutionOtherName = r.InstitutionOtherName,
                    Status = r.Status,
                    ContactPerson = r.ContactPerson,
                    ContactPhone = r.ContactPhone,
                    ContactEmail = r.ContactEmail,
                    ShippingAddressLine = r.ShippingAddressLine,
                    ShippingCity = r.ShippingCity,
                    ShippingDistrict = r.ShippingDistrict,
                    IsUrgent = r.IsUrgent,
                    ExtraItems = r.ExtraItems,
                    ShippedExtraItems = r.ShippedExtraItems,
                    StockItems = r.RequestStockItems.Select(rsi => new RequestStockItemDto
                    {
                        Id = rsi.Id,
                        StockItemId = rsi.StockItemId,
                        ItemName = rsi.StockItem.ItemName,
                        Quantity = rsi.Quantity,
                        IsDeducted = rsi.IsDeducted,
                        IsCancelled = rsi.IsCancelled,
                        IsShipped = rsi.IsShipped
                    }).ToList(),
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    CompletedAt = r.CompletedAt,
                    CreatedByUserId = r.CreatedByUserId
                })
                .ToListAsync();

            return new PagedResult<RequestDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<RequestDto?> GetRequestByIdAsync(int id, int? userId = null, string? userRole = null)
            {
                var query = _context.Requests
                .Include(r => r.RequestStockItems)
                    .ThenInclude(rsi => rsi.StockItem)
                .AsQueryable();

                // SahaElemani rolü için sadece kendi kayıtlarını göster
                if (userRole == AppRoles.SahaElemani && userId.HasValue)
                {
                    query = query.Where(r => r.CreatedByUserId == userId.Value);
                }

                var request = await query.FirstOrDefaultAsync(r => r.Id == id);

                if (request == null) return null;

                return new RequestDto
                {
                    Id = request.Id,
                DeviceSerialNumber = request.DeviceSerialNumber,
                    Description = request.Description,
                    InstitutionName = (request.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(request.InstitutionOtherName)) ? request.InstitutionOtherName : request.InstitutionName,
                    InstitutionOtherName = request.InstitutionOtherName,
                    Status = request.Status,
                    ContactPerson = request.ContactPerson,
                    ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                    ShippingAddressLine = request.ShippingAddressLine,
                    ShippingCity = request.ShippingCity,
                    ShippingDistrict = request.ShippingDistrict,
                IsUrgent = request.IsUrgent,
                ExtraItems = request.ExtraItems,
                ShippedExtraItems = request.ShippedExtraItems,
                StockItems = request.RequestStockItems.Select(rsi => new RequestStockItemDto
                {
                    Id = rsi.Id,
                    StockItemId = rsi.StockItemId,
                    ItemName = rsi.StockItem.ItemName,
                    Quantity = rsi.Quantity,
                    IsDeducted = rsi.IsDeducted,
                    IsCancelled = rsi.IsCancelled,
                    IsShipped = rsi.IsShipped
                }).ToList(),
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt,
                CompletedAt = request.CompletedAt,
                CreatedByUserId = request.CreatedByUserId
                };
        }

        public async Task<RequestDto> CreateRequestAsync(CreateRequestDto dto, int? createdByUserId = null)
        {
            // Transaction başlat - tüm işlemler atomik olmalı
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Request oluştur
                var request = new Request
                {
                    DeviceSerialNumber = dto.DeviceSerialNumber,
                    Description = dto.Description,
                    InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName)) ? dto.InstitutionOtherName : dto.InstitutionName,
                    InstitutionOtherName = dto.InstitutionOtherName,
                    Status = RequestStatuses.Pending,
                    ContactPerson = dto.ContactPerson,
                    ContactPhone = dto.ContactPhone,
                    ContactEmail = dto.ContactEmail,
                    ShippingAddressLine = dto.ShippingAddressLine,
                    ShippingCity = dto.ShippingCity,
                    ShippingDistrict = dto.ShippingDistrict,
                    IsUrgent = dto.IsUrgent,
                    ExtraItems = dto.ExtraItems,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();
                await _auditLogService.AddAuditEventAsync("Request", request.Id, createdByUserId, "Created", "Request created");
                await _context.SaveChangesAsync();

                // 2. Stok kalemlerini işle
                if (dto.StockItems != null && dto.StockItems.Any())
                {
                    foreach (var stockItemDto in dto.StockItems)
                    {
                        // Stok kontrolü
                        var stockItem = await _context.StockItems.FindAsync(stockItemDto.StockItemId);
                        if (stockItem == null)
                        {
                            throw new NotFoundException($"Stok kalemi bulunamadı: ID {stockItemDto.StockItemId}");
                        }

                        // 3. RequestStockItem ilişkisi oluştur
                        var requestStockItem = new RequestStockItem
                        {
                            RequestId = request.Id,
                            StockItemId = stockItemDto.StockItemId,
                            Quantity = stockItemDto.Quantity,
                            IsDeducted = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.RequestStockItems.Add(requestStockItem);

                        // 4. Stoktan düş
                        stockItem.CurrentStock -= stockItemDto.Quantity;
                        stockItem.UpdatedAt = DateTime.UtcNow;

                        // 5. Stok hareketi kaydet
                        StockMovement stockMovement = new StockMovement
                        {
                            StockItemId = stockItemDto.StockItemId,
                            MovementType = StockMovementTypes.Out,
                            Quantity = stockItemDto.Quantity,
                            Reference = $"Talep #{request.Id}",
                            InstitutionName = request.InstitutionName ?? "Bilinmeyen kurum",
                            Notes = "Talep oluşturuldu",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.StockMovements.Add(stockMovement);

                        // Kritik stok kontrolü
                        if (stockItem.IsActive && stockItem.CurrentStock <= stockItem.MinimumStock)
                        {
                            await SendLowStockNotification(stockItem);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                // Transaction'ı onayla
                await transaction.CommitAsync();

                // Event publish - bildirim gönder
                await _mediator.Publish(new RequestCreatedEvent
                {
                    RequestId = request.Id,
                    InstitutionName = request.InstitutionName ?? "Bilinmeyen Kurum",
                    DeviceSerialNumber = request.DeviceSerialNumber ?? "",
                    Description = request.Description ?? "",
                    IsUrgent = request.IsUrgent
                });

                // Eğer hiç ürün yoksa (ne stok ürünü ne de ekstra ürün), otomatik iptal et
                var hasStockItems = dto.StockItems != null && dto.StockItems.Any();
                var hasExtraItems = !string.IsNullOrWhiteSpace(dto.ExtraItems);
                
                if (!hasStockItems && !hasExtraItems)
                {
                    request.Status = RequestStatuses.Cancelled;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Talep otomatik iptal edildi (ürün yok): #{RequestId}", request.Id);
                }

                return (await GetRequestByIdAsync(request.Id, null, null))!;
            }
            catch (Exception ex)
            {
                // Hata durumunda rollback
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Talep oluşturulurken hata: {Message}", ex.Message);
                throw new BusinessValidationException($"Talep oluşturulamadı: {ex.Message}", ex);
            }
        }



        public async Task<RequestDto?> UpdateRequestAsync(int id, UpdateRequestDto dto, int? userId = null, string? userRole = null)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null)
            {
                _logger.LogWarning("Talep bulunamadı: #{RequestId}", id);
                return null;
            }

            // SahaElemani rolü için sadece kendi kayıtlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                if (request.CreatedByUserId != userId.Value)
                {
                    _logger.LogWarning("SahaElemani başkasının talebini güncellemeye çalıştı: RequestId={RequestId}, UserId={UserId}", id, userId);
                    return null;
                }
            }

            // Tüm alanları güncelle (null değilse)
            var oldDeviceSerialNumber = request.DeviceSerialNumber;
            var oldDescription = request.Description;
            var oldInstitutionName = request.InstitutionName;
            var oldInstitutionOtherName = request.InstitutionOtherName;
            var oldStatus = request.Status;
            var oldContactPerson = request.ContactPerson;
            var oldContactPhone = request.ContactPhone;
            var oldContactEmail = request.ContactEmail;
            var oldShippingAddressLine = request.ShippingAddressLine;
            var oldShippingCity = request.ShippingCity;
            var oldShippingDistrict = request.ShippingDistrict;
            var oldIsUrgent = request.IsUrgent;
            var oldExtraItems = request.ExtraItems;
            var oldCompletedAt = request.CompletedAt;

            if (dto.DeviceSerialNumber != null)
                request.DeviceSerialNumber = dto.DeviceSerialNumber;

            if (dto.Description != null)
                    request.Description = dto.Description;

                if (dto.InstitutionName != null)
                {
                    request.InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName)) 
                        ? dto.InstitutionOtherName 
                        : dto.InstitutionName;
                }

                if (dto.InstitutionOtherName != null)
                    request.InstitutionOtherName = dto.InstitutionOtherName;

                if (!string.IsNullOrEmpty(dto.Status))
                    request.Status = dto.Status;

                if (dto.ContactPerson != null)
                    request.ContactPerson = dto.ContactPerson;

                if (dto.ContactPhone != null)
                    request.ContactPhone = dto.ContactPhone;

            if (dto.ContactEmail != null)
                request.ContactEmail = dto.ContactEmail;

                if (dto.ShippingAddressLine != null)
                    request.ShippingAddressLine = dto.ShippingAddressLine;

                if (dto.ShippingCity != null)
                    request.ShippingCity = dto.ShippingCity;

                if (dto.ShippingDistrict != null)
                    request.ShippingDistrict = dto.ShippingDistrict;

                if (dto.IsUrgent.HasValue)
                    request.IsUrgent = dto.IsUrgent.Value;

            // ExtraItems için özel kontrol (boş string de geçerli)
            if (dto.ExtraItems != null)
                request.ExtraItems = string.IsNullOrWhiteSpace(dto.ExtraItems) ? null : dto.ExtraItems;

            // İptal edilen stok kalemlerini işaretle
            if (dto.CancelledStockItemIds != null && dto.CancelledStockItemIds.Any())
            {
                var itemsToCancel = await _context.RequestStockItems
                    .Include(rsi => rsi.StockItem)
                    .Where(rsi => rsi.RequestId == id && dto.CancelledStockItemIds.Contains(rsi.Id))
                    .ToListAsync();

                foreach (var item in itemsToCancel)
                {
                    if (!item.IsCancelled && item.IsDeducted && item.StockItem != null)
                    {
                        // İptal edilen ürünün stokunu geri ekle
                        item.StockItem.CurrentStock += item.Quantity;
                        item.StockItem.UpdatedAt = DateTime.UtcNow;

                        // Geri ekleme stok hareketi kaydet
                        var returnMovement = new StockMovement
                        {
                            StockItemId = item.StockItemId,
                            MovementType = StockMovementTypes.In,
                            Quantity = item.Quantity,
                            Reference = $"Talep #{id} (İptal)",
                            InstitutionName = request.InstitutionName ?? "Bilinmeyen kurum",
                            Notes = $"Talep ürünü iptal edildi. Geri eklenen: {item.Quantity}",
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.StockMovements.Add(returnMovement);
                    }
                    
                    item.IsCancelled = true;
                }
            }

            // İptal edilen ekstra urunleri isaretle (ExtraItems string'inden cikar)
            if (dto.CancelledExtraItemIndexes != null && dto.CancelledExtraItemIndexes.Any() && !string.IsNullOrEmpty(request.ExtraItems))
            {
                var separator = new[] { ", " };
                var extraItems = request.ExtraItems.Split(separator, StringSplitOptions.None).ToList();
                var itemsToRemove = dto.CancelledExtraItemIndexes.OrderByDescending(i => i).ToList();
                
                foreach (var index in itemsToRemove)
                {
                    if (index >= 0 && index < extraItems.Count)
                    {
                        extraItems.RemoveAt(index);
                    }
                }

                request.ExtraItems = extraItems.Any() ? string.Join(", ", extraItems) : null;
            }

            // Stok kalemlerini güncelle (sadece gönderildiyse)
            if (dto.StockItems != null)
            {
                // Transaction başlat
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // Mevcut stok kalemlerini al (iptal edilenler hariç)
                    var existingStockItems = await _context.RequestStockItems
                        .Where(rsi => rsi.RequestId == id && !rsi.IsCancelled)
                        .ToListAsync();

                    // Mevcut stok kalemlerini stok kalemi ID'sine göre grupla
                    var existingItemsDict = existingStockItems
                        .Where(item => item.IsDeducted)
                        .GroupBy(item => item.StockItemId)
                        .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

                    // Yeni stok kalemlerini stok kalemi ID'sine göre grupla
                    var newItemsDict = dto.StockItems
                        .GroupBy(item => item.StockItemId)
                        .ToDictionary(g => g.Key, g => g.Sum(item => item.Quantity));

                    // Tüm stok kalemi ID'lerini birleştir
                    var allStockItemIds = existingItemsDict.Keys.Union(newItemsDict.Keys).ToList();

                    // Her stok kalemi için net değişikliği hesapla ve işle
                    foreach (var stockItemId in allStockItemIds)
                    {
                        var stockItem = await _context.StockItems.FindAsync(stockItemId);
                        if (stockItem == null)
                        {
                            throw new NotFoundException($"Stok kalemi bulunamadı: ID {stockItemId}");
                        }

                        var oldQuantity = existingItemsDict.ContainsKey(stockItemId) ? existingItemsDict[stockItemId] : 0;
                        var newQuantity = newItemsDict.ContainsKey(stockItemId) ? newItemsDict[stockItemId] : 0;
                        var netChange = newQuantity - oldQuantity; // Yeni - Eski = Net değişiklik (pozitif = daha fazla düşüldü, negatif = geri eklendi)

                        // Stok hesaplaması: önce geri ekle, sonra yeni miktarı düş
                        if (oldQuantity > 0 && existingStockItems.Any(item => item.StockItemId == stockItemId && item.IsDeducted))
                        {
                            stockItem.CurrentStock += oldQuantity;
                        }
                        if (newQuantity > 0)
                        {
                            stockItem.CurrentStock -= newQuantity;
                        }
                        stockItem.UpdatedAt = DateTime.UtcNow;

                        // Sadece net değişiklik varsa stok hareketi kaydet
                        if (netChange != 0)
                        {
                            var stockMovement = new StockMovement
                            {
                                StockItemId = stockItemId,
                                MovementType = netChange > 0 ? StockMovementTypes.Out : StockMovementTypes.In, // Pozitif = daha fazla düşüldü (Out), Negatif = geri eklendi (In)
                                Quantity = Math.Abs(netChange),
                                Reference = $"Talep #{request.Id} (Güncelleme)",
                                InstitutionName = request.InstitutionName ?? "Bilinmeyen kurum",
                                Notes = "Talep güncellendi",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.StockMovements.Add(stockMovement);
                        }

                        // Kritik stok kontrolü (Sadece düşüş varsa)
                        if (netChange > 0 && stockItem.IsActive && stockItem.CurrentStock <= stockItem.MinimumStock)
                        {
                            await SendLowStockNotification(stockItem);
                        }
                    }

                    // Mevcut stok kalemlerini sil (iptal edilenler hariç)
                    foreach (var existingItem in existingStockItems)
                    {
                        _context.RequestStockItems.Remove(existingItem);
                    }

                    // Yeni stok kalemlerini ekle
                    foreach (var stockItemDto in dto.StockItems)
                    {
                        var requestStockItem = new RequestStockItem
                        {
                            RequestId = request.Id,
                            StockItemId = stockItemDto.StockItemId,
                            Quantity = stockItemDto.Quantity,
                            IsDeducted = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.RequestStockItems.Add(requestStockItem);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Talep güncellenirken hata: {Message}", ex.Message);
                    throw new BusinessValidationException($"Talep güncellenemedi: {ex.Message}", ex);
                }
            }
            
            // Eğer hiç ürün yoksa (ne stok ürünü ne de ekstra ürün), otomatik iptal et
            var hasStockItems = dto.StockItems != null && dto.StockItems.Any();
            var hasExtraItems = !string.IsNullOrWhiteSpace(dto.ExtraItems);
            
            if (!hasStockItems && !hasExtraItems)
            {
                request.Status = RequestStatuses.Cancelled;
                await _context.SaveChangesAsync();
            }
            
            await _context.SaveChangesAsync();

                request.UpdatedAt = DateTime.UtcNow;

            // Tüm ürünler iptal edildi mi kontrol et
            var allStockItems = await _context.RequestStockItems
                .Where(rsi => rsi.RequestId == id)
                .ToListAsync();
            
            var allStockItemsCancelled = allStockItems.Count > 0 && allStockItems.All(rsi => rsi.IsCancelled);
            var extraItemsEmpty = string.IsNullOrEmpty(request.ExtraItems);
            
            // Eğer tüm stok ürünleri iptal edildi ve ekstra ürünler yoksa, status'u Cancelled yap
            if (allStockItemsCancelled && extraItemsEmpty)
            {
                request.Status = RequestStatuses.Cancelled;
                _logger.LogInformation("Talep iptal edildi (tüm ürünler silindi): #{RequestId}", request.Id);
            }

            if (request.Status == "Completed" && request.CompletedAt == null)
            {
                request.CompletedAt = DateTime.UtcNow;
                await SendProjeNotificationAsync(request);
            }

            var changes = new List<AuditLogChange>();
            AddChange(changes, "DeviceSerialNumber", oldDeviceSerialNumber, request.DeviceSerialNumber);
            AddChange(changes, "Description", oldDescription, request.Description);
            AddChange(changes, "InstitutionName", oldInstitutionName, request.InstitutionName);
            AddChange(changes, "InstitutionOtherName", oldInstitutionOtherName, request.InstitutionOtherName);
            AddChange(changes, "Status", oldStatus, request.Status);
            AddChange(changes, "ContactPerson", oldContactPerson, request.ContactPerson);
            AddChange(changes, "ContactPhone", oldContactPhone, request.ContactPhone);
            AddChange(changes, "ContactEmail", oldContactEmail, request.ContactEmail);
            AddChange(changes, "ShippingAddressLine", oldShippingAddressLine, request.ShippingAddressLine);
            AddChange(changes, "ShippingCity", oldShippingCity, request.ShippingCity);
            AddChange(changes, "ShippingDistrict", oldShippingDistrict, request.ShippingDistrict);
            AddChange(changes, "IsUrgent", oldIsUrgent.ToString(), request.IsUrgent.ToString());
            AddChange(changes, "ExtraItems", oldExtraItems, request.ExtraItems);
            AddChange(changes, "CompletedAt", oldCompletedAt?.ToString("O"), request.CompletedAt?.ToString("O"));

            await _auditLogService.AddAuditLogsAsync("Request", request.Id, userId, "Updated", changes);

            if (dto.StockItems != null)
            {
                await _auditLogService.AddAuditEventAsync("Request", request.Id, userId, "StockItemsUpdated", "Request stock items updated");
            }

            if (dto.CancelledStockItemIds != null && dto.CancelledStockItemIds.Any())
            {
                await _auditLogService.AddAuditEventAsync("Request", request.Id, userId, "StockItemsCancelled", $"Cancelled stock item ids: {string.Join(",", dto.CancelledStockItemIds)}");
            }

            if (dto.CancelledExtraItemIndexes != null && dto.CancelledExtraItemIndexes.Any())
            {
                await _auditLogService.AddAuditEventAsync("Request", request.Id, userId, "ExtraItemsCancelled", $"Cancelled extra item indexes: {string.Join(",", dto.CancelledExtraItemIndexes)}");
            }

                await _context.SaveChangesAsync();

                return await GetRequestByIdAsync(id);
            }

        public async Task<RequestDto?> UpdateRequestStatusAsync(int id, string status, int? userId = null, string? userRole = null)
        {
            var request = await _context.Requests
                .Include(r => r.RequestStockItems)
                    .ThenInclude(rsi => rsi.StockItem)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (request == null) return null;

            // SahaElemani rolü için sadece kendi kayıtlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                if (request.CreatedByUserId != userId.Value)
                {
                    _logger.LogWarning("SahaElemani başkasının talebini güncellemeye çalıştı: RequestId={RequestId}, UserId={UserId}", id, userId);
                    return null;
                }
            }

            var oldStatus = request.Status;
            var oldCompletedAt = request.CompletedAt;
            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;

            if (status == RequestStatuses.Completed && request.CompletedAt == null)
            {
                request.CompletedAt = DateTime.UtcNow;

                // Proje kullanıcılarına bildirim gönder
                await SendProjeNotificationAsync(request);
            }

            // Eğer iptal ediliyorsa ve daha önce iptal olmadıysa, stokları iade et
            if (status == RequestStatuses.Cancelled && oldStatus != RequestStatuses.Cancelled)
            {
                var stockItemsToReturn = request.RequestStockItems
                    .Where(rsi => rsi.IsDeducted && !rsi.IsCancelled && rsi.StockItem != null)
                    .ToList();

                foreach (var item in stockItemsToReturn)
                {
                    // Stoku geri ekle
                    item.StockItem.CurrentStock += item.Quantity;
                    item.StockItem.UpdatedAt = DateTime.UtcNow;

                    // Stok hareketi kaydet
                    var returnMovement = new StockMovement
                    {
                        StockItemId = item.StockItemId,
                        MovementType = StockMovementTypes.In,
                        Quantity = item.Quantity,
                        Reference = $"Talep #{id} (İptal)",
                        InstitutionName = request.InstitutionName ?? "Bilinmeyen kurum",
                        Notes = $"Talep iptal edildi. Geri eklenen: {item.Quantity}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.StockMovements.Add(returnMovement);

                    // İlgili RequestStockItem'ı da iptal edilmiş olarak işaretle
                    item.IsCancelled = true;
                }
            }

            var changes = new List<AuditLogChange>();
            AddChange(changes, "Status", oldStatus, request.Status);
            AddChange(changes, "CompletedAt", oldCompletedAt?.ToString("O"), request.CompletedAt?.ToString("O"));
            await _auditLogService.AddAuditLogsAsync("Request", request.Id, userId, "StatusChanged", changes);

            await _context.SaveChangesAsync();

            return await GetRequestByIdAsync(id, userId, userRole);
        }

        public async Task<bool> DeleteRequestAsync(int id, int? userId = null, string? userRole = null)
            {
                var request = await _context.Requests.FindAsync(id);
                if (request == null) return false;

                // SahaElemani rolü için sadece kendi kayıtlarını silebilir
                if (userRole == AppRoles.SahaElemani && userId.HasValue)
                {
                    if (request.CreatedByUserId != userId.Value)
                    {
                        _logger.LogWarning("SahaElemani başkasının talebini silmeye çalıştı: RequestId={RequestId}, UserId={UserId}", id, userId);
                        return false;
                    }
                }

                await _auditLogService.AddAuditEventAsync("Request", request.Id, userId, "Deleted", "Request deleted");
                _context.Requests.Remove(request);
                await _context.SaveChangesAsync();

                return true;
        }

        public async Task<RequestDto?> ShipRequestItemsAsync(int requestId, List<int> requestStockItemIds, List<string> extraItems, int? userId = null, string? userRole = null)
        {
            var request = await _context.Requests
                .Include(r => r.RequestStockItems)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return null;

            // SahaElemani rolü için sadece kendi kayıtlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                if (request.CreatedByUserId != userId.Value)
                {
                    _logger.LogWarning("SahaElemani başkasının talebini güncellemeye çalıştı: RequestId={RequestId}, UserId={UserId}", requestId, userId);
                    return null;
                }
            }

            // İlgili kalemleri bul ve güncelle
            var itemsToShip = request.RequestStockItems
                .Where(rsi => requestStockItemIds.Contains(rsi.Id) && !rsi.IsCancelled)
                .ToList();

            foreach (var item in itemsToShip)
            {
                item.IsShipped = true;
            }

            // Ekstra Ürünleri Güncelle (Append Logic)
            if (extraItems != null && extraItems.Any())
            {
                var currentShipped = (request.ShippedExtraItems ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                
                // Sadece yeni olanları ekle? veya direkt ekle (aynı isimde birden fazla olabilir mi? Evet olabilir "1x Kablo" "1x Kablo")
                // Kullanıcı seçimleri unique key ile geldiği için muhtemelen yeni seçimlerdir.
                currentShipped.AddRange(extraItems);
                request.ShippedExtraItems = string.Join(", ", currentShipped);
            }

            // Durum kontrolü
            // İptal edilmemiş tüm kalemler
            var activeItems = request.RequestStockItems.Where(rsi => !rsi.IsCancelled).ToList();
            
            // Stok kalemleri bitti mi?
            bool stockItemsCompleted = !activeItems.Any() || activeItems.All(rsi => rsi.IsShipped);

            // Ekstra ürünler bitti mi?
            // Basit kontrol: Eğer talepte ExtraItems varsa, ShippedExtraItems dolu olmalı mı?
            // Bu kısım karmaşık çünkü parsing gerekir.
            // Şimdilik sadece StockItems kontrolüne sadık kalalım + kullanıcı manuel "Tamamlandı" yapabilir.
            // Veya: Eğer ExtraItems varsa ve ShippedExtraItems yoksa completed yapma?
            
            if (stockItemsCompleted)
            {
                request.Status = RequestStatuses.Completed;
                if (request.CompletedAt == null)
                {
                    request.CompletedAt = DateTime.UtcNow;
                    await SendProjeNotificationAsync(request);
                }
            }

            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetRequestByIdAsync(requestId, userId, userRole);
        }

        private async Task SendProjeNotificationAsync(Request request)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Talep Tamamlandı",
                    message: $"{request.InstitutionName ?? "Bilinmeyen Kurum"} - {request.Description}",
                    type: "Request",
                    link: $"/requests/{request.Id}",
                    relatedId: request.Id,
                    relatedType: "Request",
                    userId: null,
                    targetRole: AppRoles.Proje
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proje talep bildirimi gönderilirken hata oluştu: {RequestId}", request.Id);
            }
        }

        private async Task SendRequestNotification(Request request)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Yeni Malzeme Talebi",
                    message: $"{request.InstitutionName} - {request.DeviceSerialNumber}\n{request.Description}",
                    type: "Request",
                    link: $"/requests/{request.Id}",
                    relatedId: request.Id,
                    relatedType: "Request",
                    userId: null,
                    targetRole: AppRoles.Teknik
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Talep bildirimi gönderilirken hata oluştu: {RequestId}", request.Id);
            }
        }

        private async Task SendLowStockNotification(StockItem item)
        {
            try
            {
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

        private static void AddChange(List<AuditLogChange> changes, string field, string? oldValue, string? newValue)
        {
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                return;
            }

            changes.Add(new AuditLogChange(field, oldValue, newValue));
        }
    }
}
