# Incident Flow

## Lifecycle

1. Detect activity.
2. Correlate evidence.
3. Create incident record.
4. Contain suspicious activity.
5. Preserve forensic evidence.
6. Generate intelligence artifacts.
7. Support recovery.
8. Produce executive summary.

## Detailed Flow

### 1. Detect Activity

The protection service receives filesystem, process, decoy, and system events.
Events are normalized and grouped into time windows. Feature extraction computes:

- File write rate.
- Rename/delete rate.
- Entropy shifts.
- Extension changes.
- Protected path impact.
- Decoy touch severity.
- Process reputation and signer status.
- Parent process lineage.
- Historical baseline deviation.

### 2. Correlate Evidence

The correlator links events by:

- Process GUID.
- Parent process chain.
- User/session context.
- Protected scope.
- Time window.
- Decoy path interaction.
- Similar rename or extension patterns.

### 3. Create Incident Record

An incident is opened when the confidence score reaches the policy threshold.
The incident record is persisted before containment begins.

### 4. Contain Suspicious Activity

Containment is policy-driven:

- Medium confidence:
  - Open alert.
  - Increase logging.
  - Snapshot volatile evidence.
- High confidence:
  - Suspend suspicious process where allowed.
  - Temporarily protect critical paths from writes.
  - Queue admin notification.
- Critical confidence:
  - Suspend or terminate suspicious process.
  - Lock protected scopes.
  - Isolate suspicious share access.
  - Trigger immediate recovery impact estimation.

Containment actions must be idempotent and auditable.

### 5. Preserve Forensic Evidence

Evidence includes:

- Process metadata.
- Signed executable status.
- File activity summaries.
- Decoy touch records.
- Timeline entries.
- Containment action results.
- Hash-chained forensic log references.

Full user file contents are never collected.

### 6. Generate Intelligence Artifacts

The intelligence node creates:

- Behavioral fingerprint.
- Extension and rename pattern summary.
- Process indicator summary.
- Infrastructure indicator hashes where available.
- Detection statistics.
- Recovery statistics.

### 7. Support Recovery

The recovery engine receives an impact estimate and recommends recovery points.
Admins can stage or restore files from validated recovery points.

### 8. Produce Executive Summary

The summary includes:

- Incident confidence and severity.
- First and last observed activity.
- Affected protected scopes.
- Estimated modified, encrypted, recoverable, and recovered files.
- Containment actions and outcomes.
- Recovery status.
- Intelligence classification.
- Recommended follow-up actions.

## Confidence Scoring

Example scoring model:

| Signal | Weight |
| --- | ---: |
| Decoy overwrite/delete | 0.35 |
| Protected file modification velocity | 0.25 |
| Entropy and extension anomaly | 0.15 |
| Process reputation risk | 0.10 |
| Historical baseline deviation | 0.05 |
| Recovery impact severity | 0.10 |

Thresholds:

- Low: `0.20` to `0.39`
- Medium: `0.40` to `0.64`
- High: `0.65` to `0.84`
- Critical: `0.85` to `1.00`

Thresholds are configurable per tenant and protected scope.

