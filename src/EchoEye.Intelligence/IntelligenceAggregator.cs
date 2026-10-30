using EchoEye.Core.Data;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Intelligence;

public interface IIntelligenceAggregator
{
    Task<IntelligenceFeed> GenerateFeedAsync(FeedFilter? filter = null);
    Task<ThreatStatistics> GetStatisticsAsync();
    Task<List<ThreatFamily>> GetTrendingThreatsAsync(TimeSpan timeWindow);
    Task<List<ThreatReport>> SearchThreatsAsync(ThreatSearchQuery query);
}

/// <summary>
/// Aggregates threat intelligence from multiple sources
/// </summary>
public class IntelligenceAggregator : IIntelligenceAggregator
{
    private readonly Database _database;
    private readonly ILogger _logger;

    public IntelligenceAggregator(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<IntelligenceFeed> GenerateFeedAsync(FeedFilter? filter = null)
    {
        _logger.LogInformation("Generating intelligence feed");

        var reports = await QueryThreatReportsAsync(filter);
        var families = await GetThreatFamiliesAsync(filter);
        var statistics = await GetStatisticsAsync();

        var feed = new IntelligenceFeed
        {
            FeedId = CryptoHelper.GenerateUuid(),
            GeneratedAt = DateTime.UtcNow,
            Reports = reports,
            Families = families,
            Statistics = statistics
        };

        _logger.LogInformation("Intelligence feed generated: {FeedId} with {Count} reports",
            feed.FeedId, feed.Reports.Count);

        return feed;
    }

    public async Task<ThreatStatistics> GetStatisticsAsync()
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

        // Get family counts
        var familyCountsSql = @"
            SELECT ThreatFamilyId, COUNT(*) as Count
            FROM ThreatReport
            GROUP BY ThreatFamilyId";

        var familyCounts = _database.ExecuteQuery(familyCountsSql, reader =>
        {
            return new
            {
                Family = reader.GetString("ThreatFamilyId"),
                Count = reader.GetInt32("Count")
            };
        }).ToDictionary(x => x.Family, x => x.Count);

        // Get regional distribution
        var regionSql = @"
            SELECT GeographicRegion, COUNT(*) as Count
            FROM ThreatReport
            WHERE GeographicRegion IS NOT NULL
            GROUP BY GeographicRegion";

        var regionalDistribution = _database.ExecuteQuery(regionSql, reader =>
        {
            return new
            {
                Region = reader.GetString("GeographicRegion"),
                Count = reader.GetInt32("Count")
            };
        }).ToDictionary(x => x.Region, x => x.Count);

        return new ThreatStatistics
        {
            TotalReports = result?.TotalReports ?? 0,
            UniqueThreatFamilies = result?.UniqueFamilies ?? 0,
            FamilyCounts = familyCounts,
            RegionalDistribution = regionalDistribution,
            AverageConfidence = result?.AvgConfidence ?? 0.0
        };
    }

    public async Task<List<ThreatFamily>> GetTrendingThreatsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        var sql = @"
            SELECT ThreatFamilyId, COUNT(*) as DetectionCount
            FROM ThreatReport
            WHERE GeneratedAt >= @Cutoff
            GROUP BY ThreatFamilyId
            ORDER BY DetectionCount DESC
            LIMIT 10";

        var parameters = new[] { new SqliteParameter("@Cutoff", cutoff.ToString("o")) };

        var trendingFamilies = _database.ExecuteQuery(sql, reader =>
        {
            return new
            {
                FamilyId = reader.GetString("ThreatFamilyId"),
                DetectionCount = reader.GetInt32("DetectionCount")
            };
        });

        var families = new List<ThreatFamily>();

        foreach (var tf in trendingFamilies)
        {
            var family = await GetThreatFamilyAsync(tf.FamilyId);
            if (family != null)
            {
                family.TotalDetections = tf.DetectionCount;
                families.Add(family);
            }
        }

        return families;
    }

    public async Task<List<ThreatReport>> SearchThreatsAsync(ThreatSearchQuery query)
    {
        var sql = "SELECT * FROM ThreatReport WHERE 1=1";
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrEmpty(query.Query))
        {
            sql += " AND (ThreatFamilyId LIKE @Query OR BehavioralFingerprint LIKE @Query)";
            parameters.Add(new SqliteParameter("@Query", $"%{query.Query}%"));
        }

        if (query.StartDate.HasValue)
        {
            sql += " AND GeneratedAt >= @StartDate";
            parameters.Add(new SqliteParameter("@StartDate", query.StartDate.Value.ToString("o")));
        }

        if (query.EndDate.HasValue)
        {
            sql += " AND GeneratedAt <= @EndDate";
            parameters.Add(new SqliteParameter("@EndDate", query.EndDate.Value.ToString("o")));
        }

        if (!string.IsNullOrEmpty(query.ThreatFamily))
        {
            sql += " AND ThreatFamilyId = @ThreatFamily";
            parameters.Add(new SqliteParameter("@ThreatFamily", query.ThreatFamily));
        }

        sql += " ORDER BY GeneratedAt DESC";

        if (query.Limit.HasValue)
        {
            sql += " LIMIT @Limit";
            parameters.Add(new SqliteParameter("@Limit", query.Limit.Value));
        }

        return _database.ExecuteQuery(sql, reader =>
        {
            return new ThreatReport
            {
                ReportId = reader.GetString("ReportId"),
                ThreatFamilyId = reader.GetString("ThreatFamilyId"),
                DeviceId = reader.GetString("DeviceId"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                FirstSeen = reader.IsDBNull("FirstSeen") ? null : DateTime.Parse(reader.GetString("FirstSeen")),
                LastSeen = reader.IsDBNull("LastSeen") ? null : DateTime.Parse(reader.GetString("LastSeen")),
                BehavioralFingerprint = reader.GetString("BehavioralFingerprint"),
                GeographicRegion = reader.IsDBNull("GeographicRegion") ? null : reader.GetString("GeographicRegion"),
                DetectionCount = reader.GetInt32("DetectionCount"),
                AverageConfidenceScore = reader.GetDouble("AverageConfidenceScore")
            };
        }, parameters.ToArray());
    }

    private async Task<List<ThreatReport>> QueryThreatReportsAsync(FeedFilter? filter)
    {
        var sql = "SELECT * FROM ThreatReport WHERE 1=1";
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

            if (filter.ThreatFamilies != null && filter.ThreatFamilies.Any())
            {
                sql += " AND ThreatFamilyId IN (" + string.Join(",", filter.ThreatFamilies.Select((_, i) => $"@Family{i}")) + ")";
                foreach (var family in filter.ThreatFamilies.Select((f, i) => (f, i)))
                {
                    parameters.Add(new SqliteParameter($"@Family{family.i}", family.f));
                }
            }

            if (!string.IsNullOrEmpty(filter.GeographicRegion))
            {
                sql += " AND GeographicRegion = @GeographicRegion";
                parameters.Add(new SqliteParameter("@GeographicRegion", filter.GeographicRegion));
            }

            if (filter.MinConfidence.HasValue)
            {
                sql += " AND AverageConfidenceScore >= @MinConfidence";
                parameters.Add(new SqliteParameter("@MinConfidence", filter.MinConfidence.Value));
            }
        }

        sql += " ORDER BY GeneratedAt DESC LIMIT 100";

        return _database.ExecuteQuery(sql, reader =>
        {
            return new ThreatReport
            {
                ReportId = reader.GetString("ReportId"),
                ThreatFamilyId = reader.GetString("ThreatFamilyId"),
                DeviceId = reader.GetString("DeviceId"),
                GeneratedAt = DateTime.Parse(reader.GetString("GeneratedAt")),
                FirstSeen = reader.IsDBNull("FirstSeen") ? null : DateTime.Parse(reader.GetString("FirstSeen")),
                LastSeen = reader.IsDBNull("LastSeen") ? null : DateTime.Parse(reader.GetString("LastSeen")),
                BehavioralFingerprint = reader.GetString("BehavioralFingerprint"),
                GeographicRegion = reader.IsDBNull("GeographicRegion") ? null : reader.GetString("GeographicRegion"),
                DetectionCount = reader.GetInt32("DetectionCount"),
                AverageConfidenceScore = reader.GetDouble("AverageConfidenceScore")
            };
        }, parameters.ToArray());
    }

    private async Task<List<ThreatFamily>> GetThreatFamiliesAsync(FeedFilter? filter)
    {
        var sql = "SELECT * FROM ThreatFamily WHERE IsActive = 1";
        var parameters = new List<SqliteParameter>();

        if (filter?.ThreatFamilies != null && filter.ThreatFamilies.Any())
        {
            sql += " AND FamilyId IN (" + string.Join(",", filter.ThreatFamilies.Select((_, i) => $"@Family{i}")) + ")";
            foreach (var family in filter.ThreatFamilies.Select((f, i) => (f, i)))
            {
                parameters.Add(new SqliteParameter($"@Family{family.i}", family.f));
            }
        }

        return _database.ExecuteQuery(sql, reader =>
        {
            return new ThreatFamily
            {
                FamilyId = reader.GetString("FamilyId"),
                Name = reader.GetString("Name"),
                Category = reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                FirstDetected = DateTime.Parse(reader.GetString("FirstDetected")),
                LastDetected = DateTime.Parse(reader.GetString("LastDetected")),
                TotalDetections = reader.GetInt32("TotalDetections"),
                Severity = (ThreatSeverity)reader.GetInt32("Severity"),
                MitigationAdvice = reader.IsDBNull("MitigationAdvice") ? null : reader.GetString("MitigationAdvice")
            };
        }, parameters.ToArray());
    }

    private async Task<ThreatFamily?> GetThreatFamilyAsync(string familyId)
    {
        var sql = "SELECT * FROM ThreatFamily WHERE FamilyId = @FamilyId";
        var parameters = new[] { new SqliteParameter("@FamilyId", familyId) };

        var results = _database.ExecuteQuery(sql, reader =>
        {
            return new ThreatFamily
            {
                FamilyId = reader.GetString("FamilyId"),
                Name = reader.GetString("Name"),
                Category = reader.IsDBNull("Category") ? null : reader.GetString("Category"),
                Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                FirstDetected = DateTime.Parse(reader.GetString("FirstDetected")),
                LastDetected = DateTime.Parse(reader.GetString("LastDetected")),
                TotalDetections = reader.GetInt32("TotalDetections"),
                Severity = (ThreatSeverity)reader.GetInt32("Severity"),
                MitigationAdvice = reader.IsDBNull("MitigationAdvice") ? null : reader.GetString("MitigationAdvice")
            };
        }, parameters);

        return results.FirstOrDefault();
    }
}

public class IntelligenceFeed
{
    public string FeedId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ThreatReport> Reports { get; set; } = new();
    public List<ThreatFamily> Families { get; set; } = new();
    public ThreatStatistics Statistics { get; set; } = new();
}

public class FeedFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? ThreatFamilies { get; set; }
    public string? GeographicRegion { get; set; }
    public double? MinConfidence { get; set; }
}

public class ThreatStatistics
{
    public int TotalReports { get; set; }
    public int UniqueThreatFamilies { get; set; }
    public Dictionary<string, int> FamilyCounts { get; set; } = new();
    public Dictionary<string, int> RegionalDistribution { get; set; } = new();
    public double AverageConfidence { get; set; }
}

public class ThreatSearchQuery
{
    public string? Query { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? ThreatFamily { get; set; }
    public int? Limit { get; set; }
}
