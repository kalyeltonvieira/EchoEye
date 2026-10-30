namespace EchoEye.Core.Models;

/// <summary>
/// Represents a decoy asset deployed for honeypot detection
/// </summary>
public class DecoyAsset
{
    /// <summary>
    /// Unique decoy identifier (UUID v4)
    /// </summary>
    public string DecoyId { get; set; } = string.Empty;

    /// <summary>
    /// Device where decoy is deployed
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the decoy file
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of decoy
    /// </summary>
    public DecoyType Type { get; set; }

    /// <summary>
    /// Timestamp when decoy was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp of last access
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Timestamp of last modification
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Number of times the decoy was accessed
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// Number of times the decoy was modified
    /// </summary>
    public int ModificationCount { get; set; }

    /// <summary>
    /// Whether the decoy is currently active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// SHA-256 hash of decoy content
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Types of decoy assets
/// </summary>
public enum DecoyType
{
    /// <summary>
    /// Document file decoy (docx, xlsx, pdf)
    /// </summary>
    Document = 1,

    /// <summary>
    /// Image file decoy (jpg, png)
    /// </summary>
    Image = 2,

    /// <summary>
    /// Database file decoy (db, sqlite)
    /// </summary>
    Database = 3,

    /// <summary>
    /// Archive file decoy (zip, rar)
    /// </summary>
    Archive = 4,

    /// <summary>
    /// Configuration file decoy (json, xml, ini)
    /// </summary>
    Configuration = 5
}
