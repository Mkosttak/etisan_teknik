using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Services;
using TeknikServisAPI.Models;
using TeknikServisAPI.Data;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace TeknikServisAPI.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")] // Geriye uyumluluk için
    [Authorize]
    public class FaultsController : ControllerBase
    {
        private readonly IFaultService _faultService;
        private readonly IRequestService _requestService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FaultsController> _logger;

        public FaultsController(IFaultService faultService, IRequestService requestService, ApplicationDbContext context, ILogger<FaultsController> logger)
        {
            _faultService = faultService;
            _requestService = requestService;
            _context = context;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var faults = await _faultService.GetAllFaultsAsync(userId, userRole);
            return Ok(faults);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] FaultFilterParams filterParams)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var result = await _faultService.GetPagedFaultsAsync(filterParams, userId, userRole);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFaultDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var fault = await _faultService.CreateFaultAsync(dto, userId);
                
                // Yedek cihaz gönderilecekse otomatik Request oluştur
                if (dto.SendBackupDevice)
                {
                    try
                    {
                        // Stoktan "POS Cihazı" bul
                        var posDevice = await _context.StockItems
                            .Where(s => s.ItemName != null && s.ItemName.ToLower().Contains("pos") && s.IsActive)
                            .OrderByDescending(s => s.CurrentStock)
                            .FirstOrDefaultAsync();
                        
                        var stockItems = new List<CreateRequestStockItemDto>();
                        var extraItems = (posDevice == null || posDevice.CurrentStock <= 0) ? "Yedek POS cihaz" : null;
                        if (posDevice != null && posDevice.CurrentStock > 0)
                        {
                            stockItems.Add(new CreateRequestStockItemDto
                            {
                                StockItemId = posDevice.Id,
                                Quantity = 1
                            });
                        }
                        
                        var requestDto = new CreateRequestDto
                        {
                            InstitutionName = dto.InstitutionName,
                            InstitutionOtherName = dto.InstitutionOtherName,
                            Description = $"{dto.DeviceSerialNumber} SN numaralı cihazın yerine yedek cihaz gönderilecek. {dto.Description} (Bu talep otomatik olarak oluşturulmuştur.)",
                            ShippingAddressLine = dto.ShippingAddressLine,
                            ShippingCity = dto.ShippingCity,
                            ShippingDistrict = dto.ShippingDistrict,
                            ContactPerson = dto.ContactPerson,
                            ContactPhone = dto.ContactPhone,
                            ContactEmail = dto.ContactEmail,
                            IsUrgent = true, // Yedek cihaz talepleri acil kabul edilir
                            ExtraItems = extraItems,
                            StockItems = stockItems
                        };
                        
                        var request = await _requestService.CreateRequestAsync(requestDto);
                        
                        // Request ID'sini Fault'ta güncelle ve veritabanına kaydet
                        await _faultService.UpdateBackupDeviceRequestIdAsync(fault.Id, request.Id);
                        fault.BackupDeviceRequestId = request.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Yedek cihaz talebi oluşturulamadı");
                    }
                }
                
                // Taslak form oluşturulacaksa
                if (dto.CreateDraftServiceForm)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        
                        // İletişim kişisi adını parçala
                        string? firstName = null;
                        string? lastName = null;
                        if (!string.IsNullOrWhiteSpace(dto.ContactPerson))
                        {
                            var parts = dto.ContactPerson.Trim().Split(' ', 2);
                            firstName = parts.Length > 0 ? parts[0] : null;
                            lastName = parts.Length > 1 ? parts[1] : null;
                        }
                        
                        var serviceForm = new ServiceForm
                        {
                            Status = ServiceFormStatuses.Draft,
                            FormDate = now,
                            InstitutionName = dto.InstitutionName,
                            InstitutionOtherName = dto.InstitutionOtherName,
                            InstitutionIsOther = dto.InstitutionName == "DİĞER",
                            Address = dto.ShippingAddressLine,
                            ContactFirstName = firstName,
                            ContactLastName = lastName,
                            Phone = dto.ContactPhone,
                            Email = dto.ContactEmail,
                            ServiceStartDate = now,
                            ServiceEndDate = now,
                            ReportedIssue = $"{dto.DeviceSerialNumber} SN numaralı POS cihazının çalışmadığı bildirilmiştir.",
                            WorkPerformed = $"Teknik ekibin yapmış olduğu tetkikler sonucunda {dto.DeviceSerialNumber} SN numaralı cihazın arızalandığı tespit edilmiştir. Cihazın tamir edilmek üzere teslim alınmıştır.",
                            CreatedByUserId = userId,
                            CreatedAt = now
                        };
                        
                        _context.ServiceForms.Add(serviceForm);
                        await _context.SaveChangesAsync();
                        
                        // RelatedServiceFormId'yi Fault'ta güncelle
                        var faultEntity = await _context.Faults.FindAsync(fault.Id);
                        if (faultEntity != null)
                        {
                            faultEntity.RelatedServiceFormId = serviceForm.Id;
                            await _context.SaveChangesAsync();
                        }
                        
                        fault.RelatedServiceFormId = serviceForm.Id;
                        _logger.LogInformation("Taslak servis formu oluşturuldu: {ServiceFormId}", serviceForm.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Taslak servis formu oluşturulamadı");
                    }
                }
                return CreatedAtAction(nameof(GetById), new { id = fault.Id }, fault);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arıza kaydı oluşturma hatası");
                return StatusCode(500, new { message = "Arıza kaydı oluşturulurken hata oluştu" });
            }
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var history = await _faultService.GetFaultHistoryAsync(id);
            return Ok(history);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateFaultStatusDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            if (userRole == AppRoles.SahaElemani)
            {
                var existingFault = await _faultService.GetFaultByIdAsync(id);
                if (existingFault != null && existingFault.CreatedByUserId != userId)
                {
                    return Forbid();
                }
            }

            var fault = await _faultService.UpdateFaultStatusAsync(id, dto.Status, dto.Notes, dto.ErrorCode, userId, userRole);
            if (fault == null)
                return NotFound(new { message = "Arıza kaydı bulunamadı" });

            // Taslak form oluşturulacaksa (Delivered durumunda)
            if (dto.CreateDraftServiceForm && dto.Status == "Delivered")
            {
                try
                {
                    var faultData = await _faultService.GetFaultByIdAsync(id);
                    if (faultData != null && !faultData.RelatedServiceFormId.HasValue)
                    {
                        var now = DateTime.UtcNow;
                        
                        // İletişim kişisi adını parçala
                        string? firstName = null;
                        string? lastName = null;
                        if (!string.IsNullOrWhiteSpace(faultData.ContactPerson))
                        {
                            var parts = faultData.ContactPerson.Trim().Split(' ', 2);
                            firstName = parts.Length > 0 ? parts[0] : null;
                            lastName = parts.Length > 1 ? parts[1] : null;
                        }
                        
                        var serviceForm = new ServiceForm
                        {
                            Status = ServiceFormStatuses.Draft,
                            FormDate = now,
                            InstitutionName = faultData.InstitutionName,
                            InstitutionOtherName = faultData.InstitutionOtherName,
                            InstitutionIsOther = faultData.InstitutionName == "DİĞER",
                            Address = faultData.ShippingAddressLine,
                            ContactFirstName = firstName,
                            ContactLastName = lastName,
                            Phone = faultData.ContactPhone,
                            Email = faultData.ContactEmail,
                            ServiceStartDate = now,
                            ServiceEndDate = now,
                            ReportedIssue = null, // Boş bırak
                            WorkPerformed = $"{faultData.DeviceSerialNumber} SN numaralı cihazın tamiri tamamlanmış olup, çalışır vaziyette kuruma teslim edilmiştir.",
                            CreatedByUserId = userId,
                            CreatedAt = now
                        };
                        
                        _context.ServiceForms.Add(serviceForm);
                        await _context.SaveChangesAsync();
                        
                        // RelatedServiceFormId'yi güncelle
                        var faultEntity = await _context.Faults.FindAsync(id);
                        if (faultEntity != null)
                        {
                            faultEntity.RelatedServiceFormId = serviceForm.Id;
                            await _context.SaveChangesAsync();
                        }
                        
                        _logger.LogInformation("Taslak servis formu oluşturuldu (Delivered): {ServiceFormId}", serviceForm.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Taslak servis formu oluşturulamadı (Delivered)");
                }
            }

            var updatedFault = await _faultService.GetFaultByIdAsync(id, userId, userRole);
            return Ok(updatedFault);
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddFaultCommentDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            if (userRole == AppRoles.SahaElemani)
            {
                var existingFault = await _faultService.GetFaultByIdAsync(id);
                if (existingFault != null && existingFault.CreatedByUserId != userId)
                {
                    return Forbid();
                }
            }

            try
            {
                var comment = await _faultService.AddFaultCommentAsync(id, dto.Notes, userId, userRole);
                return Ok(comment);
            }
            catch (ArgumentException)
            {
                return NotFound(new { message = "Kayıt bulunamadı" });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var fault = await _faultService.GetFaultByIdAsync(id);
            if (fault == null)
                return NotFound(new { message = "Arıza kaydı bulunamadı" });

            if (userRole == AppRoles.SahaElemani && fault.CreatedByUserId != userId)
            {
                return Forbid();
            }

            return Ok(fault);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFaultDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            
            var existingFault = await _faultService.GetFaultByIdAsync(id);
            if (existingFault == null)
                return NotFound(new { message = "Arıza kaydı bulunamadı" });

            if (userRole == AppRoles.SahaElemani && existingFault.CreatedByUserId != userId)
            {
                return Forbid();
            }
            
            var oldSendBackupDevice = existingFault.SendBackupDevice;
            var newSendBackupDevice = dto.SendBackupDevice ?? oldSendBackupDevice;
            
            var fault = await _faultService.UpdateFaultAsync(id, dto, userId, userRole);
            if (fault == null)
                return NotFound(new { message = "Arıza kaydı bulunamadı" });
            
            if (oldSendBackupDevice != newSendBackupDevice)
            {
                try
                {
                    if (!oldSendBackupDevice && newSendBackupDevice)
                    {
                        var posDevice = await _context.StockItems
                            .FirstOrDefaultAsync(s => s.ItemName != null && s.ItemName.ToLower().Contains("pos"));
                        
                        var stockItems = new List<CreateRequestStockItemDto>();
                        var extraItems = (posDevice == null || posDevice.CurrentStock <= 0) ? "Yedek POS cihaz" : null;
                        if (posDevice != null && posDevice.CurrentStock > 0)
                        {
                            stockItems.Add(new CreateRequestStockItemDto
                            {
                                StockItemId = posDevice.Id,
                                Quantity = 1
                            });
                        }
                        
                        var requestDto = new CreateRequestDto
                        {
                            InstitutionName = fault.InstitutionName,
                            InstitutionOtherName = fault.InstitutionOtherName,
                            Description = $"{fault.DeviceSerialNumber} SN numaralı cihazın yerine yedek cihaz gönderilecek. {fault.Description} (Bu talep otomatik olarak oluşturulmuştur.)",
                            ShippingAddressLine = dto.ShippingAddressLine,
                            ShippingCity = dto.ShippingCity,
                            ShippingDistrict = dto.ShippingDistrict,
                            ContactPerson = existingFault.ContactPerson,
                            ContactPhone = existingFault.ContactPhone,
                            ContactEmail = existingFault.ContactEmail,
                            IsUrgent = true,
                            ExtraItems = extraItems,
                            StockItems = stockItems
                        };
                        
                        var request = await _requestService.CreateRequestAsync(requestDto);
                        await _faultService.UpdateBackupDeviceRequestIdAsync(fault.Id, request.Id);
                        fault.BackupDeviceRequestId = request.Id;
                    }
                    else if (oldSendBackupDevice && !newSendBackupDevice)
                    {
                        if (existingFault.BackupDeviceRequestId.HasValue)
                        {
                            await _requestService.UpdateRequestStatusAsync(existingFault.BackupDeviceRequestId.Value, RequestStatuses.Cancelled);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Yedek cihaz talebi güncelleme hatası");
                }
            }
            
            // Taslak form oluşturulacaksa ve henüz bağlı bir form yoksa
            if (dto.CreateDraftServiceForm == true && !existingFault.RelatedServiceFormId.HasValue)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    
                    // İletişim kişisi adını parçala
                    string? firstName = null;
                    string? lastName = null;
                    var contactPerson = dto.ContactPerson ?? existingFault.ContactPerson;
                    if (!string.IsNullOrWhiteSpace(contactPerson))
                    {
                        var parts = contactPerson.Trim().Split(' ', 2);
                        firstName = parts.Length > 0 ? parts[0] : null;
                        lastName = parts.Length > 1 ? parts[1] : null;
                    }
                    
                    var serviceForm = new ServiceForm
                    {
                        Status = ServiceFormStatuses.Draft,
                        FormDate = now,
                        InstitutionName = fault.InstitutionName,
                        InstitutionOtherName = fault.InstitutionOtherName,
                        InstitutionIsOther = fault.InstitutionName == "DİĞER",
                        Address = dto.ShippingAddressLine ?? existingFault.ShippingAddressLine,
                        ContactFirstName = firstName,
                        ContactLastName = lastName,
                        Phone = dto.ContactPhone ?? existingFault.ContactPhone,
                        Email = dto.ContactEmail ?? existingFault.ContactEmail,
                        ServiceStartDate = now,
                        ServiceEndDate = now,
                        ReportedIssue = $"{fault.DeviceSerialNumber} SN numaralı POS cihazının çalışmadığı bildirilmiştir.",
                        WorkPerformed = $"Teknik ekibin yapmış olduğu tetkikler sonucunda {fault.DeviceSerialNumber} SN numaralı cihazın arızalandığı tespit edilmiştir. Cihazın tamir edilmek üzere teslim alınmıştır.",
                        CreatedByUserId = userId,
                        CreatedAt = now
                    };
                    
                    _context.ServiceForms.Add(serviceForm);
                    await _context.SaveChangesAsync();
                    
                    // RelatedServiceFormId'yi Fault'ta güncelle
                    var faultEntity = await _context.Faults.FindAsync(fault.Id);
                    if (faultEntity != null)
                    {
                        faultEntity.RelatedServiceFormId = serviceForm.Id;
                        await _context.SaveChangesAsync();
                    }
                    
                    fault.RelatedServiceFormId = serviceForm.Id;
                    _logger.LogInformation("Taslak servis formu oluşturuldu (Update): {ServiceFormId}", serviceForm.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Taslak servis formu oluşturulamadı (Update)");
                }
            }

            return Ok(fault);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            if (userRole == AppRoles.SahaElemani)
            {
                var existingFault = await _faultService.GetFaultByIdAsync(id);
                if (existingFault != null && existingFault.CreatedByUserId != userId)
                {
                    return Forbid();
                }
            }

            var result = await _faultService.DeleteFaultAsync(id, userId, userRole);
            if (!result)
                return NotFound(new { message = "Arıza kaydı bulunamadı" });

            return Ok(new { message = "Arıza kaydı iptal edildi" });
        }
    }
}









