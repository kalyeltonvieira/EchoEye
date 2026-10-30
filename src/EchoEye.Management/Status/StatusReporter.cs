using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using EchoEye.Intelligence;
using EchoEye.Recovery;
using Microsoft.Extensions.Logging;
using IncidentModel = EchoEye.Core.Models.Incident;

namespace EchoEye.Management.Status;

public interface IStatusReporter
{
    Task<SystemStatus> GetSystemStatusAsync();
    Task<ModuleStatus?> GetModuleStatusAsync(string module);
    Task<DashboardData> GetDashboardDataAsync();
    Task<StatusReport> GenerateReportAsync(StatusReportOptions options);
}

/// <summary>
/// Aggregates and reports system status
/// </summary>
public class StatusReporter : IStatusReporter
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public StatusReporter(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<SystemStatus> GetSystemStatusAsync()
    {
        var moduleStatuses = await GetAllModuleStatusesAsync();
        var activeAlerts = await GetActiveAlertsAsync();
        var activeIncidents = await GetActiveIncidentsAsync();

        var overallStatus = moduleStatuses.Values.Any(s => s == HealthStatus.Unhealthy)
            ? HealthStatus.Unhealthy
            : moduleStatuses.Values.Any(s => s == HealthStatus.Degraded)
                ? HealthStatus.Degraded
                : HealthStatus.Healthy;

        return new SystemStatus
        {
            OverallHealth = overallStatus,
            ModuleStatus = moduleStatuses.ToDictionary(
                kvp => kvp.Key,
                kvp => new ModuleStatus
                {
                    Module = kvp.Key,
                    Health = kvp.Value,
                    IsRunning = kvp.Value != HealthStatus.Unknown,
                    LastActivity = DateTime.UtcNow,
                    Message = kvp.Value.ToString()
                }),
            LastUpdated = DateTime.UtcNow,
            ActiveAlerts = activeAlerts.Select(a => a.AlertId).ToList(),
            ActiveIncidents = activeIncidents.Select(i => i.IncidentId).ToList()
        };
    }

    public async Task<ModuleStatus?> GetModuleStatusAsync(string module)
    {
        var sql = @"
            SELECT * FROM SystemHealth
            WHERE Module = @Module
            ORDER BY RecordedAt DESC
            LIMIT 1";

        var parameters = new[] { new SqliteParameter("@Module", module) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new
            {
                Status = (HealthStatus)reader.GetInt32("Status"),
                Message = reader.IsDBNull("Message") ? null : reader.GetString("Message"),
                Metrics = reader.IsDBNull("Metrics") ? null : reader.GetString("Metrics"),
                RecordedAt = DateTime.Parse(reader.GetString("RecordedAt"))
            };
        }, parameters);

        var result = results.FirstOrDefault();

        if (result == null)
        {
            return null;
        }

        return new ModuleStatus
        {
            Module = module,
            Health = result.Status,
            IsRunning = result.Status != HealthStatus.Unknown,
            LastActivity = result.RecordedAt,
            Message = result.Message,
            Metrics = !string.IsNullOrEmpty(result.Metrics)
                ? JsonHelper.Deserialize<Dictionary<string, object>>(result.Metrics)
                : new Dictionary<string, object>()
        };
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        var systemStatus = await GetSystemStatusAsync();
        var recentAlerts = await GetRecentAlertsAsync(10);
        var activeIncidents = await GetActiveIncidentsAsync();
        var recoveryStats = await GetRecoveryStatisticsAsync();
        var threatStats = await GetThreatStatisticsAsync();
        var decoyStatus = await GetDecoyStatusAsync();

        return new DashboardData
        {
            SystemStatus = systemStatus,
            RecentAlerts = recentAlerts,
            ActiveIncidents = activeIncidents,
            RecoveryStats = recoveryStats,
            ThreatStats = threatStats,
            DecoyStatus = decoyStatus
        };
    }

    public async Task<StatusReport> GenerateReportAsync(StatusReportOptions options)
    {
        var systemStatus = await GetSystemStatusAsync();
        var incidents = await ListIncidentsAsync(options);
        var alerts = await ListAlertsAsync(options);

        return new StatusReport
        {
            GeneratedAt = DateTime.UtcNow,
            SystemStatus = systemStatus,
            IncidentCount = incidents.Count,
            AlertCount = alerts.Count,
            HealthStatus = systemStatus.OverallHealth.ToString(),
            Summary = GenerateSummary(systemStatus, incidents, alerts)
        };
    }

    private async Task<Dictionary<string, HealthStatus>> GetAllModuleStatusesAsync()
    {
        var sql = @"
            SELECT Module, Status
            FROM SystemHealth
            WHERE RecordedAt > datetime('now', '-1 hour')
            GROUP BY Module
            ORDER BY RecordedAt DESC";

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new
            {
                Module = reader.GetString("Module"),
                Status = (HealthStatus)reader.GetInt32("Status")
            };
        });

        return results.ToDictionary(r => r.Module, r => r.Status);
    }

    private async Task<List<Alert>> GetActiveAlertsAsync()
    {
        var sql = "SELECT * FROM Alert WHERE IsResolved = 0 ORDER BY GeneratedAt DESC LIMIT 50";
        return _database.ExecuteQuery(sql, reader =>
        {
            return new Alert
            {
                AlertId = reader.GetString("AlertId"),
                Severity = (AlertSeverity)reader.GetInt32("Severity"),
                Type = (AlertType)reader.GetInt32("Type"),
                Title = reader.GetString("Title"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt"))
            };
        });
    }

    private async Task<List<IncidentModel>> GetActiveIncidentsAsync()
    {
        var sql = "SELECT * FROM Incident WHERE Status IN (1,2,3,4,5) ORDER BY DetectedAt DESC LIMIT 50";
        return _database.ExecuteQuery(sql, reader =>
        {
            return new IncidentModel
            {
                IncidentId = reader.GetString("IncidentId"),
                Status = (IncidentStatus)reader.GetInt32("Status"),
                Confidence = (ConfidenceLevel)reader.GetInt32("Confidence"),
                DetectedAt = DateTime.Parse(reader.GetString("DetectedAt")),
                AffectedFileCount = reader.GetInt32("AffectedFileCount"),
                Summary = reader.IsDBNull("Summary") ? null : reader.GetString("Summary")
            };
        });
    }

    private async Task<List<Alert>> GetRecentAlertsAsync(int count)
    {
        var sql = $"SELECT * FROM Alert ORDER BY GeneratedAt DESC LIMIT {count}";
        return _database.ExecuteQuery(sql, reader =>
        {
            return new Alert
            {
                AlertId = reader.GetString("AlertId"),
                Severity = (AlertSeverity)reader.GetInt32("Severity"),
                Type = (AlertType)reader.GetInt32("Type"),
                Title = reader.GetString("Title"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                IsResolved = reader.GetBoolean("IsResolved")
            };
        });
    }

    private async Task<RecoveryPointStatistics> GetRecoveryStatisticsAsync()
    {
        var sql = @"
            SELECT
                COUNT(*) as TotalPoints,
                SUM(TotalSizeBytes) as TotalStorage,
                SUM(CASE WHEN IsValid = 1 THEN 1 ELSE 0 END) as ValidPoints
            FROM RecoveryPoint";

        var result = _database.ExecuteQuery(sql, reader =>
        {
            return new
            {
                TotalPoints = reader.GetInt32("TotalPoints"),
                TotalStorage = reader.IsDBNull("TotalStorage") ? 0L : reader.GetInt64("TotalStorage"),
                ValidPoints = reader.GetInt32("ValidPoints")
            };
        }).FirstOrDefault();

        return new RecoveryPointStatistics
        {
            TotalRecoveryPoints = result?.TotalPoints ?? 0,
            TotalStorageUsed = result?.TotalStorage ?? 0,
            ValidPoints = result?.ValidPoints ?? 0,
            InvalidPoints = (result?.TotalPoints ?? 0) - (result?.ValidPoints ?? 0)
        };
    }

    private async Task<ThreatStatistics> GetThreatStatisticsAsync()
    {
        var sql = @"
            SELECT
                COUNT(*) as TotalReports,
                COUNT(DISTINCT ThreatFamilyId) as UniqueFamilies,
                AVG(AverageConfidenceScore) as AvgConfidence
            FROM ThreatReport";

        var result = _database.ExecuteQuery(sql, reader =>
        {
            return new
            {
                TotalReports = reader.GetInt32("TotalReports"),
                UniqueFamilies = reader.GetInt32("UniqueFamilies"),
                AvgConfidence = reader.IsDBNull("AvgConfidence") ? 0.0 : reader.GetDouble("AvgConfidence")
            };
        }).FirstOrDefault();

        return new ThreatStatistics
        {
            TotalReports = result?.TotalReports ?? 0,
            UniqueThreatFamilies = result?.UniqueFamilies ?? 0,
            AverageConfidence = result?.AvgConfidence ?? 0.0
        };
    }

    private async Task<List<DecoyAsset>> GetDecoyStatusAsync()
    {
        var sql = "SELECT * FROM DecoyAsset WHERE IsActive = 1 ORDER BY CreatedAt DESC LIMIT 20";
        return _database.ExecuteQuery(sql, reader =>
        {
            return new DecoyAsset
            {
                DecoyId = reader.GetString("DecoyId"),
                FilePath = reader.GetString("FilePath"),
                Type = (DecoyType)reader.GetInt32("Type"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                AccessCount = reader.GetInt32("AccessCount"),
                ModificationCount = reader.GetInt32("ModificationCount"),
                IsActive = reader.GetBoolean("IsActive")
            };
        });
    }

    private async Task<List<IncidentModel>> ListIncidentsAsync(StatusReportOptions options)
    {
        var sql = "SELECT * FROM Incident";
        if (options.StartDate.HasValue)
        {
            sql += " WHERE DetectedAt >= @StartDate";
        }
        sql += " ORDER BY DetectedAt DESC";

        var parameters = options.StartDate.HasValue
            ? new[] { new SqliteParameter("@StartDate", options.StartDate.Value.ToString("o")) }
            : Array.Empty<SqliteParameter>();

        return _database.ExecuteQuery(sql, reader =>
        {
            return new IncidentModel
            {
                IncidentId = reader.GetString("IncidentId"),
                Status = (IncidentStatus)reader.GetInt32("Status"),
                Confidence = (ConfidenceLevel)reader.GetInt32("Confidence"),
                DetectedAt = DateTime.Parse(reader.GetString("DetectedAt"))
            };
        }, parameters);
    }

    private async Task<List<Alert>> ListAlertsAsync(StatusReportOptions options)
    {
        var sql = "SELECT * FROM Alert";
        if (options.StartDate.HasValue)
        {
            sql += " WHERE GeneratedAt >= @StartDate";
        }
        sql += " ORDER BY GeneratedAt DESC";

        var parameters = options.StartDate.HasValue
            ? new[] { new SqliteParameter("@StartDate", options.StartDate.Value.ToString("o")) }
            : Array.Empty<SqliteParameter>();

        return _database.ExecuteQuery(sql, reader =>
        {
            return new Alert
            {
                AlertId = reader.GetString("AlertId"),
                Severity = (AlertSeverity)reader.GetInt32("Severity"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt"))
            };
        }, parameters);
    }

    private string GenerateSummary(SystemStatus systemStatus, List<IncidentModel> incidents, List<Alert> alerts)
    {
        var parts = new List<string>();

        parts.Add($"System health: {systemStatus.OverallHealth}");
        parts.Add($"{incidents.Count} incidents");
        parts.Add($"{alerts.Count} alerts");

        if (systemStatus.ActiveIncidents.Any())
        {
            parts.Add($"{systemStatus.ActiveIncidents.Count} active incidents");
        }

        return string.Join(", ", parts);
    }
}

public class SystemStatus
{
    public HealthStatus OverallHealth { get; set; }
    public Dictionary<string, ModuleStatus> ModuleStatus { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public List<string> ActiveAlerts { get; set; } = new();
    public List<string> ActiveIncidents { get; set; } = new();
}

public class ModuleStatus
{
    public string Module { get; set; } = string.Empty;
    public HealthStatus Health { get; set; }
    public bool IsRunning { get; set; }
    public DateTime LastActivity { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

public class DashboardData
{
    public SystemStatus SystemStatus { get; set; } = new();
    public List<Alert> RecentAlerts { get; set; } = new();
    public List<IncidentModel> ActiveIncidents { get; set; } = new();
    public RecoveryPointStatistics RecoveryStats { get; set; } = new();
    public ThreatStatistics ThreatStats { get; set; } = new();
    public List<DecoyAsset> DecoyStatus { get; set; } = new();
}

public class StatusReport
{
    public DateTime GeneratedAt { get; set; }
    public SystemStatus SystemStatus { get; set; } = new();
    public int IncidentCount { get; set; }
    public int AlertCount { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class StatusReportOptions
{
    public DateTime? StartDate { get; set; }
}
