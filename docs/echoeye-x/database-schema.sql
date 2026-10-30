-- EchoEye X database schema
-- Target: PostgreSQL 15+ for central management.
-- Local endpoint storage can use the same logical schema in SQLite with
-- compatible UUID/text/timestamp mappings.

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE tenants (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('active', 'suspended', 'deleted')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE devices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  install_id UUID NOT NULL,
  hostname_hash TEXT NOT NULL,
  os_family TEXT NOT NULL CHECK (os_family IN ('windows')),
  os_version TEXT NOT NULL,
  agent_version TEXT NOT NULL,
  public_key TEXT NOT NULL,
  posture TEXT NOT NULL CHECK (posture IN ('healthy', 'degraded', 'critical', 'offline')),
  last_seen_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, install_id)
);

CREATE TABLE policies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  version INTEGER NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('draft', 'active', 'retired')),
  policy_json JSONB NOT NULL,
  created_by TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  activated_at TIMESTAMPTZ,
  UNIQUE (tenant_id, version)
);

CREATE TABLE protected_scopes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID REFERENCES devices(id),
  scope_type TEXT NOT NULL CHECK (scope_type IN ('documents', 'desktop', 'share', 'custom')),
  path_hash TEXT NOT NULL,
  priority TEXT NOT NULL CHECK (priority IN ('normal', 'critical')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE incidents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  status TEXT NOT NULL CHECK (status IN ('active', 'contained', 'recovering', 'recovered', 'closed', 'escalated')),
  confidence TEXT NOT NULL CHECK (confidence IN ('low', 'medium', 'high', 'critical')),
  confidence_score NUMERIC(5,4) NOT NULL CHECK (confidence_score >= 0 AND confidence_score <= 1),
  summary TEXT NOT NULL,
  primary_process_ref UUID,
  impacted_scope JSONB NOT NULL DEFAULT '{}'::jsonb,
  opened_at TIMESTAMPTZ NOT NULL,
  contained_at TIMESTAMPTZ,
  closed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX incidents_tenant_status_idx ON incidents (tenant_id, status, opened_at DESC);
CREATE INDEX incidents_device_opened_idx ON incidents (device_id, opened_at DESC);

CREATE TABLE alerts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  incident_id UUID REFERENCES incidents(id),
  severity TEXT NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
  category TEXT NOT NULL,
  title TEXT NOT NULL,
  description TEXT NOT NULL,
  confidence_score NUMERIC(5,4) NOT NULL CHECK (confidence_score >= 0 AND confidence_score <= 1),
  status TEXT NOT NULL CHECK (status IN ('open', 'acknowledged', 'suppressed', 'resolved')),
  evidence_refs UUID[] NOT NULL DEFAULT '{}',
  first_seen_at TIMESTAMPTZ NOT NULL,
  last_seen_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX alerts_tenant_status_idx ON alerts (tenant_id, status, severity, last_seen_at DESC);
CREATE INDEX alerts_incident_idx ON alerts (incident_id);

CREATE TABLE evidence_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  incident_id UUID REFERENCES incidents(id),
  event_type TEXT NOT NULL,
  occurred_at TIMESTAMPTZ NOT NULL,
  process_json JSONB NOT NULL DEFAULT '{}'::jsonb,
  file_json JSONB NOT NULL DEFAULT '{}'::jsonb,
  risk_json JSONB NOT NULL DEFAULT '{}'::jsonb,
  raw_ref TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX evidence_incident_time_idx ON evidence_events (incident_id, occurred_at);
CREATE INDEX evidence_device_time_idx ON evidence_events (device_id, occurred_at DESC);
CREATE INDEX evidence_type_time_idx ON evidence_events (tenant_id, event_type, occurred_at DESC);

CREATE TABLE incident_timeline (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  incident_id UUID NOT NULL REFERENCES incidents(id),
  occurred_at TIMESTAMPTZ NOT NULL,
  event_type TEXT NOT NULL,
  title TEXT NOT NULL,
  severity TEXT NOT NULL CHECK (severity IN ('low', 'medium', 'high', 'critical')),
  evidence_ref UUID REFERENCES evidence_events(id),
  details JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX incident_timeline_idx ON incident_timeline (incident_id, occurred_at);

CREATE TABLE decoy_assets (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  asset_type TEXT NOT NULL CHECK (asset_type IN ('document', 'spreadsheet', 'archive', 'image_placeholder')),
  location_class TEXT NOT NULL CHECK (location_class IN ('documents', 'desktop', 'shared', 'custom')),
  path_hash TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('active', 'touched', 'rotated', 'retired')),
  deployed_at TIMESTAMPTZ NOT NULL,
  rotated_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX decoy_device_status_idx ON decoy_assets (device_id, status);

CREATE TABLE recovery_points (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  scope_id UUID REFERENCES protected_scopes(id),
  type TEXT NOT NULL CHECK (type IN ('snapshot', 'file_manifest', 'external_backup', 'cloud_backup')),
  status TEXT NOT NULL CHECK (status IN ('creating', 'valid', 'degraded', 'failed', 'retired')),
  file_count BIGINT NOT NULL DEFAULT 0,
  total_bytes BIGINT NOT NULL DEFAULT 0,
  manifest_hash TEXT NOT NULL,
  integrity_score NUMERIC(5,4) NOT NULL DEFAULT 0 CHECK (integrity_score >= 0 AND integrity_score <= 1),
  created_at TIMESTAMPTZ NOT NULL,
  validated_at TIMESTAMPTZ,
  retention_until TIMESTAMPTZ NOT NULL
);

CREATE INDEX recovery_points_device_status_idx ON recovery_points (device_id, status, created_at DESC);

CREATE TABLE recovery_jobs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  incident_id UUID REFERENCES incidents(id),
  recovery_point_id UUID NOT NULL REFERENCES recovery_points(id),
  status TEXT NOT NULL CHECK (status IN ('queued', 'validating', 'staging', 'restoring', 'completed', 'failed', 'cancelled')),
  requested_by TEXT NOT NULL,
  approved_by TEXT,
  restore_scope JSONB NOT NULL,
  mode TEXT NOT NULL CHECK (mode IN ('stage_only', 'restore_in_place', 'restore_to_alternate_location')),
  estimated_files BIGINT NOT NULL DEFAULT 0,
  restored_files BIGINT NOT NULL DEFAULT 0,
  failed_files BIGINT NOT NULL DEFAULT 0,
  skipped_files BIGINT NOT NULL DEFAULT 0,
  report_ref UUID,
  started_at TIMESTAMPTZ,
  completed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX recovery_jobs_incident_idx ON recovery_jobs (incident_id);
CREATE INDEX recovery_jobs_status_idx ON recovery_jobs (tenant_id, status, created_at DESC);

CREATE TABLE threat_reports (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID REFERENCES devices(id),
  incident_id UUID REFERENCES incidents(id),
  privacy_level TEXT NOT NULL CHECK (privacy_level IN ('anonymous', 'tenant_private', 'shareable')),
  threat_family TEXT NOT NULL DEFAULT 'unknown',
  behavioral_fingerprint TEXT NOT NULL,
  process_indicators JSONB NOT NULL DEFAULT '[]'::jsonb,
  filesystem_indicators JSONB NOT NULL DEFAULT '{}'::jsonb,
  infrastructure_indicators JSONB NOT NULL DEFAULT '[]'::jsonb,
  campaign_window JSONB NOT NULL DEFAULT '{}'::jsonb,
  detection_statistics JSONB NOT NULL DEFAULT '{}'::jsonb,
  recovery_statistics JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX threat_reports_fingerprint_idx ON threat_reports (behavioral_fingerprint);
CREATE INDEX threat_reports_family_idx ON threat_reports (tenant_id, threat_family, created_at DESC);

CREATE TABLE intelligence_feeds (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID REFERENCES tenants(id),
  feed_name TEXT NOT NULL,
  version INTEGER NOT NULL,
  privacy_level TEXT NOT NULL CHECK (privacy_level IN ('tenant_private', 'federated', 'public_anonymous')),
  generated_at TIMESTAMPTZ NOT NULL,
  indicator_count INTEGER NOT NULL DEFAULT 0,
  feed_json JSONB NOT NULL,
  signature TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (tenant_id, feed_name, version)
);

CREATE TABLE peer_metadata (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID REFERENCES tenants(id),
  peer_name TEXT NOT NULL,
  peer_public_key TEXT NOT NULL,
  trust_level TEXT NOT NULL CHECK (trust_level IN ('internal', 'trusted_partner', 'community')),
  sharing_policy JSONB NOT NULL,
  last_seen_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE system_health_metrics (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID REFERENCES devices(id),
  metric_name TEXT NOT NULL,
  metric_value NUMERIC NOT NULL,
  unit TEXT NOT NULL,
  observed_at TIMESTAMPTZ NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('healthy', 'warning', 'critical')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX health_tenant_metric_time_idx ON system_health_metrics (tenant_id, metric_name, observed_at DESC);
CREATE INDEX health_device_time_idx ON system_health_metrics (device_id, observed_at DESC);

CREATE TABLE audit_trails (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  actor_id TEXT NOT NULL,
  actor_type TEXT NOT NULL CHECK (actor_type IN ('admin', 'service', 'device')),
  action TEXT NOT NULL,
  resource_type TEXT NOT NULL,
  resource_id UUID,
  request_id UUID,
  ip_hash TEXT,
  outcome TEXT NOT NULL CHECK (outcome IN ('success', 'failure')),
  details JSONB NOT NULL DEFAULT '{}'::jsonb,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX audit_tenant_time_idx ON audit_trails (tenant_id, created_at DESC);
CREATE INDEX audit_resource_idx ON audit_trails (resource_type, resource_id);

CREATE TABLE forensic_log_chain (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES tenants(id),
  device_id UUID NOT NULL REFERENCES devices(id),
  incident_id UUID REFERENCES incidents(id),
  sequence BIGINT NOT NULL,
  entry_hash TEXT NOT NULL,
  previous_hash TEXT,
  entry_ref UUID,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (device_id, sequence)
);

CREATE INDEX forensic_incident_idx ON forensic_log_chain (incident_id, sequence);

