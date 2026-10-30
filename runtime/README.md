# EchoEye X Local Runner

This runner provides a dependency-free local dashboard and defensive API
simulation for EchoEye X when the .NET SDK is not installed.

It exposes:

- `GET /`
- `GET /api/v1/dashboard/posture`
- `GET /api/v1/alerts`
- `GET /api/v1/incidents`
- `GET /api/v1/incidents/{id}/timeline`
- `GET /api/v1/recovery/status`
- `GET /api/v1/intelligence/summary`
- `GET /api/v1/health`
- `POST /api/v1/detection/score`

Run:

```powershell
.\run-local.ps1
```

Or:

```powershell
node .\runtime\server.mjs 8080
```

The local runner is a safe defensive simulation. It does not perform filesystem
monitoring, process containment, encryption, or destructive actions.

