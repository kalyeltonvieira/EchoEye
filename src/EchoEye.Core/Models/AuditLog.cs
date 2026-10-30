namespace EchoEye.Core.Models;

/// <summary>
/// Represents an audit log entry for compliance
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique log identifier (UUID v4)
    /// </summary>
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// Device where event occurred
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the event
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Category of the event
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Action that was performed
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Actor who performed the action
    /// </summary>
    public string? Actor { get; set; }

    /// <summary>
    /// Target of the action
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Additional details (JSON)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Module that generated the log
    /// </summary>
    public string? SourceModule { get; set; }

    /// <summary>
    /// Related entity identifier
    /// </summary>
    public string? RelatedEntityId { get; set; }

    /// <summary>
    /// Type of related entity
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Whether this log contains sensitive information
    /// </summary>
    public bool IsSensitive { get; set; }
}
