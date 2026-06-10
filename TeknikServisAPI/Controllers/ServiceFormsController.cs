using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using System.Linq;
using System.Security.Claims;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceFormsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly PdfService _pdfService;
        private readonly ILogger<ServiceFormsController> _logger;
        private readonly INotificationService _notificationService;

        public ServiceFormsController(ApplicationDbContext context, PdfService pdfService, ILogger<ServiceFormsController> logger, INotificationService notificationService)
        {
            _context = context;
            _pdfService = pdfService;
            _logger = logger;
            _notificationService = notificationService;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        // GET: api/serviceforms
        [HttpGet]
        public async Task<ActionResult<List<ServiceFormListDto>>> GetAll([FromQuery] string? status)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            
            var query = _context.ServiceForms
                .Include(f => f.CreatedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (userRole == AppRoles.SahaElemani && userId > 0)
            {
                query = query.Where(f => f.CreatedByUserId == userId);
            }

            var forms = await query
                .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                .Select(f => new ServiceFormListDto
                {
                    Id = f.Id,
                    Status = f.Status,
                    FormDate = f.FormDate,
                    InstitutionName = (f.InstitutionIsOther && !string.IsNullOrEmpty(f.InstitutionOtherName)) ? f.InstitutionOtherName : f.InstitutionName,
                    InstitutionOtherName = f.InstitutionOtherName,
                    IssueStatus = f.IssueStatus,
                    PartsToReplace = f.PartsToReplace,
                    ServiceStartDate = f.ServiceStartDate,
                    ServiceEndDate = f.ServiceEndDate,
                    CreatedByUserName = f.CreatedByUser != null
                        ? $"{f.CreatedByUser.FirstName} {f.CreatedByUser.LastName}"
                        : null,
                    CreatedAt = f.CreatedAt,
                    SubmittedAt = f.SubmittedAt
                })
                .ToListAsync();

            return Ok(forms);
        }

        // GET: api/serviceforms/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceFormDto>> GetById(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var form = await _context.ServiceForms
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound("Form bulunamadi");

            if (userRole == AppRoles.SahaElemani && userId > 0 && form.CreatedByUserId != userId)
                return Forbid();

            var dto = new ServiceFormDto
            {
                Id = form.Id,
                Status = form.Status,
                FormDate = form.FormDate,
                InstitutionName = (form.InstitutionIsOther && !string.IsNullOrEmpty(form.InstitutionOtherName)) ? form.InstitutionOtherName : form.InstitutionName,
                InstitutionIsOther = form.InstitutionIsOther,
                InstitutionOtherName = form.InstitutionOtherName,
                Address = form.Address,
                ContactFirstName = form.ContactFirstName,
                ContactLastName = form.ContactLastName,
                Phone = form.Phone,
                Email = form.Email,
                ServiceStartDate = form.ServiceStartDate,
                ServiceEndDate = form.ServiceEndDate,
                ServiceScope = form.ServiceScope,
                ServiceScopeOther = form.ServiceScopeOther,
                ServiceType = form.ServiceType,
                ServiceTypeOther = form.ServiceTypeOther,
                ReportedIssue = form.ReportedIssue,
                WorkPerformed = form.WorkPerformed,
                MaterialsUsed = form.MaterialsUsed,
                IssueStatus = form.IssueStatus,
                IssueStatusOther = form.IssueStatusOther,
                PartsToReplace = form.PartsToReplace,
                Photos = form.Photos,
                EtisanFirstName = form.EtisanFirstName,
                EtisanLastName = form.EtisanLastName,
                EtisanSignature = form.EtisanSignature,
                AuthorityFirstName = form.AuthorityFirstName,
                AuthorityLastName = form.AuthorityLastName,
                AuthorityEmail = form.AuthorityEmail,
                AuthorityPhone = form.AuthorityPhone,
                AuthoritySignature = form.AuthoritySignature,
                CreatedByUserId = form.CreatedByUserId,
                CreatedByUserName = form.CreatedByUser != null
                    ? $"{form.CreatedByUser.FirstName} {form.CreatedByUser.LastName}"
                    : null,
                CreatedAt = form.CreatedAt,
                UpdatedAt = form.UpdatedAt,
                SubmittedAt = form.SubmittedAt
            };

            return Ok(dto);
        }

        // POST: api/serviceforms
        [HttpPost]
        public async Task<ActionResult<ServiceFormDto>> Create(CreateServiceFormDto dto)
        {
            var userId = GetCurrentUserId();

            // Eğer "Diğer" seçildiyse, InstitutionOtherName'i InstitutionName olarak kaydet
            var institutionName = dto.InstitutionIsOther && !string.IsNullOrEmpty(dto.InstitutionOtherName)
                ? dto.InstitutionOtherName
                : dto.InstitutionName;

            var form = new ServiceForm
            {
                Status = ServiceFormStatuses.Draft,
                FormDate = dto.FormDate ?? DateTime.UtcNow,
                InstitutionName = institutionName,
                InstitutionIsOther = dto.InstitutionIsOther,
                InstitutionOtherName = dto.InstitutionOtherName,
                Address = dto.Address,
                ContactFirstName = dto.ContactFirstName,
                ContactLastName = dto.ContactLastName,
                Phone = dto.Phone,
                Email = dto.Email,
                ServiceStartDate = dto.ServiceStartDate,
                ServiceEndDate = dto.ServiceEndDate,
                ServiceScope = dto.ServiceScope,
                ServiceScopeOther = dto.ServiceScopeOther,
                ServiceType = dto.ServiceType,
                ServiceTypeOther = dto.ServiceTypeOther,
                ReportedIssue = dto.ReportedIssue,
                WorkPerformed = dto.WorkPerformed,
                MaterialsUsed = dto.MaterialsUsed,
                IssueStatus = dto.IssueStatus,
                IssueStatusOther = dto.IssueStatusOther,
                PartsToReplace = dto.PartsToReplace,
                Photos = dto.Photos,
                EtisanFirstName = dto.EtisanFirstName,
                EtisanLastName = dto.EtisanLastName,
                EtisanSignature = dto.EtisanSignature,
                AuthorityFirstName = dto.AuthorityFirstName,
                AuthorityLastName = dto.AuthorityLastName,
                AuthorityEmail = dto.AuthorityEmail,
                AuthorityPhone = dto.AuthorityPhone,
                AuthoritySignature = dto.AuthoritySignature,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ServiceForms.Add(form);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = form.Id }, new { id = form.Id });
        }

        // PUT: api/serviceforms/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateServiceFormDto dto)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            
            var form = await _context.ServiceForms.FindAsync(id);

            if (form == null)
                return NotFound("Form bulunamadı");

            // SahaElemani rolü için sadece kendi formlarını güncelleyebilir
            if (userRole == AppRoles.SahaElemani && userId > 0)
            {
                if (form.CreatedByUserId != userId)
                {
                    return StatusCode(403, "Bu formu güncelleme yetkiniz yok");
                }
            }

            // SUBMITTED formlar güncellenemez!
            if (form.Status == ServiceFormStatuses.Submitted)
                return StatusCode(403, "Gönderilmiş formlar değiştirilemez");

            // Güncelleme
            if (dto.FormDate.HasValue) form.FormDate = dto.FormDate.Value;
            
            // Eğer "Diğer" seçildiyse, InstitutionOtherName'i InstitutionName olarak kaydet
            if (dto.InstitutionIsOther.HasValue && dto.InstitutionIsOther.Value && !string.IsNullOrEmpty(dto.InstitutionOtherName))
            {
                form.InstitutionName = dto.InstitutionOtherName;
            }
            else if (dto.InstitutionName != null)
            {
                form.InstitutionName = dto.InstitutionName;
            }
            
            if (dto.InstitutionIsOther.HasValue) form.InstitutionIsOther = dto.InstitutionIsOther.Value;
            if (dto.InstitutionOtherName != null) form.InstitutionOtherName = dto.InstitutionOtherName;
            if (dto.Address != null) form.Address = dto.Address;
            if (dto.ContactFirstName != null) form.ContactFirstName = dto.ContactFirstName;
            if (dto.ContactLastName != null) form.ContactLastName = dto.ContactLastName;
            if (dto.Phone != null) form.Phone = dto.Phone;
            if (dto.Email != null) form.Email = dto.Email;
            if (dto.ServiceStartDate.HasValue) form.ServiceStartDate = dto.ServiceStartDate;
            if (dto.ServiceEndDate.HasValue) form.ServiceEndDate = dto.ServiceEndDate;
            if (dto.ServiceScope != null) form.ServiceScope = dto.ServiceScope;
            if (dto.ServiceScopeOther != null) form.ServiceScopeOther = dto.ServiceScopeOther;
            if (dto.ServiceType != null) form.ServiceType = dto.ServiceType;
            if (dto.ServiceTypeOther != null) form.ServiceTypeOther = dto.ServiceTypeOther;
            if (dto.ReportedIssue != null) form.ReportedIssue = dto.ReportedIssue;
            if (dto.WorkPerformed != null) form.WorkPerformed = dto.WorkPerformed;
            if (dto.MaterialsUsed != null) form.MaterialsUsed = dto.MaterialsUsed;
            if (dto.IssueStatus != null) form.IssueStatus = dto.IssueStatus;
            if (dto.IssueStatusOther != null) form.IssueStatusOther = dto.IssueStatusOther;
            if (dto.PartsToReplace != null) form.PartsToReplace = dto.PartsToReplace;
            if (dto.Photos != null) form.Photos = dto.Photos;
            if (dto.EtisanFirstName != null) form.EtisanFirstName = dto.EtisanFirstName;
            if (dto.EtisanLastName != null) form.EtisanLastName = dto.EtisanLastName;
            if (dto.EtisanSignature != null) form.EtisanSignature = dto.EtisanSignature;
            if (dto.AuthorityFirstName != null) form.AuthorityFirstName = dto.AuthorityFirstName;
            if (dto.AuthorityLastName != null) form.AuthorityLastName = dto.AuthorityLastName;
            if (dto.AuthorityEmail != null) form.AuthorityEmail = dto.AuthorityEmail;
            if (dto.AuthorityPhone != null) form.AuthorityPhone = dto.AuthorityPhone;
            if (dto.AuthoritySignature != null) form.AuthoritySignature = dto.AuthoritySignature;

            form.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/serviceforms/{id}/submit
        [HttpPut("{id}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var form = await _context.ServiceForms.FindAsync(id);

            if (form == null)
                return NotFound("Form bulunamadi");

            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // SahaElemani rolu icin sadece kendi formlarini gonderebilir
            if (userRole == AppRoles.SahaElemani && userId > 0)
            {
                if (form.CreatedByUserId != userId)
                {
                    return StatusCode(403, "Bu formu gonderme yetkiniz yok");
                }
            }

            if (form.Status == ServiceFormStatuses.Submitted)
                return BadRequest("Form zaten gonderilmis");

            form.Status = ServiceFormStatuses.Submitted;
            form.SubmittedAt = DateTime.UtcNow;
            form.UpdatedAt = DateTime.UtcNow;

            // MaterialsUsed icindeki malzemeleri parse et ve stoktan dus
            var lowStockItems = new List<StockItem>();
            if (!string.IsNullOrEmpty(form.MaterialsUsed))
            {
                await ProcessMaterialsUsed(form, lowStockItems);
            }

            // PDF olustur ve kaydet
            try
            {
                if (form.CreatedByUser == null && form.CreatedByUserId.HasValue)
                {
                    form.CreatedByUser = await _context.Users.FindAsync(form.CreatedByUserId);
                }

                string institution = (form.InstitutionIsOther || form.InstitutionName == "DIGER")
                    ? (!string.IsNullOrEmpty(form.InstitutionOtherName) ? form.InstitutionOtherName : "DigerKurum")
                    : (!string.IsNullOrEmpty(form.InstitutionName) ? form.InstitutionName : "ServisFormu");

                string cleanInstitution = institution
                    .Replace("İ", "I").Replace("ı", "i")
                    .Replace("Ğ", "G").Replace("ğ", "g")
                    .Replace("Ü", "U").Replace("ü", "u")
                    .Replace("Ş", "S").Replace("ş", "s")
                    .Replace("Ö", "O").Replace("ö", "o")
                    .Replace("Ç", "C").Replace("ç", "c");

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    cleanInstitution = cleanInstitution.Replace(c, '_');
                }

                cleanInstitution = cleanInstitution.Replace(' ', '_').Replace('.', '_').Replace(',', '_');

                string formNo = $"Form{form.Id.ToString().PadLeft(4, '0')}";
                string fileName = $"{formNo}_{cleanInstitution}_{form.FormDate:yyyyMMdd}.pdf";

                var pdfBytes = _pdfService.GenerateServiceFormPdf(form);

                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var pdfDir = Path.Combine(wwwrootPath, "pdfs");

                if (!Directory.Exists(pdfDir))
                {
                    Directory.CreateDirectory(pdfDir);
                }

                string contentRootPath = Path.Combine(pdfDir, fileName);

                await System.IO.File.WriteAllBytesAsync(contentRootPath, pdfBytes);

                form.PdfPath = $"/pdfs/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF olusturma hatasi (ID: {Id})", id);
                return StatusCode(500, "PDF olusturulamadi. Lutfen daha sonra tekrar deneyin.");
            }

            await _context.SaveChangesAsync();

            foreach (var item in lowStockItems)
            {
                await SendLowStockNotification(item);
            }

            // Bildirim olustur (Teknik birime)
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Yeni Servis Formu Gonderildi",
                    message: $"{form.InstitutionName} - {form.ServiceType}",
                    type: "ServiceForm",
                    link: $"/service-forms/{form.Id}",
                    relatedId: form.Id,
                    relatedType: "ServiceForm",
                    userId: null,
                    targetRole: AppRoles.Teknik
                );

                if (form.IssueStatus == "ParcaDegistirilecek")
                {
                    var workItem = new WorkItem
                    {
                        Title = $"Parca Degisim Talebi: {form.InstitutionName}",
                        Description = $"Degistirilecek Parcalar: {form.PartsToReplace}\n\nServis Formu Notu: {form.WorkPerformed}",
                        Kind = WorkItemKind.PlannedWork,
                        Stage = WorkItemStage.Backlog,
                        Priority = WorkItemPriority.Normal,
                        InstitutionName = form.InstitutionName,
                        InstitutionOtherName = form.InstitutionOtherName,
                        ShippingAddressLine = form.Address,
                        ContactPerson = $"{form.ContactFirstName} {form.ContactLastName}",
                        ContactPhone = form.Phone,
                        ContactEmail = form.Email,
                        CreatedByUserId = form.CreatedByUserId,
                        RelatedServiceFormId = form.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WorkItems.Add(workItem);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim veya is karti olusturulamadi");
            }

            return Ok(new { message = "Form basariyla gonderildi" });
        }

        private async Task ProcessMaterialsUsed(ServiceForm form, List<StockItem> lowStockItems)
        {
            string institutionName = form.InstitutionIsOther && !string.IsNullOrEmpty(form.InstitutionOtherName)
                ? form.InstitutionOtherName
                : form.InstitutionName ?? "Bilinmeyen Kurum";

            var materials = (form.MaterialsUsed ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .ToArray();

            foreach (var material in materials)
            {
                try
                {
                    int quantity = 1;
                    string itemName = material.Trim();

                    if (itemName.Contains("x"))
                    {
                        var parts = itemName.Split(new[] { 'x' }, 2).Select(p => p.Trim()).ToArray();
                        if (parts.Length == 2 && int.TryParse(parts[0], out int qty))
                        {
                            quantity = qty;
                            itemName = parts[1];
                        }
                    }
                    else if (itemName.Contains("(") && itemName.Contains(")"))
                    {
                        var nameAndQty = itemName.Split('(');
                        if (nameAndQty.Length == 2)
                        {
                            itemName = nameAndQty[0].Trim();
                            var qtyStr = nameAndQty[1].Replace(")", "").Trim();
                            if (int.TryParse(qtyStr, out int qty))
                            {
                                quantity = qty;
                            }
                        }
                    }

                    var stockItem = await _context.StockItems
                        .FirstOrDefaultAsync(s => s.ItemName.ToLower() == itemName.ToLower());

                    if (stockItem != null)
                    {
                        var movement = new StockMovement
                        {
                            StockItemId = stockItem.Id,
                            MovementType = StockMovementTypes.Out,
                            Quantity = quantity,
                            Reference = $"Servis Formu #{form.Id}",
                            InstitutionName = institutionName,
                            Notes = "Servis formu tamamlandi",
                            UserId = form.CreatedByUserId,
                            RelatedEntityType = "ServiceForm",
                            RelatedEntityId = form.Id,
                            MovementDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.StockMovements.Add(movement);

                        stockItem.CurrentStock -= quantity;
                        stockItem.UpdatedAt = DateTime.UtcNow;

                        if (stockItem.CurrentStock <= stockItem.MinimumStock &&
                            !lowStockItems.Any(i => i.Id == stockItem.Id))
                        {
                            lowStockItems.Add(stockItem);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private async Task SendLowStockNotification(StockItem item)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(
                    title: "Kritik Stok Seviyesi",
                    message: $"{item.ItemName} stok seviyesi kritik. Mevcut: {item.CurrentStock}, Min: {item.MinimumStock}",
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
                // Bildirim hatasi akis bozmasin
            }
        }

        // DELETE: api/serviceforms/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var form = await _context.ServiceForms.FindAsync(id);

            if (form == null)
                return NotFound("Form bulunamadi");

            if (userRole == AppRoles.SahaElemani && userId > 0)
            {
                if (form.CreatedByUserId != userId)
                {
                    return StatusCode(403, "Bu formu silme yetkiniz yok");
                }
            }

            if (form.Status == ServiceFormStatuses.Submitted)
                return StatusCode(403, "Gonderilmis formlar silinemez");

            _context.ServiceForms.Remove(form);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/serviceforms/my-draft
        [HttpGet("my-draft")]
        public async Task<ActionResult<ServiceFormDto?>> GetMyDraft()
        {
            var userId = GetCurrentUserId();

            var draft = await _context.ServiceForms
                .Where(f => f.CreatedByUserId == userId && f.Status == ServiceFormStatuses.Draft)
                .OrderByDescending(f => f.UpdatedAt ?? f.CreatedAt)
                .FirstOrDefaultAsync();

            if (draft == null)
                return Ok(null);

#pragma warning disable CS8619
            return await GetById(draft.Id);
#pragma warning restore CS8619
        }

        // GET: api/serviceforms/{id}/pdf/{fileName?}
        [HttpGet("{id}/pdf")]
        [HttpGet("{id}/pdf/{fileName}")]
        public async Task<IActionResult> DownloadPdf(int id, string? fileName = null)
        {
            var userId = GetCurrentUserId();
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            var form = await _context.ServiceForms
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (form == null)
                return NotFound("Form bulunamadi");

            if (userRole == AppRoles.SahaElemani && form.CreatedByUserId != userId)
                return Forbid();

            if (form.Status != ServiceFormStatuses.Submitted)
                return BadRequest("PDF sadece gonderilmis formlar icin olusturulabilir");

            if (!string.IsNullOrEmpty(form.PdfPath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", form.PdfPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    var dbFileName = Path.GetFileName(filePath);

                    // PDF dosyasını doğru isimle serve et
                    Response.Headers["Content-Disposition"] = $"inline; filename=\"{dbFileName}\"";

                    return File(fileBytes, "application/pdf");
                }
            }

            return NotFound("PDF dosyasi bulunamadi. Form gonderilirken bir hata olmus olabilir.");
        }
    }
}
