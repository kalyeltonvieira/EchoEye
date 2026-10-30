using EchoEye.Core.Constants;
using EchoEye.Core.Data;
using EchoEye.Core.Models;
using Microsoft.Extensions.Logging;

namespace EchoEye.Installer.Services;

/// <summary>
/// Initializes the database schema
/// </summary>
public class DatabaseInitializer
{
    private readonly ILogger _logger;

    public DatabaseInitializer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initialize database with schema and initial data
    /// </summary>
    public async Task InitializeAsync(DeviceIdentity identity)
    {
        using var database = new Database(Paths.DatabasePath);

        _logger.LogInformation("Initializing database at: {Path}", Paths.DatabasePath);

        // Create schema
        var schema = GetSchemaSql();
        database.InitializeSchema(schema);

        _logger.LogInformation("Database schema created");

        // Insert device identity
        await InsertDeviceIdentityAsync(database, identity);

        _logger.LogInformation("Device identity inserted into database");

        // Enable WAL mode for better performance
        database.ExecuteNonQuery("PRAGMA journal_mode=WAL");
        database.ExecuteNonQuery("PRAGMA synchronous=NORMAL");

        _logger.LogInformation("Database optimizations applied");
    }

    /// <summary>
    /// Insert device identity into database
    /// </summary>
    private async Task InsertDeviceIdentityAsync(Database database, DeviceIdentity identity)
    {
        var sql = @"
            INSERT INTO DeviceIdentity (
                DeviceId, MachineName, OsVersion, Fingerprint,
                FirstRegistered, LastSeen, RegistrationKey
            ) VALUES (
                @DeviceId, @MachineName, @OsVersion, @Fingerprint,
                @FirstRegistered, @LastSeen, @RegistrationKey
            )";

        var parameters = new[]
        {
            new SqliteParameter("@DeviceId", identity.DeviceId),
            new SqliteParameter("@MachineName", identity.MachineName),
            new SqliteParameter("@OsVersion", identity.OsVersion),
            new SqliteParameter("@Fingerprint", identity.Fingerprint),
            new SqliteParameter("@FirstRegistered", identity.FirstRegistered.ToString("o")),
            new SqliteParameter("@LastSeen", identity.LastSeen.ToString("o")),
            new SqliteParameter("@RegistrationKey", identity.RegistrationKey)
        };

        database.ExecuteNonQuery(sql, parameters);
    }

    /// <summary>
    /// Get complete database schema SQL
    /// </summary>
    private string GetSchemaSql()
    {
        return @"
-- Device Identity
CREATE TABLE IF NOT EXISTS DeviceIdentity (
    DeviceId TEXT PRIMARY KEY,
    MachineName TEXT NOT NULL,
    OsVersion TEXT NOT NULL,
    Fingerprint TEXT NOT NULL UNIQUE,
    FirstRegistered TEXT NOT NULL,
    LastSeen TEXT NOT NULL,
    RegistrationKey BLOB NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_device_fingerprint ON DeviceIdentity(Fingerprint);
CREATE INDEX IF NOT EXISTS idx_device_last_seen ON DeviceIdentity(LastSeen);

-- Incident
CREATE TABLE IF NOT EXISTS Incident (
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

CREATE INDEX IF NOT EXISTS idx_incident_device ON Incident(DeviceId);
CREATE INDEX IF NOT EXISTS idx_incident_status ON Incident(Status);
CREATE INDEX IF NOT EXISTS idx_incident_confidence ON Incident(Confidence);
CREATE INDEX IF NOT EXISTS idx_incident_detected ON Incident(DetectedAt);
CREATE INDEX IF NOT EXISTS idx_incident_threat ON Incident(ThreatFamilyId);

-- Alert
CREATE TABLE IF NOT EXISTS Alert (
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

CREATE INDEX IF NOT EXISTS idx_alert_incident ON Alert(IncidentId);
CREATE INDEX IF NOT EXISTS idx_alert_severity ON Alert(Severity);
CREATE INDEX IF NOT EXISTS idx_alert_type ON Alert(Type);
CREATE INDEX IF NOT EXISTS idx_alert_generated ON Alert(GeneratedAt);
CREATE INDEX IF NOT EXISTS idx_alert_resolved ON Alert(IsResolved);

-- File Event
CREATE TABLE IF NOT EXISTS FileEvent (
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

CREATE INDEX IF NOT EXISTS idx_file_event_incident ON FileEvent(IncidentId);
CREATE INDEX IF NOT EXISTS idx_file_event_path ON FileEvent(FilePath);
CREATE INDEX IF NOT EXISTS idx_file_event_occurred ON FileEvent(OccurredAt);
CREATE INDEX IF NOT EXISTS idx_file_event_process ON FileEvent(ProcessId);
CREATE INDEX IF NOT EXISTS idx_file_event_decoy ON FileEvent(IsDecoy);

-- Process Info
CREATE TABLE IF NOT EXISTS ProcessInfo (
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

CREATE INDEX IF NOT EXISTS idx_process_device ON ProcessInfo(DeviceId);
CREATE INDEX IF NOT EXISTS idx_process_name ON ProcessInfo(ProcessName);
CREATE INDEX IF NOT EXISTS idx_process_reputation ON ProcessInfo(Reputation);
CREATE INDEX IF NOT EXISTS idx_process_started ON ProcessInfo(StartedAt);

-- Decoy Asset
CREATE TABLE IF NOT EXISTS DecoyAsset (
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

CREATE INDEX IF NOT EXISTS idx_decoy_device ON DecoyAsset(DeviceId);
CREATE INDEX IF NOT EXISTS idx_decoy_type ON DecoyAsset(Type);
CREATE INDEX IF NOT EXISTS idx_decoy_active ON DecoyAsset(IsActive);
CREATE INDEX IF NOT EXISTS idx_decoy_accessed ON DecoyAsset(LastAccessedAt);

-- Decoy Interaction
CREATE TABLE IF NOT EXISTS DecoyInteraction (
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

CREATE INDEX IF NOT EXISTS idx_decoy_interaction_decoy ON DecoyInteraction(DecoyId);
CREATE INDEX IF NOT EXISTS idx_decoy_interaction_incident ON DecoyInteraction(IncidentId);
CREATE INDEX IF NOT EXISTS idx_decoy_interaction_occurred ON DecoyInteraction(OccurredAt);
CREATE INDEX IF NOT EXISTS idx_decoy_interaction_type ON DecoyInteraction(Type);

-- Recovery Point
CREATE TABLE IF NOT EXISTS RecoveryPoint (
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

CREATE INDEX IF NOT EXISTS idx_recovery_device ON RecoveryPoint(DeviceId);
CREATE INDEX IF NOT EXISTS idx_recovery_type ON RecoveryPoint(Type);
CREATE INDEX IF NOT EXISTS idx_recovery_created ON RecoveryPoint(CreatedAt);
CREATE INDEX IF NOT EXISTS idx_recovery_valid ON RecoveryPoint(IsValid);
CREATE INDEX IF NOT EXISTS idx_recovery_incident ON RecoveryPoint(RelatedIncidentId);

-- Recovery Point File
CREATE TABLE IF NOT EXISTS RecoveryPointFile (
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

CREATE INDEX IF NOT EXISTS idx_recovery_file_point ON RecoveryPointFile(RecoveryPointId);
CREATE INDEX IF NOT EXISTS idx_recovery_file_path ON RecoveryPointFile(OriginalPath);
CREATE INDEX IF NOT EXISTS idx_recovery_file_hash ON RecoveryPointFile(Hash);

-- Recovery Operation
CREATE TABLE IF NOT EXISTS RecoveryOperation (
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

CREATE INDEX IF NOT EXISTS idx_recovery_operation_incident ON RecoveryOperation(IncidentId);
CREATE INDEX IF NOT EXISTS idx_recovery_operation_point ON RecoveryOperation(RecoveryPointId);
CREATE INDEX IF NOT EXISTS idx_recovery_operation_status ON RecoveryOperation(Status);
CREATE INDEX IF NOT EXISTS idx_recovery_operation_started ON RecoveryOperation(StartedAt);

-- Threat Report
CREATE TABLE IF NOT EXISTS ThreatReport (
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

CREATE INDEX IF NOT EXISTS idx_threat_family ON ThreatReport(ThreatFamilyId);
CREATE INDEX IF NOT EXISTS idx_threat_device ON ThreatReport(DeviceId);
CREATE INDEX IF NOT EXISTS idx_threat_generated ON ThreatReport(GeneratedAt);
CREATE INDEX IF NOT EXISTS idx_threat_region ON ThreatReport(GeographicRegion);
CREATE INDEX IF NOT EXISTS idx_threat_shared ON ThreatReport(IsShared);

-- Threat Indicator
CREATE TABLE IF NOT EXISTS ThreatIndicator (
    IndicatorId TEXT PRIMARY KEY,
    Type TEXT NOT NULL,
    Value TEXT NOT NULL,
    Confidence REAL NOT NULL,
    GeneratedAt TEXT NOT NULL,
    Source TEXT NOT NULL,
    Metadata TEXT
);

CREATE INDEX IF NOT EXISTS idx_threat_indicator_type ON ThreatIndicator(Type);
CREATE INDEX IF NOT EXISTS idx_threat_indicator_generated ON ThreatIndicator(GeneratedAt);
CREATE INDEX IF NOT EXISTS idx_threat_indicator_confidence ON ThreatIndicator(Confidence);

-- Threat Family
CREATE TABLE IF NOT EXISTS ThreatFamily (
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

CREATE INDEX IF NOT EXISTS idx_threat_family_name ON ThreatFamily(Name);
CREATE INDEX IF NOT EXISTS idx_threat_family_category ON ThreatFamily(Category);
CREATE INDEX IF NOT EXISTS idx_threat_family_severity ON ThreatFamily(Severity);
CREATE INDEX IF NOT EXISTS idx_threat_family_active ON ThreatFamily(IsActive);

-- Intelligence Peer
CREATE TABLE IF NOT EXISTS IntelligencePeer (
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

CREATE INDEX IF NOT EXISTS idx_peer_device ON IntelligencePeer(DeviceId);
CREATE INDEX IF NOT EXISTS idx_peer_address ON IntelligencePeer(PeerAddress);
CREATE INDEX IF NOT EXISTS idx_peer_status ON IntelligencePeer(Status);
CREATE INDEX IF NOT EXISTS idx_peer_last_seen ON IntelligencePeer(LastSeen);

-- Configuration
CREATE TABLE IF NOT EXISTS Configuration (
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

CREATE INDEX IF NOT EXISTS idx_config_section ON Configuration(Section);
CREATE INDEX IF NOT EXISTS idx_config_modified ON Configuration(ModifiedAt);

-- Audit Log
CREATE TABLE IF NOT EXISTS AuditLog (
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

CREATE INDEX IF NOT EXISTS idx_audit_device ON AuditLog(DeviceId);
CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON AuditLog(Timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_category ON AuditLog(Category);
CREATE INDEX IF NOT EXISTS idx_audit_action ON AuditLog(Action);
CREATE INDEX IF NOT EXISTS idx_audit_entity ON AuditLog(RelatedEntityId, RelatedEntityType);

-- System Health
CREATE TABLE IF NOT EXISTS SystemHealth (
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

CREATE INDEX IF NOT EXISTS idx_health_device ON SystemHealth(DeviceId);
CREATE INDEX IF NOT EXISTS idx_health_module ON SystemHealth(Module);
CREATE INDEX IF NOT EXISTS idx_health_status ON SystemHealth(Status);
CREATE INDEX IF NOT EXISTS idx_health_recorded ON SystemHealth(RecordedAt);
";
    }
}
