namespace EchoEye.Core.Interfaces;

using EchoEye.Core.Models;

/// <summary>
/// Reports health status from all modules
/// </summary>
public interface IHealthReporter
{
    /// <summary>
    /// Report health status for a module
    /// </summary>
    Task ReportHealthAsync(string module, HealthStatus status, string? message = null,
                          Dictionary<string, object>? metrics = null);

    /// <summary>
    /// Get health status of a specific module
    /// </summary>
    Task<SystemHealth?> GetModuleHealthAsync(string module);

    /// <summary>
    /// Get overall system health summary
    /// </summary>
    Task<SystemHealthSummary> GetSystemHealthAsync();

    /// <summary>
    /// Check if the system is healthy
    /// </summary>
    Task<bool> IsHealthyAsync();
}

/// <summary>
/// Summary of system health across all modules
/// </summary>
public class SystemHealthSummary
{
    public HealthStatus OverallStatus { get; set; }
    public Dictionary<string, HealthStatus> ModuleStatus { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public List<string> Issues { get; set; } = new();
}
