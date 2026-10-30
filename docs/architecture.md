# EchoEye X System Architecture

## Overview

EchoEye X is a modular ransomware resilience platform designed for Windows environments. The architecture follows a microservices-inspired pattern with clear separation of concerns, enabling independent deployment, scaling, and maintenance of each component.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Management Layer                          │
│  (Configuration, Monitoring, Health, Alerting, Audit)          │
└─────────────────────────────────────────────────────────────────┘
                                  │
        ┌─────────────────────────┼─────────────────────────┐
        │                         │                         │
┌───────▼────────┐    ┌──────────▼──────────┐    ┌────────▼────────┐
│ Protection     │    │ Decoy Engine         │    │ Recovery        │
│ Service        │    │                      │    │ Engine          │
│                │    │                      │    │                 │
│ - File Monitor │    │ - Asset Deployment   │    │ - Backup Mgmt   │
│ - Behavior     │    │ - Interaction Track  │    │ - Integrity     │
│   Analysis     │    │ - Indicator Gen      │    │   Validation    │
│ - Detection    │    │                      │    │ - Restore       │
│ - Containment  │    │                      │    │   Workflows     │
└───────┬────────┘    └──────────┬──────────┘    └────────┬────────┘
        │                         │                         │
        └─────────────────────────┼─────────────────────────┘
                                  │
                    ┌─────────────▼─────────────┐
                    │   Shared Data Layer       │
                    │                           │
                    │ - SQLite Database         │
                    │ - Event Log              │
                    │ - Forensic Store          │
                    │ - Recovery Points         │
                    └─────────────┬─────────────┘
                                  │
                    ┌─────────────▼─────────────┐
                    │   Intelligence Node       │
                    │                           │
                    │ - Threat Collection       │
                    │ - Anonymization           │
                    │ - Peer Communication      │
                    │ - Intelligence Feeds      │
                    └───────────────────────────┘
```

## Module Descriptions

### 1. Installer Module

**Purpose**: System preparation and initial deployment

**Responsibilities**:
- Validate system prerequisites (Windows version, disk space, permissions)
- Create application directory structure
- Generate unique device identity
- Configure Windows Service registration
- Deploy initial decoy assets
- Set up database schema
- Configure logging infrastructure
- Generate installation report
- Register startup requirements

**Key Components**:
- `PrerequisiteValidator` - System capability checks
- `DirectoryManager` - Directory structure creation
- `IdentityGenerator` - Device fingerprint generation
- `ServiceInstaller` - Windows Service registration
- `DecoyDeployer` - Initial honeypot deployment
- `DatabaseInitializer` - Schema creation
- `InstallationReporter` - Installation audit

**Installation Flow**:
```
1. Validate Prerequisites
   ↓
2. Generate Device Identity
   ↓
3. Create Directory Structure
   ↓
4. Initialize Database
   ↓
5. Deploy Decoy Assets
   ↓
6. Register Windows Services
   ↓
7. Configure Logging
   ↓
8. Generate Installation Report
```

### 2. Protection Service

**Purpose**: Continuous monitoring and ransomware detection

**Responsibilities**:
- Monitor file system activity using FileSystemWatcher
- Track process behavior and reputation
- Detect mass file modification patterns
- Analyze encryption signature patterns
- Correlate events across multiple signals
- Generate confidence scores
- Trigger containment workflows
- Produce forensic logs
- Manage alert lifecycle

**Key Components**:
- `FileSystemMonitor` - Real-time file change tracking
- `ProcessTracker` - Process behavior monitoring
- `BehaviorAnalyzer` - Pattern recognition engine
- `ConfidenceScorer` - Multi-signal confidence calculation
- `ContainmentManager` - Threat isolation workflows
- `AlertGenerator` - Security alert creation
- `ForensicLogger` - Evidence preservation
- `DetectionEngine` - Core detection orchestration

**Detection Signals**:
1. **File Modification Velocity** - Rate of file changes per second
2. **Entropy Analysis** - Detection of encryption patterns
3. **Extension Changes** - Suspicious file renaming
4. **Decoy Interactions** - Access to honeypot files
5. **Process Reputation** - Known malicious/suspicious processes
6. **Behavioral Anomalies** - Deviation from baseline
7. **Network Activity** - Command & control communication

**Confidence Scoring Model**:
```
Confidence = w1 * FileVelocity + w2 * Entropy + w3 * DecoyHits + 
             w4 * ProcessReputation + w5 * BehaviorAnomaly + 
             w6 * NetworkActivity

Where weights sum to 1.0 and are configurable.

Score Ranges:
- 0-25: Low (informational)
- 26-50: Medium (investigate)
- 51-75: High (contain)
- 76-100: Critical (immediate action)
```

### 3. Decoy Engine

**Purpose**: Deploy and monitor honeypot assets for early detection

**Responsibilities**:
- Generate realistic decoy files (documents, images, databases)
- Deploy decoys in strategic locations
- Monitor decoy access and modification
- Generate high-confidence ransomware indicators
- Associate decoy events with incident timelines
- Manage decoy lifecycle (creation, rotation, deletion)

**Key Components**:
- `DecoyGenerator` - Realistic file creation
- `DecoyDeployer` - Strategic placement
- `DecoyMonitor` - Access tracking
- `IndicatorGenerator` - Threat signal creation
- `DecoyLifecycleManager` - Asset rotation

**Decoy Types**:
- **Document Decoys**: .docx, .xlsx, .pdf with realistic content
- **Image Decoys**: .jpg, .png with embedded metadata
- **Database Decoys**: .db, .sqlite with structured data
- **Archive Decoys**: .zip, .rar with multiple files
- **Config Decoys**: .json, .xml, .ini with realistic settings

**Deployment Strategy**:
```
User Profile/Documents/
  ├── Financial_Records/
  │   ├── tax_returns_2023.docx (decoy)
  │   └── bank_statements.xlsx (decoy)
  ├── Personal_Photos/
  │   ├── vacation_summer.jpg (decoy)
  │   └── family_portrait.png (decoy)
  └── Work_Projects/
      ├── project_proposal.pdf (decoy)
      └── client_database.db (decoy)

Desktop/
  └── important_files.zip (decoy)
```

### 4. Recovery Engine

**Purpose**: Manage backup creation and restoration workflows

**Responsibilities**:
- Create incremental recovery points
- Validate backup integrity
- Manage storage allocation
- Support selective and full restoration
- Generate recovery reports
- Calculate recovery success metrics
- Estimate data loss prevention

**Key Components**:
- `RecoveryPointManager` - Backup creation and lifecycle
- `IntegrityValidator` - Backup verification
- `StorageManager` - Disk space management
- `RestorationEngine` - File recovery workflows
- `RecoveryReporter` - Metrics and reporting
- `RetentionPolicy` - Backup lifecycle management

**Recovery Point Structure**:
```
Recovery Point
├── Metadata
│   ├── Timestamp
│   ├── File Count
│   ├── Total Size
│   ├── Checksum
│   └── Source Paths
├── File Index
│   ├── Original Path
│   ├── Backup Path
│   ├── Size
│   ├── Modified Time
│   └── Hash
└── Data Blocks
    ├── Encrypted Blocks
    └── Compression Metadata
```

**Retention Policy**:
- **Hourly**: Keep last 24 hours
- **Daily**: Keep last 30 days
- **Weekly**: Keep last 12 weeks
- **Monthly**: Keep last 12 months
- **Automatic cleanup**: When storage exceeds 80% capacity

### 5. Intelligence Node

**Purpose**: Collect, anonymize, and share threat intelligence

**Responsibilities**:
- Collect anonymized threat indicators
- Store threat reports locally
- Maintain peer information
- Generate aggregated intelligence feeds
- Share privacy-preserving data
- Receive community intelligence
- Maintain threat family database

**Key Components**:
- `ThreatCollector` - Indicator gathering
- `Anonymizer` - Privacy-preserving data transformation
- `PeerManager` - Peer discovery and communication
- `IntelligenceAggregator` - Feed generation
- `ThreatDatabase` - Threat family storage
- `CommunicationChannel` - Secure peer exchange

**Data Collection Policy** (Privacy-First):
- **Collected**:
  - File hashes (SHA-256) of malicious files
  - Process names and paths
  - Behavioral fingerprints
  - Timestamp patterns
  - Geographic region (country-level only)
  - Threat family identifiers
  
- **Never Collected**:
  - User documents or file contents
  - Personal photos or images
  - Passwords or credentials
  - Personal identification information
  - File names from user directories
  - Network traffic payloads

**Threat Intelligence Model**:
```
Threat Report
├── Threat Family ID
├── Behavioral Fingerprint
├── File Hashes (SHA-256)
├── Process Indicators
├── Timeline Pattern
├── Geographic Region
├── Detection Count
└── Recovery Statistics
```

### 6. Management Layer

**Purpose**: Configuration, monitoring, and administrative control

**Responsibilities**:
- Configuration management
- Service health monitoring
- Alert management and routing
- Audit trail maintenance
- Status reporting
- User authentication
- Role-based access control
- Dashboard data aggregation

**Key Components**:
- `ConfigurationManager` - Settings persistence
- `HealthMonitor` - Service status tracking
- `AlertManager` - Alert lifecycle
- `AuditLogger` - Compliance logging
- `StatusReporter` - Health aggregation
- `AuthenticationProvider` - Identity management
- `DashboardDataProvider` - UI data serving

**Configuration Structure**:
```json
{
  "protection": {
    "enabled": true,
    "fileMonitoring": {
      "watchedPaths": ["C:\\Users", "D:\\Data"],
      "excludedPaths": ["C:\\Windows", "C:\\Program Files"]
    },
    "detection": {
      "confidenceThreshold": 75,
      "fileVelocityThreshold": 100,
      "entropyThreshold": 7.5
    }
  },
  "decoy": {
    "enabled": true,
    "decoyCount": 50,
    "deploymentPaths": ["Documents", "Desktop"]
  },
  "recovery": {
    "enabled": true,
    "backupPath": "C:\\ProgramData\\EchoEye\\Recovery",
    "retentionDays": 90,
    "maxStorageGB": 100
  },
  "intelligence": {
    "enabled": true,
    "sharingEnabled": true,
    "peerDiscovery": true
  }
}
```

## Data Flow

### Incident Detection Flow
```
1. File System Event
   ↓
2. Process Tracker Correlation
   ↓
3. Behavior Analyzer
   ↓
4. Decoy Monitor Check
   ↓
5. Confidence Scorer
   ↓
6. Threshold Evaluation
   ↓
7. Alert Generation
   ↓
8. Containment Trigger
   ↓
9. Forensic Evidence Collection
   ↓
10. Intelligence Artifact Generation
```

### Recovery Flow
```
1. Incident Containment
   ↓
2. Impact Assessment
   ↓
3. Recovery Point Selection
   ↓
4. Integrity Validation
   ↓
5. Restoration Execution
   ↓
6. Verification
   ↓
7. Recovery Report Generation
```

## Inter-Module Communication

### Service Interfaces

All modules communicate through well-defined interfaces:

```csharp
// Protection Service → Decoy Engine
IDecoyMonitor
- CheckDecoyInteraction(filePath)
- GetDecoyStatus()
- GenerateIndicator()

// Protection Service → Recovery Engine
IRecoveryManager
- CreateRecoveryPoint()
- RestoreFile(filePath, timestamp)
- ValidateIntegrity()

// Protection Service → Intelligence Node
IThreatCollector
- CollectIndicator(indicator)
- GetThreatFeed()
- SubmitThreatReport()

// All Modules → Management Layer
IConfigurationProvider
- GetConfiguration(section)
- UpdateConfiguration(section, value)

IHealthReporter
- ReportHealth(status)
- GetModuleHealth(module)

IAuditLogger
- LogEvent(event)
- GetAuditTrail(filter)
```

## Security Architecture

### Defense in Depth

1. **Application Level**
   - Code signing verification
   - Secure configuration defaults
   - Input validation and sanitization

2. **Service Level**
   - Windows Service with least privileges
   - Secure inter-process communication
   - Encrypted local storage

3. **Data Level**
   - Encryption at rest (AES-256)
   - Secure key management
   - Data anonymization

4. **Network Level**
   - TLS 1.3 for peer communication
   - Certificate validation
   - Rate limiting

### Secure-by-Default Configuration

- All sensitive data encrypted at rest
- No plaintext credentials in configuration
- Automatic security updates
- Audit logging enabled by default
- Principle of least privilege for service accounts
- Secure default passwords (auto-generated)

## Scalability Considerations

### Horizontal Scaling

- **Protection Service**: Can run multiple instances monitoring different paths
- **Intelligence Node**: Distributed peer-to-peer architecture
- **Dashboard**: Stateless design supports multiple instances

### Vertical Scaling

- Configurable resource limits per module
- Adaptive sampling based on system load
- Background task prioritization

### Performance Optimization

- Async/await patterns throughout
- Connection pooling for database
- Batch processing for bulk operations
- Lazy loading for large datasets
- Memory-mapped files for large recovery points

## Fault Tolerance

### Failure Modes

1. **Service Crash**
   - Automatic restart by Windows Service Controller
   - State recovery from database
   - In-memory queue persistence

2. **Database Corruption**
   - Automatic backup and recovery
   - Schema validation on startup
   - Graceful degradation mode

3. **Disk Full**
   - Automatic cleanup of old data
   - Alert generation before critical
   - Pause non-critical operations

4. **Network Failure**
   - Offline mode for intelligence
   - Local operation continues
   - Queue data for later sync

### Recovery Procedures

Each module implements:
- Health check endpoints
- Graceful shutdown procedures
- State checkpointing
- Automatic retry logic
- Circuit breakers for external dependencies

## Monitoring and Observability

### Metrics Collected

- **Protection**: Files monitored, detections per hour, confidence scores
- **Decoy**: Decoys deployed, interactions detected, indicators generated
- **Recovery**: Recovery points created, storage used, restore success rate
- **Intelligence**: Threats collected, peers connected, feeds generated
- **System**: CPU, memory, disk usage, service uptime

### Logging Strategy

- **Structured Logging**: JSON format with consistent schema
- **Log Levels**: Debug, Info, Warning, Error, Critical
- **Log Rotation**: Automatic cleanup based on age and size
- **Log Retention**: 90 days default, configurable
- **Sensitive Data**: Automatically redacted from logs

### Health Checks

- **Liveness**: Service is running
- **Readiness**: Service can accept requests
- **Dependencies**: Database, disk space, network connectivity
- **Module-Specific**: Decoy status, recovery point health, peer connectivity

## Deployment Architecture

### Development Environment
- Local SQLite database
- Single service instance
- Debug logging enabled
- Mock intelligence node

### Production Environment
- Encrypted SQLite database
- Multiple service instances (if needed)
- Info-level logging
- Full intelligence node with peer discovery
- Windows Service with automatic startup

### Enterprise Deployment
- Centralized management console
- Distributed intelligence network
- Custom retention policies
- Integration with SIEM
- Custom alert routing
- Role-based access control

## Technology Stack

### Core Framework
- **.NET 8.0** - Latest LTS with performance improvements
- **C# 12** - Modern language features

### Data Storage
- **SQLite** - Embedded, zero-configuration database
- **Dapper** - Micro-ORM for data access

### Windows Integration
- **Windows Service API** - Background service hosting
- **FileSystemWatcher** - Real-time file monitoring
- **WMI** - System information gathering

### Security
- **AES-256** - Encryption at rest
- **SHA-256** - File hashing
- **HMAC** - Message authentication

### Web Dashboard
- **ASP.NET Core** - Web framework
- **SignalR** - Real-time updates
- **Blazor** - Modern UI framework

### Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library

## Compliance Considerations

### Data Privacy
- GDPR-compliant data minimization
- No personal data collection
- Right to deletion (local data only)
- Transparent data processing

### Security Standards
- Secure-by-default principles
- Defense in depth architecture
- Regular security updates
- Vulnerability disclosure process

### Audit Trail
- All configuration changes logged
- All security events recorded
- Immutable audit logs
- Tamper-evident storage
