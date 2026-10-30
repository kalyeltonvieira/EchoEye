namespace EchoEye.Core.Models;

/// <summary>
/// Represents a threat intelligence report
/// </summary>
public class ThreatReport
{
    /// <summary>
    /// Unique report identifier (UUID v4)
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Threat family identifier
    /// </summary>
    public string ThreatFamilyId { get; set; } = string.Empty;

    /// <summary>
    /// Device that generated the report
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// First time this threat was seen
    /// </summary>
    public DateTime? FirstSeen { get; set; }

    /// <summary>
    /// Last time this threat was seen
    /// </summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Behavioral fingerprint of the threat
    /// </summary>
    public string BehavioralFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// List of file hashes (SHA-256) - JSON array
    /// </summary>
    public string? FileHashes { get; set; }

    /// <summary>
    /// List of process indicators - JSON array
    /// </summary>
    public string? ProcessIndicators { get; set; }

    /// <summary>
    /// List of network indicators - JSON array
    /// </summary>
    public string? NetworkIndicators { get; set; }

    /// <summary>
    /// Geographic region (country-level only)
    /// </summary>
    public string? GeographicRegion { get; set; }

    /// <summary>
    /// Number of times this threat was detected
    /// </summary>
    public int DetectionCount { get; set; }

    /// <summary>
    /// Number of successful recoveries
    /// </summary>
    public int RecoverySuccessCount { get; set; }

    /// <summary>
    /// Number of failed recoveries
    /// </summary>
    public int RecoveryFailureCount { get; set; }

    /// <summary>
    /// Average confidence score across detections
    /// </summary>
    public double AverageConfidenceScore { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this report has been shared
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Timestamp when report was shared
    /// </summary>
    public DateTime? SharedAt { get; set; }
}
