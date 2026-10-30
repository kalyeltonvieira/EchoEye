using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace EchoEye.Decoy.Detection;

public interface IIndicatorGenerator
{
    Task<ThreatIndicator> GenerateAsync(DecoyInteraction interaction);
    Task<ThreatIndicator> GenerateFromBatchAsync(List<DecoyInteraction> interactions);
    double CalculateConfidence(DecoyInteraction interaction);
}

/// <summary>
/// Generates threat indicators from decoy interactions
/// </summary>
public class IndicatorGenerator : IIndicatorGenerator
{
    private readonly ILogger _logger;

    public IndicatorGenerator(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ThreatIndicator> GenerateAsync(DecoyInteraction interaction)
    {
        var indicator = new ThreatIndicator
        {
            IndicatorId = CryptoHelper.GenerateUuid(),
            Type = "decoy_interaction",
            Value = $"{interaction.ProcessName}|{interaction.Type}",
            Confidence = interaction.ConfidenceScore,
            GeneratedAt = DateTime.UtcNow,
            Source = "DecoyEngine",
            Metadata = new Dictionary<string, object>
            {
                ["decoyId"] = interaction.DecoyId,
                ["interactionType"] = interaction.Type.ToString(),
                ["processId"] = interaction.ProcessId ?? "unknown",
                ["processName"] = interaction.ProcessName ?? "unknown"
            }
        };

        _logger.LogInformation("Generated threat indicator: {IndicatorId} with confidence {Score}",
            indicator.IndicatorId, indicator.Confidence);

        return indicator;
    }

    public async Task<ThreatIndicator> GenerateFromBatchAsync(List<DecoyInteraction> interactions)
    {
        if (!interactions.Any())
        {
            throw new ArgumentException("No interactions provided");
        }

        // Aggregate confidence from all interactions
        var avgConfidence = interactions.Average(i => i.ConfidenceScore);
        var maxConfidence = interactions.Max(i => i.ConfidenceScore);

        // Get most common process
        var commonProcess = interactions
            .GroupBy(i => i.ProcessName)
            .OrderByDescending(g => g.Count())
            .First().Key;

        var indicator = new ThreatIndicator
        {
            IndicatorId = CryptoHelper.GenerateUuid(),
            Type = "decoy_interaction_batch",
            Value = $"{commonProcess}|{interactions.Count}_interactions",
            Confidence = maxConfidence, // Use max confidence for batch
            GeneratedAt = DateTime.UtcNow,
            Source = "DecoyEngine",
            Metadata = new Dictionary<string, object>
            {
                ["interactionCount"] = interactions.Count,
                ["averageConfidence"] = avgConfidence,
                ["maxConfidence"] = maxConfidence,
                ["commonProcess"] = commonProcess ?? "unknown",
                ["interactionTypes"] = interactions.Select(i => i.Type.ToString()).Distinct().ToList()
            }
        };

        _logger.LogInformation("Generated batch threat indicator: {IndicatorId} from {Count} interactions",
            indicator.IndicatorId, interactions.Count);

        return indicator;
    }

    public double CalculateConfidence(DecoyInteraction interaction)
    {
        return interaction.ConfidenceScore;
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
