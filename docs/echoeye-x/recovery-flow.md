# Recovery Flow

## Goals

- Maintain validated recovery points.
- Estimate recovery impact during incidents.
- Restore safely with auditability.
- Report recovery success metrics.

## Recovery Point Creation

1. Load recovery policy.
2. Enumerate protected scopes.
3. Create snapshot or file manifest depending on policy and backend support.
4. Hash manifest and selected metadata.
5. Store catalog in protected local storage.
6. Validate recovery point integrity.
7. Emit health metric and recovery point record.

## Integrity Validation

Validation checks:

- Manifest hash matches.
- Recovery storage is reachable.
- File count is within expected range.
- Sample restore succeeds.
- ACL and ownership metadata are restorable.
- Recovery point is inside retention window.

Integrity scores:

- `0.90` to `1.00`: healthy.
- `0.70` to `0.89`: degraded.
- Below `0.70`: critical.

## Incident Recovery

1. Protection service creates impact estimate.
2. Recovery engine ranks recovery points by recency and integrity.
3. Admin reviews recommended recovery plan.
4. Restore job is created with explicit scope and mode.
5. Recovery engine validates recovery point.
6. Files are staged to a safe location.
7. Hashes and ACLs are verified.
8. Files are restored in place or to alternate location.
9. Recovery report is generated.
10. Incident timeline is updated.

## Restore Modes

- `stage_only`
  - Restore files to a staging location for review.
- `restore_in_place`
  - Restore files to original locations after approval.
- `restore_to_alternate_location`
  - Restore files to a safe alternate path for manual review.

## Recovery Report

The report includes:

- Recovery job ID.
- Incident ID.
- Recovery point ID.
- Requested and approved principals.
- Start and completion timestamps.
- Estimated impacted files.
- Restored files.
- Skipped files.
- Failed files.
- Verification failures.
- Recovery success rate.
- Residual risk notes.

## Failure Handling

- Restore jobs are resumable.
- Failed file restores are isolated and listed in the report.
- In-place restore requires a pre-restore checkpoint when policy allows.
- Recovery engine never deletes suspected encrypted files until retention policy
  and admin approval allow cleanup.

