namespace EchoEye.Core.Models;

/// <summary>
/// Represents health status of a system component
/// </summary>
public class SystemHealth
{
    /// <summary>
    /// Unique health identifier (UUID v4)
    /// </summary>
    public string HealthId { get; set; } = string.Empty;

    /// <summary>
    /// Device being monitored
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Module being reported on
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Health status
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Timestamp when health was recorded
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Health metrics (JSON)
    /// </summary>
    public string? Metrics { get; set; }

    /// <summary>
    /// Additional details (JSON)
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Health status levels
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Component is healthy
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// Component is degraded but functional
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// Component is unhealthy
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// Component status is unknown
    /// </summary>
    Unknown = 4
}
