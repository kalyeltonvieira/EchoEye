using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Protection.Services;

/// <summary>
/// Reports health status for the Protection module
/// </summary>
public class HealthReporter : IHealthReporter
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public HealthReporter(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task ReportHealthAsync(string module, HealthStatus status, string? message = null,
                                       Dictionary<string, object>? metrics = null)
    {
        try
        {
            var health = new SystemHealth
            {
                HealthId = CryptoHelper.GenerateUuid(),
                DeviceId = await GetDeviceIdAsync(),
                Module = module,
                Status = status,
                RecordedAt = DateTime.UtcNow,
                Message = message,
                Metrics = metrics != null ? JsonHelper.Serialize(metrics) : null
            };

            var sql = @"
                INSERT INTO SystemHealth (
                    HealthId, DeviceId, Module, Status, RecordedAt, Message, Metrics
                ) VALUES (
                    @HealthId, @DeviceId, @Module, @Status, @RecordedAt, @Message, @Metrics
                )";

            var parameters = new[]
            {
                new SqliteParameter("@HealthId", health.HealthId),
                new SqliteParameter("@DeviceId", health.DeviceId),
                new SqliteParameter("@Module", health.Module),
                new SqliteParameter("@Status", (int)health.Status),
                new SqliteParameter("@RecordedAt", health.RecordedAt.ToString("o")),
                new SqliteParameter("@Message", health.Message ?? (object)DBNull.Value),
                new SqliteParameter("@Metrics", health.Metrics ?? (object)DBNull.Value)
            };

            _database.ExecuteNonQuery(sql, parameters);

            _logger.LogDebug("Health reported for module {Module}: {Status}", module, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report health for module {Module}", module);
        }
    }

    public async Task<SystemHealth?> GetModuleHealthAsync(string module)
    {
        try
        {
            var sql = @"
                SELECT * FROM SystemHealth
                WHERE Module = @Module
                ORDER BY RecordedAt DESC
                LIMIT 1";

            var parameters = new[]
            {
                new SqliteParameter("@Module", module)
            };

            var results = _database.ExecuteQuery(sql, reader =>
            {
                return new SystemHealth
                {
                    HealthId = reader.GetString("HealthId"),
                    DeviceId = reader.GetString("DeviceId"),
                    Module = reader.GetString("Module"),
                    Status = (HealthStatus)reader.GetInt32("Status"),
                    RecordedAt = DateTime.Parse(reader.GetString("RecordedAt")),
                    Message = reader.IsDBNull("Message") ? null : reader.GetString("Message"),
                    Metrics = reader.IsDBNull("Metrics") ? null : reader.GetString("Metrics")
                };
            }, parameters);

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health for module {Module}", module);
            return null;
        }
    }

    public async Task<SystemHealthSummary> GetSystemHealthAsync()
    {
        var summary = new SystemHealthSummary
        {
            ModuleStatus = new Dictionary<string, HealthStatus>(),
            LastUpdated = DateTime.UtcNow,
            Issues = new List<string>()
        };

        try
        {
            var sql = @"
                SELECT Module, Status, Message
                FROM SystemHealth
                WHERE RecordedAt > datetime('now', '-1 hour')
                GROUP BY Module
                ORDER BY RecordedAt DESC";

            var results = _database.ExecuteQuery(sql, reader =>
            {
                var module = reader.GetString("Module");
                var status = (HealthStatus)reader.GetInt32("Status");
                var message = reader.IsDBNull("Message") ? null : reader.GetString("Message");

                summary.ModuleStatus[module] = status;

                if (status != HealthStatus.Healthy && !string.IsNullOrEmpty(message))
                {
                    summary.Issues.Add($"{module}: {message}");
                }

                return true;
            });

            // Determine overall status
            if (summary.ModuleStatus.Values.Any(s => s == HealthStatus.Unhealthy))
            {
                summary.OverallStatus = HealthStatus.Unhealthy;
            }
            else if (summary.ModuleStatus.Values.Any(s => s == HealthStatus.Degraded))
            {
                summary.OverallStatus = HealthStatus.Degraded;
            }
            else
            {
                summary.OverallStatus = HealthStatus.Healthy;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health summary");
            summary.OverallStatus = HealthStatus.Unknown;
        }

        return summary;
    }

    public async Task<bool> IsHealthyAsync()
    {
        var summary = await GetSystemHealthAsync();
        return summary.OverallStatus == HealthStatus.Healthy;
    }

    private async Task<string> GetDeviceIdAsync()
    {
        var sql = "SELECT DeviceId FROM DeviceIdentity LIMIT 1";
        var result = _database.ExecuteScalar(sql);
        return result?.ToString() ?? string.Empty;
    }
}
