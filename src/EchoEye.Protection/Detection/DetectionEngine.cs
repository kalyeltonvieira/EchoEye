using EchoEye.Core.Constants;
using EchoEye.Core.Models;
using EchoEye.Core.Utilities;
using EchoEye.Protection.Detection;
using EchoEye.Protection.Monitoring;
using Microsoft.Extensions.Logging;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EchoEye.Protection.Detection;

public interface IDetectionEngine
{
    IObservable<DetectionResult> Detections { get; }
    Task StartAsync();
    Task StopAsync();
    Task<DetectionResult> ProcessEventAsync(FileEvent fileEvent);
    Task<DetectionStatus> GetStatusAsync();
}

/// <summary>
/// Orchestrates the detection pipeline
/// </summary>
public class DetectionEngine : IDetectionEngine
{
    private readonly IFileSystemMonitor _fileSystemMonitor;
    private readonly IProcessTracker _processTracker;
    private readonly IBehaviorAnalyzer _behaviorAnalyzer;
    private readonly IConfidenceScorer _confidenceScorer;
    private readonly IContainmentManager _containmentManager;
    private readonly ILogger _logger;
    private readonly Subject<DetectionResult> _detectionSubject;
    private bool _isRunning;
    private long _eventsProcessed;
    private long _threatsDetected;
    private DateTime _startedAt;
    private readonly Queue<DateTime> _recentFileEvents;
    private readonly object _eventWindowLock;

    public IObservable<DetectionResult> Detections => _detectionSubject.AsObservable();

    public DetectionEngine(
        IFileSystemMonitor fileSystemMonitor,
        IProcessTracker processTracker,
        IBehaviorAnalyzer behaviorAnalyzer,
        IConfidenceScorer confidenceScorer,
        IContainmentManager containmentManager,
        ILogger logger)
    {
        _fileSystemMonitor = fileSystemMonitor;
        _processTracker = processTracker;
        _behaviorAnalyzer = behaviorAnalyzer;
        _confidenceScorer = confidenceScorer;
        _containmentManager = containmentManager;
        _logger = logger;
        _detectionSubject = new Subject<DetectionResult>();
        _recentFileEvents = new Queue<DateTime>();
        _eventWindowLock = new object();
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Detection engine already running");
            return;
        }

        _logger.LogInformation("Starting detection engine");

        // Start file system monitoring
        await _fileSystemMonitor.StartMonitoringAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), true);

        // Start process tracking
        await _processTracker.StartTrackingAsync();

        // Subscribe to file events
        _fileSystemMonitor.FileEvents.Subscribe(async fileEvent =>
        {
            await ProcessEventAsync(fileEvent);
        });

        _isRunning = true;
        _startedAt = DateTime.UtcNow;
        _eventsProcessed = 0;
        _threatsDetected = 0;

        _logger.LogInformation("Detection engine started");
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping detection engine");

        await _fileSystemMonitor.StopMonitoringAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        await _processTracker.StopTrackingAsync();

        _isRunning = false;
        _logger.LogInformation("Detection engine stopped");
    }

    public async Task<DetectionResult> ProcessEventAsync(FileEvent fileEvent)
    {
        Interlocked.Increment(ref _eventsProcessed);
        RecordFileEvent(fileEvent.OccurredAt == default ? DateTime.UtcNow : fileEvent.OccurredAt);

        try
        {
            // Get process info
            var processInfo = await _processTracker.GetProcessInfoAsync(fileEvent.ProcessId ?? "0");

            // Analyze file event
            var fileAnalysis = await _behaviorAnalyzer.AnalyzeFileEventAsync(fileEvent);

            // Analyze process if available
            var processAnalysis = processInfo != null
                ? await _behaviorAnalyzer.AnalyzeProcessAsync(processInfo)
                : null;

            // Combine signals
            var signals = new DetectionSignals
            {
                FileVelocityScore = CalculateFileVelocityScore(),
                EntropyScore = fileEvent.EntropyScore / 8.0,
                DecoyInteractionScore = fileEvent.IsDecoy ? 1.0 : 0.0,
                ProcessReputationScore = processInfo?.Reputation == ProcessReputation.Malicious ? 1.0 :
                                       processInfo?.Reputation == ProcessReputation.Suspicious ? 0.6 : 0.0,
                BehaviorAnomalyScore = fileAnalysis.ConfidenceScore,
                NetworkActivityScore = 0.0
            };

            // Calculate confidence
            var confidence = await _confidenceScorer.CalculateAsync(signals);

            // Create detection result
            var result = new DetectionResult
            {
                DetectionId = CryptoHelper.GenerateUuid(),
                IsThreat = confidence.Level >= ConfidenceLevel.High,
                Confidence = confidence,
                Indicators = fileAnalysis.Indicators.Union(processAnalysis?.Indicators ?? new List<string>()).ToList(),
                DetectedAt = DateTime.UtcNow,
                RecommendedAction = GetRecommendedAction(confidence.Level)
            };

            if (result.IsThreat)
            {
                Interlocked.Increment(ref _threatsDetected);
                _detectionSubject.OnNext(result);
                _logger.LogWarning("Threat detected: {DetectionId} with confidence {Score}",
                    result.DetectionId, confidence.Score);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file event");
            return new DetectionResult
            {
                DetectionId = CryptoHelper.GenerateUuid(),
                IsThreat = false,
                Confidence = new ConfidenceScore { Score = 0, Level = ConfidenceLevel.Low },
                DetectedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<DetectionStatus> GetStatusAsync()
    {
        var stats = await _fileSystemMonitor.GetStatisticsAsync();

        return new DetectionStatus
        {
            IsRunning = _isRunning,
            EventsProcessed = _eventsProcessed,
            ThreatsDetected = _threatsDetected,
            StartedAt = _startedAt
        };
    }

    private double CalculateFileVelocityScore()
    {
        lock (_eventWindowLock)
        {
            PruneFileEventWindow(DateTime.UtcNow);
            return Math.Min(_recentFileEvents.Count / (double)Defaults.Protection.FileVelocityThreshold, 1.0);
        }
    }

    private void RecordFileEvent(DateTime occurredAt)
    {
        lock (_eventWindowLock)
        {
            _recentFileEvents.Enqueue(occurredAt);
            PruneFileEventWindow(occurredAt);
        }
    }

    private void PruneFileEventWindow(DateTime now)
    {
        var cutoff = now - TimeSpan.FromMinutes(1);
        while (_recentFileEvents.Count > 0 && _recentFileEvents.Peek() < cutoff)
        {
            _recentFileEvents.Dequeue();
        }
    }

    private ContainmentAction GetRecommendedAction(ConfidenceLevel level)
    {
        return level switch
        {
            ConfidenceLevel.Critical => ContainmentAction.TerminateProcess,
            ConfidenceLevel.High => ContainmentAction.SuspendProcess,
            ConfidenceLevel.Medium => ContainmentAction.BlockNetworkAccess,
            _ => ContainmentAction.SuspendProcess
        };
    }
}

public class DetectionResult
{
    public string DetectionId { get; set; } = string.Empty;
    public bool IsThreat { get; set; }
    public ConfidenceScore Confidence { get; set; } = new();
    public List<string> Indicators { get; set; } = new();
    public string IncidentId { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public ContainmentAction RecommendedAction { get; set; }
}

public class DetectionStatus
{
    public bool IsRunning { get; set; }
    public long EventsProcessed { get; set; }
    public long ThreatsDetected { get; set; }
    public DateTime StartedAt { get; set; }
}
