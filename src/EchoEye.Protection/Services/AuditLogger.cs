using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Protection.Services;

/// <summary>
/// Logs audit events for compliance
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public AuditLogger(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task LogAsync(AuditEvent auditEvent)
    {
        try
        {
            var log = new AuditLog
            {
                LogId = CryptoHelper.GenerateUuid(),
                DeviceId = await GetDeviceIdAsync(),
                Timestamp = DateTime.UtcNow,
                Category = auditEvent.Category,
                Action = auditEvent.Action,
                Actor = auditEvent.Actor,
                Target = auditEvent.Target,
                Details = auditEvent.Details != null ? JsonHelper.Serialize(auditEvent.Details) : null,
                SourceModule = auditEvent.SourceModule,
                RelatedEntityId = auditEvent.RelatedEntityId,
                RelatedEntityType = auditEvent.RelatedEntityType,
                IsSensitive = auditEvent.IsSensitive
            };

            var sql = @"
                INSERT INTO AuditLog (
                    LogId, DeviceId, Timestamp, Category, Action, Actor, Target,
                    Details, SourceModule, RelatedEntityId, RelatedEntityType, IsSensitive
                ) VALUES (
                    @LogId, @DeviceId, @Timestamp, @Category, @Action, @Actor, @Target,
                    @Details, @SourceModule, @RelatedEntityId, @RelatedEntityType, @IsSensitive
                )";

            var parameters = new[]
            {
                new SqliteParameter("@LogId", log.LogId),
                new SqliteParameter("@DeviceId", log.DeviceId),
                new SqliteParameter("@Timestamp", log.Timestamp.ToString("o")),
                new SqliteParameter("@Category", log.Category),
                new SqliteParameter("@Action", log.Action),
                new SqliteParameter("@Actor", log.Actor ?? (object)DBNull.Value),
                new SqliteParameter("@Target", log.Target ?? (object)DBNull.Value),
                new SqliteParameter("@Details", log.Details ?? (object)DBNull.Value),
                new SqliteParameter("@SourceModule", log.SourceModule ?? (object)DBNull.Value),
                new SqliteParameter("@RelatedEntityId", log.RelatedEntityId ?? (object)DBNull.Value),
                new SqliteParameter("@RelatedEntityType", log.RelatedEntityType ?? (object)DBNull.Value),
                new SqliteParameter("@IsSensitive", log.IsSensitive)
            };

            _database.ExecuteNonQuery(sql, parameters);

            _logger.LogDebug("Audit log entry created: {Category}/{Action}", auditEvent.Category, auditEvent.Action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event");
        }
    }

    public async Task<List<AuditLog>> QueryAsync(AuditQuery query)
    {
        try
        {
            var sql = "SELECT * FROM AuditLog WHERE 1=1";
            var parameters = new List<SqliteParameter>();

            if (query.StartDate.HasValue)
            {
                sql += " AND Timestamp >= @StartDate";
                parameters.Add(new SqliteParameter("@StartDate", query.StartDate.Value.ToString("o")));
            }

            if (query.EndDate.HasValue)
            {
                sql += " AND Timestamp <= @EndDate";
                parameters.Add(new SqliteParameter("@EndDate", query.EndDate.Value.ToString("o")));
            }

            if (!string.IsNullOrEmpty(query.Category))
            {
                sql += " AND Category = @Category";
                parameters.Add(new SqliteParameter("@Category", query.Category));
            }

            if (!string.IsNullOrEmpty(query.Action))
            {
                sql += " AND Action = @Action";
                parameters.Add(new SqliteParameter("@Action", query.Action));
            }

            if (!string.IsNullOrEmpty(query.Actor))
            {
                sql += " AND Actor = @Actor";
                parameters.Add(new SqliteParameter("@Actor", query.Actor));
            }

            if (!string.IsNullOrEmpty(query.SourceModule))
            {
                sql += " AND SourceModule = @SourceModule";
                parameters.Add(new SqliteParameter("@SourceModule", query.SourceModule));
            }

            sql += " ORDER BY Timestamp DESC";

            if (query.Limit.HasValue)
            {
                sql += " LIMIT @Limit";
                parameters.Add(new SqliteParameter("@Limit", query.Limit.Value));
            }

            if (query.Offset.HasValue)
            {
                sql += " OFFSET @Offset";
                parameters.Add(new SqliteParameter("@Offset", query.Offset.Value));
            }

            return _database.ExecuteQuery(sql, reader =>
            {
                return new AuditLog
                {
                    LogId = reader.GetString("LogId"),
                    DeviceId = reader.GetString("DeviceId"),
                    Timestamp = DateTime.Parse(reader.GetString("Timestamp")),
                    Category = reader.GetString("Category"),
                    Action = reader.GetString("Action"),
                    Actor = reader.IsDBNull("Actor") ? null : reader.GetString("Actor"),
                    Target = reader.IsDBNull("Target") ? null : reader.GetString("Target"),
                    Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                    SourceModule = reader.IsDBNull("SourceModule") ? null : reader.GetString("SourceModule"),
                    RelatedEntityId = reader.IsDBNull("RelatedEntityId") ? null : reader.GetString("RelatedEntityId"),
                    RelatedEntityType = reader.IsDBNull("RelatedEntityType") ? null : reader.GetString("RelatedEntityType"),
                    IsSensitive = reader.GetBoolean("IsSensitive")
                };
            }, parameters.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query audit logs");
            return new List<AuditLog>();
        }
    }

    public async Task<List<AuditLog>> GetEntityTrailAsync(string entityId, string entityType)
    {
        var query = new AuditQuery
        {
            RelatedEntityId = entityId,
            RelatedEntityType = entityType
        };

        return await QueryAsync(query);
    }

    private async Task<string> GetDeviceIdAsync()
    {
        var sql = "SELECT DeviceId FROM DeviceIdentity LIMIT 1";
        var result = _database.ExecuteScalar(sql);
        return result?.ToString() ?? string.Empty;
    }
}
