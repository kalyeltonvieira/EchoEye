namespace EchoEye.Core.Models;

/// <summary>
/// Represents a recovery point (backup) created by the system
/// </summary>
public class RecoveryPoint
{
    /// <summary>
    /// Unique recovery point identifier (UUID v4)
    /// </summary>
    public string RecoveryPointId { get; set; } = string.Empty;

    /// <summary>
    /// Device where recovery point was created
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of recovery point
    /// </summary>
    public RecoveryPointType Type { get; set; }

    /// <summary>
    /// Timestamp when recovery point was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of files in the recovery point
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total size of all files in bytes
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// SHA-256 checksum of the recovery point manifest
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Path where recovery point is stored
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether the recovery point is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Timestamp of last validation
    /// </summary>
    public DateTime? ValidatedAt { get; set; }

    /// <summary>
    /// Validation errors if any
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// Whether this was an automatic recovery point
    /// </summary>
    public bool IsAutomatic { get; set; }

    /// <summary>
    /// Associated incident if created in response
    /// </summary>
    public string? RelatedIncidentId { get; set; }

    /// <summary>
    /// Timestamp when recovery point expires
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Types of recovery points
/// </summary>
public enum RecoveryPointType
{
    /// <summary>
    /// Full backup of all files
    /// </summary>
    Full = 1,

    /// <summary>
    /// Incremental backup since last point
    /// </summary>
    Incremental = 2,

    /// <summary>
    /// Differential backup since last full
    /// </summary>
    Differential = 3
}

public class RecoveryPointFile
{
    public string FileId { get; set; } = string.Empty;
    public string RecoveryPointId { get; set; } = string.Empty;
    public string OriginalPath { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime OriginalModifiedTime { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? CompressionAlgorithm { get; set; }
    public long? CompressedSize { get; set; }
    public bool IsEncrypted { get; set; }
}
