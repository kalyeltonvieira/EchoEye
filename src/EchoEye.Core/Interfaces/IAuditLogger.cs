namespace EchoEye.Core.Interfaces;

using EchoEye.Core.Models;

/// <summary>
/// Logs audit events for compliance
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Log an audit event
    /// </summary>
    Task LogAsync(AuditEvent auditEvent);

    /// <summary>
    /// Query audit logs
    /// </summary>
    Task<List<AuditLog>> QueryAsync(AuditQuery query);

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    Task<List<AuditLog>> GetEntityTrailAsync(string entityId, string entityType);
}

/// <summary>
/// Represents an audit event to be logged
/// </summary>
public class AuditEvent
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Actor { get; set; }
    public string? Target { get; set; }
    public object? Details { get; set; }
    public string? SourceModule { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public bool IsSensitive { get; set; }
}

/// <summary>
/// Query parameters for audit logs
/// </summary>
public class AuditQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Category { get; set; }
    public string? Action { get; set; }
    public string? Actor { get; set; }
    public string? SourceModule { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}
