using EchoEye.Core.Models;
using Microsoft.Extensions.Logging;

namespace EchoEye.Protection.Detection;

public interface IConfidenceScorer
{
    Task<ConfidenceScore> CalculateAsync(DetectionSignals signals);
    ConfidenceLevel GetLevel(double score);
    Task UpdateWeightsAsync(Dictionary<string, double> weights);
    Task<Dictionary<string, double>> GetWeightsAsync();
}

/// <summary>
/// Calculates confidence scores from multiple detection signals
/// </summary>
public class ConfidenceScorer : IConfidenceScorer
{
    private readonly ILogger _logger;
    private Dictionary<string, double> _weights;

    public ConfidenceScorer(ILogger logger)
    {
        _logger = logger;
        _weights = new Dictionary<string, double>
        {
            ["fileVelocity"] = 0.25,
            ["entropy"] = 0.25,
            ["decoyInteraction"] = 0.20,
            ["processReputation"] = 0.15,
            ["behaviorAnomaly"] = 0.10,
            ["networkActivity"] = 0.05
        };
    }

    public Task<ConfidenceScore> CalculateAsync(DetectionSignals signals)
    {
        var score = new ConfidenceScore
        {
            ComponentScores = new Dictionary<string, double>(),
            Reasoning = string.Empty
        };

        // Calculate weighted score
        var totalScore = 0.0;

        totalScore += signals.FileVelocityScore * _weights["fileVelocity"];
        score.ComponentScores["fileVelocity"] = signals.FileVelocityScore;

        totalScore += signals.EntropyScore * _weights["entropy"];
        score.ComponentScores["entropy"] = signals.EntropyScore;

        totalScore += signals.DecoyInteractionScore * _weights["decoyInteraction"];
        score.ComponentScores["decoyInteraction"] = signals.DecoyInteractionScore;

        totalScore += signals.ProcessReputationScore * _weights["processReputation"];
        score.ComponentScores["processReputation"] = signals.ProcessReputationScore;

        totalScore += signals.BehaviorAnomalyScore * _weights["behaviorAnomaly"];
        score.ComponentScores["behaviorAnomaly"] = signals.BehaviorAnomalyScore;

        totalScore += signals.NetworkActivityScore * _weights["networkActivity"];
        score.ComponentScores["networkActivity"] = signals.NetworkActivityScore;

        // Add additional signals if present
        if (signals.AdditionalSignals != null)
        {
            foreach (var kvp in signals.AdditionalSignals)
            {
                if (_weights.ContainsKey(kvp.Key))
                {
                    totalScore += kvp.Value * _weights[kvp.Key];
                    score.ComponentScores[kvp.Key] = kvp.Value;
                }
            }
        }

        score.Score = Math.Min(totalScore, 1.0);
        score.Level = GetLevel(score.Score);
        score.Reasoning = GenerateReasoning(score.ComponentScores);

        return Task.FromResult(score);
    }

    public ConfidenceLevel GetLevel(double score)
    {
        return score switch
        {
            < 0.40 => ConfidenceLevel.Low,
            < 0.65 => ConfidenceLevel.Medium,
            < 0.85 => ConfidenceLevel.High,
            _ => ConfidenceLevel.Critical
        };
    }

    public Task UpdateWeightsAsync(Dictionary<string, double> weights)
    {
        var total = weights.Values.Sum();
        if (Math.Abs(total - 1.0) > 0.01)
        {
            throw new InvalidOperationException("Weights must sum to 1.0");
        }

        _weights = weights;
        _logger.LogInformation("Confidence scoring weights updated");
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, double>> GetWeightsAsync()
    {
        return Task.FromResult(new Dictionary<string, double>(_weights));
    }

    private string GenerateReasoning(Dictionary<string, double> componentScores)
    {
        var significant = componentScores
            .Where(kvp => kvp.Value > 0.5)
            .OrderByDescending(kvp => kvp.Value)
            .Take(3)
            .Select(kvp => $"{kvp.Key} ({kvp.Value:F2})");

        return significant.Any()
            ? $"Primary indicators: {string.Join(", ", significant)}"
            : "No significant indicators";
    }
}

public class DetectionSignals
{
    public double FileVelocityScore { get; set; }
    public double EntropyScore { get; set; }
    public double DecoyInteractionScore { get; set; }
    public double ProcessReputationScore { get; set; }
    public double BehaviorAnomalyScore { get; set; }
    public double NetworkActivityScore { get; set; }
    public Dictionary<string, double>? AdditionalSignals { get; set; }
}

public class ConfidenceScore
{
    public double Score { get; set; }
    public ConfidenceLevel Level { get; set; }
    public Dictionary<string, double> ComponentScores { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
}
