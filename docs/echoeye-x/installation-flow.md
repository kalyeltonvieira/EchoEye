# Installation Flow

## Goals

- Install endpoint services safely.
- Establish a unique device identity.
- Seed secure-by-default policies.
- Deploy decoys and recovery metadata.
- Produce an auditable installation report.

## Flow

1. Launch installer with tenant enrollment token or standalone flag.
2. Validate operating system version, architecture, disk space, permissions,
   Windows service manager access, Event Log access, and recovery prerequisites.
3. Create directories under `%ProgramFiles%\EchoEye X` and
   `%ProgramData%\EchoEyeX`.
4. Create local service account or configure a managed service account.
5. Generate device key pair and installation UUID.
6. Register device with management backend when managed mode is enabled.
7. Write signed policy cache.
8. Install Windows services:
   - `EchoEye.ProtectionService`
   - `EchoEye.DecoyWorker`
   - `EchoEye.RecoveryWorker`
   - `EchoEye.IntelligenceWorker`
9. Configure service restart policy and dependency ordering.
10. Deploy decoy assets according to policy.
11. Initialize local storage and durable queues.
12. Create initial recovery catalog.
13. Start services and verify health heartbeats.
14. Produce installation report.

## Prerequisite Checks

- Supported Windows release and patch level.
- Administrator rights for installation.
- Required service account rights.
- Local database creation.
- Event source registration.
- Disk free space threshold.
- Protected path accessibility.
- Recovery storage availability.
- TLS trust and backend reachability for managed installs.

## Installation Report

The report includes:

- Installation ID.
- Device ID.
- Agent version.
- Installed services and versions.
- Directory ACL summary.
- Policy version.
- Decoy deployment summary.
- Recovery catalog status.
- Prerequisite check results.
- Backend registration status.
- Signature over report contents.

## Rollback

Rollback is triggered on critical installation failure:

- Stop any started services.
- Remove service registrations.
- Remove files created during the failed transaction.
- Retain installation logs under a temporary diagnostic folder.
- Do not retain device identity unless backend registration succeeded and the
  installer can mark the failed installation state.

## Secure Defaults

- Local logs and data are writable only by service identity and administrators.
- Decoys contain generated non-sensitive content.
- Backend communication requires TLS certificate validation.
- Endpoint identity private key is non-exportable where platform support allows.
- Telemetry export uses privacy-preserving policy by default.

