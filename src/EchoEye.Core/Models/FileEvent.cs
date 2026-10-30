namespace EchoEye.Core.Models;

/// <summary>
/// Represents a file system event captured by the monitoring system
/// </summary>
public class FileEvent
{
    /// <summary>
    /// Unique event identifier (UUID v4)
    /// </summary>
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Associated incident identifier
    /// </summary>
    public string? IncidentId { get; set; }

    /// <summary>
    /// Full path to the file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of change that occurred
    /// </summary>
    public FileChangeType ChangeType { get; set; }

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Process ID that caused the event
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
    /// Original file size before change
    /// </summary>
    public long OriginalSize { get; set; }

    /// <summary>
    /// New file size after change
    /// </summary>
    public long NewSize { get; set; }

    /// <summary>
    /// SHA-256 hash of original file
    /// </summary>
    public string? OriginalHash { get; set; }

    /// <summary>
    /// SHA-256 hash of new file
    /// </summary>
    public string? NewHash { get; set; }

    /// <summary>
    /// Entropy score (0-8) indicating randomness
    /// </summary>
    public double EntropyScore { get; set; }

    /// <summary>
    /// Whether this file is a decoy
    /// </summary>
    public bool IsDecoy { get; set; }

    /// <summary>
    /// Related decoy identifier if applicable
    /// </summary>
    public string? RelatedDecoyId { get; set; }
}

/// <summary>
/// Types of file changes that can be detected
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// File was created
    /// </summary>
    Created = 1,

    /// <summary>
    /// File was modified
    /// </summary>
    Modified = 2,

    /// <summary>
    /// File was deleted
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// File was renamed
    /// </summary>
    Renamed = 4,

    /// <summary>
    /// File was encrypted (detected via entropy)
    /// </summary>
    Encrypted = 5
}
