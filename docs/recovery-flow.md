# EchoEye X Recovery Flow

## Overview

This document describes the recovery workflows in EchoEye X, including backup creation, validation, and restoration procedures.

## Recovery Point Types

### Full Recovery Point

- **Description**: Complete backup of all monitored files
- **Creation Time**: Longer (depends on data size)
- **Storage**: Largest
- **Use Case**: Baseline backup, major incidents
- **Retention**: Long-term (90+ days)

### Incremental Recovery Point

- **Description**: Changes since last recovery point
- **Creation Time**: Fast
- **Storage**: Minimal
- **Use Case**: Regular automatic backups
- **Retention**: Medium-term (30-90 days)

### Differential Recovery Point

- **Description**: Changes since last full backup
- **Creation Time**: Medium
- **Storage**: Moderate
- **Use Case**: Periodic backups between full points
- **Retention**: Medium-term (30-90 days)

## Recovery Point Creation

### Automatic Creation

**Schedule**: Every 6 hours (configurable)

**Process**:
```
1. Check storage availability
   ↓
2. Determine recovery point type
   ↓
3. Scan monitored paths
   ↓
4. Backup changed files
   ↓
5. Calculate checksum
   ↓
6. Validate integrity
   ↓
7. Save to database
   ↓
8. Apply retention policy
```

**Configuration**:
```json
{
  "recovery": {
    "enabled": true,
    "automaticBackupIntervalHours": 6,
    "retentionDays": 90,
    "maxStorageGB": 100
  }
}
```

### Manual Creation

**Trigger**: On-demand by administrator

**Process**:
```
1. Select backup type
   ↓
2. Select paths to include
   ↓
3. Select paths to exclude
   ↓
4. Set recovery point name
   ↓
5. Execute backup
   ↓
6. Validate integrity
   ↓
7. Generate report
```

**Command Line**:
```powershell
# Create manual recovery point
EchoEye.Recovery.exe create --type=Full --name="Manual Backup" --include="C:\Users"
```

### Incident-Triggered Creation

**Trigger**: Automatically on high-confidence incident detection

**Process**:
```
1. Incident detected (confidence >= High)
   ↓
2. Create immediate recovery point
   ↓
3. Associate with incident
   ↓
4. Mark for extended retention
   ↓
5. Notify administrator
```

## Recovery Point Structure

### Directory Layout

```
C:\ProgramData\EchoEye\Recovery\
└── {RecoveryPointId}\
    ├── manifest.json          # Recovery point metadata
    ├── checksum.txt           # Overall checksum
    └── files\
        ├── {relative_path_1}  # Backed up files
        ├── {relative_path_2}
        └── ...
```

### Manifest Format

```json
{
  "recoveryPointId": "uuid",
  "name": "Automatic-20240101-120000",
  "type": "Incremental",
  "createdAt": "2024-01-01T12:00:00Z",
  "fileCount": 1500,
  "totalSizeBytes": 10737418240,
  "checksum": "sha256_hash",
  "relatedIncidentId": null,
  "files": [
    {
      "fileId": "uuid",
      "originalPath": "C:\\Users\\Documents\\file.docx",
      "backupPath": "files\\Users_Documents_file.docx",
      "size": 102400,
      "originalModifiedTime": "2024-01-01T11:00:00Z",
      "hash": "sha256_hash"
    }
  ]
}
```

## Recovery Point Validation

### Validation Checks

1. **Storage Verification**
   - Directory exists
   - Files accessible
   - Permissions correct

2. **Checksum Verification**
   - Calculate current checksum
   - Compare with stored checksum
   - Flag mismatches

3. **File Index Verification**
   - Count files in index
   - Count files on disk
   - Verify each file exists

4. **Integrity Verification**
   - Verify file hashes
   - Check for corruption
   - Validate metadata

### Validation Process

```
Load Recovery Point
         ↓
Verify Storage
         ↓
Calculate Checksum
         ↓
Compare with Stored
         ↓
Verify File Index
         ↓
Validate File Hashes
         ↓
Update Validation Status
         ↓
Generate Validation Report
```

### Validation Results

**Valid**:
- All checks passed
- Recovery point marked as valid
- Safe to use for restoration

**Invalid**:
- One or more checks failed
- Recovery point marked as invalid
- Error details recorded
- Administrator notified

**Degraded**:
- Some files corrupted
- Partial recovery possible
- Files with errors identified

## Restoration Workflows

### Single File Restoration

**Use Case**: Restore individual file

**Process**:
```
1. Select file to restore
   ↓
2. Select recovery point
   ↓
3. Verify file exists in recovery point
   ↓
4. Restore file to original location
   ↓
5. Verify restoration
   ↓
6. Update file metadata
```

**Command**:
```powershell
EchoEye.Recovery.exe restore-file --file="C:\Users\Documents\file.docx" --recovery-point="uuid"
```

### Selective Restoration

**Use Case**: Restore multiple specific files

**Process**:
```
1. Select files to restore
   ↓
2. Select recovery point
   ↓
3. Verify all files exist
   ↓
4. Restore files in batch
   ↓
5. Verify each restoration
   ↓
6. Generate restoration report
```

**Command**:
```powershell
EchoEye.Recovery.exe restore-batch --files="file1.txt,file2.docx" --recovery-point="uuid"
```

### Full Restoration

**Use Case**: Complete system recovery after incident

**Process**:
```
1. Select recovery point
   ↓
2. Validate recovery point
   ↓
3. List all files in recovery point
   ↓
4. Begin restoration
   ↓
5. Restore files in batches
   ↓
6. Track progress
   ↓
7. Verify restoration
   ↓
8. Generate restoration report
```

**Command**:
```powershell
EchoEye.Recovery.exe restore-full --recovery-point="uuid"
```

## Restoration Progress Tracking

### Progress Metrics

- **Total Files**: Number of files to restore
- **Completed Files**: Number successfully restored
- **Failed Files**: Number that failed to restore
- **Bytes Restored**: Total data restored
- **Progress Percentage**: Completion percentage
- **Elapsed Time**: Time since start
- **Estimated Time Remaining**: Time to completion

### Progress Query

```powershell
# Query restoration progress
EchoEye.Recovery.exe progress --operation="uuid"
```

### Progress Output

```json
{
  "operationId": "uuid",
  "status": "InProgress",
  "totalFiles": 1500,
  "completedFiles": 750,
  "progressPercentage": 50.0,
  "elapsedTime": "00:10:00",
  "estimatedTimeRemaining": "00:10:00"
}
```

## Restoration Results

### Success Criteria

- All files restored successfully
- File hashes match
- Metadata preserved
- No errors reported

### Partial Success

- Some files failed to restore
- Failed files identified
- Cause of failure documented
- Alternative recovery options suggested

### Failure

- Restoration could not complete
- Critical error occurred
- Recovery point may be corrupted
- Alternative recovery point required

### Restoration Report

```json
{
  "operationId": "uuid",
  "status": "Completed",
  "filesAttempted": 1500,
  "filesSucceeded": 1495,
  "filesFailed": 5,
  "bytesRestored": 10737418240,
  "failedFiles": [
    "C:\\Users\\Documents\\file1.docx",
    "C:\\Users\\Documents\\file2.xlsx"
  ],
  "summary": "Restored 1495 of 1500 files successfully",
  "startTime": "2024-01-01T10:00:00Z",
  "endTime": "2024-01-01T10:30:00Z",
  "duration": "00:30:00"
}
```

## Retention Policy

### Default Retention

| Recovery Point Type | Retention Period |
|---------------------|------------------|
| Full | 90 days |
| Incremental | 30 days |
| Differential | 60 days |
| Incident-Related | 365 days |

### Automatic Cleanup

**Trigger**: Storage usage exceeds 80% of maximum

**Process**:
```
1. Check storage usage
   ↓
2. If > 80%, trigger cleanup
   ↓
3. Identify expired recovery points
   ↓
4. Delete oldest expired points
   ↓
5. Verify storage below threshold
   ↓
6. Log cleanup action
```

**Configuration**:
```json
{
  "recovery": {
    "maxStorageGB": 100,
    "maxStorageUsagePercentage": 0.8,
    "retentionDays": 90
  }
}
```

### Manual Cleanup

**Command**:
```powershell
# Clean up old recovery points
EchoEye.Recovery.exe cleanup --older-than="30d"
```

## Storage Management

### Storage Information

```powershell
# Get storage information
EchoEye.Recovery.exe storage-info
```

**Output**:
```
Total Capacity: 100 GB
Used Space: 45 GB
Available Space: 55 GB
Usage Percentage: 45%
```

### Compression

Recovery points can be compressed to save space:

```powershell
# Compress recovery point
EchoEye.Recovery.exe compress --recovery-point="uuid"
```

### Storage Optimization

1. **Compress Old Recovery Points**
   - Compress points older than 30 days
   - Save 30-50% space
   - Slightly slower restoration

2. **Delete Invalid Points**
   - Remove corrupted recovery points
   - Free up space
   - Prevent failed restorations

3. **Adjust Retention Policy**
   - Shorten retention for less critical data
   - Extend retention for critical data
   - Balance space vs. recovery window

## Best Practices

### Backup Strategy

1. **Regular Automatic Backups**
   - Every 6 hours for incremental
   - Weekly for differential
   - Monthly for full

2. **Pre-Incident Backups**
   - Trigger on high-confidence detection
   - Capture state before damage
   - Extend retention

3. **Critical Data Priority**
   - More frequent backups for critical paths
   - Higher retention for important data
   - Multiple recovery points

### Restoration Strategy

1. **Test Restoration Regularly**
   - Monthly test restores
   - Verify recovery point integrity
   - Practice restoration procedures

2. **Start with Critical Files**
   - Restore important documents first
   - Prioritize by business impact
   - Staged restoration

3. **Verify Before Proceeding**
   - Validate recovery point first
   - Check file hashes
   - Verify no corruption

### Storage Management

1. **Monitor Storage Usage**
   - Regular storage checks
   - Alert at 70% usage
   - Automatic cleanup at 80%

2. **Optimize Storage**
   - Compress old points
   - Delete invalid points
   - Adjust retention policy

3. **Plan for Growth**
   - Estimate storage needs
   - Budget for additional storage
   - Consider external storage

## Troubleshooting

### Recovery Point Creation Fails

**Problem**: Cannot create recovery point

**Solutions**:
- Check disk space availability
- Verify write permissions
- Check database connectivity
- Review logs for specific errors

### Restoration Fails

**Problem**: Files cannot be restored

**Solutions**:
- Validate recovery point integrity
- Check file permissions
- Verify target directory exists
- Try alternative recovery point

### Validation Errors

**Problem**: Checksum mismatch

**Solutions**:
- Recovery point may be corrupted
- Delete and recreate recovery point
- Check for disk errors
- Verify storage integrity

### Storage Full

**Problem**: Cannot create new recovery points

**Solutions**:
- Trigger manual cleanup
- Adjust retention policy
- Compress old recovery points
- Add additional storage

## Recovery Metrics

### Key Performance Indicators

- **Backup Success Rate**: Percentage of successful backups
- **Backup Duration**: Time to create recovery point
- **Validation Success Rate**: Percentage of valid recovery points
- **Restoration Success Rate**: Percentage of successful restorations
- **Restoration Speed**: Files restored per minute
- **Storage Efficiency**: Data compressed ratio

### Monitoring

Regular monitoring of:
- Storage usage trends
- Backup success/failure rates
- Restoration success/failure rates
- Recovery point age distribution
- Compression effectiveness
