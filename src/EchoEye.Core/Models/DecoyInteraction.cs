namespace EchoEye.Core.Models;

/// <summary>
/// Represents an interaction with a decoy asset
/// </summary>
public class DecoyInteraction
{
    /// <summary>
    /// Unique interaction identifier (UUID v4)
    /// </summary>
    public string InteractionId { get; set; } = string.Empty;

    /// <summary>
    /// Decoy that was interacted with
    /// </summary>
    public string DecoyId { get; set; } = string.Empty;

    /// <summary>
    /// Associated incident identifier
    /// </summary>
    public string? IncidentId { get; set; }

    /// <summary>
    /// Type of interaction
    /// </summary>
    public InteractionType Type { get; set; }

    /// <summary>
    /// Timestamp when interaction occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Process ID that caused the interaction
    /// </summary>
    public string? ProcessId { get; set; }

    /// <summary>
    /// Name of the process
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Full path to the process executable
    /// </summary>
    public string? ProcessPath { get; set; }

    /// <summary>
    /// SHA-256 hash of original file
    /// </summary>
    public string? OriginalHash { get; set; }

    /// <summary>
    /// SHA-256 hash after interaction
    /// </summary>
    public string? NewHash { get; set; }

    /// <summary>
    /// Confidence score that this indicates malicious activity
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Additional details (JSON)
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Types of interactions with decoys
/// </summary>
public enum InteractionType
{
    /// <summary>
    /// File was read
    /// </summary>
    Read = 1,

    /// <summary>
    /// File was written to
    /// </summary>
    Write = 2,

    /// <summary>
    /// File was deleted
    /// </summary>
    Delete = 3,

    /// <summary>
    /// File was renamed
    /// </summary>
    Rename = 4,

    /// <summary>
    /// File was encrypted
    /// </summary>
    Encrypt = 5
}
