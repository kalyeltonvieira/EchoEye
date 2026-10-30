# API Specifications

## API Style

EchoEye X exposes REST APIs for dashboard and automation, plus optional gRPC
contracts for high-volume endpoint ingestion. All external APIs require TLS,
signed device authentication, tenant scoping, and audit logging.

Base URL:

```text
https://management.echoeye.example/api/v1
```

## Authentication

Supported mechanisms:

- Admin dashboard: OIDC with MFA and role-based access control.
- Endpoint ingestion: mutual TLS or signed device JWT using installer-generated
  device keys.
- Service-to-service: short-lived workload identity tokens.

## Endpoint Ingestion

### Register Device

```http
POST /devices/register
```

Request:

```json
{
  "install_id": "uuid",
  "device_public_key": "base64",
  "agent_version": "1.0.0",
  "os_family": "windows",
  "os_version": "10.0.22631",
  "hostname_hash": "sha256"
}
```

Response:

```json
{
  "device_id": "uuid",
  "tenant_id": "uuid",
  "policy_version": 12,
  "ingestion_endpoint": "https://management.echoeye.example/api/v1"
}
```

### Send Telemetry Batch

```http
POST /telemetry/batches
```

Request:

```json
{
  "device_id": "uuid",
  "batch_id": "uuid",
  "sequence": 1001,
  "events": [
    {
      "id": "uuid",
      "event_type": "file_write",
      "occurred_at": "timestamp",
      "payload": {}
    }
  ]
}
```

Response:

```json
{
  "accepted": true,
  "batch_id": "uuid",
  "next_policy_version": 12
}
```

### Send Alert

```http
POST /alerts
```

Request:

```json
{
  "device_id": "uuid",
  "severity": "high",
  "category": "file_velocity",
  "title": "Suspicious mass modification detected",
  "confidence_score": 0.82,
  "evidence_refs": ["uuid"]
}
```

Response:

```json
{
  "alert_id": "uuid",
  "status": "open"
}
```

### Send Incident

```http
POST /incidents
```

Request:

```json
{
  "device_id": "uuid",
  "confidence": "critical",
  "confidence_score": 0.96,
  "summary": "Decoy overwrite plus rapid protected file renames",
  "timeline_refs": ["uuid"],
  "alert_refs": ["uuid"]
}
```

Response:

```json
{
  "incident_id": "uuid",
  "status": "active"
}
```

### Send Intelligence Report

```http
POST /intelligence/reports
```

Request:

```json
{
  "device_id": "uuid",
  "incident_id": "uuid",
  "privacy_level": "shareable",
  "behavioral_fingerprint": "sha256",
  "filesystem_indicators": {},
  "recovery_statistics": {}
}
```

Response:

```json
{
  "report_id": "uuid",
  "accepted": true,
  "feed_version": 44
}
```

## Management APIs

### List Security Posture

```http
GET /dashboard/posture
```

Response:

```json
{
  "tenant_id": "uuid",
  "overall_status": "warning",
  "active_incidents": 2,
  "critical_alerts": 1,
  "protected_devices": 1280,
  "offline_devices": 12,
  "recovery_integrity_score": 0.93,
  "last_updated_at": "timestamp"
}
```

### List Alerts

```http
GET /alerts?status=open&severity=high,critical&limit=100
```

Response:

```json
{
  "items": [],
  "next_cursor": "string|null"
}
```

### Update Alert Status

```http
PATCH /alerts/{alert_id}
```

Request:

```json
{
  "status": "acknowledged",
  "comment": "Reviewed by SOC"
}
```

### Get Incident Timeline

```http
GET /incidents/{incident_id}/timeline
```

Response:

```json
{
  "incident_id": "uuid",
  "items": [
    {
      "occurred_at": "timestamp",
      "event_type": "decoy_touch",
      "title": "Decoy document overwritten",
      "severity": "critical",
      "evidence_ref": "uuid"
    }
  ]
}
```

### Trigger Containment

```http
POST /incidents/{incident_id}/containment
```

Request:

```json
{
  "actions": ["suspend_process", "block_hash", "isolate_share_access"],
  "approval_token": "string"
}
```

Response:

```json
{
  "job_id": "uuid",
  "status": "queued"
}
```

### Create Restore Job

```http
POST /recovery/jobs
```

Request:

```json
{
  "incident_id": "uuid",
  "recovery_point_id": "uuid",
  "restore_scope": {
    "path_hashes": ["sha256"],
    "protected_scope_ids": ["uuid"]
  },
  "mode": "stage_only|restore_in_place|restore_to_alternate_location"
}
```

Response:

```json
{
  "job_id": "uuid",
  "status": "queued"
}
```

## Error Model

```json
{
  "error": {
    "code": "policy_version_conflict",
    "message": "Endpoint policy version is stale.",
    "request_id": "uuid",
    "details": {}
  }
}
```

Common status codes:

- `400`: invalid request.
- `401`: unauthenticated.
- `403`: unauthorized.
- `409`: version conflict or duplicate sequence.
- `429`: rate limited.
- `500`: server error.

