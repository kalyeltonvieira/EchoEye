using EchoEye.Core.Models;
using Microsoft.Extensions.Logging;

namespace EchoEye.Protection.Detection;

public interface IBehaviorAnalyzer
{
    Task<BehaviorAnalysisResult> AnalyzeFileEventAsync(FileEvent fileEvent);
    Task<BehaviorAnalysisResult> AnalyzeProcessAsync(ProcessInfo process);
    Task<BehaviorAnalysisResult> AnalyzeDecoyInteractionAsync(DecoyInteraction interaction);
    Task<BehaviorBaseline> GetBaselineAsync();
    Task UpdateBaselineAsync(BehaviorBaseline baseline);
}

/// <summary>
/// Analyzes behavioral patterns for suspicious activity
/// </summary>
public class BehaviorAnalyzer : IBehaviorAnalyzer
{
    private readonly ILogger _logger;
    private BehaviorBaseline? _baseline;

    public BehaviorAnalyzer(ILogger logger)
    {
        _logger = logger;
    }

    public Task<BehaviorAnalysisResult> AnalyzeFileEventAsync(FileEvent fileEvent)
    {
        var result = new BehaviorAnalysisResult
        {
            ConfidenceScore = 0,
            Indicators = new List<string>(),
            Reasoning = string.Empty,
            SignalScores = new Dictionary<string, double>(),
            IsSuspicious = false
        };

        // Analyze entropy
        if (fileEvent.EntropyScore > 7.5)
        {
            result.Indicators.Add("High entropy detected (possible encryption)");
            result.SignalScores["entropy"] = fileEvent.EntropyScore / 8.0;
            result.ConfidenceScore += 0.4;
        }

        // Analyze file size change
        if (fileEvent.OriginalSize > 0 && fileEvent.NewSize > 0)
        {
            var sizeRatio = (double)fileEvent.NewSize / fileEvent.OriginalSize;
            if (sizeRatio > 1.5 || sizeRatio < 0.5)
            {
                result.Indicators.Add("Significant size change detected");
                result.SignalScores["sizeChange"] = Math.Abs(sizeRatio - 1.0);
                result.ConfidenceScore += 0.2;
            }
        }

        // Analyze file extension
        var extension = Path.GetExtension(fileEvent.FilePath).ToLowerInvariant();
        var suspiciousExtensions = new[] { ".encrypted", ".locked", ".crypto", ".enc" };
        if (suspiciousExtensions.Contains(extension))
        {
            result.Indicators.Add("Suspicious file extension");
            result.SignalScores["extension"] = 1.0;
            result.ConfidenceScore += 0.3;
        }

        result.IsSuspicious = result.ConfidenceScore > 0.5;
        result.Reasoning = string.Join("; ", result.Indicators);

        return Task.FromResult(result);
    }

    public Task<BehaviorAnalysisResult> AnalyzeProcessAsync(ProcessInfo process)
    {
        var result = new BehaviorAnalysisResult
        {
            ConfidenceScore = 0,
            Indicators = new List<string>(),
            Reasoning = string.Empty,
            SignalScores = new Dictionary<string, double>(),
            IsSuspicious = false
        };

        // Analyze file modification count
        if (process.FileModificationCount > 100)
        {
            result.Indicators.Add("High file modification count");
            result.SignalScores["fileModifications"] = Math.Min(process.FileModificationCount / 1000.0, 1.0);
            result.ConfidenceScore += 0.3;
        }

        // Analyze decoy interactions
        if (process.DecoyInteractionCount > 0)
        {
            result.Indicators.Add($"Decoy interactions detected: {process.DecoyInteractionCount}");
            result.SignalScores["decoyInteractions"] = Math.Min(process.DecoyInteractionCount / 10.0, 1.0);
            result.ConfidenceScore += 0.5;
        }

        // Analyze reputation
        if (process.Reputation == ProcessReputation.Malicious)
        {
            result.Indicators.Add("Known malicious process");
            result.SignalScores["reputation"] = 1.0;
            result.ConfidenceScore += 0.8;
        }
        else if (process.Reputation == ProcessReputation.Suspicious)
        {
            result.Indicators.Add("Suspicious process reputation");
            result.SignalScores["reputation"] = 0.6;
            result.ConfidenceScore += 0.4;
        }

        result.IsSuspicious = result.ConfidenceScore > 0.5;
        result.Reasoning = string.Join("; ", result.Indicators);

        return Task.FromResult(result);
    }

    public Task<BehaviorAnalysisResult> AnalyzeDecoyInteractionAsync(DecoyInteraction interaction)
    {
        var result = new BehaviorAnalysisResult
        {
            ConfidenceScore = interaction.ConfidenceScore,
            Indicators = new List<string> { "Decoy file interaction detected" },
            Reasoning = $"Process {interaction.ProcessName} interacted with decoy file",
            SignalScores = new Dictionary<string, double>
            {
                ["decoyInteraction"] = interaction.ConfidenceScore
            },
            IsSuspicious = interaction.ConfidenceScore > 0.7
        };

        return Task.FromResult(result);
    }

    public Task<BehaviorBaseline> GetBaselineAsync()
    {
        return Task.FromResult(_baseline ?? new BehaviorBaseline
        {
            AverageFileModificationRate = 10,
            TypicalProcessCount = 50,
            CommonProcesses = new List<string>(),
            ExtensionFrequencies = new Dictionary<string, double>(),
            LastUpdated = DateTime.UtcNow
        });
    }

    public Task UpdateBaselineAsync(BehaviorBaseline baseline)
    {
        _baseline = baseline;
        _logger.LogInformation("Behavior baseline updated");
        return Task.CompletedTask;
    }
}

public class BehaviorBaseline
{
    public double AverageFileModificationRate { get; set; }
    public double TypicalProcessCount { get; set; }
    public List<string> CommonProcesses { get; set; } = new();
    public Dictionary<string, double> ExtensionFrequencies { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class BehaviorAnalysisResult
{
    public double ConfidenceScore { get; set; }
    public List<string> Indicators { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public Dictionary<string, double> SignalScores { get; set; } = new();
    public bool IsSuspicious { get; set; }
}
