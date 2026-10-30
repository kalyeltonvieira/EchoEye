using EchoEye.Core.Models;
using EchoEye.Protection.Detection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EchoEye.Tests;

public class ConfidenceScorerTests
{
    [Fact]
    public async Task CalculateAsync_WeightsSignalsIntoCriticalScore()
    {
        var scorer = new ConfidenceScorer(NullLogger.Instance);

        var result = await scorer.CalculateAsync(new DetectionSignals
        {
            FileVelocityScore = 1,
            EntropyScore = 1,
            DecoyInteractionScore = 1,
            ProcessReputationScore = 1,
            BehaviorAnomalyScore = 1,
            NetworkActivityScore = 1
        });

        Assert.Equal(1, result.Score, precision: 4);
        Assert.Equal(ConfidenceLevel.Critical, result.Level);
        Assert.Contains("fileVelocity", result.ComponentScores.Keys);
    }

    [Fact]
    public async Task UpdateWeightsAsync_RejectsInvalidTotal()
    {
        var scorer = new ConfidenceScorer(NullLogger.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scorer.UpdateWeightsAsync(new Dictionary<string, double>
            {
                ["fileVelocity"] = 0.5,
                ["entropy"] = 0.5,
                ["decoyInteraction"] = 0.5
            }));
    }
}
