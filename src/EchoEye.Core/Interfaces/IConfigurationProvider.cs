namespace EchoEye.Core.Interfaces;

/// <summary>
/// Provides configuration management across all modules
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Get a configuration value
    /// </summary>
    T GetValue<T>(string section, string key, T? defaultValue = default);

    /// <summary>
    /// Set a configuration value
    /// </summary>
    void SetValue(string section, string key, object value);

    /// <summary>
    /// Get an entire configuration section
    /// </summary>
    IDictionary<string, object> GetSection(string section);

    /// <summary>
    /// Reload configuration from storage
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Watch for configuration changes
    /// </summary>
    IObservable<ConfigurationChange> WatchChanges(string? section = null);

    /// <summary>
    /// Validate current configuration
    /// </summary>
    ValidationResult Validate();
}

/// <summary>
/// Represents a configuration change event
/// </summary>
public class ConfigurationChange
{
    public string Section { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Result of configuration validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
