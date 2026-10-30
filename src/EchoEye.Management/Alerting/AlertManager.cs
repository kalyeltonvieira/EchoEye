using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Management.Alerting;

public interface IAlertManager
{
    IObservable<Alert> NewAlerts { get; }
    Task<Alert> CreateAlertAsync(Alert alert);
    Task<Alert?> GetAlertAsync(string alertId);
    Task<List<Alert>> ListAlertsAsync(AlertFilter? filter = null);
    Task AcknowledgeAsync(string alertId, string acknowledgedBy);
    Task ResolveAsync(string alertId, string resolutionNotes);
}

/// <summary>
/// Manages security alerts
/// </summary>
public class AlertManager : IAlertManager
{
    private readonly Database _database;
    private readonly ILogger _logger;
    private readonly Subject<Alert> _newAlertSubject;

    public IObservable<Alert> NewAlerts => _newAlertSubject.AsObservable();

    public AlertManager(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
        _newAlertSubject = new Subject<Alert>();
    }

    public async Task<Alert> CreateAlertAsync(Alert alert)
    {
        alert.AlertId = CryptoHelper.GenerateUuid();
        alert.GeneratedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO Alert (
                AlertId, IncidentId, Severity, Type, Title, Description, SourceModule,
                GeneratedAt, IsResolved
            ) VALUES (
                @AlertId, @IncidentId, @Severity, @Type, @Title, @Description, @SourceModule,
                @GeneratedAt, @IsResolved
            )";

        var parameters = new[]
        {
            new SqliteParameter("@AlertId", alert.AlertId),
            new SqliteParameter("@IncidentId", alert.IncidentId ?? (object)DBNull.Value),
            new SqliteParameter("@Severity", (int)alert.Severity),
            new SqliteParameter("@Type", (int)alert.Type),
            new SqliteParameter("@Title", alert.Title),
            new SqliteParameter("@Description", alert.Description ?? (object)DBNull.Value),
            new SqliteParameter("@SourceModule", alert.SourceModule),
            new SqliteParameter("@GeneratedAt", alert.GeneratedAt.ToString("o")),
            new SqliteParameter("@IsResolved", alert.IsResolved)
        };

        _database.ExecuteNonQuery(sql, parameters);

        _newAlertSubject.OnNext(alert);

        _logger.LogInformation("Alert created: {AlertId} - {Title}", alert.AlertId, alert.Title);
        return alert;
    }

    public async Task<Alert?> GetAlertAsync(string alertId)
    {
        var sql = "SELECT * FROM Alert WHERE AlertId = @AlertId";
        var parameters = new[] { new SqliteParameter("@AlertId", alertId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new Alert
            {
                AlertId = reader.GetString("AlertId"),
                IncidentId = reader.IsDBNull("IncidentId") ? null : reader.GetString("IncidentId"),
                Severity = (AlertSeverity)reader.GetInt32("Severity"),
                Type = (AlertType)reader.GetInt32("Type"),
                Title = reader.GetString("Title"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                SourceModule = reader.GetString("SourceModule"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                AcknowledgedAt = reader.IsDBNull("AcknowledgedAt") ? null : DateTime.Parse(reader.GetString("AcknowledgedAt")),
                AcknowledgedBy = reader.IsDBNull("AcknowledgedBy") ? null : reader.GetString("AcknowledgedBy"),
                IsResolved = reader.GetBoolean("IsResolved"),
                ResolvedAt = reader.IsDBNull("ResolvedAt") ? null : DateTime.Parse(reader.GetString("ResolvedAt")),
                ResolutionNotes = reader.IsDBNull("ResolutionNotes") ? null : reader.GetString("ResolutionNotes"),
                RelatedData = reader.IsDBNull("RelatedData") ? null : reader.GetString("RelatedData")
            };
        }, parameters);

        return results.FirstOrDefault();
    }

    public async Task<List<Alert>> ListAlertsAsync(AlertFilter? filter = null)
    {
        var sql = "SELECT * FROM Alert WHERE 1=1";
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

            if (filter.Severity.HasValue)
            {
                sql += " AND Severity = @Severity";
                parameters.Add(new SqliteParameter("@Severity", (int)filter.Severity.Value));
            }

            if (filter.Type.HasValue)
            {
                sql += " AND Type = @Type";
                parameters.Add(new SqliteParameter("@Type", (int)filter.Type.Value));
            }

            if (filter.IsResolved.HasValue)
            {
                sql += " AND IsResolved = @IsResolved";
                parameters.Add(new SqliteParameter("@IsResolved", filter.IsResolved.Value));
            }

            if (!string.IsNullOrEmpty(filter.IncidentId))
            {
                sql += " AND IncidentId = @IncidentId";
                parameters.Add(new SqliteParameter("@IncidentId", filter.IncidentId));
            }
        }

        sql += " ORDER BY GeneratedAt DESC";

        if (filter?.Limit.HasValue == true)
        {
            sql += " LIMIT @Limit";
            parameters.Add(new SqliteParameter("@Limit", filter.Limit.Value));
        }

        return _database.ExecuteQuery(sql, reader =>
        {
            return new Alert
            {
                AlertId = reader.GetString("AlertId"),
                IncidentId = reader.IsDBNull("IncidentId") ? null : reader.GetString("IncidentId"),
                Severity = (AlertSeverity)reader.GetInt32("Severity"),
                Type = (AlertType)reader.GetInt32("Type"),
                Title = reader.GetString("Title"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                SourceModule = reader.GetString("SourceModule"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                AcknowledgedAt = reader.IsDBNull("AcknowledgedAt") ? null : DateTime.Parse(reader.GetString("AcknowledgedAt")),
                AcknowledgedBy = reader.IsDBNull("AcknowledgedBy") ? null : reader.GetString("AcknowledgedBy"),
                IsResolved = reader.GetBoolean("IsResolved"),
                ResolvedAt = reader.IsDBNull("ResolvedAt") ? null : DateTime.Parse(reader.GetString("ResolvedAt")),
                ResolutionNotes = reader.IsDBNull("ResolutionNotes") ? null : reader.GetString("ResolutionNotes"),
                RelatedData = reader.IsDBNull("RelatedData") ? null : reader.GetString("RelatedData")
            };
        }, parameters.ToArray());
    }

    public async Task AcknowledgeAsync(string alertId, string acknowledgedBy)
    {
        var sql = @"
            UPDATE Alert SET
                AcknowledgedAt = @AcknowledgedAt,
                AcknowledgedBy = @AcknowledgedBy
            WHERE AlertId = @AlertId";

        var parameters = new[]
        {
            new SqliteParameter("@AcknowledgedAt", DateTime.UtcNow.ToString("o")),
            new SqliteParameter("@AcknowledgedBy", acknowledgedBy),
            new SqliteParameter("@AlertId", alertId)
        };

        _database.ExecuteNonQuery(sql, parameters);

        _logger.LogInformation("Alert acknowledged: {AlertId} by {User}", alertId, acknowledgedBy);
    }

    public async Task ResolveAsync(string alertId, string resolutionNotes)
    {
        var sql = @"
            UPDATE Alert SET
                IsResolved = 1,
                ResolvedAt = @ResolvedAt,
                ResolutionNotes = @ResolutionNotes
            WHERE AlertId = @AlertId";

        var parameters = new[]
        {
            new SqliteParameter("@ResolvedAt", DateTime.UtcNow.ToString("o")),
            new SqliteParameter("@ResolutionNotes", resolutionNotes),
            new SqliteParameter("@AlertId", alertId)
        };

        _database.ExecuteNonQuery(sql, parameters);

        _logger.LogInformation("Alert resolved: {AlertId}", alertId);
    }
}

public class AlertFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public AlertSeverity? Severity { get; set; }
    public AlertType? Type { get; set; }
    public bool? IsResolved { get; set; }
    public string? IncidentId { get; set; }
    public int? Limit { get; set; }
}
