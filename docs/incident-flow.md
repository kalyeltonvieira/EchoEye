# EchoEye X Incident Flow

## Overview

This document describes the complete incident lifecycle in EchoEye X, from detection through resolution and recovery.

## Incident Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                     INCIDENT LIFECYCLE                          │
└─────────────────────────────────────────────────────────────────┘

1. DETECTION
   ↓
2. CORRELATION
   ↓
3. INCIDENT CREATION
   ↓
4. CONTAINMENT
   ↓
5. INVESTIGATION
   ↓
6. RECOVERY
   ↓
7. RESOLUTION
   ↓
8. INTELLIGENCE GENERATION
```

## Phase 1: Detection

### Detection Triggers

1. **File System Events**
   - Mass file modifications
   - High entropy changes (encryption)
   - Suspicious file extensions
   - Rapid file changes

2. **Decoy Interactions**
   - Decoy file access
   - Decoy modification
   - Decoy encryption
   - Multiple decoy hits

3. **Process Behavior**
   - Suspicious process activity
   - Unknown process reputation
   - Abnormal file access patterns
   - Command & control communication

### Detection Pipeline

```
File Event / Decoy Interaction / Process Event
         ↓
   Signal Collection
         ↓
   Behavior Analysis
         ↓
   Confidence Scoring
         ↓
   Threshold Evaluation
         ↓
   Alert Generation
```

### Confidence Scoring

The detection engine combines multiple signals:

| Signal | Weight | Description |
|--------|--------|-------------|
| File Velocity | 25% | Rate of file changes |
| Entropy | 25% | Encryption detection |
| Decoy Interaction | 20% | Honeypot hits |
| Process Reputation | 15% | Known malicious/suspicious |
| Behavior Anomaly | 10% | Deviation from baseline |
| Network Activity | 5% | C2 communication |

**Confidence Levels**:
- **0-25**: Low (informational)
- **26-50**: Medium (investigate)
- **51-75**: High (contain)
- **76-100**: Critical (immediate action)

## Phase 2: Correlation

### Evidence Collection

1. **File Events**
   - All file modifications
   - Process associations
   - Timestamps and hashes
   - Entropy scores

2. **Process Information**
   - Process tree
   - Command lines
   - Parent processes
   - Digital signatures

3. **Decoy Interactions**
   - Access patterns
   - Modification attempts
   - Process associations
   - Confidence scores

4. **Network Activity**
   - Connection attempts
   - DNS queries
   - Data exfiltration attempts

### Correlation Logic

```
Collect all related events within time window
         ↓
Group by process ID
         ↓
Identify common patterns
         ↓
Calculate aggregate confidence
         ↓
Determine threat family (if known)
```

## Phase 3: Incident Creation

### Incident Record

```json
{
  "incidentId": "uuid",
  "deviceId": "uuid",
  "status": "Detected",
  "confidence": "High",
  "detectedAt": "2024-01-01T00:00:00Z",
  "threatFamilyId": "uuid",
  "affectedFileCount": 150,
  "affectedDataSizeBytes": 1073741824,
  "containmentAction": null,
  "summary": "Ransomware activity detected",
  "details": "{...}",
  "forensicEvidencePath": "C:\\ProgramData\\EchoEye\\Forensics\\incident_uuid",
  "isFalsePositive": false
}
```

### Initial Alert

An alert is automatically created:
- Severity based on confidence level
- Type indicates detection source
- Source module identified
- Related data attached

## Phase 4: Containment

### Containment Actions

| Confidence Level | Recommended Action |
|------------------|-------------------|
| Critical | Terminate Process, Block Network |
| High | Suspend Process, Block Network |
| Medium | Block Network Access |
| Low | Monitor Only |

### Containment Process

```
Evaluate Confidence
         ↓
Select Containment Action
         ↓
Execute Containment
         ↓
Verify Containment
         ↓
Update Incident Status
```

### Containment Options

1. **Process Containment**
   - Suspend process execution
   - Terminate process
   - Block network access

2. **File Containment**
   - Quarantine suspicious files
   - Delete malicious files
   - Lock file access

3. **Network Containment**
   - Block process network access
   - Isolate device (extreme cases)

## Phase 5: Investigation

### Investigation Steps

1. **Forensic Evidence Collection**
   - Memory dump of suspicious process
   - File system snapshot
   - Registry changes
   - Network traffic capture

2. **Threat Identification**
   - Match against known threat families
   - Analyze behavioral fingerprint
   - Check threat intelligence feeds

3. **Impact Assessment**
   - Count affected files
   - Calculate data loss
   - Identify critical systems affected
   - Estimate recovery time

4. **Root Cause Analysis**
   - Identify initial infection vector
   - Trace process lineage
   - Determine attack timeline

### Investigation Timeline

```
Incident Detected
         ↓
Immediate Containment (0-5 minutes)
         ↓
Forensic Collection (5-30 minutes)
         ↓
Threat Analysis (30-60 minutes)
         ↓
Impact Assessment (60-90 minutes)
         ↓
Root Cause Analysis (90-120 minutes)
```

## Phase 6: Recovery

### Recovery Options

1. **Automatic Recovery**
   - Select most recent recovery point
   - Validate integrity
   - Restore affected files
   - Verify restoration

2. **Manual Recovery**
   - Select specific recovery point
   - Choose files to restore
   - Execute restoration
   - Verify results

3. **Selective Recovery**
   - Restore critical files first
   - Prioritize by importance
   - Staged restoration process

### Recovery Process

```
Select Recovery Point
         ↓
Validate Integrity
         ↓
Select Files to Restore
         ↓
Execute Restoration
         ↓
Verify Restoration
         ↓
Generate Recovery Report
```

### Recovery Metrics

- Files attempted
- Files succeeded
- Files failed
- Bytes restored
- Restoration duration
- Success rate

## Phase 7: Resolution

### Resolution Criteria

An incident is resolved when:
- Containment is successful
- Recovery is complete
- No ongoing threat activity
- System is verified clean
- Documentation is complete

### Resolution Steps

1. **Verify Clean State**
   - Scan for remaining threats
   - Verify no suspicious processes
   - Check for backdoors
   - Validate system integrity

2. **Update Incident Status**
   - Set status to Resolved
   - Record resolution timestamp
   - Add resolution notes
   - Close related alerts

3. **Generate Summary**
   - Executive summary
   - Technical details
   - Timeline of events
   - Lessons learned

## Phase 8: Intelligence Generation

### Threat Report Creation

```json
{
  "reportId": "uuid",
  "threatFamilyId": "uuid",
  "deviceId": "uuid",
  "generatedAt": "2024-01-01T00:00:00Z",
  "behavioralFingerprint": "hash",
  "fileHashes": ["sha256", ...],
  "processIndicators": ["process", ...],
  "detectionCount": 1,
  "averageConfidenceScore": 0.85,
  "isShared": false
}
```

### Anonymization

Before sharing, data is anonymized:
- Device ID hashed
- File paths sanitized
- Process names hashed
- Geographic region only (country-level)

### Intelligence Sharing

- Share with peer nodes
- Update threat family database
- Contribute to community intelligence
- Receive updates from peers

## Incident Timeline Example

```
2024-01-01 10:00:00 - File system event detected (high entropy)
2024-01-01 10:00:05 - Decoy interaction detected (encryption)
2024-01-01 10:00:10 - Confidence score calculated (0.85)
2024-01-01 10:00:15 - Alert generated (Critical)
2024-01-01 10:00:20 - Incident created
2024-01-01 10:00:25 - Process suspended
2024-01-01 10:00:30 - Network access blocked
2024-01-01 10:01:00 - Forensic evidence collected
2024-01-01 10:05:00 - Threat identified (LockBit variant)
2024-01-01 10:10:00 - Impact assessment complete
2024-01-01 10:15:00 - Recovery initiated
2024-01-01 10:30:00 - Recovery complete (145/150 files)
2024-01-01 10:35:00 - System verified clean
2024-01-01 10:40:00 - Incident resolved
2024-01-01 10:45:00 - Threat report generated
2024-01-01 10:50:00 - Intelligence shared
```

## False Positive Handling

### Identification

An incident may be marked as false positive if:
- Legitimate backup operation
- Authorized file encryption
- Known safe process
- Configuration error
- Testing activity

### Process

1. **Mark as False Positive**
   - Update incident status
   - Record reason
   - Update threat intelligence

2. **Lift Containment**
   - Restore process execution
   - Unblock network access
   - Restore quarantined files

3. **Learn from Incident**
   - Update detection rules
   - Adjust confidence thresholds
   - Improve baseline behavior

## Key Performance Indicators

### Detection Metrics
- Mean Time to Detect (MTTD)
- Detection accuracy rate
- False positive rate
- Threat coverage

### Response Metrics
- Mean Time to Contain (MTTC)
- Mean Time to Recover (MTTR)
- Containment success rate
- Recovery success rate

### Intelligence Metrics
- Threat reports generated
- Intelligence shared
- Peer connections
- Threat family coverage

## Best Practices

1. **Monitor Alerts Continuously**
   - Set up alert notifications
   - Review critical alerts immediately
   - Investigate high-confidence detections

2. **Maintain Recovery Points**
   - Regular automatic backups
   - Validate integrity periodically
   - Test restoration procedures

3. **Update Threat Intelligence**
   - Share threat reports
   - Receive community updates
   - Update detection rules

4. **Review Incidents**
   - Conduct post-incident reviews
   - Document lessons learned
   - Improve detection capabilities

5. **Test Containment**
   - Regular containment drills
   - Verify recovery procedures
   - Update playbooks
