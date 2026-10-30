using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Installer.Services;

/// <summary>
/// Initializes default configuration
/// </summary>
public class ConfigurationInitializer
{
    private readonly ILogger _logger;

    public ConfigurationInitializer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize default configuration values
    /// </summary>
    public async Task InitializeAsync()
    {
        using var database = new Database(Paths.DatabasePath);

        _logger.LogInformation("Initializing default configuration");

        // Protection configuration
        await SetConfigValueAsync(database, "protection", "enabled", Defaults.Protection.Enabled, "bool");
        await SetConfigValueAsync(database, "protection", "confidenceThreshold", Defaults.Protection.ConfidenceThreshold, "int");
        await SetConfigValueAsync(database, "protection", "fileVelocityThreshold", Defaults.Protection.FileVelocityThreshold, "int");
        await SetConfigValueAsync(database, "protection", "entropyThreshold", Defaults.Protection.EntropyThreshold, "double");
        await SetConfigValueAsync(database, "protection", "maxFileSizeBytes", Defaults.Protection.MaxFileSizeBytes, "int");

        // Decoy configuration
        await SetConfigValueAsync(database, "decoy", "enabled", Defaults.Decoy.Enabled, "bool");
        await SetConfigValueAsync(database, "decoy", "decoyCount", Defaults.Decoy.DecoyCount, "int");
        await SetConfigValueAsync(database, "decoy", "maxDecoysPerPath", Defaults.Decoy.MaxDecoysPerPath, "int");

        // Recovery configuration
        await SetConfigValueAsync(database, "recovery", "enabled", Defaults.Recovery.Enabled, "bool");
        await SetConfigValueAsync(database, "recovery", "retentionDays", Defaults.Recovery.RetentionDays, "int");
        await SetConfigValueAsync(database, "recovery", "maxStorageGB", Defaults.Recovery.MaxStorageGB, "int");
        await SetConfigValueAsync(database, "recovery", "automaticBackupIntervalHours", Defaults.Recovery.AutomaticBackupIntervalHours, "int");
        await SetConfigValueAsync(database, "recovery", "maxStorageUsagePercentage", Defaults.Recovery.MaxStorageUsagePercentage, "double");

        // Intelligence configuration
        await SetConfigValueAsync(database, "intelligence", "enabled", Defaults.Intelligence.Enabled, "bool");
        await SetConfigValueAsync(database, "intelligence", "sharingEnabled", Defaults.Intelligence.SharingEnabled, "bool");
        await SetConfigValueAsync(database, "intelligence", "peerDiscoveryEnabled", Defaults.Intelligence.PeerDiscoveryEnabled, "bool");
        await SetConfigValueAsync(database, "intelligence", "syncIntervalMinutes", Defaults.Intelligence.SyncIntervalMinutes, "int");
        await SetConfigValueAsync(database, "intelligence", "defaultPeerPort", Defaults.Intelligence.DefaultPeerPort, "int");

        // Logging configuration
        await SetConfigValueAsync(database, "logging", "retentionDays", Defaults.Logging.RetentionDays, "int");
        await SetConfigValueAsync(database, "logging", "maxLogFileSizeMB", Defaults.Logging.MaxLogFileSizeMB, "int");
        await SetConfigValueAsync(database, "logging", "defaultLogLevel", Defaults.Logging.DefaultLogLevel, "string");

        _logger.LogInformation("Default configuration initialized");
    }

    /// <summary>
    /// Set a configuration value
    /// </summary>
    private async Task SetConfigValueAsync(Database database, string section, string key, object value, string valueType)
    {
        var sql = @"
            INSERT OR REPLACE INTO Configuration (
                ConfigId, Section, Key, Value, ValueType, ModifiedAt, ModifiedBy
            ) VALUES (
                @ConfigId, @Section, @Key, @Value, @ValueType, @ModifiedAt, @ModifiedBy
            )";

        var parameters = new[]
        {
            new SqliteParameter("@ConfigId", CryptoHelper.GenerateUuid()),
            new SqliteParameter("@Section", section),
            new SqliteParameter("@Key", key),
            new SqliteParameter("@Value", value.ToString()),
            new SqliteParameter("@ValueType", valueType),
            new SqliteParameter("@ModifiedAt", DateTime.UtcNow.ToString("o")),
            new SqliteParameter("@ModifiedBy", "Installer")
        };

        database.ExecuteNonQuery(sql, parameters);
    }
}
