/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IWorkItemService.cs
 * TİP: Service Interface - İş Akışı (Kanban) Yönetimi
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Kanban board, work items, comments, attachments, checklist, tags
 * SORUMLULUKLAR: Full Kanban workflow management
 * IMPLEMENTATION: WorkItemService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public interface IWorkItemService
    {
        // CRUD
        Task<PagedResult<WorkItemListDto>> GetAllAsync(WorkItemFilterDto filter);
        Task<KanbanBoardDto> GetKanbanBoardAsync(WorkItemFilterDto filter);
        Task<WorkItemDto?> GetByIdAsync(int id);
        Task<WorkItemDto> CreateAsync(CreateWorkItemDto dto, int? userId);
        Task<WorkItemDto?> UpdateAsync(int id, UpdateWorkItemDto dto, int? userId);
        Task<bool> DeleteAsync(int id, int? userId);

        // Stage
        Task<WorkItemDto?> UpdateStageAsync(int id, UpdateStageDto dto, int? userId);

        // Comments
        Task<List<WorkItemCommentDto>> GetCommentsAsync(int workItemId);
        Task<WorkItemCommentDto> AddCommentAsync(int workItemId, CreateCommentDto dto, int userId);

        // Attachments
        Task<List<WorkItemAttachmentDto>> GetAttachmentsAsync(int workItemId);
        Task<WorkItemAttachment?> GetAttachmentEntityAsync(int workItemId, int attachmentId);
        Task<WorkItemAttachmentDto> AddAttachmentAsync(int workItemId, string fileName, string filePath, long fileSize, string mimeType, int? userId);
        Task<bool> DeleteAttachmentAsync(int workItemId, int attachmentId, int? userId);

        // Checklist
        Task<List<WorkItemChecklistItemDto>> GetChecklistAsync(int workItemId);
        Task<WorkItemChecklistItemDto> AddChecklistItemAsync(int workItemId, CreateChecklistItemDto dto, int? userId);
        Task<WorkItemChecklistItemDto?> UpdateChecklistItemAsync(int workItemId, int itemId, UpdateChecklistItemDto dto, int? userId);
        Task<bool> DeleteChecklistItemAsync(int workItemId, int itemId, int? userId);

        // History
        Task<List<WorkItemHistoryDto>> GetHistoryAsync(int workItemId);

        // Tags
        Task<List<WorkItemTagDto>> GetTagsAsync(int workItemId);
        Task<WorkItemTagDto> AddTagAsync(int workItemId, CreateTagDto dto, int? userId);
        Task<bool> RemoveTagAsync(int workItemId, int tagId, int? userId);

        // Migration
        Task<int> MigrateFromRequestsAsync(int? userId);
    }
}
