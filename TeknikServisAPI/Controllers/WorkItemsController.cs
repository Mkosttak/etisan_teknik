using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Services;

namespace TeknikServisAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkItemsController : ControllerBase
    {
        private readonly IWorkItemService _workItemService;
        private readonly ILogger<WorkItemsController> _logger;
        private readonly IWebHostEnvironment _env;

        public WorkItemsController(IWorkItemService workItemService, ILogger<WorkItemsController> logger, IWebHostEnvironment env)
        {
            _workItemService = workItemService;
            _logger = logger;
            _env = env;
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        #region CRUD

        /// <summary>
        /// Tüm WorkItem'ları listele (pagination + filter)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] WorkItemFilterDto filter)
        {
            try
            {
                var result = await _workItemService.GetAllAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work items");
                return StatusCode(500, new { message = "İş öğeleri alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Kanban board için stage bazlı gruplu liste
        /// </summary>
        [HttpGet("kanban")]
        public async Task<IActionResult> GetKanbanBoard([FromQuery] WorkItemFilterDto filter)
        {
            try
            {
                var result = await _workItemService.GetKanbanBoardAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting kanban board");
                return StatusCode(500, new { message = "Kanban board alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// WorkItem detayı getir
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var item = await _workItemService.GetByIdAsync(id);
                if (item == null)
                    return NotFound(new { message = "İş öğesi bulunamadı" });

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting work item {Id}", id);
                return StatusCode(500, new { message = "İş öğesi alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Yeni WorkItem oluştur
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWorkItemDto dto)
        {
            try
            {
                var item = await _workItemService.CreateAsync(dto, GetUserId());
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating work item");
                return StatusCode(500, new { message = "İş öğesi oluşturulurken hata oluştu" });
            }
        }

        /// <summary>
        /// WorkItem güncelle
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkItemDto dto)
        {
            try
            {
                var item = await _workItemService.UpdateAsync(id, dto, GetUserId());
                if (item == null)
                    return NotFound(new { message = "İş öğesi bulunamadı" });

                return Ok(item);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Kayıt başka bir kullanıcı tarafından değiştirilmiş. Lütfen sayfayı yenileyip tekrar deneyin." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work item {Id}", id);
                return StatusCode(500, new { message = "İş öğesi güncellenirken hata oluştu" });
            }
        }

        /// <summary>
        /// WorkItem sil
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _workItemService.DeleteAsync(id, GetUserId());
                if (!result)
                    return NotFound(new { message = "İş öğesi bulunamadı" });

                return Ok(new { message = "İş öğesi silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting work item {Id}", id);
                return StatusCode(500, new { message = "İş öğesi silinirken hata oluştu" });
            }
        }

        #endregion

        #region Stage

        /// <summary>
        /// Stage değiştir (Kanban sürükle-bırak)
        /// </summary>
        [HttpPatch("{id}/stage")]
        public async Task<IActionResult> UpdateStage(int id, [FromBody] UpdateStageDto dto)
        {
            try
            {
                var item = await _workItemService.UpdateStageAsync(id, dto, GetUserId());
                if (item == null)
                    return NotFound(new { message = "İş öğesi bulunamadı" });

                return Ok(item);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid stage update for work item {Id}", id);
                return BadRequest(new { message = "Geçersiz işlem" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stage for work item {Id}", id);
                return StatusCode(500, new { message = "Durum güncellenirken hata oluştu" });
            }
        }

        #endregion

        #region Comments

        /// <summary>
        /// Yorumları listele
        /// </summary>
        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(int id)
        {
            try
            {
                var comments = await _workItemService.GetCommentsAsync(id);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for work item {Id}", id);
                return StatusCode(500, new { message = "Yorumlar alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Yorum ekle
        /// </summary>
        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] CreateCommentDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });

                var comment = await _workItemService.AddCommentAsync(id, dto, userId.Value);
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to work item {Id}", id);
                return StatusCode(500, new { message = "Yorum eklenirken hata oluştu" });
            }
        }

        #endregion

        #region Attachments

        /// <summary>
        /// Ekleri listele
        /// </summary>
        [HttpGet("{id}/attachments")]
        public async Task<IActionResult> GetAttachments(int id)
        {
            try
            {
                var attachments = await _workItemService.GetAttachmentsAsync(id);
                return Ok(attachments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachments for work item {Id}", id);
                return StatusCode(500, new { message = "Ekler alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Dosya yükle
        /// ⭐ SECURITY: FileUploadHelper ile MIME spoofing, magic bytes validation
        /// </summary>
        [HttpPost("{id}/attachments")]
        public async Task<IActionResult> AddAttachment(int id, IFormFile file)
        {
            try
            {
                // ✅ SECURITY: Comprehensive file validation (MIME, size, magic bytes)
                var (isValid, errorMessage) = Helpers.FileUploadHelper.ValidateFile(file);
                if (!isValid)
                    return BadRequest(new { message = errorMessage });

                // ✅ SECURITY: Secure filename generation (prevents path traversal)
                var originalFileName = Path.GetFileName(file.FileName);
                var secureFileName = Helpers.FileUploadHelper.GenerateSecureFileName(originalFileName);

                // ✅ SECURITY: Store outside wwwroot for extra protection
                var uploadsFolder = Helpers.FileUploadHelper.GetSecureStoragePath(
                    _env.ContentRootPath, "workitems");

                var filePath = Path.Combine(uploadsFolder, secureFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Store relative path for database (will be served via authenticated endpoint)
                var relativePath = $"/secure-uploads/workitems/{secureFileName}";
                var attachment = await _workItemService.AddAttachmentAsync(
                    id, originalFileName, relativePath, file.Length, file.ContentType, GetUserId());

                return Ok(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attachment to work item {Id}", id);
                return StatusCode(500, new { message = "Dosya yüklenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Ek indir
        /// ⭐ SECURITY: Path traversal protection
        /// </summary>
        [HttpGet("{id}/attachments/{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(int id, int attachmentId)
        {
            try
            {
                var attachment = await _workItemService.GetAttachmentEntityAsync(id, attachmentId);
                if (attachment == null)
                    return NotFound(new { message = "Ek bulunamadı" });

                // ✅ SECURITY: Determine storage location based on path prefix
                string baseDirectory;
                string relativePath;
                
                if (attachment.FilePath.StartsWith("/secure-uploads/", StringComparison.OrdinalIgnoreCase) || 
                    attachment.FilePath.StartsWith("secure-uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    // New secure storage (outside wwwroot)
                    baseDirectory = Path.Combine(_env.ContentRootPath, "SecureUploads");
                    relativePath = attachment.FilePath
                        .Replace("/secure-uploads/", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("secure-uploads/", "", StringComparison.OrdinalIgnoreCase)
                        .TrimStart('/', '\\');
                }
                else
                {
                    // Legacy storage (wwwroot) - backward compatibility
                    baseDirectory = Path.Combine(_env.ContentRootPath, "wwwroot");
                    relativePath = attachment.FilePath.TrimStart('/', '\\');
                }

                if (!Directory.Exists(baseDirectory))
                {
                    _logger.LogError("Storage base directory NOT FOUND: {Path}", baseDirectory);
                    return StatusCode(500, new { message = "Sunucu depolama klasörü bulunamadı. Lütfen proje birimine başvurun." });
                }

                var filePath = Path.Combine(baseDirectory, relativePath);
                
                // ✅ SECURITY: Path traversal attack prevention
                var fullPath = Path.GetFullPath(filePath);
                var basePath = Path.GetFullPath(baseDirectory);
                
                if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Path traversal attempt detected: {FilePath}", attachment.FilePath);
                    return BadRequest(new { message = "Geçersiz dosya yolu" });
                }

                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogWarning("Attachment file not found on disk: {FullPath}", fullPath);
                    return NotFound(new { message = "Dosya diskte bulunamadı" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                var contentType = string.IsNullOrEmpty(attachment.MimeType) ? "application/octet-stream" : attachment.MimeType;

                // ⭐ SECURITY & UX: Use File() without the 3rd parameter to avoid built-in header issues with special characters.
                // We'll set the Content-Disposition header manually to ensure 'inline' behavior for viewing.
                var contentDisposition = new System.Net.Mime.ContentDisposition
                {
                    FileName = attachment.FileName,
                    Inline = true // Suggesting to view in browser instead of forced download
                };
                Response.Headers["Content-Disposition"] = contentDisposition.ToString();

                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading attachment {AttachmentId} from work item {Id}. Path: {Path}", attachmentId, id, attachmentId);
                return StatusCode(500, new { message = "Dosya indirilirken hata oluştu: " + ex.Message });
            }
        }

        /// <summary>
        /// Ek sil
        /// </summary>
        [HttpDelete("{id}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int id, int attachmentId)
        {
            try
            {
                var result = await _workItemService.DeleteAttachmentAsync(id, attachmentId, GetUserId());
                if (!result)
                    return NotFound(new { message = "Ek bulunamadı" });

                return Ok(new { message = "Ek silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId} from work item {Id}", attachmentId, id);
                return StatusCode(500, new { message = "Ek silinirken hata oluştu" });
            }
        }

        #endregion

        #region Checklist

        /// <summary>
        /// Checklist listele
        /// </summary>
        [HttpGet("{id}/checklist")]
        public async Task<IActionResult> GetChecklist(int id)
        {
            try
            {
                var items = await _workItemService.GetChecklistAsync(id);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checklist for work item {Id}", id);
                return StatusCode(500, new { message = "Checklist alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Checklist maddesi ekle
        /// </summary>
        [HttpPost("{id}/checklist")]
        public async Task<IActionResult> AddChecklistItem(int id, [FromBody] CreateChecklistItemDto dto)
        {
            try
            {
                var item = await _workItemService.AddChecklistItemAsync(id, dto, GetUserId());
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding checklist item to work item {Id}", id);
                return StatusCode(500, new { message = "Checklist maddesi eklenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Checklist maddesi güncelle
        /// </summary>
        [HttpPatch("{id}/checklist/{itemId}")]
        public async Task<IActionResult> UpdateChecklistItem(int id, int itemId, [FromBody] UpdateChecklistItemDto dto)
        {
            try
            {
                var item = await _workItemService.UpdateChecklistItemAsync(id, itemId, dto, GetUserId());
                if (item == null)
                    return NotFound(new { message = "Checklist maddesi bulunamadı" });

                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating checklist item {ItemId} in work item {Id}", itemId, id);
                return StatusCode(500, new { message = "Checklist maddesi güncellenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Checklist maddesi sil
        /// </summary>
        [HttpDelete("{id}/checklist/{itemId}")]
        public async Task<IActionResult> DeleteChecklistItem(int id, int itemId)
        {
            try
            {
                var result = await _workItemService.DeleteChecklistItemAsync(id, itemId, GetUserId());
                if (!result)
                    return NotFound(new { message = "Checklist maddesi bulunamadı" });

                return Ok(new { message = "Checklist maddesi silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting checklist item {ItemId} from work item {Id}", itemId, id);
                return StatusCode(500, new { message = "Checklist maddesi silinirken hata oluştu" });
            }
        }

        #endregion

        #region History

        /// <summary>
        /// Timeline / History listele
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var history = await _workItemService.GetHistoryAsync(id);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for work item {Id}", id);
                return StatusCode(500, new { message = "Geçmiş alınırken hata oluştu" });
            }
        }

        #endregion

        #region Tags

        /// <summary>
        /// Etiketleri listele
        /// </summary>
        [HttpGet("{id}/tags")]
        public async Task<IActionResult> GetTags(int id)
        {
            try
            {
                var tags = await _workItemService.GetTagsAsync(id);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags for work item {Id}", id);
                return StatusCode(500, new { message = "Etiketler alınırken hata oluştu" });
            }
        }

        /// <summary>
        /// Etiket ekle
        /// </summary>
        [HttpPost("{id}/tags")]
        public async Task<IActionResult> AddTag(int id, [FromBody] CreateTagDto dto)
        {
            try
            {
                var tag = await _workItemService.AddTagAsync(id, dto, GetUserId());
                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tag to work item {Id}", id);
                return StatusCode(500, new { message = "Etiket eklenirken hata oluştu" });
            }
        }

        /// <summary>
        /// Etiket sil
        /// </summary>
        [HttpDelete("{id}/tags/{tagId}")]
        public async Task<IActionResult> RemoveTag(int id, int tagId)
        {
            try
            {
                var result = await _workItemService.RemoveTagAsync(id, tagId, GetUserId());
                if (!result)
                    return NotFound(new { message = "Etiket bulunamadı" });

                return Ok(new { message = "Etiket silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing tag {TagId} from work item {Id}", tagId, id);
                return StatusCode(500, new { message = "Etiket silinirken hata oluştu" });
            }
        }

        #endregion

        #region Migration

        /// <summary>
        /// Request'leri WorkItem'a migrate et (Admin only)
        /// </summary>
        [HttpPost("migrate-requests")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> MigrateRequests()
        {
            try
            {
                var count = await _workItemService.MigrateFromRequestsAsync(GetUserId());
                return Ok(new { message = $"{count} talep başarıyla aktarıldı", count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating requests to work items");
                return StatusCode(500, new { message = "Talepler aktarılırken hata oluştu" });
            }
        }

        #endregion
    }
}
