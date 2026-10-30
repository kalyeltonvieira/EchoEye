# Service Contracts

## Event Envelope

All internal services exchange events using a common envelope:

```json
{
  "event_id": "uuid",
  "event_type": "string",
  "schema_version": 1,
  "producer": "EchoEye.ProtectionService",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "occurred_at": "timestamp",
  "correlation_id": "uuid|null",
  "payload": {}
}
```

## Installer Contract

### Inputs

- Tenant enrollment token or standalone mode.
- Installation directory.
- Protected path policy.
- Decoy policy.
- Recovery policy.
- Optional backend URL.

### Outputs

- Device identity key pair.
- Installed service manifests.
- Initial policy cache.
- Installation report.
- Decoy deployment report.

### Failure Behavior

- Installation is transactional where practical.
- Partially installed services are stopped and removed during rollback.
- Device identity is generated only after prerequisite validation.
- Installation report records all rollback actions.

## Protection Service Contract

### Consumes

- Filesystem events.
- Process events.
- Decoy events.
- Policy updates.
- Recovery point metadata.
- Local reputation cache.

### Produces

- Evidence events.
- Alerts.
- Incidents.
- Containment jobs.
- Forensic log entries.
- Health metrics.
- Intelligence artifacts.

### Guarantees

- Does not require backend connectivity for local detection.
- Deduplicates events within bounded time windows.
- Persists incident state before triggering containment.
- Records failed containment attempts with reason codes.

## Decoy Engine Contract

### Consumes

- Decoy policy.
- Protected scope inventory.
- File activity events for decoy paths.

### Produces

- Decoy deployment records.
- Decoy touch events.
- Decoy rotation events.
- Decoy integrity findings.

### Guarantees

- Decoys contain no personal content.
- Decoy paths are tracked in metadata and can be hashed before export.
- Decoys are rotated without invalidating historical incident evidence.

## Recovery Engine Contract

### Consumes

- Recovery policy.
- Protected scope inventory.
- Incident impact estimates.
- Restore job requests.

### Produces

- Recovery point records.
- Recovery manifests.
- Integrity validation events.
- Restore job status events.
- Recovery reports.

### Guarantees

- Restore jobs perform preflight checks before writing files.
- Restore reports distinguish restored, skipped, failed, and manually excluded
  files.
- Recovery manifests are hash-validated.
- Restore approvals are audit logged.

## Intelligence Node Contract

### Consumes

- Incidents.
- Evidence summaries.
- Recovery reports.
- Local reputation observations.
- Peer feed updates.

### Produces

- Threat reports.
- Behavioral fingerprints.
- Aggregated feeds.
- Peer health records.
- Detection and recovery statistics.

### Guarantees

- No user content is collected.
- Full paths, hostnames, usernames, and document names are redacted or hashed.
- Sharing policy is enforced before export.
- Tenant-private indicators are not published to federated feeds.

## Management Layer Contract

### Consumes

- Endpoint telemetry.
- Alerts.
- Incidents.
- Recovery reports.
- Threat reports.
- Health metrics.
- Admin actions.

### Produces

- Policy updates.
- Dashboard summaries.
- Alert state transitions.
- Containment commands.
- Recovery job requests.
- Audit records.

### Guarantees

- Every admin action is auditable.
- API responses are tenant-scoped.
- Policy changes are versioned and rollbackable.
- Endpoint commands are signed and expire.

## Containment Actions

Containment must be policy-driven and auditable. Supported actions:

- Suspend process.
- Terminate process.
- Quarantine executable hash.
- Temporarily deny write access to protected paths.
- Disable suspicious scheduled tasks associated with the incident.
- Isolate network share access for the suspicious principal.
- Increase logging level.

Default behavior:

- Low confidence: no automatic containment.
- Medium confidence: alert and increase logging.
- High confidence: suspend process and protect critical paths when policy allows.
- Critical confidence: terminate or suspend process, lock protected scopes, and
  start recovery impact estimation.

