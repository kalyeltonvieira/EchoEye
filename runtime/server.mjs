import http from "node:http";
import { readFile, stat } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const publicDir = path.join(__dirname, "public");

const port = Number(process.env.ECHOEYE_PORT || process.argv[2] || 8080);
const host = process.env.ECHOEYE_HOST || "127.0.0.1";

const now = new Date();
const minutesAgo = (minutes) => new Date(now.getTime() - minutes * 60_000).toISOString();

const state = {
  posture: {
    tenantId: "local-demo",
    overallStatus: "warning",
    activeIncidents: 2,
    criticalAlerts: 1,
    protectedDevices: 1280,
    offlineDevices: 12,
    recoveryIntegrityScore: 0.93,
    latestFeedVersion: 44,
    lastUpdatedAt: now.toISOString()
  },
  health: [
    { module: "Protection Service", status: "healthy", detail: "Monitoring simulated protected scopes", observedAt: minutesAgo(1) },
    { module: "Decoy Engine", status: "healthy", detail: "24 active decoys deployed", observedAt: minutesAgo(3) },
    { module: "Recovery Engine", status: "warning", detail: "1 recovery point validation is overdue", observedAt: minutesAgo(6) },
    { module: "Intelligence Node", status: "healthy", detail: "Anonymized feed v44 ready", observedAt: minutesAgo(4) },
    { module: "Management Layer", status: "healthy", detail: "Local dashboard online", observedAt: minutesAgo(1) }
  ],
  alerts: [
    {
      id: "alt-1001",
      severity: "critical",
      category: "decoy_access",
      title: "Decoy document overwritten",
      confidenceScore: 0.96,
      deviceAlias: "win-endpoint-042",
      status: "open",
      firstSeenAt: minutesAgo(24),
      lastSeenAt: minutesAgo(18),
      incidentId: "inc-9001"
    },
    {
      id: "alt-1002",
      severity: "high",
      category: "file_velocity",
      title: "Protected file rename burst",
      confidenceScore: 0.82,
      deviceAlias: "win-endpoint-042",
      status: "open",
      firstSeenAt: minutesAgo(25),
      lastSeenAt: minutesAgo(15),
      incidentId: "inc-9001"
    },
    {
      id: "alt-1003",
      severity: "medium",
      category: "recovery_risk",
      title: "Recovery point validation overdue",
      confidenceScore: 0.52,
      deviceAlias: "win-endpoint-118",
      status: "acknowledged",
      firstSeenAt: minutesAgo(180),
      lastSeenAt: minutesAgo(30),
      incidentId: null
    }
  ],
  incidents: [
    {
      id: "inc-9001",
      status: "contained",
      confidence: "critical",
      confidenceScore: 0.96,
      summary: "Decoy overwrite correlated with rapid protected file renames and entropy shifts.",
      deviceAlias: "win-endpoint-042",
      openedAt: minutesAgo(25),
      containedAt: minutesAgo(16),
      impactedScope: {
        protectedPaths: 3,
        estimatedFilesModified: 418,
        estimatedFilesEncrypted: 117,
        estimatedRecoverableFiles: 116
      }
    },
    {
      id: "inc-9002",
      status: "active",
      confidence: "high",
      confidenceScore: 0.78,
      summary: "Unsigned process produced abnormal write velocity in a protected desktop scope.",
      deviceAlias: "win-endpoint-317",
      openedAt: minutesAgo(7),
      containedAt: null,
      impactedScope: {
        protectedPaths: 1,
        estimatedFilesModified: 64,
        estimatedFilesEncrypted: 12,
        estimatedRecoverableFiles: 12
      }
    }
  ],
  timeline: {
    "inc-9001": [
      { occurredAt: minutesAgo(25), eventType: "file_velocity", severity: "high", title: "Write and rename velocity exceeded baseline" },
      { occurredAt: minutesAgo(24), eventType: "decoy_touch", severity: "critical", title: "Decoy document overwritten" },
      { occurredAt: minutesAgo(22), eventType: "correlation", severity: "critical", title: "Evidence correlated into critical incident" },
      { occurredAt: minutesAgo(17), eventType: "containment_action", severity: "high", title: "Suspicious process suspended" },
      { occurredAt: minutesAgo(16), eventType: "recovery_estimate", severity: "medium", title: "116 of 117 encrypted files estimated recoverable" }
    ],
    "inc-9002": [
      { occurredAt: minutesAgo(7), eventType: "process_reputation", severity: "high", title: "Unsigned process started from temporary path" },
      { occurredAt: minutesAgo(6), eventType: "file_velocity", severity: "high", title: "Protected desktop write burst detected" },
      { occurredAt: minutesAgo(5), eventType: "alert", severity: "high", title: "Incident opened and elevated logging enabled" }
    ]
  },
  recovery: {
    integrityScore: 0.93,
    validRecoveryPoints: 182,
    degradedRecoveryPoints: 4,
    activeRestoreJobs: 1,
    restoreSuccessRate: 0.982,
    jobs: [
      { id: "rec-7001", incidentId: "inc-9001", status: "staging", estimatedFiles: 116, restoredFiles: 92, failedFiles: 0, skippedFiles: 1 },
      { id: "rec-6998", incidentId: "inc-8871", status: "completed", estimatedFiles: 42, restoredFiles: 42, failedFiles: 0, skippedFiles: 0 }
    ]
  },
  intelligence: {
    feedVersion: 44,
    privacyLevel: "anonymous",
    threatFamilies: [
      { name: "unknown-locker", reports: 12, confidence: "medium" },
      { name: "blackcat-like", reports: 4, confidence: "low" },
      { name: "lockbit-like", reports: 3, confidence: "low" }
    ],
    behavioralFingerprints: 27,
    infrastructureIndicators: 18,
    campaignsTracked: 5,
    detectionStatistics: {
      medium: 31,
      high: 17,
      critical: 6
    },
    recoveryStatistics: {
      estimatedImpactedFiles: 8942,
      estimatedRecoveredFiles: 8768,
      recoverySuccessRate: 0.9805
    }
  },
  trends: [
    { label: "Mon", alerts: 8, incidents: 1, recovered: 24 },
    { label: "Tue", alerts: 12, incidents: 2, recovered: 41 },
    { label: "Wed", alerts: 9, incidents: 1, recovered: 18 },
    { label: "Thu", alerts: 16, incidents: 2, recovered: 116 },
    { label: "Fri", alerts: 7, incidents: 0, recovered: 0 },
    { label: "Sat", alerts: 4, incidents: 0, recovered: 0 },
    { label: "Sun", alerts: 5, incidents: 1, recovered: 12 }
  ]
};

function calculateConfidence(signals) {
  const weights = {
    fileVelocity: 0.25,
    entropy: 0.25,
    decoyInteraction: 0.2,
    processReputation: 0.15,
    behaviorAnomaly: 0.1,
    networkActivity: 0.05
  };

  const components = {};
  let score = 0;

  for (const [key, weight] of Object.entries(weights)) {
    const value = clamp(Number(signals?.[key] ?? 0), 0, 1);
    components[key] = value;
    score += value * weight;
  }

  score = clamp(score, 0, 1);

  return {
    score: Number(score.toFixed(4)),
    level: getLevel(score),
    componentScores: components,
    reasoning: Object.entries(components)
      .filter(([, value]) => value > 0.5)
      .sort((a, b) => b[1] - a[1])
      .slice(0, 3)
      .map(([key, value]) => `${key} (${value.toFixed(2)})`)
      .join(", ") || "No significant indicators"
  };
}

function getLevel(score) {
  if (score < 0.4) return "low";
  if (score < 0.65) return "medium";
  if (score < 0.85) return "high";
  return "critical";
}

function clamp(value, min, max) {
  if (Number.isNaN(value)) return min;
  return Math.max(min, Math.min(max, value));
}

function json(res, status, payload) {
  res.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store"
  });
  res.end(JSON.stringify(payload, null, 2));
}

function notFound(res) {
  json(res, 404, {
    error: {
      code: "not_found",
      message: "The requested EchoEye X resource was not found."
    }
  });
}

async function parseJsonBody(req) {
  const chunks = [];
  for await (const chunk of req) chunks.push(chunk);
  if (chunks.length === 0) return {};
  return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

async function serveStatic(req, res, pathname) {
  const relative = pathname === "/" ? "index.html" : pathname.replace(/^\/+/, "");
  const filePath = path.normalize(path.join(publicDir, relative));

  if (!filePath.startsWith(publicDir)) {
    notFound(res);
    return;
  }

  try {
    const fileStat = await stat(filePath);
    if (!fileStat.isFile()) {
      notFound(res);
      return;
    }

    const ext = path.extname(filePath).toLowerCase();
    const contentType = {
      ".html": "text/html; charset=utf-8",
      ".css": "text/css; charset=utf-8",
      ".js": "text/javascript; charset=utf-8",
      ".json": "application/json; charset=utf-8",
      ".svg": "image/svg+xml; charset=utf-8"
    }[ext] || "application/octet-stream";

    res.writeHead(200, {
      "Content-Type": contentType,
      "Cache-Control": "no-store"
    });
    res.end(await readFile(filePath));
  } catch {
    notFound(res);
  }
}

async function handleApi(req, res, url) {
  if (req.method === "GET" && url.pathname === "/api/v1/dashboard/posture") return json(res, 200, state.posture);
  if (req.method === "GET" && url.pathname === "/api/v1/health") return json(res, 200, { items: state.health });
  if (req.method === "GET" && url.pathname === "/api/v1/alerts") return json(res, 200, { items: state.alerts });
  if (req.method === "GET" && url.pathname === "/api/v1/incidents") return json(res, 200, { items: state.incidents });
  if (req.method === "GET" && url.pathname === "/api/v1/recovery/status") return json(res, 200, state.recovery);
  if (req.method === "GET" && url.pathname === "/api/v1/intelligence/summary") return json(res, 200, state.intelligence);
  if (req.method === "GET" && url.pathname === "/api/v1/trends") return json(res, 200, { items: state.trends });

  const timelineMatch = url.pathname.match(/^\/api\/v1\/incidents\/([^/]+)\/timeline$/);
  if (req.method === "GET" && timelineMatch) {
    const incidentId = timelineMatch[1];
    return json(res, 200, { incidentId, items: state.timeline[incidentId] || [] });
  }

  if (req.method === "POST" && url.pathname === "/api/v1/detection/score") {
    try {
      const body = await parseJsonBody(req);
      return json(res, 200, calculateConfidence(body));
    } catch {
      return json(res, 400, {
        error: {
          code: "invalid_json",
          message: "Request body must be valid JSON."
        }
      });
    }
  }

  notFound(res);
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url || "/", `http://${host}:${port}`);

  try {
    if (url.pathname.startsWith("/api/")) {
      await handleApi(req, res, url);
      return;
    }

    await serveStatic(req, res, url.pathname);
  } catch (error) {
    json(res, 500, {
      error: {
        code: "internal_error",
        message: error instanceof Error ? error.message : "Unknown server error"
      }
    });
  }
});

server.listen(port, host, () => {
  console.log(`EchoEye X local runner listening on http://${host}:${port}`);
});
