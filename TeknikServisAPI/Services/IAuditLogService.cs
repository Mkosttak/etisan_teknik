/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IAuditLogService.cs
 * TİP: Service Interface - Audit Log (Denetim Kaydı)
 * ═══════════════════════════════════════════════════════════════════════════
 * AMAÇ: Tüm entity değişikliklerini ve olayları kaydetme
 * SORUMLULUKLAR: Change tracking, event logging
 * IMPLEMENTATION: AuditLogService.cs
 * ═══════════════════════════════════════════════════════════════════════════
 */

namespace TeknikServisAPI.Services
{
    public interface IAuditLogService
    {
        Task AddAuditLogsAsync(string entityName, int entityId, int? userId, string action, IReadOnlyList<AuditLogChange> changes, string? notes = null);
        Task AddAuditEventAsync(string entityName, int entityId, int? userId, string action, string? notes = null);
    }
}
