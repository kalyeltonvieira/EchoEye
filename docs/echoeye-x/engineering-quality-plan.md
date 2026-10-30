# Engineering Quality Plan

## Architecture Standards

- Modules have explicit contracts and own their storage writes.
- Shared libraries are limited to primitives, logging, configuration, identity,
  serialization, and validation.
- Detection logic is isolated from telemetry collection and containment
  execution.
- Recovery workflows are isolated from incident scoring.
- Intelligence export requires a privacy gate before persistence or sharing.

## Logging

Required log streams:

- Operational logs.
- Detection decision logs.
- Forensic logs.
- Recovery logs.
- Intelligence export logs.
- Audit logs.
- Health metrics.

Logging requirements:

- Include correlation IDs.
- Avoid sensitive content.
- Use structured JSON logs.
- Record policy version and agent version.
- Hash-chain forensic logs.
- Make log level adjustable by signed policy.

## Unit Tests

Required coverage:

- Confidence scoring.
- Event normalization.
- Event window aggregation.
- Decoy touch classification.
- Process reputation classification.
- Privacy redaction.
- Threat report anonymization.
- Recovery manifest validation.
- Restore report calculations.
- Policy validation.

## Integration Tests

Required scenarios:

- Installer creates directories, services, identity, and report.
- Protection service detects synthetic mass modification.
- Decoy touch escalates confidence.
- Incident creation persists before containment.
- Containment job is idempotent.
- Recovery point creation and validation.
- Restore job stage-only mode.
- Intelligence report generation with redaction.
- Backend ingestion accepts idempotent batches.
- Dashboard APIs enforce tenant isolation.

## Fault Tolerance Tests

Required scenarios:

- Backend unreachable while endpoint detects incident.
- Local queue reaches warning threshold.
- Recovery storage unavailable.
- Corrupt recovery manifest.
- Service crash and restart.
- Policy update conflict.
- Partial containment failure.
- Duplicate telemetry batch.
- Feed signature verification failure.

## Secure Build Requirements

- Code signing for Windows binaries.
- Signed installer.
- Dependency lockfiles.
- Software bill of materials.
- Static analysis in CI.
- Secret scanning in CI.
- Unit and integration test gates.
- Reproducible release metadata.
- Release artifact checksums.

## Release Gates

A production release requires:

- Passing unit tests.
- Passing integration tests.
- Passing privacy redaction tests.
- Passing service restart tests.
- Passing recovery restore tests.
- No critical static analysis findings.
- Signed installer and binaries.
- Approved deployment ring plan.
- Rollback package available.

## Maintainability

- Version every public contract.
- Keep migrations backward-compatible across two minor versions.
- Use explicit deprecation windows for APIs and policies.
- Store policy as validated schema, not unstructured settings.
- Keep detection rules explainable and auditable.
- Preserve incident records when rules or scores are retuned.

