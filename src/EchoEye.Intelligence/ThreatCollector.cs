using EchoEye.Core.Data;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Intelligence;

public interface IThreatCollector
{
    Task CollectAsync(ThreatIndicator indicator);
    Task CollectFromIncidentAsync(string incidentId);
    Task<List<ThreatIndicator>> GetIndicatorsAsync(ThreatIndicatorFilter? filter = null);
    Task<ThreatReport> GenerateReportAsync(List<ThreatIndicator> indicators);
}

/// <summary>
/// Collects and manages threat indicators
/// </summary>
public class ThreatCollector : IThreatCollector
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public ThreatCollector(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task CollectAsync(ThreatIndicator indicator)
    {
        _logger.LogInformation("Collecting threat indicator: {Type}", indicator.Type);

        await EnsureIndicatorTableAsync();

        indicator.IndicatorId = string.IsNullOrWhiteSpace(indicator.IndicatorId)
            ? CryptoHelper.GenerateUuid()
            : indicator.IndicatorId;
        indicator.GeneratedAt = indicator.GeneratedAt == default
            ? DateTime.UtcNow
            : indicator.GeneratedAt;
        indicator.Source = string.IsNullOrWhiteSpace(indicator.Source)
            ? "EchoEye.Intelligence"
            : indicator.Source;

        var sql = @"
            INSERT INTO ThreatIndicator (
                IndicatorId, Type, Value, Confidence, GeneratedAt, Source, Metadata
            ) VALUES (
                @IndicatorId, @Type, @Value, @Confidence, @GeneratedAt, @Source, @Metadata
            )";

        var parameters = new[]
        {
            new SqliteParameter("@IndicatorId", indicator.IndicatorId),
            new SqliteParameter("@Type", indicator.Type),
            new SqliteParameter("@Value", indicator.Value),
            new SqliteParameter("@Confidence", indicator.Confidence),
            new SqliteParameter("@GeneratedAt", indicator.GeneratedAt.ToString("o")),
            new SqliteParameter("@Source", indicator.Source),
            new SqliteParameter("@Metadata", JsonHelper.Serialize(indicator.Metadata))
        };

        _database.ExecuteNonQuery(sql, parameters);
    }

    public async Task CollectFromIncidentAsync(string incidentId)
    {
        _logger.LogInformation("Collecting threat indicators from incident: {IncidentId}", incidentId);

        // Get incident details
        var sql = "SELECT * FROM Incident WHERE IncidentId = @IncidentId";
        var parameters = new[] { new SqliteParameter("@IncidentId", incidentId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new Incident
            {
                IncidentId = reader.GetString("IncidentId"),
                DeviceId = reader.GetString("DeviceId"),
                Status = (IncidentStatus)reader.GetInt32("Status"),
                Confidence = (ConfidenceLevel)reader.GetInt32("Confidence"),
                DetectedAt = DateTime.Parse(reader.GetString("DetectedAt")),
                ThreatFamilyId = reader.IsDBNull("ThreatFamilyId") ? null : reader.GetString("ThreatFamilyId"),
                AffectedFileCount = reader.GetInt32("AffectedFileCount"),
                AffectedDataSizeBytes = reader.GetInt64("AffectedDataSizeBytes")
            };
        }, parameters);

        var incident = results.FirstOrDefault();
        if (incident == null)
        {
            _logger.LogWarning("Incident not found: {IncidentId}", incidentId);
            return;
        }

        // Get related file events
        var fileEvents = await GetIncidentFileEventsAsync(incidentId);

        // Get related decoy interactions
        var decoyInteractions = await GetIncidentDecoyInteractionsAsync(incidentId);

        // Generate threat report
        await GenerateReportAsync(incident, fileEvents, decoyInteractions);
    }

    public async Task<List<ThreatIndicator>> GetIndicatorsAsync(ThreatIndicatorFilter? filter = null)
    {
        await EnsureIndicatorTableAsync();

        var sql = "SELECT * FROM ThreatIndicator WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                sql += " AND GeneratedAt >= @StartDate";
                parameters.Add(new SqliteParameter("@StartDate", filter.StartDate.Value.ToString("o")));
            }

            if (filter.EndDate.HasValue)
            {
                sql += " AND GeneratedAt <= @EndDate";
                parameters.Add(new SqliteParameter("@EndDate", filter.EndDate.Value.ToString("o")));
            }

            if (!string.IsNullOrWhiteSpace(filter.Type))
            {
                sql += " AND Type = @Type";
                parameters.Add(new SqliteParameter("@Type", filter.Type));
            }

            if (filter.MinConfidence.HasValue)
            {
                sql += " AND Confidence >= @MinConfidence";
                parameters.Add(new SqliteParameter("@MinConfidence", filter.MinConfidence.Value));
            }
        }

        sql += " ORDER BY GeneratedAt DESC";

        return _database.ExecuteQuery(sql, reader =>
        {
            var metadataJson = reader.IsDBNull("Metadata") ? "{}" : reader.GetString("Metadata");
            return new ThreatIndicator
            {
                IndicatorId = reader.GetString("IndicatorId"),
                Type = reader.GetString("Type"),
                Value = reader.GetString("Value"),
                Confidence = reader.GetDouble("Confidence"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                Source = reader.GetString("Source"),
                Metadata = JsonHelper.Deserialize<Dictionary<string, object>>(metadataJson) ?? new Dictionary<string, object>()
            };
        }, parameters.ToArray());
    }

    public async Task<ThreatReport> GenerateReportAsync(List<ThreatIndicator> indicators)
    {
        if (!indicators.Any())
        {
            throw new ArgumentException("No indicators provided");
        }

        var report = new ThreatReport
        {
            ReportId = CryptoHelper.GenerateUuid(),
            ThreatFamilyId = DetermineThreatFamily(indicators),
            DeviceId = await GetDeviceIdAsync(),
            GeneratedAt = DateTime.UtcNow,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            BehavioralFingerprint = GenerateBehavioralFingerprint(indicators),
            DetectionCount = indicators.Count,
            AverageConfidenceScore = indicators.Average(i => i.Confidence),
            IsShared = false
        };

        await SaveThreatReportAsync(report);

        _logger.LogInformation("Threat report generated: {ReportId}", report.ReportId);
        return report;
    }

    private async Task<List<FileEvent>> GetIncidentFileEventsAsync(string incidentId)
    {
        var sql = "SELECT * FROM FileEvent WHERE IncidentId = @IncidentId";
        var parameters = new[] { new SqliteParameter("@IncidentId", incidentId) };

        return _database.ExecuteQuery(sql, reader =>
        {
            return new FileEvent
            {
                EventId = reader.GetString("EventId"),
                FilePath = reader.GetString("FilePath"),
                ChangeType = (FileChangeType)reader.GetInt32("ChangeType"),
                OccurredAt = DateTime.Parse(reader.GetString("OccurredAt")),
                ProcessId = reader.IsDBNull("ProcessId") ? null : reader.GetString("ProcessId"),
                ProcessName = reader.IsDBNull("ProcessName") ? null : reader.GetString("ProcessName"),
                EntropyScore = reader.IsDBNull("EntropyScore") ? 0 : reader.GetDouble("EntropyScore")
            };
        }, parameters);
    }

    private async Task<List<DecoyInteraction>> GetIncidentDecoyInteractionsAsync(string incidentId)
    {
        var sql = "SELECT * FROM DecoyInteraction WHERE IncidentId = @IncidentId";
        var parameters = new[] { new SqliteParameter("@IncidentId", incidentId) };

        return _database.ExecuteQuery(sql, reader =>
        {
            return new DecoyInteraction
            {
                InteractionId = reader.GetString("InteractionId"),
                DecoyId = reader.GetString("DecoyId"),
                Type = (InteractionType)reader.GetInt32("Type"),
                OccurredAt = DateTime.Parse(reader.GetString("OccurredAt")),
                ProcessName = reader.IsDBNull("ProcessName") ? null : reader.GetString("ProcessName"),
                ConfidenceScore = reader.GetDouble("ConfidenceScore")
            };
        }, parameters);
    }

    private async Task GenerateReportAsync(Incident incident, List<FileEvent> fileEvents, List<DecoyInteraction> decoyInteractions)
    {
        var report = new ThreatReport
        {
            ReportId = CryptoHelper.GenerateUuid(),
            ThreatFamilyId = incident.ThreatFamilyId ?? "unknown",
            DeviceId = incident.DeviceId,
            GeneratedAt = DateTime.UtcNow,
            FirstSeen = incident.DetectedAt,
            LastSeen = DateTime.UtcNow,
            BehavioralFingerprint = GenerateIncidentFingerprint(incident, fileEvents, decoyInteractions),
            DetectionCount = 1,
            AverageConfidenceScore = (double)incident.Confidence / 4.0,
            IsShared = false
        };

        await SaveThreatReportAsync(report);
    }

    private string DetermineThreatFamily(List<ThreatIndicator> indicators)
    {
        // Simple heuristic - in production would use more sophisticated classification
        var processNames = indicators
            .Where(i => i.Metadata.ContainsKey("processName"))
            .Select(i => i.Metadata["processName"].ToString())
            .Distinct()
            .ToList();

        return processNames.Any() ? $"family-{processNames.First()}" : "unknown";
    }

    private string GenerateBehavioralFingerprint(List<ThreatIndicator> indicators)
    {
        var features = new List<string>
        {
            indicators.Count.ToString(),
            indicators.Average(i => i.Confidence).ToString("F2"),
            indicators.GroupBy(i => i.Type).Count().ToString()
        };

        return CryptoHelper.ComputeStringHash(string.Join("|", features));
    }

    private string GenerateIncidentFingerprint(Incident incident, List<FileEvent> fileEvents, List<DecoyInteraction> decoyInteractions)
    {
        var features = new List<string>
        {
            incident.Confidence.ToString(),
            incident.AffectedFileCount.ToString(),
            fileEvents.Count.ToString(),
            decoyInteractions.Count.ToString(),
            (fileEvents.Any() ? fileEvents.Average(f => f.EntropyScore) : 0).ToString("F2")
        };

        return CryptoHelper.ComputeStringHash(string.Join("|", features));
    }

    private Task EnsureIndicatorTableAsync()
    {
        var sql = @"
            CREATE TABLE IF NOT EXISTS ThreatIndicator (
                IndicatorId TEXT PRIMARY KEY,
                Type TEXT NOT NULL,
                Value TEXT NOT NULL,
                Confidence REAL NOT NULL,
                GeneratedAt TEXT NOT NULL,
                Source TEXT NOT NULL,
                Metadata TEXT
            );

            CREATE INDEX IF NOT EXISTS idx_threat_indicator_type ON ThreatIndicator(Type);
            CREATE INDEX IF NOT EXISTS idx_threat_indicator_generated ON ThreatIndicator(GeneratedAt);
            CREATE INDEX IF NOT EXISTS idx_threat_indicator_confidence ON ThreatIndicator(Confidence);";

        _database.ExecuteNonQuery(sql);
        return Task.CompletedTask;
    }

    private async Task SaveThreatReportAsync(ThreatReport report)
    {
        var sql = @"
            INSERT INTO ThreatReport (
                ReportId, ThreatFamilyId, DeviceId, GeneratedAt, FirstSeen, LastSeen,
                BehavioralFingerprint, DetectionCount, AverageConfidenceScore, IsShared
            ) VALUES (
                @ReportId, @ThreatFamilyId, @DeviceId, @GeneratedAt, @FirstSeen, @LastSeen,
                @BehavioralFingerprint, @DetectionCount, @AverageConfidenceScore, @IsShared
            )";

        var parameters = new[]
        {
            new SqliteParameter("@ReportId", report.ReportId),
            new SqliteParameter("@ThreatFamilyId", report.ThreatFamilyId),
            new SqliteParameter("@DeviceId", report.DeviceId),
            new SqliteParameter("@GeneratedAt", report.GeneratedAt.ToString("o")),
            new SqliteParameter("@FirstSeen", report.FirstSeen?.ToString("o") ?? (object)DBNull.Value),
            new SqliteParameter("@LastSeen", report.LastSeen?.ToString("o") ?? (object)DBNull.Value),
            new SqliteParameter("@BehavioralFingerprint", report.BehavioralFingerprint),
            new SqliteParameter("@DetectionCount", report.DetectionCount),
            new SqliteParameter("@AverageConfidenceScore", report.AverageConfidenceScore),
            new SqliteParameter("@IsShared", report.IsShared)
        };

        _database.ExecuteNonQuery(sql, parameters);
    }

    private async Task<string> GetDeviceIdAsync()
    {
        var sql = "SELECT DeviceId FROM DeviceIdentity LIMIT 1";
        var result = _database.ExecuteScalar(sql);
        return result?.ToString() ?? string.Empty;
    }
}

public class ThreatIndicator
{
    public string IndicatorId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ThreatIndicatorFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Type { get; set; }
    public double? MinConfidence { get; set; }
}
