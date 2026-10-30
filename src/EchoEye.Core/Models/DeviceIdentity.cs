namespace EchoEye.Core.Models;

/// <summary>
/// Represents the unique identity of a device running EchoEye X
/// </summary>
public class DeviceIdentity
{
    /// <summary>
    /// Unique device identifier (UUID v4)
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Machine name from the operating system
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Operating system version
    /// </summary>
    public string OsVersion { get; set; } = string.Empty;

    /// <summary>
    /// Hardware fingerprint (SHA-256 of hardware identifiers)
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when device was first registered
    /// </summary>
    public DateTime FirstRegistered { get; set; }

    /// <summary>
    /// Timestamp of last activity
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// Encrypted registration key
    /// </summary>
    public byte[] RegistrationKey { get; set; } = Array.Empty<byte>();
}
