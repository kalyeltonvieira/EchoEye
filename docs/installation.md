# EchoEye X Installation Guide

## Overview

This guide describes the installation process for EchoEye X, a production-grade ransomware resilience platform for Windows.

## Prerequisites

### System Requirements

- **Operating System**: Windows 10 (build 10240) or later
- **.NET Runtime**: .NET 8.0 Runtime
- **Disk Space**: Minimum 5 GB free space (10 GB recommended)
- **Privileges**: Administrator rights required for installation
- **Services**: Windows Service Control Manager must be available

### Supported Editions

- Windows 10 Pro/Enterprise/Education
- Windows 11 Pro/Enterprise/Education
- Windows Server 2019 or later

## Installation Process

### Step 1: Prepare the System

1. **Verify Administrator Privileges**
   ```powershell
   # Check if running as administrator
   ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
   ```

2. **Verify .NET Runtime**
   ```powershell
   # Check for .NET 8.0
   dotnet --list-runtimes
   ```

3. **Check Disk Space**
   ```powershell
   # Check available disk space
   Get-PSDrive C | Select-Object Used, Free
   ```

### Step 2: Run the Installer

1. **Extract the Installation Package**
   ```powershell
   Expand-Archive -Path EchoEye-X.zip -DestinationPath .\EchoEye-X
   ```

2. **Run the Installer**
   ```powershell
   cd EchoEye-X
   .\EchoEye.Installer.exe
   ```

3. **Follow the Installation Wizard**
   - Review prerequisites check
   - Confirm installation directory
   - Review configuration options
   - Start installation

### Step 3: Installation Steps

The installer performs the following operations:

1. **Prerequisite Validation**
   - Windows version check
   - .NET runtime verification
   - Disk space verification
   - Administrator privilege check
   - Windows Service availability check

2. **Directory Structure Creation**
   ```
   C:\ProgramData\EchoEye\
   ├── Data\              # Database and data files
   ├── Logs\               # Log files
   ├── Config\             # Configuration files
   ├── Recovery\           # Recovery points
   ├── Forensics\          # Forensic evidence
   ├── Decoys\             # Decoy assets
   ├── Intelligence\       # Threat intelligence
   └── Temp\               # Temporary files
   ```

3. **Device Identity Generation**
   - Generates unique device UUID
   - Creates hardware fingerprint
   - Generates registration key
   - Stores in database

4. **Database Initialization**
   - Creates SQLite database
   - Initializes all tables
   - Creates indexes
   - Enables WAL mode for performance

5. **Default Configuration**
   - Protection settings
   - Decoy settings
   - Recovery settings
   - Intelligence settings
   - Logging settings

6. **Windows Service Registration**
   - Registers EchoEye Protection Service
   - Sets startup type to Automatic
   - Configures service description
   - Sets appropriate permissions

7. **Decoy Asset Deployment**
   - Generates initial decoy files
   - Deploys to strategic locations
   - Registers in database

8. **Installation Report Generation**
   - Creates JSON report
   - Logs all installation steps
   - Saves to Logs directory

### Step 4: Verify Installation

1. **Check Service Status**
   ```powershell
   Get-Service -Name EchoEyeProtection
   ```

2. **Verify Directory Structure**
   ```powershell
   Get-ChildItem -Path "C:\ProgramData\EchoEye" -Recurse
   ```

3. **Check Installation Report**
   ```powershell
   Get-Content "C:\ProgramData\EchoEye\Logs\installation_*.json"
   ```

4. **Review Logs**
   ```powershell
   Get-Content "C:\ProgramData\EchoEye\Logs\*.log" -Tail 50
   ```

## Post-Installation Configuration

### Basic Configuration

1. **Access Configuration**
   ```powershell
   # Configuration is stored in the database
   # Use the Management Layer to modify settings
   ```

2. **Configure Watched Paths**
   - Default: User Profile Documents, Desktop, Pictures, Downloads
   - Add additional paths as needed
   - Exclude system directories

3. **Adjust Detection Thresholds**
   - Confidence threshold (default: 75)
   - File velocity threshold (default: 100 files/min)
   - Entropy threshold (default: 7.5)

4. **Configure Recovery Settings**
   - Backup interval (default: 6 hours)
   - Retention period (default: 90 days)
   - Maximum storage (default: 100 GB)

### Advanced Configuration

1. **Custom Decoy Deployment**
   - Adjust decoy count per path
   - Customize decoy types
   - Set deployment strategy

2. **Intelligence Sharing**
   - Enable/disable peer discovery
   - Configure sync interval
   - Set sharing preferences

3. **Logging Configuration**
   - Set log retention period
   - Configure log file size limits
   - Adjust log level

## Starting the Service

### Manual Start

```powershell
Start-Service -Name EchoEyeProtection
```

### Automatic Start

The service is configured to start automatically by default.

### Verify Service is Running

```powershell
Get-Service -Name EchoEyeProtection | Select-Object Status, StartType
```

## Uninstallation

### Step 1: Stop the Service

```powershell
Stop-Service -Name EchoEyeProtection
```

### Step 2: Run Uninstaller

```powershell
.\EchoEye.Installer.exe --uninstall
```

### Step 3: Manual Cleanup (if needed)

```powershell
# Remove service
sc.exe delete EchoEyeProtection

# Remove directories
Remove-Item -Path "C:\ProgramData\EchoEye" -Recurse -Force
```

## Troubleshooting

### Installation Fails

**Problem**: Prerequisite check fails

**Solution**:
- Verify Windows version meets requirements
- Install .NET 8.0 Runtime if missing
- Ensure sufficient disk space
- Run installer as Administrator

### Service Won't Start

**Problem**: Service fails to start

**Solution**:
- Check Windows Event Viewer for errors
- Verify database file is not corrupted
- Check file permissions
- Review log files in Logs directory

### Database Errors

**Problem**: Database initialization fails

**Solution**:
- Ensure Data directory has write permissions
- Check disk space
- Delete corrupted database and reinstall
- Review installation logs

### Permission Issues

**Problem**: Access denied errors

**Solution**:
- Verify service account has appropriate permissions
- Check directory ACLs
- Ensure service runs with correct credentials

## Installation Report

The installation report includes:

- Installation timestamp
- Device ID and fingerprint
- Prerequisites validation results
- Directory structure
- Service registration status
- Any warnings or errors

Example report:
```json
{
  "InstallationDate": "2024-01-01T00:00:00Z",
  "Version": "1.0.0",
  "DeviceId": "550e8400-e29b-41d4-a716-446655440000",
  "MachineName": "DESKTOP-ABC123",
  "OsVersion": "Microsoft Windows NT 10.0.19045.0",
  "Fingerprint": "a1b2c3d4e5f6...",
  "Prerequisites": {
    "IsValid": true,
    "Errors": [],
    "Warnings": ["Low disk space: 8 GB available. 10 GB recommended."]
  },
  "Directories": {
    "Base": "C:\\ProgramData\\EchoEye",
    "Data": "C:\\ProgramData\\EchoEye\\Data",
    "Logs": "C:\\ProgramData\\EchoEye\\Logs",
    "Config": "C:\\ProgramData\\EchoEye\\Config",
    "Recovery": "C:\\ProgramData\\EchoEye\\Recovery",
    "Forensics": "C:\\ProgramData\\EchoEye\\Forensics",
    "Decoys": "C:\\ProgramData\\EchoEye\\Decoys",
    "Intelligence": "C:\\ProgramData\\EchoEye\\Intelligence"
  },
  "Services": [
    {
      "Name": "EchoEyeProtection",
      "DisplayName": "EchoEye X Protection Service",
      "Status": "Registered"
    }
  ]
}
```

## Silent Installation

For automated deployments, use silent installation:

```powershell
.\EchoEye.Installer.exe --silent --config="config.json"
```

Configuration file format:
```json
{
  "watchedPaths": ["C:\\Users", "D:\\Data"],
  "excludedPaths": ["C:\\Windows"],
  "confidenceThreshold": 75,
  "decoyCount": 50,
  "recoveryRetentionDays": 90,
  "intelligenceSharingEnabled": true
}
```

## Verification Checklist

After installation, verify:

- [ ] Service is running
- [ ] All directories created
- [ ] Database initialized
- [ ] Configuration loaded
- [ ] Decoys deployed
- [ ] No errors in logs
- [ ] Installation report generated
- [ ] Dashboard accessible (if configured)

## Support

For installation issues:
1. Review installation logs
2. Check Windows Event Viewer
3. Consult troubleshooting section
4. Contact support with installation report
