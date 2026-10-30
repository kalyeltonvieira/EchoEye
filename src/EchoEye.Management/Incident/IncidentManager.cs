using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using IncidentModel = EchoEye.Core.Models.Incident;

namespace EchoEye.Management.Incident;

public interface IIncidentManager
{
    Task<IncidentModel> CreateIncidentAsync(IncidentModel incident);
    Task<IncidentModel?> GetIncidentAsync(string incidentId);
    Task UpdateIncidentAsync(string incidentId, IncidentUpdate update);
    Task<List<IncidentModel>> ListIncidentsAsync(IncidentFilter? filter = null);
    Task MarkFalsePositiveAsync(string incidentId, string reason);
    Task<IncidentTimeline> GetTimelineAsync(string incidentId);
}

/// <summary>
/// Manages security incidents
/// </summary>
public class IncidentManager : IIncidentManager
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public IncidentManager(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<IncidentModel> CreateIncidentAsync(IncidentModel incident)
    {
        incident.IncidentId = CryptoHelper.GenerateUuid();
        incident.DetectedAt = DateTime.UtcNow;
        incident.Status = IncidentStatus.Detected;

        var sql = @"
            INSERT INTO Incident (
                IncidentId, DeviceId, Status, Confidence, DetectedAt,
                AffectedFileCount, AffectedDataSizeBytes, Summary, Details
            ) VALUES (
                @IncidentId, @DeviceId, @Status, @Confidence, @DetectedAt,
                @AffectedFileCount, @AffectedDataSizeBytes, @Summary, @Details
            )";

        var parameters = new[]
            {
                new SqliteParameter("@IncidentId", incident.IncidentId),
                new SqliteParameter("@DeviceId", incident.DeviceId),
                new SqliteParameter("@Status", (int)incident.Status),
                new SqliteParameter("@Confidence", (int)incident.Confidence),
                new SqliteParameter("@DetectedAt", incident.DetectedAt.ToString("o")),
                new SqliteParameter("@AffectedFileCount", incident.AffectedFileCount),
                new SqliteParameter("@AffectedDataSizeBytes", incident.AffectedDataSizeBytes),
                new SqliteParameter("@Summary", incident.Summary ?? (object)DBNull.Value),
                new SqliteParameter("@Details", incident.Details ?? (object)DBNull.Value)
            };

        _database.ExecuteNonQuery(sql, parameters);

        _logger.LogInformation("Incident created: {IncidentId} with confidence {Confidence}",
            incident.IncidentId, incident.Confidence);

        return incident;
    }

    public async Task<IncidentModel?> GetIncidentAsync(string incidentId)
    {
        var sql = "SELECT * FROM Incident WHERE IncidentId = @IncidentId";
        var parameters = new[] { new SqliteParameter("@IncidentId", incidentId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new IncidentModel
            {
                IncidentId = reader.GetString("IncidentId"),
                DeviceId = reader.GetString("DeviceId"),
                Status = (IncidentStatus)reader.GetInt32("Status"),
                Confidence = (ConfidenceLevel)reader.GetInt32("Confidence"),
                DetectedAt = DateTime.Parse(reader.GetString("DetectedAt")),
                ContainedAt = reader.IsDBNull("ContainedAt") ? null : DateTime.Parse(reader.GetString("ContainedAt")),
                ResolvedAt = reader.IsDBNull("ResolvedAt") ? null : DateTime.Parse(reader.GetString("ResolvedAt")),
                ThreatFamilyId = reader.IsDBNull("ThreatFamilyId") ? null : reader.GetString("ThreatFamilyId"),
                AffectedFileCount = reader.GetInt32("AffectedFileCount"),
                AffectedDataSizeBytes = reader.GetInt64("AffectedDataSizeBytes"),
                ContainmentAction = reader.IsDBNull("ContainmentAction") ? null : reader.GetString("ContainmentAction"),
                Summary = reader.IsDBNull("Summary") ? null : reader.GetString("Summary"),
                Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                ForensicEvidencePath = reader.IsDBNull("ForensicEvidencePath") ? null : reader.GetString("ForensicEvidencePath"),
                IsFalsePositive = reader.GetBoolean("IsFalsePositive"),
                FalsePositiveReason = reader.IsDBNull("FalsePositiveReason") ? null : reader.GetString("FalsePositiveReason")
            };
        }, parameters);

        return results.FirstOrDefault();
    }

    public async Task UpdateIncidentAsync(string incidentId, IncidentUpdate update)
    {
        var sql = "UPDATE Incident SET ";
        var updates = new List<string>();
        var parameters = new List<SqliteParameter>();

        if (update.Status.HasValue)
        {
            updates.Add("Status = @Status");
            parameters.Add(new SqliteParameter("@Status", (int)update.Status.Value));
        }

        if (!string.IsNullOrEmpty(update.ContainmentAction))
        {
            updates.Add("ContainmentAction = @ContainmentAction");
            parameters.Add(new SqliteParameter("@ContainmentAction", update.ContainmentAction));
        }

        if (!string.IsNullOrEmpty(update.Summary))
        {
            updates.Add("Summary = @Summary");
            parameters.Add(new SqliteParameter("@Summary", update.Summary));
        }

        if (!string.IsNullOrEmpty(update.Details))
        {
            updates.Add("Details = @Details");
            parameters.Add(new SqliteParameter("@Details", update.Details));
        }

        if (update.Status == IncidentStatus.Contained)
        {
            updates.Add("ContainedAt = @ContainedAt");
            parameters.Add(new SqliteParameter("@ContainedAt", DateTime.UtcNow.ToString("o")));
        }

        if (update.Status == IncidentStatus.Resolved)
        {
            updates.Add("ResolvedAt = @ResolvedAt");
            parameters.Add(new SqliteParameter("@ResolvedAt", DateTime.UtcNow.ToString("o")));
        }

        if (!updates.Any())
        {
            return;
        }

        sql += string.Join(", ", updates);
        sql += " WHERE IncidentId = @IncidentId";
        parameters.Add(new SqliteParameter("@IncidentId", incidentId));

        _database.ExecuteNonQuery(sql, parameters.ToArray());

        _logger.LogInformation("Incident updated: {IncidentId}", incidentId);
    }

    public async Task<List<IncidentModel>> ListIncidentsAsync(IncidentFilter? filter = null)
    {
        var sql = "SELECT * FROM Incident WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (filter != null)
        {
            if (filter.StartDate.HasValue)
            {
                sql += " AND DetectedAt >= @StartDate";
                parameters.Add(new SqliteParameter("@StartDate", filter.StartDate.Value.ToString("o")));
            }

            if (filter.EndDate.HasValue)
            {
                sql += " AND DetectedAt <= @EndDate";
                parameters.Add(new SqliteParameter("@EndDate", filter.EndDate.Value.ToString("o")));
            }

            if (filter.Status.HasValue)
            {
                sql += " AND Status = @Status";
                parameters.Add(new SqliteParameter("@Status", (int)filter.Status.Value));
            }

            if (filter.MinConfidence.HasValue)
            {
                sql += " AND Confidence >= @MinConfidence";
                parameters.Add(new SqliteParameter("@MinConfidence", (int)filter.MinConfidence.Value));
            }

            if (filter.IsFalsePositive.HasValue)
            {
                sql += " AND IsFalsePositive = @IsFalsePositive";
                parameters.Add(new SqliteParameter("@IsFalsePositive", filter.IsFalsePositive.Value));
            }
        }

        sql += " ORDER BY DetectedAt DESC";

        return _database.ExecuteQuery(sql, reader =>
        {
            return new IncidentModel
            {
                IncidentId = reader.GetString("IncidentId"),
                DeviceId = reader.GetString("DeviceId"),
                Status = (IncidentStatus)reader.GetInt32("Status"),
                Confidence = (ConfidenceLevel)reader.GetInt32("Confidence"),
                DetectedAt = DateTime.Parse(reader.GetString("DetectedAt")),
                ContainedAt = reader.IsDBNull("ContainedAt") ? null : DateTime.Parse(reader.GetString("ContainedAt")),
                ResolvedAt = reader.IsDBNull("ResolvedAt") ? null : DateTime.Parse(reader.GetString("ResolvedAt")),
                ThreatFamilyId = reader.IsDBNull("ThreatFamilyId") ? null : reader.GetString("ThreatFamilyId"),
                AffectedFileCount = reader.GetInt32("AffectedFileCount"),
                AffectedDataSizeBytes = reader.GetInt64("AffectedDataSizeBytes"),
                ContainmentAction = reader.IsDBNull("ContainmentAction") ? null : reader.GetString("ContainmentAction"),
                Summary = reader.IsDBNull("Summary") ? null : reader.GetString("Summary"),
                Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                ForensicEvidencePath = reader.IsDBNull("ForensicEvidencePath") ? null : reader.GetString("ForensicEvidencePath"),
                IsFalsePositive = reader.GetBoolean("IsFalsePositive"),
                FalsePositiveReason = reader.IsDBNull("FalsePositiveReason") ? null : reader.GetString("FalsePositiveReason")
            };
        }, parameters.ToArray());
    }

    public async Task MarkFalsePositiveAsync(string incidentId, string reason)
    {
        var sql = @"
            UPDATE Incident SET
                IsFalsePositive = 1,
                FalsePositiveReason = @FalsePositiveReason,
                Status = @Status
            WHERE IncidentId = @IncidentId";

        var parameters = new[]
        {
            new SqliteParameter("@FalsePositiveReason", reason),
            new SqliteParameter("@Status", (int)IncidentStatus.FalsePositive),
            new SqliteParameter("@IncidentId", incidentId)
        };

        _database.ExecuteNonQuery(sql, parameters);

        _logger.LogInformation("Incident marked as false positive: {IncidentId}", incidentId);
    }

    public async Task<IncidentTimeline> GetTimelineAsync(string incidentId)
    {
        var incident = await GetIncidentAsync(incidentId);
        if (incident == null)
        {
            throw new InvalidOperationException($"Incident not found: {incidentId}");
        }

        var timeline = new IncidentTimeline
        {
            IncidentId = incidentId,
            Events = new List<TimelineEvent>()
        };

        // Add detection event
        timeline.Events.Add(new TimelineEvent
        {
            Timestamp = incident.DetectedAt,
            EventType = "Detected",
            Description = "Incident detected",
            Details = new { Confidence = incident.Confidence.ToString() }
        });

        // Add containment event if applicable
        if (incident.ContainedAt.HasValue)
        {
            timeline.Events.Add(new TimelineEvent
            {
                Timestamp = incident.ContainedAt.Value,
                EventType = "Contained",
                Description = $"Incident contained: {incident.ContainmentAction}",
                Details = new { Action = incident.ContainmentAction }
            });
        }

        // Add resolution event if applicable
        if (incident.ResolvedAt.HasValue)
        {
            timeline.Events.Add(new TimelineEvent
            {
                Timestamp = incident.ResolvedAt.Value,
                EventType = "Resolved",
                Description = "Incident resolved",
                Details = new { Status = incident.Status.ToString() }
            });
        }

        timeline.Events = timeline.Events.OrderBy(e => e.Timestamp).ToList();

        return timeline;
    }
}

public class IncidentUpdate
{
    public IncidentStatus? Status { get; set; }
    public string? ContainmentAction { get; set; }
    public string? Summary { get; set; }
    public string? Details { get; set; }
}

public class IncidentFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IncidentStatus? Status { get; set; }
    public ConfidenceLevel? MinConfidence { get; set; }
    public bool? IsFalsePositive { get; set; }
}

public class IncidentTimeline
{
    public string IncidentId { get; set; } = string.Empty;
    public List<TimelineEvent> Events { get; set; } = new();
}

public class TimelineEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? Details { get; set; }
}
