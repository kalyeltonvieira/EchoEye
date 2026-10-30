# Dashboard Specification

## Purpose

The EchoEye X dashboard gives administrators immediate visibility into
ransomware resilience, active incidents, recovery readiness, threat
intelligence, historical trends, and endpoint health.

## Primary Views

### Security Posture Overview

Shows:

- Overall tenant posture: healthy, warning, critical.
- Active incidents by confidence.
- Critical alerts requiring action.
- Protected endpoints.
- Offline or degraded endpoints.
- Recovery integrity score.
- Latest intelligence feed version.

Primary actions:

- Open active incidents.
- Review critical alerts.
- View degraded devices.
- Export posture report.

### Active Alerts

Shows:

- Alert severity.
- Confidence score.
- Category.
- Device label or privacy-preserving device alias.
- First and last seen timestamps.
- Linked incident.
- Status and assignee.

Primary actions:

- Acknowledge alert.
- Suppress with reason.
- Link or unlink incident.
- Escalate to incident.

### Incident Timelines

Shows:

- Incident status.
- Confidence score and confidence level.
- First observed activity.
- Process and behavior summary.
- Timeline of evidence events.
- Containment actions and outcomes.
- Recovery estimate and recovery jobs.
- Intelligence artifacts generated.

Primary actions:

- Trigger containment.
- Assign owner.
- Create restore job.
- Export forensic report.
- Generate executive summary.
- Close incident with disposition.

### Recovery Status

Shows:

- Recovery point freshness.
- Recovery integrity score.
- Protected scope coverage.
- Active restore jobs.
- Restore success rate.
- Failed validation details.

Primary actions:

- Validate recovery point.
- Create restore job.
- Stage restore.
- Approve restore in place.
- Export recovery report.

### Threat Intelligence Summaries

Shows:

- Threat families observed.
- Behavioral fingerprints.
- Infrastructure indicator counts.
- Campaign timelines.
- Detection statistics.
- Recovery statistics.
- Feed version and sharing status.

Primary actions:

- View report details.
- Export anonymized feed.
- Revoke feed version.
- Change sharing policy.

### Historical Trends

Shows:

- Alerts over time.
- Incidents over time.
- Confidence distribution.
- Mean time to detect.
- Mean time to contain.
- Mean time to recover.
- Recovery success rate.
- Endpoint health trends.

Primary actions:

- Filter by time range.
- Filter by device group.
- Export compliance report.

### Device Health

Shows:

- Service status.
- Agent version.
- Last heartbeat.
- Queue depth.
- Event lag.
- Recovery integrity score.
- Policy version.
- Update status.

Primary actions:

- Request heartbeat.
- Re-apply policy.
- Quarantine device from central commands when supported.
- Start update rollout.

## Roles

- Viewer: read dashboards and reports.
- Analyst: manage alerts and incidents.
- Responder: trigger containment and create recovery jobs.
- Recovery Admin: approve restore operations.
- Tenant Admin: manage policies, users, and sharing settings.
- Auditor: read audit trails and export compliance reports.

## Dashboard Data Freshness

- Critical alerts: target under 10 seconds.
- Incident timeline updates: target under 30 seconds.
- Device health: target under 5 minutes.
- Historical trends: target under 15 minutes.
- Intelligence feeds: generated on schedule or incident close.

## Accessibility And Usability

- Every severity uses text labels in addition to color.
- All tables support keyboard navigation and sorting.
- Incident views preserve filter and timeline context.
- Long-running recovery and containment jobs show state transitions.
- Destructive or disruptive actions require confirmation and audit reason.

