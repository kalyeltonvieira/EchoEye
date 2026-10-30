# Scalability Roadmap

## Phase 1: Enterprise Endpoint Foundation

Target:

- 1,000 managed endpoints per tenant.
- Local detection and recovery metadata.
- Central alert and incident dashboard.
- Basic anonymized intelligence reports.

Investments:

- Durable endpoint queues.
- PostgreSQL central schema.
- Policy versioning.
- Signed installer and updates.
- Core test suite.

## Phase 2: Multi-Tenant Operations

Target:

- 25,000 endpoints per region.
- Multiple enterprise tenants.
- Regional ingestion services.
- Federated dashboard search.

Investments:

- Message queue backed ingestion.
- Partitioned event storage.
- Tenant-scoped encryption keys.
- API rate limits and backpressure.
- Read replicas for dashboard queries.
- Fleet health aggregation.

## Phase 3: Intelligence Federation

Target:

- Privacy-preserving intelligence sharing across tenants and peers.
- Threat family clustering.
- Campaign timeline analysis.
- Detection and recovery benchmark feeds.

Investments:

- Signed feed artifacts.
- Peer trust registry.
- Indicator deduplication.
- Behavioral fingerprint clustering.
- Feed revocation and version pinning.

## Phase 4: Advanced Recovery Orchestration

Target:

- Automated recovery recommendations.
- Cross-provider recovery point validation.
- Recovery drills and compliance reports.

Investments:

- Recovery provider adapters.
- Restore simulation.
- Blast-radius-aware recovery planning.
- Automated recovery success scoring.

## Phase 5: Large-Scale Analytics

Target:

- Hundreds of thousands of endpoints.
- Near real-time trend analysis.
- Long-retention historical reporting.

Investments:

- Columnar analytics store for historical trends.
- Stream processing for aggregate metrics.
- Cold storage lifecycle policies.
- Query budget enforcement.
- Precomputed dashboard materializations.

## Capacity Planning

Initial assumptions:

- Average endpoint telemetry: 1 to 5 events per second during normal activity.
- Burst telemetry: 500+ filesystem events per second during suspicious activity.
- Incident evidence retention: 180 days default.
- Forensic report retention: 1 year default.
- Health metric retention: 90 days default.

Scaling controls:

- Endpoint-side event summarization.
- Adaptive sampling for low-risk repetitive events.
- Full fidelity retention for incident windows.
- Backend ingestion backpressure.
- Per-tenant storage quotas.
- Archive policies for old forensic artifacts.

