# EchoEye X API Specification

## Overview

This document defines the API contracts and service interfaces for all EchoEye X modules. All APIs follow RESTful principles where applicable, and inter-module communication uses well-defined C# interfaces.

## Service Interfaces

### Core Interfaces

#### IConfigurationProvider

Provides configuration management across all modules.

```csharp
public interface IConfigurationProvider
{
    // Get configuration value
    T GetValue<T>(string section, string key, T defaultValue = default);
    
    // Set configuration value
    void SetValue(string section, string key, object value);
    
    // Get entire section
    IDictionary<string, object> GetSection(string section);
    
    // Reload configuration from storage
    Task ReloadAsync();
    
    // Watch for configuration changes
    IObservable<ConfigurationChange> WatchChanges(string section = null);
    
    // Validate configuration
    ValidationResult Validate();
}

public class ConfigurationChange
{
    public string Section { get; set; }
    public string Key { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

#### IHealthReporter

Reports health status from all modules.

```csharp
public interface IHealthReporter
{
    // Report health status
    Task ReportHealthAsync(string module, HealthStatus status, string message = null, 
                          Dictionary<string, object> metrics = null);
    
    // Get health of specific module
    Task<SystemHealth> GetModuleHealthAsync(string module);
    
    // Get overall system health
    Task<SystemHealthSummary> GetSystemHealthAsync();
    
    // Check if system is healthy
    Task<bool> IsHealthyAsync();
}

public class SystemHealthSummary
{
    public HealthStatus OverallStatus { get; set; }
    public Dictionary<string, HealthStatus> ModuleStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> Issues { get; set; }
}
```

#### IAuditLogger

Logs audit events for compliance.

```csharp
public interface IAuditLogger
{
    // Log an audit event
    Task LogAsync(AuditEvent auditEvent);
    
    // Query audit logs
    Task<List<AuditLog>> QueryAsync(AuditQuery query);
    
    // Get audit trail for an entity
    Task<List<AuditLog>> GetEntityTrailAsync(string entityId, string entityType);
}

public class AuditEvent
{
    public string Category { get; set; }
    public string Action { get; set; }
    public string Actor { get; set; }
    public string Target { get; set; }
    public object Details { get; set; }
    public string SourceModule { get; set; }
    public string RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; }
    public bool IsSensitive { get; set; }
}

public class AuditQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Category { get; set; }
    public string Action { get; set; }
    public string Actor { get; set; }
    public string SourceModule { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}
```

### Protection Service Interfaces

#### IFileSystemMonitor

Monitors file system activity.

```csharp
public interface IFileSystemMonitor
{
    // Start monitoring a path
    Task StartMonitoringAsync(string path, bool recursive = true);
    
    // Stop monitoring a path
    Task StopMonitoringAsync(string path);
    
    // Get currently monitored paths
    Task<List<string>> GetMonitoredPathsAsync();
    
    // Subscribe to file events
    IObservable<FileEvent> FileEvents { get; }
    
    // Get monitoring statistics
    Task<MonitoringStatistics> GetStatisticsAsync();
}

public class MonitoringStatistics
{
    public int MonitoredPaths { get; set; }
    public long TotalEventsProcessed { get; set; }
    public long EventsPerSecond { get; set; }
    public DateTime StartedAt { get; set; }
}
```

#### IProcessTracker

Tracks process behavior.

```csharp
public interface IProcessTracker
{
    // Start tracking processes
    Task StartTrackingAsync();
    
    // Stop tracking
    Task StopTrackingAsync();
    
    // Get process info
    Task<ProcessInfo> GetProcessInfoAsync(string processId);
    
    // Get all active processes
    Task<List<ProcessInfo>> GetActiveProcessesAsync();
    
    // Update process reputation
    Task UpdateReputationAsync(string processId, ProcessReputation reputation);
    
    // Get suspicious processes
    Task<List<ProcessInfo>> GetSuspiciousProcessesAsync();
    
    // Subscribe to process events
    IObservable<ProcessEvent> ProcessEvents { get; }
}

public class ProcessEvent
{
    public string ProcessId { get; set; }
    public string ProcessName { get; set; }
    public ProcessEventType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public object Details { get; set; }
}

public enum ProcessEventType
{
    Started,
    Ended,
    ReputationChanged,
    FileActivity,
    NetworkActivity
}
```

#### IBehaviorAnalyzer

Analyzes behavioral patterns.

```csharp
public interface IBehaviorAnalyzer
{
    // Analyze file event
    Task<BehaviorAnalysisResult> AnalyzeFileEventAsync(FileEvent fileEvent);
    
    // Analyze process behavior
    Task<BehaviorAnalysisResult> AnalyzeProcessAsync(ProcessInfo process);
    
    // Analyze decoy interaction
    Task<BehaviorAnalysisResult> AnalyzeDecoyInteractionAsync(DecoyInteraction interaction);
    
    // Get baseline behavior
    Task<BehaviorBaseline> GetBaselineAsync();
    
    // Update baseline
    Task UpdateBaselineAsync(BehaviorBaseline baseline);
}

public class BehaviorAnalysisResult
{
    public double ConfidenceScore { get; set; }
    public List<string> Indicators { get; set; }
    public string Reasoning { get; set; }
    public Dictionary<string, double> SignalScores { get; set; }
    public bool IsSuspicious { get; set; }
}

public class BehaviorBaseline
{
    public double AverageFileModificationRate { get; set; }
    public double TypicalProcessCount { get; set; }
    public List<string> CommonProcesses { get; set; }
    public Dictionary<string, double> ExtensionFrequencies { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

#### IConfidenceScorer

Calculates confidence scores for detection.

```csharp
public interface IConfidenceScorer
{
    // Calculate confidence from multiple signals
    Task<ConfidenceScore> CalculateAsync(DetectionSignals signals);
    
    // Get confidence level from score
    ConfidenceLevel GetLevel(double score);
    
    // Update scoring weights
    Task UpdateWeightsAsync(Dictionary<string, double> weights);
    
    // Get current weights
    Task<Dictionary<string, double>> GetWeightsAsync();
}

public class DetectionSignals
{
    public double FileVelocityScore { get; set; }
    public double EntropyScore { get; set; }
    public double DecoyInteractionScore { get; set; }
    public double ProcessReputationScore { get; set; }
    public double BehaviorAnomalyScore { get; set; }
    public double NetworkActivityScore { get; set; }
    public Dictionary<string, double> AdditionalSignals { get; set; }
}

public class ConfidenceScore
{
    public double Score { get; set; }
    public ConfidenceLevel Level { get; set; }
    public Dictionary<string, double> ComponentScores { get; set; }
    public string Reasoning { get; set; }
}
```

#### IContainmentManager

Manages threat containment actions.

```csharp
public interface IContainmentManager
{
    // Contain a process
    Task<ContainmentResult> ContainProcessAsync(string processId, ContainmentAction action);
    
    // Contain a file
    Task<ContainmentResult> ContainFileAsync(string filePath, ContainmentAction action);
    
    // Isolate a device (network containment)
    Task<ContainmentResult> IsolateDeviceAsync();
    
    // Lift containment
    Task<ContainmentResult> LiftContainmentAsync(string containmentId);
    
    // Get active containments
    Task<List<Containment>> GetActiveContainmentsAsync();
}

public class ContainmentResult
{
    public bool Success { get; set; }
    public string ContainmentId { get; set; }
    public string Message { get; set; }
    public DateTime ContainedAt { get; set; }
}

public enum ContainmentAction
{
    SuspendProcess,
    TerminateProcess,
    QuarantineFile,
    DeleteFile,
    BlockNetworkAccess,
    IsolateDevice
}
```

#### IDetectionEngine

Orchestrates the detection pipeline.

```csharp
public interface IDetectionEngine
{
    // Start detection
    Task StartAsync();
    
    // Stop detection
    Task StopAsync();
    
    // Process a file event
    Task<DetectionResult> ProcessEventAsync(FileEvent fileEvent);
    
    // Get current detection status
    Task<DetectionStatus> GetStatusAsync();
    
    // Subscribe to detections
    IObservable<DetectionResult> Detections { get; }
}

public class DetectionResult
{
    public string DetectionId { get; set; }
    public bool IsThreat { get; set; }
    public ConfidenceScore Confidence { get; set; }
    public List<string> Indicators { get; set; }
    public string IncidentId { get; set; }
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
```

### Decoy Engine Interfaces

#### IDecoyGenerator

Generates realistic decoy assets.

```csharp
public interface IDecoyGenerator
{
    // Generate a decoy file
    Task<DecoyAsset> GenerateAsync(DecoyType type, string targetPath, 
                                   DecoyOptions options = null);
    
    // Generate multiple decoys
    Task<List<DecoyAsset>> GenerateBatchAsync(DecoyType type, string targetPath, 
                                             int count, DecoyOptions options = null);
    
    // Get available decoy templates
    Task<List<DecoyTemplate>> GetTemplatesAsync(DecoyType type);
}

public class DecoyOptions
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public bool IncludeRealisticContent { get; set; }
}

public class DecoyTemplate
{
    public string TemplateId { get; set; }
    public string Name { get; set; }
    public DecoyType Type { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

#### IDecoyDeployer

Deploys decoys to strategic locations.

```csharp
public interface IDecoyDeployer
{
    // Deploy a decoy
    Task<DecoyAsset> DeployAsync(DecoyAsset decoy, string targetPath);
    
    // Deploy decoys to default locations
    Task<List<DecoyAsset>> DeployToDefaultLocationsAsync(List<DecoyAsset> decoys);
    
    // Get deployment strategy
    Task<DeploymentStrategy> GetStrategyAsync();
    
    // Update deployment strategy
    Task UpdateStrategyAsync(DeploymentStrategy strategy);
}

public class DeploymentStrategy
{
    public List<string> TargetPaths { get; set; }
    public int DecoysPerPath { get; set; }
    public bool RandomizeNames { get; set; }
    public bool RandomizeLocations { get; set; }
    public List<DecoyType> AllowedTypes { get; set; }
}
```

#### IDecoyMonitor

Monitors decoy interactions.

```csharp
public interface IDecoyMonitor
{
    // Start monitoring
    Task StartMonitoringAsync();
    
    // Stop monitoring
    Task StopMonitoringAsync();
    
    // Check if a file is a decoy
    Task<bool> IsDecoyAsync(string filePath);
    
    // Get decoy by path
    Task<DecoyAsset> GetDecoyAsync(string filePath);
    
    // Record interaction
    Task<DecoyInteraction> RecordInteractionAsync(string filePath, InteractionType type, 
                                                  string processId);
    
    // Get recent interactions
    Task<List<DecoyInteraction>> GetRecentInteractionsAsync(TimeSpan timeWindow);
    
    // Subscribe to interactions
    IObservable<DecoyInteraction> Interactions { get; }
}
```

#### IIndicatorGenerator

Generates threat indicators from decoy interactions.

```csharp
public interface IIndicatorGenerator
{
    // Generate indicator from interaction
    Task<ThreatIndicator> GenerateAsync(DecoyInteraction interaction);
    
    // Generate indicator from multiple interactions
    Task<ThreatIndicator> GenerateFromBatchAsync(List<DecoyInteraction> interactions);
    
    // Get indicator confidence
    double CalculateConfidence(DecoyInteraction interaction);
}

public class ThreatIndicator
{
    public string IndicatorId { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public double Confidence { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Source { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### Recovery Engine Interfaces

#### IRecoveryPointManager

Manages recovery point creation and lifecycle.

```csharp
public interface IRecoveryPointManager
{
    // Create a recovery point
    Task<RecoveryPoint> CreateAsync(RecoveryPointOptions options);
    
    // Create automatic recovery point
    Task<RecoveryPoint> CreateAutomaticAsync();
    
    // Get recovery point
    Task<RecoveryPoint> GetAsync(string recoveryPointId);
    
    // List recovery points
    Task<List<RecoveryPoint>> ListAsync(RecoveryPointFilter filter = null);
    
    // Delete recovery point
    Task DeleteAsync(string recoveryPointId);
    
    // Validate recovery point
    Task<ValidationResult> ValidateAsync(string recoveryPointId);
    
    // Get recovery point statistics
    Task<RecoveryPointStatistics> GetStatisticsAsync();
}

public class RecoveryPointOptions
{
    public string Name { get; set; }
    public RecoveryPointType Type { get; set; }
    public List<string> IncludePaths { get; set; }
    public List<string> ExcludePaths { get; set; }
    public bool IsAutomatic { get; set; }
    public string RelatedIncidentId { get; set; }
}

public class RecoveryPointFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public RecoveryPointType? Type { get; set; }
    public bool? IsValid { get; set; }
    public bool? IsAutomatic { get; set; }
}

public class RecoveryPointStatistics
{
    public int TotalRecoveryPoints { get; set; }
    public long TotalStorageUsed { get; set; }
    public int ValidPoints { get; set; }
    public int InvalidPoints { get; set; }
    public DateTime OldestPoint { get; set; }
    public DateTime NewestPoint { get; set; }
}
```

#### IIntegrityValidator

Validates recovery point integrity.

```csharp
public interface IIntegrityValidator
{
    // Validate recovery point
    Task<ValidationResult> ValidateRecoveryPointAsync(string recoveryPointId);
    
    // Validate specific file in recovery point
    Task<FileValidationResult> ValidateFileAsync(string recoveryPointId, string filePath);
    
    // Repair corrupted recovery point
    Task<RepairResult> RepairAsync(string recoveryPointId);
    
    // Get validation history
    Task<List<ValidationRecord>> GetValidationHistoryAsync(string recoveryPointId);
}

public class FileValidationResult
{
    public string FilePath { get; set; }
    public bool IsValid { get; set; }
    public string ExpectedHash { get; set; }
    public string ActualHash { get; set; }
    public string ErrorMessage { get; set; }
}

public class RepairResult
{
    public bool Success { get; set; }
    public int FilesRepaired { get; set; }
    public int FilesFailed { get; set; }
    public string Message { get; set; }
}
```

#### IStorageManager

Manages recovery point storage.

```csharp
public interface IStorageManager
{
    // Get available storage
    Task<StorageInfo> GetStorageInfoAsync();
    
    // Clean up old recovery points
    Task<CleanupResult> CleanupAsync(CleanupPolicy policy);
    
    // Estimate storage needed for recovery point
    Task<long> EstimateStorageAsync(List<string> paths);
    
    // Compress recovery point
    Task<CompressionResult> CompressAsync(string recoveryPointId);
}

public class StorageInfo
{
    public long TotalCapacity { get; set; }
    public long UsedSpace { get; set; }
    public long AvailableSpace { get; set; }
    public double UsagePercentage { get; set; }
}

public class CleanupPolicy
{
    public TimeSpan? OlderThan { get; set; }
    public int? KeepCount { get; set; }
    public bool RemoveInvalid { get; set; }
    public double? TargetUsagePercentage { get; set; }
}

public class CleanupResult
{
    public int RecoveryPointsRemoved { get; set; }
    public long SpaceFreed { get; set; }
    public List<string> RemovedIds { get; set; }
}
```

#### IRestorationEngine

Performs file restoration.

```csharp
public interface IRestorationEngine
{
    // Restore single file
    Task<RestorationResult> RestoreFileAsync(string filePath, string recoveryPointId);
    
    // Restore multiple files
    Task<RestorationResult> RestoreFilesAsync(List<string> filePaths, string recoveryPointId);
    
    // Full restore from recovery point
    Task<RestorationResult> FullRestoreAsync(string recoveryPointId);
    
    // Get restoration progress
    Task<RestorationProgress> GetProgressAsync(string operationId);
    
    // Cancel restoration
    Task CancelAsync(string operationId);
}

public class RestorationResult
{
    public string OperationId { get; set; }
    public RecoveryStatus Status { get; set; }
    public int FilesAttempted { get; set; }
    public int FilesSucceeded { get; set; }
    public int FilesFailed { get; set; }
    public long BytesRestored { get; set; }
    public List<string> FailedFiles { get; set; }
    public string Summary { get; set; }
}

public class RestorationProgress
{
    public string OperationId { get; set; }
    public RecoveryStatus Status { get; set; }
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}
```

### Intelligence Node Interfaces

#### IThreatCollector

Collects threat indicators.

```csharp
public interface IThreatCollector
{
    // Collect indicator
    Task CollectAsync(ThreatIndicator indicator);
    
    // Collect from incident
    Task CollectFromIncidentAsync(string incidentId);
    
    // Get collected indicators
    Task<List<ThreatIndicator>> GetIndicatorsAsync(ThreatIndicatorFilter filter = null);
    
    // Generate threat report
    Task<ThreatReport> GenerateReportAsync(List<ThreatIndicator> indicators);
}

public class ThreatIndicatorFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Type { get; set; }
    public double? MinConfidence { get; set; }
}
```

#### IAnonymizer

Anonymizes sensitive data before sharing.

```csharp
public interface IAnonymizer
{
    // Anonymize file path
    string AnonymizePath(string filePath);
    
    // Anonymize process name
    string AnonymizeProcess(string processName);
    
    // Anonymize threat report
    Task<ThreatReport> AnonymizeReportAsync(ThreatReport report);
    
    // Hash identifier
    string HashIdentifier(string identifier);
}
```

#### IPeerManager

Manages intelligence peer communication.

```csharp
public interface IPeerManager
{
    // Discover peers
    Task<List<IntelligencePeer>> DiscoverPeersAsync();
    
    // Connect to peer
    Task ConnectToPeerAsync(string peerAddress, int port);
    
    // Disconnect from peer
    Task DisconnectFromPeerAsync(string peerId);
    
    // Sync with peer
    Task<SyncResult> SyncAsync(string peerId);
    
    // Get connected peers
    Task<List<IntelligencePeer>> GetConnectedPeersAsync();
    
    // Get peer status
    Task<PeerStatus> GetPeerStatusAsync(string peerId);
}

public class SyncResult
{
    public bool Success { get; set; }
    public int ReportsReceived { get; set; }
    public int ReportsSent { get; set; }
    public string Message { get; set; }
}
```

#### IIntelligenceAggregator

Aggregates threat intelligence.

```csharp
public interface IIntelligenceAggregator
{
    // Generate intelligence feed
    Task<IntelligenceFeed> GenerateFeedAsync(FeedFilter filter = null);
    
    // Get threat statistics
    Task<ThreatStatistics> GetStatisticsAsync();
    
    // Get trending threats
    Task<List<ThreatFamily>> GetTrendingThreatsAsync(TimeSpan timeWindow);
    
    // Search threats
    Task<List<ThreatReport>> SearchThreatsAsync(ThreatSearchQuery query);
}

public class IntelligenceFeed
{
    public string FeedId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<ThreatReport> Reports { get; set; }
    public List<ThreatFamily> Families { get; set; }
    public ThreatStatistics Statistics { get; set; }
}

public class FeedFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> ThreatFamilies { get; set; }
    public string GeographicRegion { get; set; }
    public double? MinConfidence { get; set; }
}

public class ThreatStatistics
{
    public int TotalReports { get; set; }
    public int UniqueThreatFamilies { get; set; }
    public Dictionary<string, int> FamilyCounts { get; set; }
    public Dictionary<string, int> RegionalDistribution { get; set; }
    public double AverageConfidence { get; set; }
}
```

### Management Layer Interfaces

#### IAlertManager

Manages security alerts.

```csharp
public interface IAlertManager
{
    // Create alert
    Task<Alert> CreateAlertAsync(Alert alert);
    
    // Get alert
    Task<Alert> GetAlertAsync(string alertId);
    
    // List alerts
    Task<List<Alert>> ListAlertsAsync(AlertFilter filter = null);
    
    // Acknowledge alert
    Task AcknowledgeAsync(string alertId, string acknowledgedBy);
    
    // Resolve alert
    Task ResolveAsync(string alertId, string resolutionNotes);
    
    // Subscribe to new alerts
    IObservable<Alert> NewAlerts { get; }
}

public class AlertFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public AlertSeverity? Severity { get; set; }
    public AlertType? Type { get; set; }
    public bool? IsResolved { get; set; }
    public string IncidentId { get; set; }
}
```

#### IIncidentManager

Manages security incidents.

```csharp
public interface IIncidentManager
{
    // Create incident
    Task<Incident> CreateIncidentAsync(Incident incident);
    
    // Get incident
    Task<Incident> GetIncidentAsync(string incidentId);
    
    // Update incident
    Task UpdateIncidentAsync(string incidentId, IncidentUpdate update);
    
    // List incidents
    Task<List<Incident>> ListIncidentsAsync(IncidentFilter filter = null);
    
    // Mark as false positive
    Task MarkFalsePositiveAsync(string incidentId, string reason);
    
    // Get incident timeline
    Task<IncidentTimeline> GetTimelineAsync(string incidentId);
}

public class IncidentUpdate
{
    public IncidentStatus? Status { get; set; }
    public string ContainmentAction { get; set; }
    public string Summary { get; set; }
    public string Details { get; set; }
}

public class IncidentFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public IncidentStatus? Status { get; set; }
    public ConfidenceLevel? MinConfidence { get; set; }
    public bool? IsFalsePositive { get; set; }
}

public class IncidentTimeline
{
    public string IncidentId { get; set; }
    public List<TimelineEvent> Events { get; set; }
}

public class TimelineEvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; }
    public string Description { get; set; }
    public object Details { get; set; }
}
```

#### IStatusReporter

Aggregates and reports system status.

```csharp
public interface IStatusReporter
{
    // Get system status
    Task<SystemStatus> GetSystemStatusAsync();
    
    // Get module status
    Task<ModuleStatus> GetModuleStatusAsync(string module);
    
    // Get dashboard data
    Task<DashboardData> GetDashboardDataAsync();
    
    // Generate status report
    Task<StatusReport> GenerateReportAsync(StatusReportOptions options);
}

public class SystemStatus
{
    public HealthStatus OverallHealth { get; set; }
    public Dictionary<string, ModuleStatus> ModuleStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public List<string> ActiveAlerts { get; set; }
    public List<string> ActiveIncidents { get; set; }
}

public class ModuleStatus
{
    public string Module { get; set; }
    public HealthStatus Health { get; set; }
    public bool IsRunning { get; set; }
    public DateTime LastActivity { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metrics { get; set; }
}

public class DashboardData
{
    public SystemStatus SystemStatus { get; set; }
    public List<Alert> RecentAlerts { get; set; }
    public List<Incident> ActiveIncidents { get; set; }
    public RecoveryPointStatistics RecoveryStats { get; set; }
    public ThreatStatistics ThreatStats { get; set; }
    public List<DecoyAsset> DecoyStatus { get; set; }
}
```

## REST API (Dashboard)

### Authentication

All API endpoints require authentication via Bearer token.

```
Authorization: Bearer <token>
```

### Endpoints

#### Authentication

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "password"
}

Response:
{
  "token": "jwt_token_here",
  "expiresIn": 3600
}
```

#### System Status

```http
GET /api/status

Response:
{
  "overallHealth": "Healthy",
  "moduleStatus": {
    "protection": {
      "health": "Healthy",
      "isRunning": true,
      "lastActivity": "2024-01-01T00:00:00Z"
    },
    "decoy": {
      "health": "Healthy",
      "isRunning": true,
      "lastActivity": "2024-01-01T00:00:00Z"
    }
  },
  "lastUpdated": "2024-01-01T00:00:00Z"
}
```

#### Alerts

```http
GET /api/alerts?severity=Critical&isResolved=false

Response:
{
  "alerts": [
    {
      "alertId": "uuid",
      "severity": "Critical",
      "type": "FileModification",
      "title": "Mass file encryption detected",
      "description": "...",
      "generatedAt": "2024-01-01T00:00:00Z",
      "isResolved": false
    }
  ],
  "totalCount": 1
}

POST /api/alerts/{alertId}/acknowledge
Content-Type: application/json

{
  "acknowledgedBy": "admin"
}

POST /api/alerts/{alertId}/resolve
Content-Type: application/json

{
  "resolutionNotes": "Investigated and confirmed"
}
```

#### Incidents

```http
GET /api/incidents?status=Active&minConfidence=High

Response:
{
  "incidents": [
    {
      "incidentId": "uuid",
      "status": "Active",
      "confidence": "Critical",
      "detectedAt": "2024-01-01T00:00:00Z",
      "affectedFileCount": 150,
      "summary": "Ransomware activity detected"
    }
  ],
  "totalCount": 1
}

GET /api/incidents/{incidentId}

Response:
{
  "incidentId": "uuid",
  "status": "Active",
  "confidence": "Critical",
  "detectedAt": "2024-01-01T00:00:00Z",
  "affectedFileCount": 150,
  "affectedDataSizeBytes": 1073741824,
  "containmentAction": "ProcessTerminated",
  "summary": "Ransomware activity detected",
  "details": {...},
  "threatFamilyId": "uuid"
}

GET /api/incidents/{incidentId}/timeline

Response:
{
  "incidentId": "uuid",
  "events": [
    {
      "timestamp": "2024-01-01T00:00:00Z",
      "eventType": "Detected",
      "description": "Suspicious activity detected",
      "details": {...}
    }
  ]
}

POST /api/incidents/{incidentId}/false-positive
Content-Type: application/json

{
  "reason": "Legitimate backup operation"
}
```

#### Recovery Points

```http
GET /api/recovery-points

Response:
{
  "recoveryPoints": [
    {
      "recoveryPointId": "uuid",
      "name": "Automatic - 2024-01-01",
      "type": "Incremental",
      "createdAt": "2024-01-01T00:00:00Z",
      "fileCount": 1000,
      "totalSizeBytes": 10737418240,
      "isValid": true
    }
  ],
  "totalCount": 10
}

POST /api/recovery-points
Content-Type: application/json

{
  "name": "Manual Backup",
  "type": "Full",
  "includePaths": ["C:\\Users"],
  "excludePaths": ["C:\\Users\\Public"]
}

POST /api/recovery-points/{recoveryPointId}/validate

DELETE /api/recovery-points/{recoveryPointId}
```

#### Restoration

```http
POST /api/restoration/file
Content-Type: application/json

{
  "filePath": "C:\\Users\\Documents\\file.docx",
  "recoveryPointId": "uuid"
}

Response:
{
  "operationId": "uuid",
  "status": "Pending",
  "message": "Restoration queued"
}

POST /api/restoration/batch
Content-Type: application/json

{
  "filePaths": ["path1", "path2"],
  "recoveryPointId": "uuid"
}

POST /api/restoration/full
Content-Type: application/json

{
  "recoveryPointId": "uuid"
}

GET /api/restoration/operations/{operationId}

Response:
{
  "operationId": "uuid",
  "status": "InProgress",
  "totalFiles": 100,
  "completedFiles": 50,
  "progressPercentage": 50.0,
  "elapsedTime": "00:05:00",
  "estimatedTimeRemaining": "00:05:00"
}

DELETE /api/restoration/operations/{operationId}
```

#### Decoys

```http
GET /api/decoys

Response:
{
  "decoys": [
    {
      "decoyId": "uuid",
      "filePath": "C:\\Users\\Documents\\decoy.docx",
      "type": "Document",
      "createdAt": "2024-01-01T00:00:00Z",
      "accessCount": 5,
      "isActive": true
    }
  ],
  "totalCount": 50
}

POST /api/decoys/deploy
Content-Type: application/json

{
  "type": "Document",
  "count": 10,
  "targetPaths": ["C:\\Users\\Documents"]
}

GET /api/decoys/interactions?timeWindow=1h

Response:
{
  "interactions": [
    {
      "interactionId": "uuid",
      "decoyId": "uuid",
      "type": "Write",
      "occurredAt": "2024-01-01T00:00:00Z",
      "processName": "suspicious.exe",
      "confidenceScore": 0.95
    }
  ],
  "totalCount": 3
}
```

#### Threat Intelligence

```http
GET /api/intelligence/feed

Response:
{
  "feedId": "uuid",
  "generatedAt": "2024-01-01T00:00:00Z",
  "reports": [...],
  "families": [...],
  "statistics": {
    "totalReports": 100,
    "uniqueThreatFamilies": 15,
    "averageConfidence": 0.85
  }
}

GET /api/intelligence/trending?timeWindow=7d

Response:
{
  "threats": [
    {
      "familyId": "uuid",
      "name": "LockBit",
      "category": "Ransomware",
      "totalDetections": 50,
      "severity": "Critical"
    }
  ]
}

GET /api/intelligence/search
Content-Type: application/json

{
  "query": "ransomware",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31"
}
```

#### Configuration

```http
GET /api/configuration/{section}

Response:
{
  "section": "protection",
  "settings": {
    "enabled": true,
    "confidenceThreshold": 75
  }
}

PUT /api/configuration/{section}
Content-Type: application/json

{
  "enabled": true,
  "confidenceThreshold": 80
}

GET /api/configuration

Response:
{
  "sections": [
    {
      "name": "protection",
      "keyCount": 10
    },
    {
      "name": "recovery",
      "keyCount": 5
    }
  ]
}
```

#### Audit Logs

```http
GET /api/audit-logs?startDate=2024-01-01&endDate=2024-01-31

Response:
{
  "logs": [
    {
      "logId": "uuid",
      "timestamp": "2024-01-01T00:00:00Z",
      "category": "Configuration",
      "action": "Update",
      "actor": "admin",
      "details": {...}
    }
  ],
  "totalCount": 100
}
```

## WebSocket API (Real-time Updates)

### Connection

```
ws://localhost:8080/ws
```

### Message Format

```json
{
  "type": "alert",
  "data": {
    "alertId": "uuid",
    "severity": "Critical",
    "title": "Mass file encryption detected"
  }
}
```

### Event Types

- `alert` - New alert created
- `incident` - Incident status changed
- `health` - Module health changed
- `decoy_interaction` - Decoy interaction detected
- `restoration_progress` - Restoration progress update

## Error Responses

All endpoints return standard error responses:

```json
{
  "error": {
    "code": "ErrorCode",
    "message": "Human readable error message",
    "details": "Additional details"
  }
}
```

### Common Error Codes

- `INVALID_REQUEST` - Malformed request
- `UNAUTHORIZED` - Authentication required
- `FORBIDDEN` - Insufficient permissions
- `NOT_FOUND` - Resource not found
- `CONFLICT` - Resource conflict
- `INTERNAL_ERROR` - Server error
- `SERVICE_UNAVAILABLE` - Service temporarily unavailable

## Rate Limiting

API endpoints are rate-limited:

- Authenticated users: 1000 requests per hour
- Unauthenticated: 100 requests per hour

Rate limit headers are included in responses:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1704067200
```
