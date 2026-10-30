using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Interfaces;
using EchoEye.Core.Logging;
using EchoEye.Core.Models;
using EchoEye.Protection.Monitoring;
using EchoEye.Protection.Detection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EchoEye.Protection.Services;

/// <summary>
/// Main protection service that runs as a Windows Service
/// </summary>
public class ProtectionService : BackgroundService
{
    private readonly ILogger<ProtectionService> _logger;
    private readonly Database _database;
    private readonly IFileSystemMonitor _fileSystemMonitor;
    private readonly IProcessTracker _processTracker;
    private readonly IBehaviorAnalyzer _behaviorAnalyzer;
    private readonly IConfidenceScorer _confidenceScorer;
    private readonly IContainmentManager _containmentManager;
    private readonly IDetectionEngine _detectionEngine;
    private readonly IHealthReporter _healthReporter;
    private readonly IAuditLogger _auditLogger;

    public ProtectionService(ILogger<ProtectionService> logger)
    {
        _logger = logger;
        _database = new Database(Paths.DatabasePath);

        // Initialize components
        var logAction = new Action<string, LogLevel, string?>((category, level, message) =>
        {
            _logger.Log(level, "[{Category}] {Message}", category, message);
        });

        var loggerProvider = new EchoEyeLoggerProvider(logAction);
        var moduleLogger = loggerProvider.CreateLogger("Protection");

        _fileSystemMonitor = new FileSystemMonitor(moduleLogger);
        _processTracker = new ProcessTracker(moduleLogger);
        _behaviorAnalyzer = new BehaviorAnalyzer(moduleLogger);
        _confidenceScorer = new ConfidenceScorer(moduleLogger);
        _containmentManager = new ContainmentManager(moduleLogger);
        _detectionEngine = new DetectionEngine(
            _fileSystemMonitor,
            _processTracker,
            _behaviorAnalyzer,
            _confidenceScorer,
            _containmentManager,
            moduleLogger);
        _healthReporter = new HealthReporter(_database, moduleLogger);
        _auditLogger = new AuditLogger(_database, moduleLogger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EchoEye X Protection Service starting...");

        try
        {
            // Report initial health
            await _healthReporter.ReportHealthAsync("Protection", HealthStatus.Healthy, "Service starting");

            // Start detection engine
            await _detectionEngine.StartAsync();

            // Subscribe to detections
            _detectionEngine.Detections.Subscribe(async detection =>
            {
                await HandleDetectionAsync(detection);
            });

            _logger.LogInformation("EchoEye X Protection Service started successfully");

            // Keep service running
            while (!stoppingToken.IsCancellationRequested)
            {
                // Periodic health check
                await _healthReporter.ReportHealthAsync("Protection", HealthStatus.Healthy);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Protection service error");
            await _healthReporter.ReportHealthAsync("Protection", HealthStatus.Unhealthy, ex.Message);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("EchoEye X Protection Service stopping...");

        await _detectionEngine.StopAsync();
        await _healthReporter.ReportHealthAsync("Protection", HealthStatus.Healthy, "Service stopped");

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Handle a detection event
    /// </summary>
    private async Task HandleDetectionAsync(DetectionResult detection)
    {
        _logger.LogWarning("Threat detected: {DetectionId} with confidence {Score}",
            detection.DetectionId, detection.Confidence.Score);

        // Log audit event
        await _auditLogger.LogAsync(new AuditEvent
        {
            Category = "Security",
            Action = "ThreatDetected",
            Details = detection,
            SourceModule = "Protection"
        });

        // If high confidence, trigger containment
        if (detection.Confidence.Level >= ConfidenceLevel.High)
        {
            _logger.LogWarning("Triggering containment for detection: {DetectionId}", detection.DetectionId);
            // Containment would be triggered here
        }
    }
}
