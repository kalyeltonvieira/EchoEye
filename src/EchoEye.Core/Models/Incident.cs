namespace EchoEye.Core.Models;

/// <summary>
/// Represents a security incident detected by the system
/// </summary>
public class Incident
{
    /// <summary>
    /// Unique incident identifier (UUID v4)
    /// </summary>
    public string IncidentId { get; set; } = string.Empty;

    /// <summary>
    /// Device where incident occurred
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the incident
    /// </summary>
    public IncidentStatus Status { get; set; }

    /// <summary>
    /// Confidence level of the detection
    /// </summary>
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Timestamp when incident was detected
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// Timestamp when incident was contained
    /// </summary>
    public DateTime? ContainedAt { get; set; }

    /// <summary>
    /// Timestamp when incident was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Associated threat family identifier
    /// </summary>
    public string? ThreatFamilyId { get; set; }

    /// <summary>
    /// Number of files affected by the incident
    /// </summary>
    public int AffectedFileCount { get; set; }

    /// <summary>
    /// Total size of affected data in bytes
    /// </summary>
    public long AffectedDataSizeBytes { get; set; }

    /// <summary>
    /// Containment action taken
    /// </summary>
    public string? ContainmentAction { get; set; }

    /// <summary>
    /// Brief summary of the incident
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Detailed incident information (JSON)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Path to forensic evidence
    /// </summary>
    public string? ForensicEvidencePath { get; set; }

    /// <summary>
    /// Whether this was a false positive
    /// </summary>
    public bool IsFalsePositive { get; set; }

    /// <summary>
    /// Reason if marked as false positive
    /// </summary>
    public string? FalsePositiveReason { get; set; }
}

/// <summary>
/// Status of an incident throughout its lifecycle
/// </summary>
public enum IncidentStatus
{
    /// <summary>
    /// Incident has been detected
    /// </summary>
    Detected = 1,

    /// <summary>
    /// Containment actions are in progress
    /// </summary>
    Containing = 2,

    /// <summary>
    /// Incident has been contained
    /// </summary>
    Contained = 3,

    /// <summary>
    /// Incident is under investigation
    /// </summary>
    Investigating = 4,

    /// <summary>
    /// Recovery operations are in progress
    /// </summary>
    Recovering = 5,

    /// <summary>
    /// Incident has been resolved
    /// </summary>
    Resolved = 6,

    /// <summary>
    /// Incident was determined to be a false positive
    /// </summary>
    FalsePositive = 7
}

/// <summary>
/// Confidence level for threat detection
/// </summary>
public enum ConfidenceLevel
{
    /// <summary>
    /// Low confidence - informational only
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence - requires investigation
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence - containment recommended
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical confidence - immediate action required
    /// </summary>
    Critical = 4
}
