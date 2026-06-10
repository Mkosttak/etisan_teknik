using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class FaultService : IFaultService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public FaultService(ApplicationDbContext context, INotificationService notificationService, IAuditLogService auditLogService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        public async Task<List<FaultDto>> GetAllFaultsAsync(int? userId = null, string? userRole = null)
        {
            var query = _context.Faults
                .AsNoTracking()
                .Include(f => f.AssignedToUser)
                .Include(f => f.SpareDevice)
                .AsQueryable();

            // SahaElemani rolü için sadece kendi kayıtlarını göster
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                query = query.Where(f => f.CreatedByUserId == userId.Value);
            }

            var faults = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Talep ID'lerini topla
            var requestIds = faults
                .Where(f => f.BackupDeviceRequestId.HasValue)
                .Select(f => f.BackupDeviceRequestId!.Value)
                .Distinct()
                .ToList();

            // Talep durumlarını toplu olarak al
            var requestStatuses = await _context.Requests
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Status);

            // FaultDto listesini oluştur
            return faults.Select(f => new FaultDto
            {
                Id = f.Id,
                DeviceId = 0, // Artık kullanılmıyor
                DeviceSerialNumber = f.DeviceSerialNumber,
                Description = f.Description,
                InstitutionName = f.InstitutionName,
                InstitutionOtherName = f.InstitutionOtherName,
                Status = f.Status,
                AssignedToUserId = f.AssignedToUserId,
                AssignedToUserName = f.AssignedToUser != null ? $"{f.AssignedToUser.FirstName} {f.AssignedToUser.LastName}" : null,
                SpareDeviceId = f.SpareDeviceId,
                SpareDeviceSerialNumber = f.SpareDevice != null ? f.SpareDevice.SerialNumber : null,
                ShippingAddressTitle = f.ShippingAddressTitle,
                ShippingAddressLine = f.ShippingAddressLine,
                ShippingCity = f.ShippingCity,
                ShippingDistrict = f.ShippingDistrict,
                ShippingPostalCode = f.ShippingPostalCode,
                ContactPerson = f.ContactPerson,
                ContactPhone = f.ContactPhone,
                ContactEmail = f.ContactEmail,
                SendBackupDevice = f.SendBackupDevice,
                BackupDeviceRequestId = f.BackupDeviceRequestId,
                BackupDeviceRequestStatus = f.BackupDeviceRequestId.HasValue && requestStatuses.ContainsKey(f.BackupDeviceRequestId.Value)
                    ? requestStatuses[f.BackupDeviceRequestId.Value]
                    : null,
                CreatedAt = f.CreatedAt,
                CompletedAt = f.CompletedAt,
                CreatedByUserId = f.CreatedByUserId
            }).ToList();
        }

        public async Task<PagedResult<FaultDto>> GetPagedFaultsAsync(FaultFilterParams filterParams, int? userId = null, string? userRole = null)
        {
            var query = _context.Faults
                .AsNoTracking()
                .Include(f => f.AssignedToUser)
                .Include(f => f.SpareDevice)
                .AsQueryable();

            // SahaElemani rolü için sadece kendi kayıtlarını göster
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                query = query.Where(f => f.CreatedByUserId == userId.Value);
            }

            // Filtreleme
            if (!string.IsNullOrWhiteSpace(filterParams.SearchTerm))
            {
                var searchLower = filterParams.SearchTerm.ToLower();
                query = query.Where(f =>
                    (f.DeviceSerialNumber != null && f.DeviceSerialNumber.ToLower().Contains(searchLower)) ||
                    (f.Description != null && f.Description.ToLower().Contains(searchLower)) ||
                    (f.ContactPerson != null && f.ContactPerson.ToLower().Contains(searchLower))
                );
            }

            if (!string.IsNullOrWhiteSpace(filterParams.Status))
            {
                query = query.Where(f => f.Status == filterParams.Status);
            }

            // DeviceId artık yok, DeviceSerialNumber kullanılıyor
            // Bu filtreleme kaldırıldı

            if (filterParams.AssignedToUserId.HasValue)
            {
                query = query.Where(f => f.AssignedToUserId == filterParams.AssignedToUserId.Value);
            }

            if (filterParams.CreatedFrom.HasValue)
            {
                query = query.Where(f => f.CreatedAt >= filterParams.CreatedFrom.Value);
            }

            if (filterParams.CreatedTo.HasValue)
            {
                query = query.Where(f => f.CreatedAt <= filterParams.CreatedTo.Value);
            }

            // Toplam kayıt sayısı
            var totalCount = await query.CountAsync();

            // Sıralama
            query = filterParams.SortBy?.ToLower() switch
            {
                "createdat" => filterParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.CreatedAt)
                    : query.OrderBy(f => f.CreatedAt),
                "status" => filterParams.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(f => f.Status)
                    : query.OrderBy(f => f.Status),
                _ => query.OrderByDescending(f => f.CreatedAt)
            };

            // Pagination
            var faults = await query
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .ToListAsync();

            // Talep ID'lerini topla
            var requestIds = faults
                .Where(f => f.BackupDeviceRequestId.HasValue)
                .Select(f => f.BackupDeviceRequestId!.Value)
                .Distinct()
                .ToList();

            // Talep durumlarını toplu olarak al
            var requestStatuses = new Dictionary<int, string>();
            if (requestIds.Any())
            {
                requestStatuses = await _context.Requests
                    .Where(r => requestIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r.Status);
            }

            // FaultDto listesini oluştur
            var items = faults.Select(f => new FaultDto
            {
                Id = f.Id,
                DeviceId = 0, // Artık kullanılmıyor
                DeviceSerialNumber = f.DeviceSerialNumber,
                Description = f.Description,
                InstitutionName = f.InstitutionName,
                InstitutionOtherName = f.InstitutionOtherName,
                Status = f.Status,
                AssignedToUserId = f.AssignedToUserId,
                AssignedToUserName = f.AssignedToUser != null ? $"{f.AssignedToUser.FirstName} {f.AssignedToUser.LastName}" : null,
                SpareDeviceId = f.SpareDeviceId,
                SpareDeviceSerialNumber = f.SpareDevice != null ? f.SpareDevice.SerialNumber : null,
                ShippingAddressTitle = f.ShippingAddressTitle,
                ShippingAddressLine = f.ShippingAddressLine,
                ShippingCity = f.ShippingCity,
                ShippingDistrict = f.ShippingDistrict,
                ShippingPostalCode = f.ShippingPostalCode,
                ContactPerson = f.ContactPerson,
                ContactPhone = f.ContactPhone,
                ContactEmail = f.ContactEmail,
                SendBackupDevice = f.SendBackupDevice,
                BackupDeviceRequestId = f.BackupDeviceRequestId,
                BackupDeviceRequestStatus = f.BackupDeviceRequestId.HasValue && requestStatuses.ContainsKey(f.BackupDeviceRequestId.Value)
                    ? requestStatuses[f.BackupDeviceRequestId.Value]
                    : null,
                CreatedAt = f.CreatedAt,
                CompletedAt = f.CompletedAt
            }).ToList();

            return new PagedResult<FaultDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filterParams.PageNumber,
                PageSize = filterParams.PageSize
            };
        }

        public async Task<FaultDto?> GetFaultByIdAsync(int id, int? userId = null, string? userRole = null)
        {
            var query = _context.Faults
                .AsNoTracking()
                .Include(f => f.AssignedToUser)
                .Include(f => f.SpareDevice)
                .AsQueryable();

            // SahaElemani rolü için sadece kendi kayıtlarını göster
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                query = query.Where(f => f.CreatedByUserId == userId.Value);
            }

            var fault = await query.FirstOrDefaultAsync(f => f.Id == id);

            if (fault == null) return null;

            // Talep durumunu al
            string? requestStatus = null;
            if (fault.BackupDeviceRequestId.HasValue)
            {
                var request = await _context.Requests.FindAsync(fault.BackupDeviceRequestId.Value);
                requestStatus = request?.Status;
            }

            return new FaultDto
            {
                Id = fault.Id,
                DeviceId = 0, // Artık kullanılmıyor
                DeviceSerialNumber = fault.DeviceSerialNumber,
                Description = fault.Description,
                InstitutionName = (fault.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(fault.InstitutionOtherName)) ? fault.InstitutionOtherName : fault.InstitutionName,
                InstitutionOtherName = fault.InstitutionOtherName,
                Status = fault.Status,
                AssignedToUserId = fault.AssignedToUserId,
                AssignedToUserName = fault.AssignedToUser != null ? $"{fault.AssignedToUser.FirstName} {fault.AssignedToUser.LastName}" : null,
                SpareDeviceId = fault.SpareDeviceId,
                SpareDeviceSerialNumber = fault.SpareDevice?.SerialNumber,
                ShippingAddressTitle = fault.ShippingAddressTitle,
                ShippingAddressLine = fault.ShippingAddressLine,
                ShippingCity = fault.ShippingCity,
                ShippingDistrict = fault.ShippingDistrict,
                ShippingPostalCode = fault.ShippingPostalCode,
                ContactPerson = fault.ContactPerson,
                ContactPhone = fault.ContactPhone,
                ContactEmail = fault.ContactEmail,
                SendBackupDevice = fault.SendBackupDevice,
                BackupDeviceRequestId = fault.BackupDeviceRequestId,
                BackupDeviceRequestStatus = requestStatus,
                CreatedAt = fault.CreatedAt,
                CompletedAt = fault.CompletedAt,
                CreatedByUserId = fault.CreatedByUserId
            };
        }


        public async Task<FaultDto> CreateFaultAsync(CreateFaultDto dto, int createdByUserId)
        {
            var fault = new Fault
            {
                DeviceSerialNumber = dto.DeviceSerialNumber,
                Description = dto.Description,
                InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName)) 
                    ? dto.InstitutionOtherName 
                    : dto.InstitutionName,
                InstitutionOtherName = dto.InstitutionOtherName,
                Status = FaultStatuses.Created, // Yeni workflow: Created
                ShippingAddressTitle = dto.ShippingAddressTitle,
                ShippingAddressLine = dto.ShippingAddressLine,
                ShippingCity = dto.ShippingCity,
                ShippingDistrict = dto.ShippingDistrict,
                ShippingPostalCode = dto.ShippingPostalCode,
                ContactPerson = dto.ContactPerson,
                ContactPhone = dto.ContactPhone,
                ContactEmail = dto.ContactEmail,
                SendBackupDevice = dto.SendBackupDevice,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Faults.Add(fault);
            await _context.SaveChangesAsync();

            // Geçmiş kaydı oluştur
            var history = new FaultHistory
            {
                FaultId = fault.Id,
                UserId = createdByUserId,
                Action = "Created",
                Notes = "Arıza kaydı oluşturuldu",
                CreatedAt = DateTime.UtcNow
            };
            _context.FaultHistories.Add(history);
            await _auditLogService.AddAuditEventAsync("Fault", fault.Id, createdByUserId, "Created", "Fault created");
            await _context.SaveChangesAsync();



            // Bildirim oluştur (Teknik birime)
            await _notificationService.CreateNotificationAsync(
                title: "Yeni Arıza Kaydı",
                message: $"{fault.InstitutionName} - {dto.DeviceSerialNumber}\n{dto.Description}",
                type: "Fault",
                link: $"/faults/{fault.Id}",
                relatedId: fault.Id,
                relatedType: "Fault",
                userId: null,
                targetRole: AppRoles.Teknik
            );

            return (await GetFaultByIdAsync(fault.Id))!;
        }



        public async Task<FaultDto?> UpdateFaultAsync(int id, UpdateFaultDto dto, int updatedByUserId, string? userRole = null)
        {
            var fault = await _context.Faults.FindAsync(id);
            if (fault == null) return null;

            // SahaElemani rolü için sadece kendi kayıtlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani)
            {
                if (fault.CreatedByUserId != updatedByUserId)
                {
                    return null;
                }
            }

            var oldStatus = fault.Status;
            var oldDeviceSerialNumber = fault.DeviceSerialNumber;
            var oldDescription = fault.Description;
            var oldInstitutionName = fault.InstitutionName;
            var oldInstitutionOtherName = fault.InstitutionOtherName;
            var oldAssignedToUserId = fault.AssignedToUserId;
            var oldSpareDeviceId = fault.SpareDeviceId;
            var oldSendBackupDevice = fault.SendBackupDevice;
            var oldShippingAddressLine = fault.ShippingAddressLine;
            var oldShippingCity = fault.ShippingCity;
            var oldShippingDistrict = fault.ShippingDistrict;
            var oldShippingPostalCode = fault.ShippingPostalCode;
            var oldContactPerson = fault.ContactPerson;
            var oldContactPhone = fault.ContactPhone;
            var oldContactEmail = fault.ContactEmail;
            var oldCompletedAt = fault.CompletedAt;

            // Cihaz bilgilerini güncelle
            if (dto.DeviceSerialNumber != null)
                fault.DeviceSerialNumber = dto.DeviceSerialNumber;

            if (dto.Description != null)
                fault.Description = dto.Description;
            if (dto.InstitutionName != null)
    {
        fault.InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName ?? fault.InstitutionOtherName)) 
            ? (dto.InstitutionOtherName ?? fault.InstitutionOtherName) 
            : dto.InstitutionName;
    }
    if (dto.InstitutionOtherName != null)
        fault.InstitutionOtherName = dto.InstitutionOtherName;
            if (!string.IsNullOrEmpty(dto.Status))
            {
                var newStatus = dto.Status;
                fault.Status = newStatus;
                if (newStatus == FaultStatuses.SentToCustomer || newStatus == FaultStatuses.Completed)
                {
                    if (fault.CompletedAt == null)
                    {
                        fault.CompletedAt = DateTime.UtcNow;
                        if (newStatus == FaultStatuses.SentToCustomer)
                        {
                            await SendProjeNotificationAsync(fault);
                        }
                    }
                }

            }
            if (dto.AssignedToUserId.HasValue)
                fault.AssignedToUserId = dto.AssignedToUserId.Value;
            if (dto.SpareDeviceId.HasValue)
                fault.SpareDeviceId = dto.SpareDeviceId.Value;
            if (dto.SendBackupDevice.HasValue)
                fault.SendBackupDevice = dto.SendBackupDevice.Value;
            
            // Adres ve iletişim bilgilerini güncelle
            if (dto.ShippingAddressLine != null)
                fault.ShippingAddressLine = dto.ShippingAddressLine;
            if (dto.ShippingCity != null)
                fault.ShippingCity = dto.ShippingCity;
            if (dto.ShippingDistrict != null)
                fault.ShippingDistrict = dto.ShippingDistrict;
            if (dto.ContactPerson != null)
                fault.ContactPerson = dto.ContactPerson;
            if (dto.ContactPhone != null)
                fault.ContactPhone = dto.ContactPhone;
            if (dto.ContactEmail != null)
                fault.ContactEmail = dto.ContactEmail;

            var changes = new List<AuditLogChange>();
            AddChange(changes, "DeviceSerialNumber", oldDeviceSerialNumber, fault.DeviceSerialNumber);
            AddChange(changes, "Description", oldDescription, fault.Description);
            AddChange(changes, "InstitutionName", oldInstitutionName, fault.InstitutionName);
            AddChange(changes, "InstitutionOtherName", oldInstitutionOtherName, fault.InstitutionOtherName);
            AddChange(changes, "Status", oldStatus, fault.Status);
            AddChange(changes, "AssignedToUserId", oldAssignedToUserId?.ToString(), fault.AssignedToUserId?.ToString());
            AddChange(changes, "SpareDeviceId", oldSpareDeviceId?.ToString(), fault.SpareDeviceId?.ToString());
            AddChange(changes, "SendBackupDevice", oldSendBackupDevice.ToString(), fault.SendBackupDevice.ToString());
            AddChange(changes, "ShippingAddressLine", oldShippingAddressLine, fault.ShippingAddressLine);
            AddChange(changes, "ShippingCity", oldShippingCity, fault.ShippingCity);
            AddChange(changes, "ShippingDistrict", oldShippingDistrict, fault.ShippingDistrict);
            AddChange(changes, "ShippingPostalCode", oldShippingPostalCode, fault.ShippingPostalCode);
            AddChange(changes, "ContactPerson", oldContactPerson, fault.ContactPerson);
            AddChange(changes, "ContactPhone", oldContactPhone, fault.ContactPhone);
            AddChange(changes, "ContactEmail", oldContactEmail, fault.ContactEmail);
            AddChange(changes, "CompletedAt", oldCompletedAt?.ToString("O"), fault.CompletedAt?.ToString("O"));

            await _auditLogService.AddAuditLogsAsync("Fault", fault.Id, updatedByUserId, "Updated", changes);

            fault.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Otomatik mesajlar oluşturulmayacak - sadece UpdateFaultStatusAsync ile notlu durum değişiklikleri kaydedilecek

            return await GetFaultByIdAsync(id, updatedByUserId, userRole);
        }

        public async Task<bool> DeleteFaultAsync(int id, int? userId = null, string? userRole = null)
        {
            var fault = await _context.Faults.FindAsync(id);
            if (fault == null) return false;

            // SahaElemani rolü için sadece kendi kayıtlarını silebilir
            if (userRole == AppRoles.SahaElemani && userId.HasValue)
            {
                if (fault.CreatedByUserId != userId.Value)
                {
                    return false;
                }
            }

            var oldStatus = fault.Status;
            fault.Status = FaultStatuses.Cancelled;
            fault.UpdatedAt = DateTime.UtcNow;
            await _auditLogService.AddAuditLogsAsync(
                "Fault",
                fault.Id,
                userId,
                "Cancelled",
                new List<AuditLogChange> { new("Status", oldStatus, fault.Status) });
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<FaultHistoryDto>> GetFaultHistoryAsync(int faultId)
        {
            var histories = await _context.FaultHistories
                .AsNoTracking()
                .Include(h => h.User)
                .Where(h => h.FaultId == faultId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
            
            return histories.Select(h => new FaultHistoryDto
            {
                Id = h.Id,
                FaultId = h.FaultId,
                UserId = h.UserId,
                UserName = h.User != null ? $"{h.User.FirstName} {h.User.LastName}" : "Bilinmeyen",
                Action = h.Action,
                Notes = h.Notes,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ErrorCode = null, // Migration yapılana kadar null (ErrorCode Notes içinde saklanıyor)
                CreatedAt = h.CreatedAt
            }).ToList();
        }

        public async Task<FaultDto?> UpdateFaultStatusAsync(int id, string status, string? notes, string? errorCode, int userId, string? userRole = null)
        {
            var fault = await _context.Faults.FindAsync(id);
            if (fault == null) return null;

            // SahaElemani rolü için sadece kendi kayıtlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani)
            {
                if (fault.CreatedByUserId != userId)
                {
                    return null;
                }
            }

            var oldStatus = fault.Status;
            var oldCompletedAt = fault.CompletedAt;
            fault.Status = status;
            fault.UpdatedAt = DateTime.UtcNow;

            if (status == FaultStatuses.SentToCustomer)
            {
                fault.CompletedAt = DateTime.UtcNow;
                await SendProjeNotificationAsync(fault);
            }


            await _context.SaveChangesAsync();

            // Her durum değişikliğinde mutlaka geçmiş kaydı oluştur (not olsun ya da olmasın)
            // Hata kodu varsa not'a ekle
            string finalNotes = notes ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                var errorCodeDescriptions = new Dictionary<string, string>
                {
                    { "A1", "Plastik kapak problemleri" },
                    { "B1", "Barkod okuyucu problemleri" },
                    { "C1", "Lens (Ekran Camı) kırık/kirli/tozlu" },
                    { "C2", "Kozmetik amaçlı parça değişimi talep ediliyor" },
                    { "E1", "Ekranda görüntü yok" },
                    { "E2", "Ekran görüntü var fakat arka ışık yanmıyor" },
                    { "E3", "Ekran kırık" },
                    { "E4", "Dokunmatik problemleri" },
                    { "F1", "Tamper" },
                    { "F2", "Terminal kilitleniyor (donuyor)" },
                    { "F3", "Terminal sürekli resetleniyor" },
                    { "G1", "Harici batarya problemleri" },
                    { "G2", "SIM konnektör problemleri" },
                    { "G3", "Printer kapağı problemleri" },
                    { "G4", "Enerji yok" },
                    { "G5", "Spiral kablo problemleri" },
                    { "K1", "Manyetik okuyucu problemleri" },
                    { "K2", "Smart kart okuyucu problemleri" },
                    { "L1", "Sıvı teması" },
                    { "M1", "Dial-up bağlantı problemleri" },
                    { "M2", "Ethernet bağlantı problemleri" },
                    { "M3", "GSM/GPRS bağlantı problemleri" },
                    { "M4", "Yükleme problemleri" },
                    { "M7", "Temassız okuyucu problemleri" },
                    { "M8", "USB problemleri" },
                    { "P1", "Printer problemleri" },
                    { "P2", "Printer silindir problemleri" },
                    { "S1", "Terminal içinde yabancı madde" },
                    { "S2", "Yazılım problemleri" },
                    { "S3", "Key yükleme problemleri" },
                    { "T1", "Ana tuş takımı problemleri" },
                    { "T2", "Fonksiyon tuş takımı problemleri" },
                    { "X", "Deklare edilen sorun yok" }
                };
                
                var errorDescription = errorCodeDescriptions.ContainsKey(errorCode) 
                    ? errorCodeDescriptions[errorCode] 
                    : $"Bilinmeyen Hata Kodu: {errorCode}";
                
                finalNotes = string.IsNullOrWhiteSpace(finalNotes) 
                    ? $"Hata Kodu: {errorCode} - {errorDescription}"
                    : $"{finalNotes}\nHata Kodu: {errorCode} - {errorDescription}";
            }

            var statusChanges = new List<AuditLogChange>();
            AddChange(statusChanges, "Status", oldStatus, status);
            AddChange(statusChanges, "CompletedAt", oldCompletedAt?.ToString("O"), fault.CompletedAt?.ToString("O"));
            await _auditLogService.AddAuditLogsAsync("Fault", fault.Id, userId, "StatusChanged", statusChanges, finalNotes);
            
            var history = new FaultHistory
            {
                FaultId = fault.Id,
                UserId = userId,
                Action = "StatusChanged",
                Notes = finalNotes, // Not yoksa boş string olacak ama kayıt oluşturulacak
                OldValue = oldStatus,
                NewValue = status,
                // ErrorCode = errorCode, // Migration yapılana kadar geçici olarak kapatıldı
                CreatedAt = DateTime.UtcNow
            };
            _context.FaultHistories.Add(history);
            await _auditLogService.AddAuditEventAsync("Fault", fault.Id, userId, "CommentAdded", notes);
            await _context.SaveChangesAsync();

            return await GetFaultByIdAsync(id);
        }

        public async Task<FaultHistoryDto> AddFaultCommentAsync(int id, string notes, int userId, string? userRole = null)
        {
            var fault = await _context.Faults.FindAsync(id);
            if (fault == null)
                throw new ArgumentException("Arıza kaydı bulunamadı");

            // SahaElemani rolü için sadece kendi kayıtlarına yorum ekleyebilir
            if (userRole == AppRoles.SahaElemani)
            {
                if (fault.CreatedByUserId != userId)
                {
                    throw new UnauthorizedAccessException("Bu arıza kaydına yorum ekleme yetkiniz yok");
                }
            }

            // Yorum eklendiğinde mevcut durumu kaydet
            var history = new FaultHistory
            {
                FaultId = fault.Id,
                UserId = userId,
                Action = "Comment",
                Notes = notes,
                NewValue = fault.Status, // Mevcut aşamayı kaydet
                CreatedAt = DateTime.UtcNow
            };

            _context.FaultHistories.Add(history);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            return new FaultHistoryDto
            {
                Id = history.Id,
                FaultId = history.FaultId,
                UserId = history.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmeyen",
                Action = history.Action,
                Notes = history.Notes,
                NewValue = history.NewValue, // Mevcut aşamayı döndür
                CreatedAt = history.CreatedAt
            };
        }

        public async Task UpdateBackupDeviceRequestIdAsync(int faultId, int requestId)
        {
            var fault = await _context.Faults.FindAsync(faultId);
            if (fault != null)
            {
                fault.BackupDeviceRequestId = requestId;
                await _context.SaveChangesAsync();
            }
        }
        private async Task SendProjeNotificationAsync(Fault fault)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Arıza Müşteriye Gönderildi",
                    message: $"{fault.InstitutionName ?? "Bilinmeyen Kurum"} - {fault.DeviceSerialNumber}",
                    type: "Fault",
                    link: $"/faults/{fault.Id}",
                    relatedId: fault.Id,
                    relatedType: "Fault",
                    userId: null,
                    targetRole: AppRoles.Proje
                );
            }
            catch (Exception ex)
            {
                // Bildirim gönderilemese bile akış devam etsin
                Console.WriteLine($"Proje arıza bildirimi gönderilirken hata: {ex.Message}");
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
