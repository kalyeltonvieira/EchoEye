# Security Review

## Security Objectives

- Detect ransomware-like behavior early.
- Protect recovery data and forensic evidence from tampering.
- Avoid collecting sensitive user content.
- Provide auditable administrative control.
- Fail safely during partial outages.

## Privacy Controls

EchoEye X must not collect:

- Documents.
- Photos.
- Passwords.
- Private messages.
- Sensitive file contents.
- Full usernames or hostnames by default.
- Full file paths outside local endpoint storage by default.

Allowed security metadata:

- File extension.
- File size delta.
- Entropy summary.
- Hashed path and directory identifiers.
- Process image name.
- Process hash.
- Signature status.
- Parent process relationship.
- Decoy interaction metadata.
- Aggregate detection and recovery statistics.

## Threat Model

### Adversaries

- Commodity ransomware.
- Human-operated ransomware.
- Malware attempting to disable services.
- Insider attempting to suppress alerts.
- Compromised endpoint with local administrator rights.
- Network attacker attempting to poison intelligence feeds.

### Protected Assets

- User data in protected scopes.
- Recovery points and manifests.
- Device identity private key.
- Forensic logs.
- Policies.
- Administrative credentials.
- Intelligence feeds.

## Key Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Agent tampering | Signed binaries, service ACLs, tamper alerts, hash validation |
| Recovery point deletion | Restricted ACLs, immutable storage where available, integrity checks |
| False positives | Multi-signal scoring, policy thresholds, suppression audit trail |
| False negatives | Decoys, behavior windows, historical baselines, continuous tuning |
| Sensitive data leakage | Redaction, hashing, privacy policy enforcement, export review |
| Feed poisoning | Signed reports, peer trust levels, anomaly checks, revocation |
| Backend outage | Local detection, durable queues, local recovery metadata |
| Admin misuse | RBAC, MFA, just-in-time approvals, complete audit trails |

## Secure Configuration

- Default telemetry privacy level: `anonymous`.
- Automatic critical containment disabled until tenant policy confirms allowed
  actions.
- Recovery cleanup disabled by default.
- Admin MFA required.
- Device commands expire and must be signed.
- API tokens are short-lived.
- Database encryption at rest required.
- TLS 1.2+ required, TLS 1.3 preferred.

## Audit Requirements

Audit records are required for:

- Policy changes.
- Alert status changes.
- Incident closure.
- Containment commands.
- Restore approvals.
- Restore execution.
- Intelligence sharing policy changes.
- Peer trust changes.
- Admin authentication and authorization failures.

## Forensic Logging

Forensic logs are:

- Append-only at the application layer.
- Hash-chained by device and sequence.
- Linked to incident timeline entries.
- Exportable as signed reports.
- Retained according to tenant policy.

## Abuse Constraints

The platform must not provide offensive capabilities:

- No ransomware payload generation.
- No encryption attack tooling.
- No credential theft features.
- No stealth persistence.
- No unauthorized process injection.

Containment features must be limited to defensive actions on managed devices
under explicit tenant policy.

## Security Test Plan

- Unit tests for redaction and anonymization.
- Unit tests for scoring thresholds.
- Integration tests for incident creation and containment.
- Recovery restore tests against staged fixtures.
- Permission tests for service directories.
- Tamper simulation tests.
- API authorization tests.
- Feed signature verification tests.
- Regression tests for false-positive-heavy workloads.

