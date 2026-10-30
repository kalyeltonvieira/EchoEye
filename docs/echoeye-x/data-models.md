# Data Models

## Common Fields

All durable records include:

- `id`: UUID.
- `tenant_id`: UUID or `local` for standalone endpoint.
- `device_id`: UUID for endpoint-scoped records.
- `created_at`: UTC timestamp.
- `updated_at`: UTC timestamp.
- `schema_version`: integer.
- `source`: service or subsystem that produced the record.

## Device

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "hostname_hash": "sha256",
  "os_family": "windows",
  "os_version": "string",
  "agent_version": "semver",
  "install_id": "uuid",
  "public_key": "base64",
  "posture": "healthy|degraded|critical|offline",
  "last_seen_at": "timestamp"
}
```

Privacy note: hostnames are hashed before leaving the endpoint unless the
tenant explicitly enables identifiable asset inventory.

## Alert

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "incident_id": "uuid|null",
  "severity": "low|medium|high|critical",
  "category": "file_velocity|decoy_access|process_reputation|recovery_risk|system_health",
  "title": "string",
  "description": "string",
  "confidence_score": 0.0,
  "evidence_refs": ["uuid"],
  "status": "open|acknowledged|suppressed|resolved",
  "first_seen_at": "timestamp",
  "last_seen_at": "timestamp"
}
```

## Incident

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "status": "active|contained|recovering|recovered|closed|escalated",
  "confidence": "low|medium|high|critical",
  "confidence_score": 0.0,
  "summary": "string",
  "primary_process_ref": "uuid|null",
  "impacted_scope": {
    "protected_paths": 0,
    "estimated_files_modified": 0,
    "estimated_files_encrypted": 0,
    "estimated_recoverable_files": 0
  },
  "timeline_refs": ["uuid"],
  "alert_refs": ["uuid"],
  "recovery_job_refs": ["uuid"],
  "intelligence_report_refs": ["uuid"],
  "opened_at": "timestamp",
  "contained_at": "timestamp|null",
  "closed_at": "timestamp|null"
}
```

## Evidence Event

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "incident_id": "uuid|null",
  "event_type": "file_write|file_rename|file_delete|decoy_touch|process_start|process_end|containment_action",
  "occurred_at": "timestamp",
  "process": {
    "pid": 1234,
    "process_guid": "string",
    "image_path_hash": "sha256",
    "image_name": "string",
    "publisher": "string|null",
    "signature_status": "trusted|untrusted|unsigned|unknown",
    "parent_process_guid": "string|null"
  },
  "file": {
    "path_hash": "sha256",
    "extension": ".docx",
    "directory_hash": "sha256",
    "size_before": 1000,
    "size_after": 1200,
    "entropy_before": 4.2,
    "entropy_after": 7.8
  },
  "risk": {
    "weight": 0.0,
    "reason_codes": ["entropy_shift", "extension_change"]
  }
}
```

Privacy note: full paths remain local by default. Backend records use hashes,
normalized extensions, directory class, and protected-scope tags.

## Decoy Asset

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "asset_type": "document|spreadsheet|archive|image_placeholder",
  "location_class": "documents|desktop|shared|custom",
  "path_hash": "sha256",
  "content_hash": "sha256",
  "deployed_at": "timestamp",
  "rotated_at": "timestamp|null",
  "status": "active|touched|rotated|retired"
}
```

## Recovery Point

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "scope_id": "uuid",
  "type": "snapshot|file_manifest|external_backup|cloud_backup",
  "status": "creating|valid|degraded|failed|retired",
  "created_at": "timestamp",
  "validated_at": "timestamp|null",
  "file_count": 0,
  "total_bytes": 0,
  "manifest_hash": "sha256",
  "integrity_score": 0.0,
  "retention_until": "timestamp"
}
```

## Recovery Job

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "incident_id": "uuid|null",
  "recovery_point_id": "uuid",
  "status": "queued|validating|staging|restoring|completed|failed|cancelled",
  "requested_by": "principal_id",
  "approved_by": "principal_id|null",
  "estimated_files": 0,
  "restored_files": 0,
  "failed_files": 0,
  "skipped_files": 0,
  "started_at": "timestamp|null",
  "completed_at": "timestamp|null",
  "report_ref": "uuid|null"
}
```

## Threat Report

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "incident_id": "uuid",
  "privacy_level": "anonymous|tenant_private|shareable",
  "threat_family": "unknown|string",
  "behavioral_fingerprint": "sha256",
  "process_indicators": [
    {
      "image_name": "string",
      "image_hash": "sha256|null",
      "signature_status": "trusted|untrusted|unsigned|unknown"
    }
  ],
  "filesystem_indicators": {
    "extensions_seen": [".locked"],
    "rename_pattern_hash": "sha256|null",
    "entropy_shift_mean": 0.0,
    "modification_velocity": 0.0
  },
  "infrastructure_indicators": [
    {
      "type": "domain|ip|url|wallet|email",
      "value_hash": "sha256",
      "confidence": "low|medium|high|critical"
    }
  ],
  "campaign_window": {
    "first_seen_at": "timestamp",
    "last_seen_at": "timestamp"
  },
  "recovery_statistics": {
    "estimated_impacted_files": 0,
    "estimated_recovered_files": 0,
    "recovery_success_rate": 0.0
  }
}
```

## System Health Metric

```json
{
  "id": "uuid",
  "tenant_id": "uuid",
  "device_id": "uuid",
  "metric_name": "service_heartbeat|queue_depth|event_lag_ms|disk_free_bytes|recovery_integrity_score",
  "metric_value": 0.0,
  "unit": "count|ms|bytes|percent",
  "observed_at": "timestamp",
  "status": "healthy|warning|critical"
}
```

