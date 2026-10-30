# EchoEye X Data Models

## Overview

This document defines the data models used across all EchoEye X modules. All models are designed for SQLite storage with appropriate indexing for performance.

## Core Data Models

### Device Identity

```csharp
public class DeviceIdentity
{
    public string DeviceId { get; set; }  // UUID v4
    public string MachineName { get; set; }
    public string OsVersion { get; set; }
    public string Fingerprint { get; set; }  // SHA-256 of hardware identifiers
    public DateTime FirstRegistered { get; set; }
    public DateTime LastSeen { get; set; }
    public string RegistrationKey { get; set; }  // Encrypted
}
```

**Table Schema**:
```sql
CREATE TABLE DeviceIdentity (
    DeviceId TEXT PRIMARY KEY,
    MachineName TEXT NOT NULL,
    OsVersion TEXT NOT NULL,
    Fingerprint TEXT NOT NULL UNIQUE,
    FirstRegistered TEXT NOT NULL,
    LastSeen TEXT NOT NULL,
    RegistrationKey BLOB NOT NULL
);

CREATE INDEX idx_device_fingerprint ON DeviceIdentity(Fingerprint);
CREATE INDEX idx_device_last_seen ON DeviceIdentity(LastSeen);
```

### Incident

```csharp
public class Incident
{
    public string IncidentId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public IncidentStatus Status { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ContainedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ThreatFamilyId { get; set; }
    public int AffectedFileCount { get; set; }
    public long AffectedDataSizeBytes { get; set; }
    public string ContainmentAction { get; set; }
    public string Summary { get; set; }
    public string Details { get; set; }  // JSON
    public string ForensicEvidencePath { get; set; }
    public bool IsFalsePositive { get; set; }
    public string FalsePositiveReason { get; set; }
}

public enum IncidentStatus
{
    Detected,
    Containing,
    Contained,
    Investigating,
    Recovering,
    Resolved,
    FalsePositive
}

public enum ConfidenceLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
```

**Table Schema**:
```sql
CREATE TABLE Incident (
    IncidentId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    Status INTEGER NOT NULL,
    Confidence INTEGER NOT NULL,
    DetectedAt TEXT NOT NULL,
    ContainedAt TEXT,
    ResolvedAt TEXT,
    ThreatFamilyId TEXT,
    AffectedFileCount INTEGER NOT NULL DEFAULT 0,
    AffectedDataSizeBytes INTEGER NOT NULL DEFAULT 0,
    ContainmentAction TEXT,
    Summary TEXT,
    Details TEXT,
    ForensicEvidencePath TEXT,
    IsFalsePositive INTEGER NOT NULL DEFAULT 0,
    FalsePositiveReason TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_incident_device ON Incident(DeviceId);
CREATE INDEX idx_incident_status ON Incident(Status);
CREATE INDEX idx_incident_confidence ON Incident(Confidence);
CREATE INDEX idx_incident_detected ON Incident(DetectedAt);
CREATE INDEX idx_incident_threat ON Incident(ThreatFamilyId);
```

### Alert

```csharp
public class Alert
{
    public string AlertId { get; set; }  // UUID v4
    public string IncidentId { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertType Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceModule { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedBy { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolutionNotes { get; set; }
    public string RelatedData { get; set; }  // JSON
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum AlertType
{
    FileModification,
    DecoyInteraction,
    ProcessSuspicious,
    NetworkAnomaly,
    RecoveryFailure,
    SystemHealth,
    ConfigurationChange
}
```

**Table Schema**:
```sql
CREATE TABLE Alert (
    AlertId TEXT PRIMARY KEY,
    IncidentId TEXT,
    Severity INTEGER NOT NULL,
    Type INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT,
    SourceModule TEXT NOT NULL,
    GeneratedAt TEXT NOT NULL,
    AcknowledgedAt TEXT,
    AcknowledgedBy TEXT,
    IsResolved INTEGER NOT NULL DEFAULT 0,
    ResolvedAt TEXT,
    ResolutionNotes TEXT,
    RelatedData TEXT,
    FOREIGN KEY (IncidentId) REFERENCES Incident(IncidentId)
);

CREATE INDEX idx_alert_incident ON Alert(IncidentId);
CREATE INDEX idx_alert_severity ON Alert(Severity);
CREATE INDEX idx_alert_type ON Alert(Type);
CREATE INDEX idx_alert_generated ON Alert(GeneratedAt);
CREATE INDEX idx_alert_resolved ON Alert(IsResolved);
```

### File Event

```csharp
public class FileEvent
{
    public string EventId { get; set; }  // UUID v4
    public string IncidentId { get; set; }
    public string FilePath { get; set; }
    public FileChangeType ChangeType { get; set; }
    public DateTime OccurredAt { get; set; }
    public string ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string ProcessPath { get; set; }
    public long OriginalSize { get; set; }
    public long NewSize { get; set; }
    public string OriginalHash { get; set; }  // SHA-256
    public string NewHash { get; set; }
    public double EntropyScore { get; set; }
    public bool IsDecoy { get; set; }
    public string RelatedDecoyId { get; set; }
}

public enum FileChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed,
    Encrypted
}
```

**Table Schema**:
```sql
CREATE TABLE FileEvent (
    EventId TEXT PRIMARY KEY,
    IncidentId TEXT,
    FilePath TEXT NOT NULL,
    ChangeType INTEGER NOT NULL,
    OccurredAt TEXT NOT NULL,
    ProcessId TEXT,
    ProcessName TEXT,
    ProcessPath TEXT,
    OriginalSize INTEGER NOT NULL DEFAULT 0,
    NewSize INTEGER NOT NULL DEFAULT 0,
    OriginalHash TEXT,
    NewHash TEXT,
    EntropyScore REAL,
    IsDecoy INTEGER NOT NULL DEFAULT 0,
    RelatedDecoyId TEXT,
    FOREIGN KEY (IncidentId) REFERENCES Incident(IncidentId)
);

CREATE INDEX idx_file_event_incident ON FileEvent(IncidentId);
CREATE INDEX idx_file_event_path ON FileEvent(FilePath);
CREATE INDEX idx_file_event_occurred ON FileEvent(OccurredAt);
CREATE INDEX idx_file_event_process ON FileEvent(ProcessId);
CREATE INDEX idx_file_event_decoy ON FileEvent(IsDecoy);
```

### Process Info

```csharp
public class ProcessInfo
{
    public string ProcessId { get; set; }
    public string DeviceId { get; set; }
    public string ProcessName { get; set; }
    public string ExecutablePath { get; set; }
    public string CommandLine { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public ProcessReputation Reputation { get; set; }
    public string ParentProcessId { get; set; }
    public int FileModificationCount { get; set; }
    public int DecoyInteractionCount { get; set; }
    public string SignatureStatus { get; set; }
    public string Publisher { get; set; }
}

public enum ProcessReputation
{
    Unknown,
    Trusted,
    Suspicious,
    Malicious
}
```

**Table Schema**:
```sql
CREATE TABLE ProcessInfo (
    ProcessId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    ProcessName TEXT NOT NULL,
    ExecutablePath TEXT NOT NULL,
    CommandLine TEXT,
    StartedAt TEXT NOT NULL,
    EndedAt TEXT,
    Reputation INTEGER NOT NULL DEFAULT 0,
    ParentProcessId TEXT,
    FileModificationCount INTEGER NOT NULL DEFAULT 0,
    DecoyInteractionCount INTEGER NOT NULL DEFAULT 0,
    SignatureStatus TEXT,
    Publisher TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_process_device ON ProcessInfo(DeviceId);
CREATE INDEX idx_process_name ON ProcessInfo(ProcessName);
CREATE INDEX idx_process_reputation ON ProcessInfo(Reputation);
CREATE INDEX idx_process_started ON ProcessInfo(StartedAt);
```

### Decoy Asset

```csharp
public class DecoyAsset
{
    public string DecoyId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public string FilePath { get; set; }
    public DecoyType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public int AccessCount { get; set; }
    public int ModificationCount { get; set; }
    public bool IsActive { get; set; }
    public string ContentHash { get; set; }  // SHA-256
    public long FileSize { get; set; }
    public string Metadata { get; set; }  // JSON
}

public enum DecoyType
{
    Document,
    Image,
    Database,
    Archive,
    Configuration
}
```

**Table Schema**:
```sql
CREATE TABLE DecoyAsset (
    DecoyId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    FilePath TEXT NOT NULL UNIQUE,
    Type INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    LastAccessedAt TEXT,
    LastModifiedAt TEXT,
    AccessCount INTEGER NOT NULL DEFAULT 0,
    ModificationCount INTEGER NOT NULL DEFAULT 0,
    IsActive INTEGER NOT NULL DEFAULT 1,
    ContentHash TEXT NOT NULL,
    FileSize INTEGER NOT NULL,
    Metadata TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_decoy_device ON DecoyAsset(DeviceId);
CREATE INDEX idx_decoy_type ON DecoyAsset(Type);
CREATE INDEX idx_decoy_active ON DecoyAsset(IsActive);
CREATE INDEX idx_decoy_accessed ON DecoyAsset(LastAccessedAt);
```

### Decoy Interaction

```csharp
public class DecoyInteraction
{
    public string InteractionId { get; set; }  // UUID v4
    public string DecoyId { get; set; }
    public string IncidentId { get; set; }
    public InteractionType Type { get; set; }
    public DateTime OccurredAt { get; set; }
    public string ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string ProcessPath { get; set; }
    public string OriginalHash { get; set; }
    public string NewHash { get; set; }
    public double ConfidenceScore { get; set; }
    public string Details { get; set; }  // JSON
}

public enum InteractionType
{
    Read,
    Write,
    Delete,
    Rename,
    Encrypt
}
```

**Table Schema**:
```sql
CREATE TABLE DecoyInteraction (
    InteractionId TEXT PRIMARY KEY,
    DecoyId TEXT NOT NULL,
    IncidentId TEXT,
    Type INTEGER NOT NULL,
    OccurredAt TEXT NOT NULL,
    ProcessId TEXT,
    ProcessName TEXT,
    ProcessPath TEXT,
    OriginalHash TEXT,
    NewHash TEXT,
    ConfidenceScore REAL NOT NULL,
    Details TEXT,
    FOREIGN KEY (DecoyId) REFERENCES DecoyAsset(DecoyId),
    FOREIGN KEY (IncidentId) REFERENCES Incident(IncidentId)
);

CREATE INDEX idx_decoy_interaction_decoy ON DecoyInteraction(DecoyId);
CREATE INDEX idx_decoy_interaction_incident ON DecoyInteraction(IncidentId);
CREATE INDEX idx_decoy_interaction_occurred ON DecoyInteraction(OccurredAt);
CREATE INDEX idx_decoy_interaction_type ON DecoyInteraction(Type);
```

### Recovery Point

```csharp
public class RecoveryPoint
{
    public string RecoveryPointId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public string Name { get; set; }
    public RecoveryPointType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public string Checksum { get; set; }  // SHA-256 of manifest
    public string StoragePath { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public string ValidationErrors { get; set; }
    public bool IsAutomatic { get; set; }
    public string RelatedIncidentId { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public enum RecoveryPointType
{
    Full,
    Incremental,
    Differential
}
```

**Table Schema**:
```sql
CREATE TABLE RecoveryPoint (
    RecoveryPointId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Type INTEGER NOT NULL,
    CreatedAt TEXT NOT NULL,
    FileCount INTEGER NOT NULL DEFAULT 0,
    TotalSizeBytes INTEGER NOT NULL DEFAULT 0,
    Checksum TEXT NOT NULL,
    StoragePath TEXT NOT NULL,
    IsValid INTEGER NOT NULL DEFAULT 1,
    ValidatedAt TEXT,
    ValidationErrors TEXT,
    IsAutomatic INTEGER NOT NULL DEFAULT 0,
    RelatedIncidentId TEXT,
    ExpiresAt TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId),
    FOREIGN KEY (RelatedIncidentId) REFERENCES Incident(IncidentId)
);

CREATE INDEX idx_recovery_device ON RecoveryPoint(DeviceId);
CREATE INDEX idx_recovery_type ON RecoveryPoint(Type);
CREATE INDEX idx_recovery_created ON RecoveryPoint(CreatedAt);
CREATE INDEX idx_recovery_valid ON RecoveryPoint(IsValid);
CREATE INDEX idx_recovery_incident ON RecoveryPoint(RelatedIncidentId);
```

### Recovery Point File

```csharp
public class RecoveryPointFile
{
    public string FileId { get; set; }  // UUID v4
    public string RecoveryPointId { get; set; }
    public string OriginalPath { get; set; }
    public string BackupPath { get; set; }
    public long Size { get; set; }
    public DateTime OriginalModifiedTime { get; set; }
    public string Hash { get; set; }  // SHA-256
    public string CompressionAlgorithm { get; set; }
    public long CompressedSize { get; set; }
    public bool IsEncrypted { get; set; }
}
```

**Table Schema**:
```sql
CREATE TABLE RecoveryPointFile (
    FileId TEXT PRIMARY KEY,
    RecoveryPointId TEXT NOT NULL,
    OriginalPath TEXT NOT NULL,
    BackupPath TEXT NOT NULL,
    Size INTEGER NOT NULL,
    OriginalModifiedTime TEXT NOT NULL,
    Hash TEXT NOT NULL,
    CompressionAlgorithm TEXT,
    CompressedSize INTEGER,
    IsEncrypted INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (RecoveryPointId) REFERENCES RecoveryPoint(RecoveryPointId)
);

CREATE INDEX idx_recovery_file_point ON RecoveryPointFile(RecoveryPointId);
CREATE INDEX idx_recovery_file_path ON RecoveryPointFile(OriginalPath);
CREATE INDEX idx_recovery_file_hash ON RecoveryPointFile(Hash);
```

### Recovery Operation

```csharp
public class RecoveryOperation
{
    public string OperationId { get; set; }  // UUID v4
    public string IncidentId { get; set; }
    public string RecoveryPointId { get; set; }
    public RecoveryOperationType Type { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RecoveryStatus Status { get; set; }
    public int FilesAttempted { get; set; }
    public int FilesSucceeded { get; set; }
    public int FilesFailed { get; set; }
    public long BytesRestored { get; set; }
    public string ErrorDetails { get; set; }
    public string Summary { get; set; }
}

public enum RecoveryOperationType
{
    FullRestore,
    SelectiveRestore,
    SingleFileRestore
}

public enum RecoveryStatus
{
    Pending,
    InProgress,
    Completed,
    PartialSuccess,
    Failed,
    Cancelled
}
```

**Table Schema**:
```sql
CREATE TABLE RecoveryOperation (
    OperationId TEXT PRIMARY KEY,
    IncidentId TEXT,
    RecoveryPointId TEXT NOT NULL,
    Type INTEGER NOT NULL,
    StartedAt TEXT NOT NULL,
    CompletedAt TEXT,
    Status INTEGER NOT NULL,
    FilesAttempted INTEGER NOT NULL DEFAULT 0,
    FilesSucceeded INTEGER NOT NULL DEFAULT 0,
    FilesFailed INTEGER NOT NULL DEFAULT 0,
    BytesRestored INTEGER NOT NULL DEFAULT 0,
    ErrorDetails TEXT,
    Summary TEXT,
    FOREIGN KEY (IncidentId) REFERENCES Incident(IncidentId),
    FOREIGN KEY (RecoveryPointId) REFERENCES RecoveryPoint(RecoveryPointId)
);

CREATE INDEX idx_recovery_operation_incident ON RecoveryOperation(IncidentId);
CREATE INDEX idx_recovery_operation_point ON RecoveryOperation(RecoveryPointId);
CREATE INDEX idx_recovery_operation_status ON RecoveryOperation(Status);
CREATE INDEX idx_recovery_operation_started ON RecoveryOperation(StartedAt);
```

### Threat Report

```csharp
public class ThreatReport
{
    public string ReportId { get; set; }  // UUID v4
    public string ThreatFamilyId { get; set; }
    public string DeviceId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
    public string BehavioralFingerprint { get; set; }
    public List<string> FileHashes { get; set; }  // JSON array
    public List<string> ProcessIndicators { get; set; }  // JSON array
    public List<string> NetworkIndicators { get; set; }  // JSON array
    public string GeographicRegion { get; set; }
    public int DetectionCount { get; set; }
    public int RecoverySuccessCount { get; set; }
    public int RecoveryFailureCount { get; set; }
    public double AverageConfidenceScore { get; set; }
    public string Metadata { get; set; }  // JSON
    public bool IsShared { get; set; }
    public DateTime? SharedAt { get; set; }
}
```

**Table Schema**:
```sql
CREATE TABLE ThreatReport (
    ReportId TEXT PRIMARY KEY,
    ThreatFamilyId TEXT NOT NULL,
    DeviceId TEXT NOT NULL,
    GeneratedAt TEXT NOT NULL,
    FirstSeen TEXT,
    LastSeen TEXT,
    BehavioralFingerprint TEXT NOT NULL,
    FileHashes TEXT,
    ProcessIndicators TEXT,
    NetworkIndicators TEXT,
    GeographicRegion TEXT,
    DetectionCount INTEGER NOT NULL DEFAULT 0,
    RecoverySuccessCount INTEGER NOT NULL DEFAULT 0,
    RecoveryFailureCount INTEGER NOT NULL DEFAULT 0,
    AverageConfidenceScore REAL NOT NULL,
    Metadata TEXT,
    IsShared INTEGER NOT NULL DEFAULT 0,
    SharedAt TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_threat_family ON ThreatReport(ThreatFamilyId);
CREATE INDEX idx_threat_device ON ThreatReport(DeviceId);
CREATE INDEX idx_threat_generated ON ThreatReport(GeneratedAt);
CREATE INDEX idx_threat_region ON ThreatReport(GeographicRegion);
CREATE INDEX idx_threat_shared ON ThreatReport(IsShared);
```

### Threat Family

```csharp
public class ThreatFamily
{
    public string FamilyId { get; set; }  // UUID v4
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public DateTime FirstDetected { get; set; }
    public DateTime LastDetected { get; set; }
    public int TotalDetections { get; set; }
    public List<string> KnownHashes { get; set; }  // JSON array
    public List<string> KnownProcesses { get; set; }  // JSON array
    public string BehavioralSignature { get; set; }
    public ThreatSeverity Severity { get; set; }
    public string MitigationAdvice { get; set; }
    public bool IsActive { get; set; }
}

public enum ThreatSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

**Table Schema**:
```sql
CREATE TABLE ThreatFamily (
    FamilyId TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Category TEXT,
    Description TEXT,
    FirstDetected TEXT NOT NULL,
    LastDetected TEXT NOT NULL,
    TotalDetections INTEGER NOT NULL DEFAULT 0,
    KnownHashes TEXT,
    KnownProcesses TEXT,
    BehavioralSignature TEXT,
    Severity INTEGER NOT NULL,
    MitigationAdvice TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1
);

CREATE INDEX idx_threat_family_name ON ThreatFamily(Name);
CREATE INDEX idx_threat_family_category ON ThreatFamily(Category);
CREATE INDEX idx_threat_family_severity ON ThreatFamily(Severity);
CREATE INDEX idx_threat_family_active ON ThreatFamily(IsActive);
```

### Intelligence Peer

```csharp
public class IntelligencePeer
{
    public string PeerId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public string PeerAddress { get; set; }
    public int Port { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime LastSyncAt { get; set; }
    public PeerStatus Status { get; set; }
    public int SyncCount { get; set; }
    public int ReportsReceived { get; set; }
    public int ReportsShared { get; set; }
    public string Version { get; set; }
    public string Metadata { get; set; }  // JSON
}

public enum PeerStatus
{
    Unknown,
    Online,
    Offline,
    Unreachable
}
```

**Table Schema**:
```sql
CREATE TABLE IntelligencePeer (
    PeerId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    PeerAddress TEXT NOT NULL,
    Port INTEGER NOT NULL,
    FirstSeen TEXT NOT NULL,
    LastSeen TEXT NOT NULL,
    LastSyncAt TEXT,
    Status INTEGER NOT NULL DEFAULT 0,
    SyncCount INTEGER NOT NULL DEFAULT 0,
    ReportsReceived INTEGER NOT NULL DEFAULT 0,
    ReportsShared INTEGER NOT NULL DEFAULT 0,
    Version TEXT,
    Metadata TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_peer_device ON IntelligencePeer(DeviceId);
CREATE INDEX idx_peer_address ON IntelligencePeer(PeerAddress);
CREATE INDEX idx_peer_status ON IntelligencePeer(Status);
CREATE INDEX idx_peer_last_seen ON IntelligencePeer(LastSeen);
```

### Configuration

```csharp
public class Configuration
{
    public string ConfigId { get; set; }  // UUID v4
    public string Section { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public string ValueType { get; set; }  // string, int, bool, json
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
    public string Description { get; set; }
}
```

**Table Schema**:
```sql
CREATE TABLE Configuration (
    ConfigId TEXT PRIMARY KEY,
    Section TEXT NOT NULL,
    Key TEXT NOT NULL,
    Value TEXT NOT NULL,
    ValueType TEXT NOT NULL,
    ModifiedAt TEXT NOT NULL,
    ModifiedBy TEXT,
    Description TEXT,
    UNIQUE(Section, Key)
);

CREATE INDEX idx_config_section ON Configuration(Section);
CREATE INDEX idx_config_modified ON Configuration(ModifiedAt);
```

### Audit Log

```csharp
public class AuditLog
{
    public string LogId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Category { get; set; }
    public string Action { get; set; }
    public string Actor { get; set; }
    public string Target { get; set; }
    public string Details { get; set; }  // JSON
    public string SourceModule { get; set; }
    public string RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; }
    public bool IsSensitive { get; set; }
}
```

**Table Schema**:
```sql
CREATE TABLE AuditLog (
    LogId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    Timestamp TEXT NOT NULL,
    Category TEXT NOT NULL,
    Action TEXT NOT NULL,
    Actor TEXT,
    Target TEXT,
    Details TEXT,
    SourceModule TEXT,
    RelatedEntityId TEXT,
    RelatedEntityType TEXT,
    IsSensitive INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_audit_device ON AuditLog(DeviceId);
CREATE INDEX idx_audit_timestamp ON AuditLog(Timestamp);
CREATE INDEX idx_audit_category ON AuditLog(Category);
CREATE INDEX idx_audit_action ON AuditLog(Action);
CREATE INDEX idx_audit_entity ON AuditLog(RelatedEntityId, RelatedEntityType);
```

### System Health

```csharp
public class SystemHealth
{
    public string HealthId { get; set; }  // UUID v4
    public string DeviceId { get; set; }
    public string Module { get; set; }
    public HealthStatus Status { get; set; }
    public DateTime RecordedAt { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object> Metrics { get; set; }  // JSON
    public string Details { get; set; }  // JSON
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}
```

**Table Schema**:
```sql
CREATE TABLE SystemHealth (
    HealthId TEXT PRIMARY KEY,
    DeviceId TEXT NOT NULL,
    Module TEXT NOT NULL,
    Status INTEGER NOT NULL,
    RecordedAt TEXT NOT NULL,
    Message TEXT,
    Metrics TEXT,
    Details TEXT,
    FOREIGN KEY (DeviceId) REFERENCES DeviceIdentity(DeviceId)
);

CREATE INDEX idx_health_device ON SystemHealth(DeviceId);
CREATE INDEX idx_health_module ON SystemHealth(Module);
CREATE INDEX idx_health_status ON SystemHealth(Status);
CREATE INDEX idx_health_recorded ON SystemHealth(RecordedAt);
```

## Database Initialization

The database is initialized with the following sequence:

1. Create all tables
2. Create all indexes
3. Insert default configuration
4. Generate device identity (if not exists)
5. Create initial recovery point
6. Deploy initial decoy assets

## Data Retention Policy

| Entity | Retention Period | Cleanup Method |
|--------|------------------|----------------|
| FileEvent | 90 days | Automatic deletion |
| ProcessInfo | 30 days after process end | Automatic deletion |
| DecoyInteraction | 180 days | Automatic deletion |
| RecoveryOperation | 365 days | Automatic deletion |
| AuditLog | 365 days | Archive then delete |
| SystemHealth | 30 days | Automatic deletion |
| ThreatReport | Permanent | Keep for intelligence |
| Incident | Permanent | Keep for compliance |

## Performance Considerations

### Indexing Strategy

- All foreign keys have indexes
- Frequently queried timestamp fields have indexes
- Status and type enums have indexes
- Composite indexes for common query patterns

### Partitioning

For large deployments, consider:
- Partition FileEvent by month
- Partition AuditLog by month
- Partition SystemHealth by week

### Archiving

Old data can be archived to:
- Compressed SQLite archives
- Separate cold storage database
- Export to external SIEM

## Security Considerations

### Encryption at Rest

- Sensitive fields (RegistrationKey) are encrypted using AES-256
- Encryption keys are stored in Windows DPAPI
- Database file itself can be encrypted using SQLite encryption extension

### Data Anonymization

- Threat reports never contain personal data
- File paths are hashed before sharing
- Process names are sanitized before sharing
- Geographic data is country-level only

### Access Control

- Database file permissions: Service account only
- Audit logs track all data access
- Sensitive operations require elevated privileges
