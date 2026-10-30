namespace EchoEye.Core.Models;

/// <summary>
/// Represents a recovery/restore operation
/// </summary>
public class RecoveryOperation
{
    /// <summary>
    /// Unique operation identifier (UUID v4)
    /// </summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>
    /// Associated incident identifier
    /// </summary>
    public string? IncidentId { get; set; }

    /// <summary>
    /// Recovery point being used for restoration
    /// </summary>
    public string RecoveryPointId { get; set; } = string.Empty;

    /// <summary>
    /// Type of recovery operation
    /// </summary>
    public RecoveryOperationType Type { get; set; }

    /// <summary>
    /// Timestamp when operation started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when operation completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current status of the operation
    /// </summary>
    public RecoveryStatus Status { get; set; }

    /// <summary>
    /// Number of files attempted to restore
    /// </summary>
    public int FilesAttempted { get; set; }

    /// <summary>
    /// Number of files successfully restored
    /// </summary>
    public int FilesSucceeded { get; set; }

    /// <summary>
    /// Number of files that failed to restore
    /// </summary>
    public int FilesFailed { get; set; }

    /// <summary>
    /// Total bytes restored
    /// </summary>
    public long BytesRestored { get; set; }

    /// <summary>
    /// Error details if operation failed
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Summary of the operation
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
/// Types of recovery operations
/// </summary>
public enum RecoveryOperationType
{
    /// <summary>
    /// Full restore from recovery point
    /// </summary>
    FullRestore = 1,

    /// <summary>
    /// Selective restore of specific files
    /// </summary>
    SelectiveRestore = 2,

    /// <summary>
    /// Restore of a single file
    /// </summary>
    SingleFileRestore = 3
}

/// <summary>
/// Status of a recovery operation
/// </summary>
public enum RecoveryStatus
{
    /// <summary>
    /// Operation is pending
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Operation is in progress
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Operation completed successfully
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Operation partially succeeded
    /// </summary>
    PartialSuccess = 4,

    /// <summary>
    /// Operation failed
    /// </summary>
    Failed = 5,

    /// <summary>
    /// Operation was cancelled
    /// </summary>
    Cancelled = 6
}
