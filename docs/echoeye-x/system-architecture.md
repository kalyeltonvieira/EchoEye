# System Architecture

## Summary

EchoEye X is composed of independent Windows endpoint modules plus an optional
management and intelligence backend. The endpoint is designed to continue
detecting, logging, containing, and recovering when disconnected from the
backend.

## Core Modules

### Installer

Responsibilities:

- Create application, data, log, quarantine, recovery, and decoy directories.
- Generate a stable device identity using a local key pair and non-sensitive
  hardware/environment claims.
- Install and configure Windows services.
- Register startup, recovery, and service restart policies.
- Deploy decoy assets and seed initial protection policies.
- Validate prerequisites such as service account rights, filesystem access,
  Windows Event Log availability, VSS availability, and free disk space.
- Produce a signed installation report.

### Protection Service

Responsibilities:

- Run as a Windows service under a least-privilege service account.
- Monitor file activity, process activity, decoy interactions, and system
  telemetry.
- Detect suspicious mass file modifications using a confidence model rather
  than single-signal matching.
- Correlate evidence across processes, filesystem paths, users, devices, and
  time windows.
- Create alerts and incidents.
- Trigger containment workflows.
- Produce tamper-resistant forensic logs.

### Decoy Engine

Responsibilities:

- Deploy realistic, non-sensitive decoy documents in protected locations.
- Track read, rename, overwrite, delete, and entropy-shift activity on decoys.
- Emit high-confidence indicators when decoys are touched by suspicious
  processes.
- Attach decoy evidence to incident timelines.

### Recovery Engine

Responsibilities:

- Manage recovery points for protected scopes.
- Validate backup integrity with manifests, hashes, and restore drills.
- Support partial and full restoration workflows.
- Generate recovery reports with estimated impacted files, recovered files,
  skipped files, failures, and confidence.
- Keep recovery metadata independent of incident metadata while allowing links
  between the two.

### Intelligence Node

Responsibilities:

- Collect anonymized indicators from local incidents.
- Store threat reports, behavioral fingerprints, campaign observations, and
  aggregate statistics.
- Maintain peer metadata without collecting personal content.
- Generate privacy-preserving intelligence feeds.
- Share only hashes, normalized behavioral patterns, infrastructure indicators,
  and aggregate metrics.

### Management Layer

Responsibilities:

- Manage configuration, policy versions, and rollout status.
- Monitor endpoint status and service health.
- Manage alerts, incidents, audit trails, and recovery workflows.
- Render the dashboard and expose administrative APIs.

## Deployment Topology

EchoEye X supports three deployment models:

1. Standalone endpoint
   - Local service, local database, local reports.
   - No external intelligence sharing.

2. Managed enterprise
   - Endpoint services connect to a central management server.
   - Centralized policies, dashboards, incidents, health, and audit trails.

3. Federated intelligence
   - Multiple organizations publish anonymized intelligence to a federation
     service or private sharing group.
   - Raw files and personal content never leave endpoints.

## Endpoint Runtime Layout

Recommended paths:

- `%ProgramFiles%\EchoEye X\`
  - Signed binaries and service manifests.
- `%ProgramData%\EchoEyeX\config\`
  - Machine-scoped configuration and policy cache.
- `%ProgramData%\EchoEyeX\data\`
  - Local database and queue state.
- `%ProgramData%\EchoEyeX\logs\`
  - Operational and forensic logs.
- `%ProgramData%\EchoEyeX\decoys\`
  - Managed decoy metadata.
- `%ProgramData%\EchoEyeX\recovery\`
  - Recovery catalogs and integrity manifests.

## Process Model

- `EchoEye.ProtectionService`
  - Main background detection, correlation, alerting, and containment service.
- `EchoEye.DecoyWorker`
  - Decoy deployment, rotation, and telemetry enrichment.
- `EchoEye.RecoveryWorker`
  - Recovery point scheduling, integrity validation, and restore execution.
- `EchoEye.IntelligenceWorker`
  - Local anonymization, feed generation, and backend sync.
- `EchoEye.ManagementApi`
  - Local or central REST/gRPC API for dashboard and automation.

Workers may run as separate Windows services in high-assurance deployments or
as isolated worker processes supervised by the protection service in smaller
deployments.

## Trust Boundaries

- Endpoint trust boundary:
  - Local services, local storage, Windows telemetry, and service identity.
- Management trust boundary:
  - Admin APIs, dashboard, central storage, and policy distribution.
- Intelligence trust boundary:
  - Anonymized indicators and aggregate statistics only.
- Recovery trust boundary:
  - Recovery data must be protected from modification by ordinary users and
    monitored processes.

## Confidence Model

Detection combines weighted evidence:

- File modification velocity.
- Entropy and extension changes.
- Rename/delete bursts.
- Decoy interaction severity.
- Process reputation and signer status.
- Parent/child process ancestry.
- Historical baseline deviation.
- Protected path impact.
- Recovery-impact estimate.

Confidence levels:

- Low: suspicious but insufficient for containment.
- Medium: correlated anomaly that warrants alerting and elevated logging.
- High: strong ransomware-like activity or decoy interaction with corroboration.
- Critical: active destructive behavior, high data impact, or high-confidence
  decoy event with mass modification.

## Reliability Guarantees

- Endpoint services use durable local queues for alerts, incidents, telemetry,
  and intelligence sync.
- Backend outages do not disable local detection or recovery.
- Failed containment actions are logged and retried where safe.
- Recovery workflows perform preflight validation before modifying restored
  data.
- Logs are append-only at the application layer and chained with hash links.

