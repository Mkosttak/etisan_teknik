namespace TeknikServisAPI.Services
{
    public record AuditLogChange(string Field, string? OldValue, string? NewValue);
}
