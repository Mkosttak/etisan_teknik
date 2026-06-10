using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class WorkItemService : IWorkItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkItemService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAuditLogService _auditLogService;

        public WorkItemService(
            ApplicationDbContext context,
            ILogger<WorkItemService> logger,
            INotificationService notificationService,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _auditLogService = auditLogService;
        }

        #region CRUD

        public async Task<PagedResult<WorkItemListDto>> GetAllAsync(WorkItemFilterDto filter)
        {
            var query = _context.WorkItems
                .Include(w => w.Tags)
                .Include(w => w.ChecklistItems)
                .AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(filter.Stage))
            {
                var stage = ParseStageOrDefault(filter.Stage);
                if (stage == WorkItemStage.Backlog)
                    query = query.Where(w => w.Stage == WorkItemStage.Backlog || w.Stage == LegacyInProgressStage);
                else
                    query = query.Where(w => w.Stage == stage);
            }

            if (!string.IsNullOrEmpty(filter.Kind))
            {
                if (Enum.TryParse<WorkItemKind>(filter.Kind, out var kind))
                    query = query.Where(w => w.Kind == kind);
            }

            if (!string.IsNullOrEmpty(filter.Priority))
            {
                if (Enum.TryParse<WorkItemPriority>(filter.Priority, out var priority))
                    query = query.Where(w => w.Priority == priority);
            }



            if (!string.IsNullOrEmpty(filter.InstitutionName))
                query = query.Where(w => w.InstitutionName == filter.InstitutionName);

            if (!string.IsNullOrEmpty(filter.ShippingCity))
                query = query.Where(w => w.ShippingCity == filter.ShippingCity);

            if (!string.IsNullOrEmpty(filter.Tag))
                query = query.Where(w => w.Tags.Any(t => t.TagName == filter.Tag));

            if (filter.DateFrom.HasValue)
                query = query.Where(w => w.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(w => w.CreatedAt <= filter.DateTo.Value);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(w =>
                    w.Title.ToLower().Contains(search) ||
                    (w.Description != null && w.Description.ToLower().Contains(search)) ||
                    (w.InstitutionName != null && w.InstitutionName.ToLower().Contains(search)));
            }

            // Count
            var totalCount = await query.CountAsync();

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "title" => filter.SortDesc ? query.OrderByDescending(w => w.Title) : query.OrderBy(w => w.Title),
                "priority" => filter.SortDesc ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
                "stage" => filter.SortDesc ? query.OrderByDescending(w => w.Stage) : query.OrderBy(w => w.Stage),
                "updatedat" => filter.SortDesc ? query.OrderByDescending(w => w.UpdatedAt) : query.OrderBy(w => w.UpdatedAt),
                "dueat" => filter.SortDesc ? query.OrderByDescending(w => w.DueAt) : query.OrderBy(w => w.DueAt),
                _ => filter.SortDesc ? query.OrderByDescending(w => w.CreatedAt) : query.OrderBy(w => w.CreatedAt)
            };

            // Pagination
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(w => MapToListDto(w))
                .ToListAsync();

            return new PagedResult<WorkItemListDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<KanbanBoardDto> GetKanbanBoardAsync(WorkItemFilterDto filter)
        {
            var query = _context.WorkItems
                .Include(w => w.Tags)
                .Include(w => w.ChecklistItems)
                .AsQueryable();

            // Apply filters except Stage
            if (!string.IsNullOrEmpty(filter.Kind))
            {
                if (Enum.TryParse<WorkItemKind>(filter.Kind, out var kind))
                    query = query.Where(w => w.Kind == kind);
            }

            if (!string.IsNullOrEmpty(filter.Priority))
            {
                if (Enum.TryParse<WorkItemPriority>(filter.Priority, out var priority))
                    query = query.Where(w => w.Priority == priority);
            }



            if (!string.IsNullOrEmpty(filter.InstitutionName))
                query = query.Where(w => w.InstitutionName == filter.InstitutionName);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(w =>
                    w.Title.ToLower().Contains(search) ||
                    (w.Description != null && w.Description.ToLower().Contains(search)));
            }

            var items = await query
                .OrderByDescending(w => w.Priority)
                .ThenByDescending(w => w.UpdatedAt)
                .ToListAsync();

            return new KanbanBoardDto
            {
                Backlog = items.Where(w => w.Stage == WorkItemStage.Backlog || w.Stage == LegacyInProgressStage)
                    .Select(MapToListDto)
                    .ToList(),
                Done = items.Where(w => w.Stage == WorkItemStage.Done).Select(MapToListDto).ToList(),
                Cancelled = items.Where(w => w.Stage == WorkItemStage.Cancelled).Select(MapToListDto).ToList(),
                TotalCount = items.Count
            };
        }

        public async Task<WorkItemDto?> GetByIdAsync(int id)
        {
            var item = await _context.WorkItems
                .Include(w => w.Device)
                .Include(w => w.CreatedByUser)
                .Include(w => w.Tags)
                .Include(w => w.Comments)
                .Include(w => w.Attachments)
                .Include(w => w.ChecklistItems)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (item == null) return null;

            return MapToDto(item);
        }

        public async Task<WorkItemDto> CreateAsync(CreateWorkItemDto dto, int? userId)
        {
            var workItem = new WorkItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Kind = Enum.TryParse<WorkItemKind>(dto.Kind, out var kind) ? kind : WorkItemKind.Request,
                Stage = ParseStageOrDefault(dto.Stage),
                Priority = Enum.TryParse<WorkItemPriority>(dto.Priority, out var priority) ? priority : WorkItemPriority.Normal,
                InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName)) 
                    ? dto.InstitutionOtherName 
                    : dto.InstitutionName,
                InstitutionOtherName = dto.InstitutionOtherName,
                ContactPerson = dto.ContactPerson,
                ContactPhone = dto.ContactPhone,
                ContactEmail = dto.ContactEmail,
                ShippingAddressLine = dto.ShippingAddressLine,
                ShippingCity = dto.ShippingCity,
                ShippingDistrict = dto.ShippingDistrict,
                ShippingPostalCode = dto.ShippingPostalCode,
                PlannedStartAt = dto.PlannedStartAt,
                PlannedEndAt = dto.PlannedEndAt,
                DueAt = dto.DueAt,
                DeviceId = dto.DeviceId,
                SourceFaultId = dto.SourceFaultId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItems.Add(workItem);
            await _context.SaveChangesAsync();

            // Add tags
            if (dto.Tags?.Any() == true)
            {
                foreach (var tagName in dto.Tags.Distinct())
                {
                    _context.WorkItemTags.Add(new WorkItemTag
                    {
                        WorkItemId = workItem.Id,
                        TagName = tagName
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Add history
            await _auditLogService.AddAuditEventAsync("WorkItem", workItem.Id, userId, "Created", $"Title: {workItem.Title}");
            await AddHistoryAsync(workItem.Id, userId, "Created", null, null, workItem.Stage.ToString());

            // Notification
            try
            {
                var priorityText = workItem.Priority == WorkItemPriority.Critical ? "🔴 KRİTİK" :
                                   workItem.Priority == WorkItemPriority.High ? "🟠 Yüksek" : "";
                var title = $"{priorityText} Yeni İş: {workItem.Title}".Trim();
                
                await _notificationService.CreateNotificationAsync(
                    title: title,
                    message: $"Yeni {GetWorkItemKindLabel(workItem.Kind)} oluşturuldu",
                    type: workItem.Priority >= WorkItemPriority.High ? "warning" : "info",
                    link: "/workflow",
                    relatedId: workItem.Id,
                    relatedType: "workitem"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification oluşturulamadı");
            }

            return (await GetByIdAsync(workItem.Id))!;
        }

        public async Task<WorkItemDto?> UpdateAsync(int id, UpdateWorkItemDto dto, int? userId)
        {
            var workItem = await _context.WorkItems
                .Include(w => w.Tags)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workItem == null) return null;

            // Concurrency check
            if (dto.RowVersion != null && workItem.RowVersion != null)
            {
                if (!workItem.RowVersion.SequenceEqual(dto.RowVersion))
                {
                    throw new DbUpdateConcurrencyException("Kayıt başka bir kullanıcı tarafından değiştirilmiş.");
                }
            }

            // Track changes for history
            var changes = new List<(string Field, string? OldValue, string? NewValue)>();

            if (dto.Title != null && dto.Title != workItem.Title)
            {
                changes.Add(("Title", workItem.Title, dto.Title));
                workItem.Title = dto.Title;
            }

            if (dto.Description != null && dto.Description != workItem.Description)
            {
                changes.Add(("Description", "...", "..."));
                workItem.Description = dto.Description;
            }

            if (dto.Kind != null && Enum.TryParse<WorkItemKind>(dto.Kind, out var newKind) && newKind != workItem.Kind)
            {
                changes.Add(("Kind", workItem.Kind.ToString(), newKind.ToString()));
                workItem.Kind = newKind;
            }

            if (dto.Stage != null)
            {
                var newStage = ParseStageOrDefault(dto.Stage);
                var currentStage = NormalizeStage(workItem.Stage);
                if (newStage != currentStage)
                {
                    changes.Add(("Stage", currentStage.ToString(), newStage.ToString()));
                    workItem.Stage = newStage;
                    if (newStage == WorkItemStage.Done)
                        workItem.CompletedAt = DateTime.UtcNow;
                }
                else if (workItem.Stage == LegacyInProgressStage)
                {
                    workItem.Stage = WorkItemStage.Backlog;
                }
            }

            if (dto.Priority != null && Enum.TryParse<WorkItemPriority>(dto.Priority, out var newPriority) && newPriority != workItem.Priority)
            {
                changes.Add(("Priority", workItem.Priority.ToString(), newPriority.ToString()));
                workItem.Priority = newPriority;
            }


            if (dto.InstitutionName != null)
    {
        workItem.InstitutionName = (dto.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(dto.InstitutionOtherName ?? workItem.InstitutionOtherName))
            ? (dto.InstitutionOtherName ?? workItem.InstitutionOtherName)
            : dto.InstitutionName;
    }
    if (dto.InstitutionOtherName != null) 
    {
        workItem.InstitutionOtherName = dto.InstitutionOtherName;
        if (workItem.InstitutionName == "DİĞER" || (dto.InstitutionName == "DİĞER"))
        {
             workItem.InstitutionName = dto.InstitutionOtherName;
        }
    }
            if (dto.ContactPerson != null) workItem.ContactPerson = dto.ContactPerson;
            if (dto.ContactPhone != null) workItem.ContactPhone = dto.ContactPhone;
            if (dto.ContactEmail != null) workItem.ContactEmail = dto.ContactEmail;
            if (dto.ShippingAddressLine != null) workItem.ShippingAddressLine = dto.ShippingAddressLine;
            if (dto.ShippingCity != null) workItem.ShippingCity = dto.ShippingCity;
            if (dto.ShippingDistrict != null) workItem.ShippingDistrict = dto.ShippingDistrict;
            if (dto.ShippingPostalCode != null) workItem.ShippingPostalCode = dto.ShippingPostalCode;
            if (dto.PlannedStartAt.HasValue) workItem.PlannedStartAt = dto.PlannedStartAt;
            if (dto.PlannedEndAt.HasValue) workItem.PlannedEndAt = dto.PlannedEndAt;
            if (dto.DueAt.HasValue) workItem.DueAt = dto.DueAt;
            if (dto.DeviceId.HasValue) workItem.DeviceId = dto.DeviceId;

            workItem.UpdatedAt = DateTime.UtcNow;

            // Update tags
            if (dto.Tags != null)
            {
                var existingTags = workItem.Tags.Select(t => t.TagName).ToList();
                var newTags = dto.Tags.Distinct().ToList();

                // Remove old tags
                var tagsToRemove = workItem.Tags.Where(t => !newTags.Contains(t.TagName)).ToList();
                _context.WorkItemTags.RemoveRange(tagsToRemove);

                // Add new tags
                foreach (var tagName in newTags.Where(t => !existingTags.Contains(t)))
                {
                    _context.WorkItemTags.Add(new WorkItemTag
                    {
                        WorkItemId = workItem.Id,
                        TagName = tagName
                    });
                }
            }

            await _context.SaveChangesAsync();

            var auditChanges = changes.Select(change => new AuditLogChange(change.Field, change.OldValue, change.NewValue)).ToList();
            await _auditLogService.AddAuditLogsAsync("WorkItem", workItem.Id, userId, "Updated", auditChanges);

            // Add history for changes
            foreach (var change in changes)
            {
                await AddHistoryAsync(workItem.Id, userId, "Updated", change.Field, change.OldValue, change.NewValue);
            }

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id, int? userId)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return false;

            _context.WorkItems.Remove(workItem);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Stage & FollowUp

        public async Task<WorkItemDto?> UpdateStageAsync(int id, UpdateStageDto dto, int? userId)
        {
            var workItem = await _context.WorkItems.FindAsync(id);
            if (workItem == null) return null;
            if (string.IsNullOrWhiteSpace(dto.Stage))
                throw new ArgumentException("Gecersiz stage");

            WorkItemStage newStage;
            if (string.Equals(dto.Stage, "InProgress", StringComparison.OrdinalIgnoreCase))
            {
                newStage = WorkItemStage.Backlog;
            }
            else if (!Enum.TryParse<WorkItemStage>(dto.Stage, true, out newStage))
            {
                throw new ArgumentException($"Gecersiz stage: {dto.Stage}");
            }

            var oldStage = NormalizeStage(workItem.Stage);
            if (workItem.Stage == LegacyInProgressStage)
                workItem.Stage = WorkItemStage.Backlog;

            workItem.Stage = newStage;
            workItem.UpdatedAt = DateTime.UtcNow;

            if (newStage == WorkItemStage.Done)
                workItem.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditLogService.AddAuditLogsAsync(
                "WorkItem",
                workItem.Id,
                userId,
                "StageChanged",
                new List<AuditLogChange> { new("Stage", oldStage.ToString(), newStage.ToString()) },
                dto.Notes);

            await AddHistoryAsync(id, userId, "StageChanged", "Stage", oldStage.ToString(), newStage.ToString(), dto.Notes);

            return await GetByIdAsync(id);
        }



        #endregion

        #region Comments

        public async Task<List<WorkItemCommentDto>> GetCommentsAsync(int workItemId)
        {
            return await _context.WorkItemComments
                .Include(c => c.User)
                .Where(c => c.WorkItemId == workItemId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new WorkItemCommentDto
                {
                    Id = c.Id,
                    WorkItemId = c.WorkItemId,
                    UserId = c.UserId,
                    UserName = c.User.FirstName + " " + c.User.LastName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<WorkItemCommentDto> AddCommentAsync(int workItemId, CreateCommentDto dto, int userId)
        {
            var comment = new WorkItemComment
            {
                WorkItemId = workItemId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItemComments.Add(comment);
            await _context.SaveChangesAsync();

            // Update parent timestamp
            var workItem = await _context.WorkItems.FindAsync(workItemId);
            if (workItem != null)
            {
                workItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await AddHistoryAsync(workItemId, userId, "CommentAdded", null, null, null, dto.Content.Length > 50 ? dto.Content.Substring(0, 50) + "..." : dto.Content);

            var user = await _context.Users.FindAsync(userId);

            return new WorkItemCommentDto
            {
                Id = comment.Id,
                WorkItemId = comment.WorkItemId,
                UserId = comment.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmiyor",
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }

        #endregion

        #region Attachments

        public async Task<List<WorkItemAttachmentDto>> GetAttachmentsAsync(int workItemId)
        {
            return await _context.WorkItemAttachments
                .Include(a => a.UploadedByUser)
                .Where(a => a.WorkItemId == workItemId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new WorkItemAttachmentDto
                {
                    Id = a.Id,
                    WorkItemId = a.WorkItemId,
                    FileName = a.FileName,
                    FilePath = $"/api/workitems/{a.WorkItemId}/attachments/{a.Id}/download",
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByUserName = a.UploadedByUser != null ? a.UploadedByUser.FirstName + " " + a.UploadedByUser.LastName : null,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<WorkItemAttachment?> GetAttachmentEntityAsync(int workItemId, int attachmentId)
        {
            return await _context.WorkItemAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.WorkItemId == workItemId);
        }

        public async Task<WorkItemAttachmentDto> AddAttachmentAsync(int workItemId, string fileName, string filePath, long fileSize, string mimeType, int? userId)
        {
            var attachment = new WorkItemAttachment
            {
                WorkItemId = workItemId,
                FileName = fileName,
                FilePath = filePath,
                FileSize = fileSize,
                MimeType = mimeType,
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItemAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            var downloadPath = $"/api/workitems/{workItemId}/attachments/{attachment.Id}/download";
            await AddHistoryAsync(workItemId, userId, "AttachmentAdded", null, null, fileName, downloadPath);

            return new WorkItemAttachmentDto
            {
                Id = attachment.Id,
                WorkItemId = attachment.WorkItemId,
                FileName = attachment.FileName,
                FilePath = downloadPath,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType,
                UploadedByUserId = attachment.UploadedByUserId,
                CreatedAt = attachment.CreatedAt
            };
        }

        public async Task<bool> DeleteAttachmentAsync(int workItemId, int attachmentId, int? userId)
        {
            var attachment = await _context.WorkItemAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.WorkItemId == workItemId);

            if (attachment == null) return false;

            var fileName = attachment.FileName;
            _context.WorkItemAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(workItemId, userId, "AttachmentRemoved", null, fileName, null);

            return true;
        }

        #endregion

        #region Checklist

        public async Task<List<WorkItemChecklistItemDto>> GetChecklistAsync(int workItemId)
        {
            return await _context.WorkItemChecklistItems
                .Include(c => c.CompletedByUser)
                .Where(c => c.WorkItemId == workItemId)
                .OrderBy(c => c.Order)
                .Select(c => new WorkItemChecklistItemDto
                {
                    Id = c.Id,
                    WorkItemId = c.WorkItemId,
                    Title = c.Title,
                    IsCompleted = c.IsCompleted,
                    Order = c.Order,
                    CompletedAt = c.CompletedAt,
                    CompletedByUserId = c.CompletedByUserId,
                    CompletedByUserName = c.CompletedByUser != null ? c.CompletedByUser.FirstName + " " + c.CompletedByUser.LastName : null,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<WorkItemChecklistItemDto> AddChecklistItemAsync(int workItemId, CreateChecklistItemDto dto, int? userId)
        {
            var maxOrder = await _context.WorkItemChecklistItems
                .Where(c => c.WorkItemId == workItemId)
                .MaxAsync(c => (int?)c.Order) ?? 0;

            var item = new WorkItemChecklistItem
            {
                WorkItemId = workItemId,
                Title = dto.Title,
                Order = dto.Order > 0 ? dto.Order : maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItemChecklistItems.Add(item);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(workItemId, userId, "ChecklistItemAdded", null, null, dto.Title);

            return new WorkItemChecklistItemDto
            {
                Id = item.Id,
                WorkItemId = item.WorkItemId,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                Order = item.Order,
                CreatedAt = item.CreatedAt
            };
        }

        public async Task<WorkItemChecklistItemDto?> UpdateChecklistItemAsync(int workItemId, int itemId, UpdateChecklistItemDto dto, int? userId)
        {
            var item = await _context.WorkItemChecklistItems
                .FirstOrDefaultAsync(c => c.Id == itemId && c.WorkItemId == workItemId);

            if (item == null) return null;

            if (dto.Title != null) item.Title = dto.Title;
            if (dto.Order.HasValue) item.Order = dto.Order.Value;

            if (dto.IsCompleted.HasValue && dto.IsCompleted.Value != item.IsCompleted)
            {
                item.IsCompleted = dto.IsCompleted.Value;
                if (dto.IsCompleted.Value)
                {
                    item.CompletedAt = DateTime.UtcNow;
                    item.CompletedByUserId = userId;
                }
                else
                {
                    item.CompletedAt = null;
                    item.CompletedByUserId = null;
                }

                await AddHistoryAsync(workItemId, userId, "ChecklistItemToggled", item.Title, (!dto.IsCompleted.Value).ToString(), dto.IsCompleted.Value.ToString());
            }

            await _context.SaveChangesAsync();

            return new WorkItemChecklistItemDto
            {
                Id = item.Id,
                WorkItemId = item.WorkItemId,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                Order = item.Order,
                CompletedAt = item.CompletedAt,
                CompletedByUserId = item.CompletedByUserId,
                CreatedAt = item.CreatedAt
            };
        }

        public async Task<bool> DeleteChecklistItemAsync(int workItemId, int itemId, int? userId)
        {
            var item = await _context.WorkItemChecklistItems
                .FirstOrDefaultAsync(c => c.Id == itemId && c.WorkItemId == workItemId);

            if (item == null) return false;

            var title = item.Title;
            _context.WorkItemChecklistItems.Remove(item);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(workItemId, userId, "ChecklistItemRemoved", null, title, null);

            return true;
        }

        #endregion

        #region History

        public async Task<List<WorkItemHistoryDto>> GetHistoryAsync(int workItemId)
        {
            var history = await _context.WorkItemHistories
                .Include(h => h.User)
                .Where(h => h.WorkItemId == workItemId)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new WorkItemHistoryDto
                {
                    Id = h.Id,
                    WorkItemId = h.WorkItemId,
                    UserId = h.UserId,
                    UserName = h.User != null ? h.User.FirstName + " " + h.User.LastName : null,
                    Action = h.Action,
                    Field = h.Field,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    Notes = h.Notes,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

            var attachments = await _context.WorkItemAttachments
                .Where(a => a.WorkItemId == workItemId)
                .Select(a => new { a.Id, a.FilePath })
                .ToListAsync();

            var attachmentLookup = attachments
                .GroupBy(a => a.FilePath)
                .ToDictionary(g => g.Key, g => g.First().Id);

            foreach (var item in history)
            {
                if (item.Action == "AttachmentAdded" && !string.IsNullOrEmpty(item.Notes) && attachmentLookup.TryGetValue(item.Notes, out var attachmentId))
                {
                    item.Notes = $"/api/workitems/{workItemId}/attachments/{attachmentId}/download";
                }
            }

            return history;
        }

        private async Task AddHistoryAsync(int workItemId, int? userId, string action, string? field, string? oldValue, string? newValue, string? notes = null)
        {
            var history = new WorkItemHistory
            {
                WorkItemId = workItemId,
                UserId = userId,
                Action = action,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItemHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Tags

        public async Task<List<WorkItemTagDto>> GetTagsAsync(int workItemId)
        {
            return await _context.WorkItemTags
                .Where(t => t.WorkItemId == workItemId)
                .Select(t => new WorkItemTagDto
                {
                    Id = t.Id,
                    TagName = t.TagName,
                    Color = t.Color
                })
                .ToListAsync();
        }

        public async Task<WorkItemTagDto> AddTagAsync(int workItemId, CreateTagDto dto, int? userId)
        {
            var tag = new WorkItemTag
            {
                WorkItemId = workItemId,
                TagName = dto.TagName,
                Color = dto.Color,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkItemTags.Add(tag);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(workItemId, userId, "TagAdded", null, null, dto.TagName);

            return new WorkItemTagDto
            {
                Id = tag.Id,
                TagName = tag.TagName,
                Color = tag.Color
            };
        }

        public async Task<bool> RemoveTagAsync(int workItemId, int tagId, int? userId)
        {
            var tag = await _context.WorkItemTags
                .FirstOrDefaultAsync(t => t.Id == tagId && t.WorkItemId == workItemId);

            if (tag == null) return false;

            var tagName = tag.TagName;
            _context.WorkItemTags.Remove(tag);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(workItemId, userId, "TagRemoved", null, tagName, null);

            return true;
        }

        #endregion

        #region Migration

        public async Task<int> MigrateFromRequestsAsync(int? userId)
        {
            var migratedCount = 0;

            try
            {
                // Get all requests that haven't been migrated yet
                var requests = await _context.Requests
                    .ToListAsync();

                foreach (var request in requests)
                {
                    // Check if already migrated (idempotent)
                    var exists = await _context.WorkItems
                        .AnyAsync(w => w.LegacyRequestId == request.Id);

                    if (exists) continue;

                    // Map Request to WorkItem
                    var workItem = new WorkItem
                    {
                        Title = string.IsNullOrEmpty(request.Description) 
                            ? $"Talep #{request.Id}" 
                            : (request.Description.Length > 200 ? request.Description.Substring(0, 197) + "..." : request.Description),
                        Description = request.Description,
                        Kind = WorkItemKind.Request,
                        Stage = MapRequestStatusToStage(request.Status),
                        Priority = request.IsUrgent ? WorkItemPriority.Critical : WorkItemPriority.Normal,
                        InstitutionName = request.InstitutionName,
                        InstitutionOtherName = request.InstitutionOtherName,
                        ContactPerson = request.ContactPerson,
                        ContactPhone = request.ContactPhone,
                        ShippingAddressLine = request.ShippingAddressLine,
                        ShippingCity = request.ShippingCity,
                        ShippingDistrict = request.ShippingDistrict,
                        DeviceId = null, // Request artık DeviceId içermiyor
                        LegacyRequestId = request.Id,
                        CreatedByUserId = userId,
                        CreatedAt = request.CreatedAt,
                        UpdatedAt = request.UpdatedAt,
                        CompletedAt = request.CompletedAt
                    };

                    _context.WorkItems.Add(workItem);
                    migratedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Migrated {Count} requests to WorkItems", migratedCount);

                return migratedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error migrating requests to WorkItems");
                throw;
            }
        }

        private static WorkItemStage MapRequestStatusToStage(string status)
        {
            return status switch
            {
                "Pending" => WorkItemStage.Backlog,
                "InProgress" => WorkItemStage.Backlog,
                "Completed" => WorkItemStage.Done,
                "Cancelled" => WorkItemStage.Cancelled,
                _ => WorkItemStage.Backlog
            };
        }

        private static readonly WorkItemStage LegacyInProgressStage = (WorkItemStage)1;

        private static WorkItemStage NormalizeStage(WorkItemStage stage)
        {
            return stage.Equals(LegacyInProgressStage) ? WorkItemStage.Backlog : stage;
        }

        private static WorkItemStage ParseStageOrDefault(string? stage)
        {
            if (string.IsNullOrWhiteSpace(stage))
                return WorkItemStage.Backlog;

            if (string.Equals(stage, "InProgress", StringComparison.OrdinalIgnoreCase))
                return WorkItemStage.Backlog;

            return Enum.TryParse<WorkItemStage>(stage, true, out var parsed) ? parsed : WorkItemStage.Backlog;
        }

        #endregion

        #region Mapping Helpers

        private static string GetWorkItemKindLabel(WorkItemKind kind)
        {
            return kind switch
            {
                WorkItemKind.Request => "Talep",
                WorkItemKind.PlannedWork => "Planlı İş",
                _ => "İş"
            };
        }

        private static WorkItemDto MapToDto(WorkItem w)
        {
            return new WorkItemDto
            {
                Id = w.Id,
                Title = w.Title,
                Description = w.Description,
                Kind = w.Kind.ToString(),
                Stage = NormalizeStage(w.Stage).ToString(),
                Priority = w.Priority.ToString(),
                InstitutionName = (w.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(w.InstitutionOtherName)) ? w.InstitutionOtherName : w.InstitutionName,
                InstitutionOtherName = w.InstitutionOtherName,
                ContactPerson = w.ContactPerson,
                ContactPhone = w.ContactPhone,
                ContactEmail = w.ContactEmail,
                ShippingAddressLine = w.ShippingAddressLine,
                ShippingCity = w.ShippingCity,
                ShippingDistrict = w.ShippingDistrict,
                ShippingPostalCode = w.ShippingPostalCode,
                PlannedStartAt = w.PlannedStartAt,
                PlannedEndAt = w.PlannedEndAt,
                DueAt = w.DueAt,
                DeviceId = w.DeviceId,
                DeviceSerialNumber = w.Device?.SerialNumber,
                SourceFaultId = w.SourceFaultId,
                LegacyRequestId = w.LegacyRequestId,
                RelatedServiceFormId = w.RelatedServiceFormId,
                CreatedByUserId = w.CreatedByUserId,
                CreatedByUserName = w.CreatedByUser != null ? $"{w.CreatedByUser.FirstName} {w.CreatedByUser.LastName}" : null,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                CompletedAt = w.CompletedAt,
                TagCount = w.Tags.Count,
                CommentCount = w.Comments.Count,
                AttachmentCount = w.Attachments.Count,
                ChecklistItemCount = w.ChecklistItems.Count,
                ChecklistCompletedCount = w.ChecklistItems.Count(c => c.IsCompleted),
                Tags = w.Tags.Select(t => new WorkItemTagDto
                {
                    Id = t.Id,
                    TagName = t.TagName,
                    Color = t.Color
                }).ToList()
            };
        }

        private static WorkItemListDto MapToListDto(WorkItem w)
        {
            return new WorkItemListDto
            {
                Id = w.Id,
                Title = w.Title,
                Kind = w.Kind.ToString(),
                Stage = NormalizeStage(w.Stage).ToString(),
                Priority = w.Priority.ToString(),

                InstitutionName = (w.InstitutionName == "DİĞER" && !string.IsNullOrEmpty(w.InstitutionOtherName)) ? w.InstitutionOtherName : w.InstitutionName,
                ShippingCity = w.ShippingCity,
                PlannedStartAt = w.PlannedStartAt,
                DueAt = w.DueAt,
                RelatedServiceFormId = w.RelatedServiceFormId,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                ChecklistItemCount = w.ChecklistItems.Count,
                ChecklistCompletedCount = w.ChecklistItems.Count(c => c.IsCompleted),
                TagNames = w.Tags.Select(t => t.TagName).ToList()
            };
        }

        #endregion
    }
}



