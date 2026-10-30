namespace EchoEye.Core.Models;

/// <summary>
/// Represents a family of related threats
/// </summary>
public class ThreatFamily
{
    /// <summary>
    /// Unique family identifier (UUID v4)
    /// </summary>
    public string FamilyId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the threat family
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of threat
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Description of the threat family
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// First time this family was detected
    /// </summary>
    public DateTime FirstDetected { get; set; }

    /// <summary>
    /// Last time this family was detected
    /// </summary>
    public DateTime LastDetected { get; set; }

    /// <summary>
    /// Total number of detections
    /// </summary>
    public int TotalDetections { get; set; }

    /// <summary>
    /// List of known file hashes - JSON array
    /// </summary>
    public string? KnownHashes { get; set; }

    /// <summary>
    /// List of known processes - JSON array
    /// </summary>
    public string? KnownProcesses { get; set; }

    /// <summary>
    /// Behavioral signature
    /// </summary>
    public string? BehavioralSignature { get; set; }

    /// <summary>
    /// Severity level
    /// </summary>
    public ThreatSeverity Severity { get; set; }

    /// <summary>
    /// Mitigation advice
    /// </summary>
    public string? MitigationAdvice { get; set; }

    /// <summary>
    /// Whether this family is still active
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Severity levels for threat families
/// </summary>
public enum ThreatSeverity
{
    /// <summary>
    /// Low severity threat
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity threat
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity threat
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity threat
    /// </summary>
    Critical = 4
}
