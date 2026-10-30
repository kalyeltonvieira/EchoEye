using EchoEye.Core.Data;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Decoy.Monitoring;

public interface IDecoyMonitor
{
    IObservable<DecoyInteraction> Interactions { get; }
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    Task<bool> IsDecoyAsync(string filePath);
    Task<DecoyAsset?> GetDecoyAsync(string filePath);
    Task<DecoyInteraction> RecordInteractionAsync(string filePath, InteractionType type, string processId);
    Task<List<DecoyInteraction>> GetRecentInteractionsAsync(TimeSpan timeWindow);
}

/// <summary>
/// Monitors decoy interactions
/// </summary>
public class DecoyMonitor : IDecoyMonitor
{
    private readonly Database _database;
    private readonly ILogger _logger;
    private readonly Dictionary<string, DecoyAsset> _decoyCache;
    private readonly Subject<DecoyInteraction> _interactionSubject;
    private bool _isMonitoring;

    public IObservable<DecoyInteraction> Interactions => _interactionSubject.AsObservable();

    public DecoyMonitor(Database database, ILogger logger)
    {
        _database = database;
        _logger = logger;
        _decoyCache = new Dictionary<string, DecoyAsset>();
        _interactionSubject = new Subject<DecoyInteraction>();
    }

    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("Starting decoy monitoring");

        // Load active decoys into cache
        await LoadDecoysAsync();

        _isMonitoring = true;
        _logger.LogInformation("Decoy monitoring started with {Count} decoys", _decoyCache.Count);
    }

    public async Task StopMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            return;
        }

        _logger.LogInformation("Stopping decoy monitoring");
        _isMonitoring = false;
        _decoyCache.Clear();
    }

    public Task<bool> IsDecoyAsync(string filePath)
    {
        var normalizedPath = PathHelper.NormalizePath(filePath);
        return Task.FromResult(_decoyCache.Values.Any(d =>
            PathHelper.NormalizePath(d.FilePath) == normalizedPath));
    }

    public Task<DecoyAsset?> GetDecoyAsync(string filePath)
    {
        var normalizedPath = PathHelper.NormalizePath(filePath);
        var decoy = _decoyCache.Values.FirstOrDefault(d =>
            PathHelper.NormalizePath(d.FilePath) == normalizedPath);
        return Task.FromResult(decoy);
    }

    public async Task<DecoyInteraction> RecordInteractionAsync(string filePath, InteractionType type, string processId)
    {
        try
        {
            var decoy = await GetDecoyAsync(filePath);
            if (decoy == null)
            {
                throw new InvalidOperationException("File is not a decoy");
            }

            // Update decoy stats
            if (type == InteractionType.Read || type == InteractionType.Write)
            {
                decoy.LastAccessedAt = DateTime.UtcNow;
                decoy.AccessCount++;
            }

            if (type == InteractionType.Write || type == InteractionType.Encrypt)
            {
                decoy.LastModifiedAt = DateTime.UtcNow;
                decoy.ModificationCount++;
            }

            // Calculate confidence score
            var confidence = CalculateConfidence(type, decoy);

            // Create interaction record
            var interaction = new DecoyInteraction
            {
                InteractionId = CryptoHelper.GenerateUuid(),
                DecoyId = decoy.DecoyId,
                Type = type,
                OccurredAt = DateTime.UtcNow,
                ProcessId = processId,
                ConfidenceScore = confidence,
                Details = JsonHelper.Serialize(new
                {
                    FilePath = filePath,
                    DecoyType = decoy.Type
                })
            };

            // Publish interaction
            _interactionSubject.OnNext(interaction);

            _logger.LogWarning("Decoy interaction recorded: {Type} on {FilePath} by process {ProcessId}",
                type, filePath, processId);

            return interaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record decoy interaction");
            throw;
        }
    }

    public async Task<List<DecoyInteraction>> GetRecentInteractionsAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;

        var sql = @"
            SELECT * FROM DecoyInteraction
            WHERE OccurredAt >= @Cutoff
            ORDER BY OccurredAt DESC";

        var parameters = new[]
        {
            new SqliteParameter("@Cutoff", cutoff.ToString("o"))
        };

        return _database.ExecuteQuery(sql, reader =>
        {
            return new DecoyInteraction
            {
                InteractionId = reader.GetString("InteractionId"),
                DecoyId = reader.GetString("DecoyId"),
                IncidentId = reader.IsDBNull("IncidentId") ? null : reader.GetString("IncidentId"),
                Type = (InteractionType)reader.GetInt32("Type"),
                OccurredAt = DateTime.Parse(reader.GetString("OccurredAt")),
                ProcessId = reader.IsDBNull("ProcessId") ? null : reader.GetString("ProcessId"),
                ProcessName = reader.IsDBNull("ProcessName") ? null : reader.GetString("ProcessName"),
                ProcessPath = reader.IsDBNull("ProcessPath") ? null : reader.GetString("ProcessPath"),
                OriginalHash = reader.IsDBNull("OriginalHash") ? null : reader.GetString("OriginalHash"),
                NewHash = reader.IsDBNull("NewHash") ? null : reader.GetString("NewHash"),
                ConfidenceScore = reader.GetDouble("ConfidenceScore"),
                Details = reader.IsDBNull("Details") ? null : reader.GetString("Details")
            };
        }, parameters);
    }

    private async Task LoadDecoysAsync()
    {
        var sql = "SELECT * FROM DecoyAsset WHERE IsActive = 1";

        var decoys = _database.ExecuteQuery(sql, reader =>
        {
            return new DecoyAsset
            {
                DecoyId = reader.GetString("DecoyId"),
                DeviceId = reader.GetString("DeviceId"),
                FilePath = reader.GetString("FilePath"),
                Type = (DecoyType)reader.GetInt32("Type"),
                CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
                LastAccessedAt = reader.IsDBNull("LastAccessedAt") ? null : DateTime.Parse(reader.GetString("LastAccessedAt")),
                LastModifiedAt = reader.IsDBNull("LastModifiedAt") ? null : DateTime.Parse(reader.GetString("LastModifiedAt")),
                AccessCount = reader.GetInt32("AccessCount"),
                ModificationCount = reader.GetInt32("ModificationCount"),
                IsActive = reader.GetBoolean("IsActive"),
                ContentHash = reader.GetString("ContentHash"),
                FileSize = reader.GetInt64("FileSize"),
                Metadata = reader.IsDBNull("Metadata") ? null : reader.GetString("Metadata")
            };
        });

        foreach (var decoy in decoys)
        {
            _decoyCache[decoy.DecoyId] = decoy;
        }
    }

    private double CalculateConfidence(InteractionType type, DecoyAsset decoy)
    {
        var baseScore = type switch
        {
            InteractionType.Encrypt => 0.95,
            InteractionType.Write => 0.85,
            InteractionType.Delete => 0.90,
            InteractionType.Rename => 0.75,
            InteractionType.Read => 0.30,
            _ => 0.50
        };

        // Increase confidence if decoy has been modified multiple times
        if (decoy.ModificationCount > 1)
        {
            baseScore += 0.05;
        }

        return Math.Min(baseScore, 1.0);
    }
}
