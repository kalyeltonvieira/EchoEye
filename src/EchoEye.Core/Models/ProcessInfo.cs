namespace EchoEye.Core.Models;

/// <summary>
/// Represents information about a running process
/// </summary>
public class ProcessInfo
{
    /// <summary>
    /// Process ID
    /// </summary>
    public string ProcessId { get; set; } = string.Empty;

    /// <summary>
    /// Device where process is running
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Process name
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the executable
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Command line arguments
    /// </summary>
    public string? CommandLine { get; set; }

    /// <summary>
    /// Timestamp when process started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Timestamp when process ended
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Reputation score of the process
    /// </summary>
    public ProcessReputation Reputation { get; set; }

    /// <summary>
    /// Parent process ID
    /// </summary>
    public string? ParentProcessId { get; set; }

    /// <summary>
    /// Number of file modifications by this process
    /// </summary>
    public int FileModificationCount { get; set; }

    /// <summary>
    /// Number of decoy interactions by this process
    /// </summary>
    public int DecoyInteractionCount { get; set; }

    /// <summary>
    /// Digital signature status
    /// </summary>
    public string? SignatureStatus { get; set; }

    /// <summary>
    /// Software publisher
    /// </summary>
    public string? Publisher { get; set; }
}

/// <summary>
/// Reputation levels for processes
/// </summary>
public enum ProcessReputation
{
    /// <summary>
    /// Reputation unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Known trusted process
    /// </summary>
    Trusted = 1,

    /// <summary>
    /// Suspicious process
    /// </summary>
    Suspicious = 2,

    /// <summary>
    /// Known malicious process
    /// </summary>
    Malicious = 3
}

public class ProcessEvent
{
    public string ProcessId { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ProcessEventType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public object? Details { get; set; }
}

public enum ProcessEventType
{
    Started = 1,
    Ended = 2,
    ReputationChanged = 3,
    SuspiciousActivity = 4
}
