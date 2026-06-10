using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public class AuditLogService : IAuditLogService
    {
        private const int MaxValueLength = 1000;
        private readonly ApplicationDbContext _context;

        public AuditLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAuditLogsAsync(string entityName, int entityId, int? userId, string action, IReadOnlyList<AuditLogChange> changes, string? notes = null)
        {
            if (changes == null || changes.Count == 0)
            {
                return;
            }

            var version = await GetNextVersionAsync(entityName, entityId);

            foreach (var change in changes)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    UserId = userId,
                    Action = action,
                    Field = change.Field,
                    OldValue = NormalizeValue(change.OldValue, change.Field),
                    NewValue = NormalizeValue(change.NewValue, change.Field),
                    Notes = notes,
                    Version = version,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task AddAuditEventAsync(string entityName, int entityId, int? userId, string action, string? notes = null)
        {
            var version = await GetNextVersionAsync(entityName, entityId);

            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                Action = action,
                Notes = notes,
                Version = version,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<int> GetNextVersionAsync(string entityName, int entityId)
        {
            var current = await _context.AuditLogs
                .Where(a => a.EntityName == entityName && a.EntityId == entityId)
                .MaxAsync(a => (int?)a.Version) ?? 0;

            return current + 1;
        }

        /// <summary>
        /// Hassas alanları maskeler (KVKK/GDPR uyumu)
        /// </summary>
        private static readonly HashSet<string> SensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Email", "PasswordHash", "Password", "PhoneNumber", "Phone",
            "IdNumber", "TcNo", "SocialSecurityNumber", "CreditCard",
            "BankAccount", "Iban", "Token", "SecretKey", "ApiKey"
        };

        private static string? NormalizeValue(string? value, string? fieldName = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // ⭐ PII Maskeleme - KVKK/GDPR uyumlu loglama
            if (!string.IsNullOrEmpty(fieldName) && SensitiveFields.Contains(fieldName))
            {
                return "***MASKED***";
            }

            return value.Length <= MaxValueLength ? value : value.Substring(0, MaxValueLength);
        }
    }
}
