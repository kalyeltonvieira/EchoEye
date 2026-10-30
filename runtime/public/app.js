const endpoints = {
  posture: "/api/v1/dashboard/posture",
  alerts: "/api/v1/alerts",
  incidents: "/api/v1/incidents",
  recovery: "/api/v1/recovery/status",
  intelligence: "/api/v1/intelligence/summary",
  health: "/api/v1/health",
  trends: "/api/v1/trends",
  score: "/api/v1/detection/score"
};

const state = {
  incidents: [],
  selectedIncidentId: null
};

document.querySelectorAll(".tab").forEach((button) => {
  button.addEventListener("click", () => activateTab(button.dataset.tab));
});

document.getElementById("refresh-button").addEventListener("click", loadAll);
document.getElementById("score-form").addEventListener("input", updateScore);

loadAll();

async function loadAll() {
  setRuntimeStatus("Loading", "warning");

  const [posture, alerts, incidents, recovery, intelligence, health, trends] = await Promise.all([
    getJson(endpoints.posture),
    getJson(endpoints.alerts),
    getJson(endpoints.incidents),
    getJson(endpoints.recovery),
    getJson(endpoints.intelligence),
    getJson(endpoints.health),
    getJson(endpoints.trends)
  ]);

  renderPosture(posture);
  renderAlerts(alerts.items);
  renderIncidents(incidents.items);
  renderRecovery(recovery);
  renderIntelligence(intelligence);
  renderHealth(health.items);
  renderTrends(trends.items);
  await updateScore();

  setRuntimeStatus("Online", "healthy");
}

async function getJson(url) {
  const response = await fetch(url, { cache: "no-store" });
  if (!response.ok) throw new Error(`${url} returned ${response.status}`);
  return response.json();
}

function renderPosture(posture) {
  text("active-incidents", posture.activeIncidents);
  text("critical-alerts", posture.criticalAlerts);
  text("protected-devices", posture.protectedDevices.toLocaleString());
  text("recovery-score", `${Math.round(posture.recoveryIntegrityScore * 100)}%`);

  const status = document.getElementById("posture-status");
  status.textContent = posture.overallStatus;
  status.className = `severity ${posture.overallStatus}`;
}

function renderAlerts(alerts) {
  text("alert-count", alerts.length);

  const table = document.getElementById("alerts-table");
  table.innerHTML = "";
  table.append(row(["Severity", "Title", "Device", "Score", "Status"], "header"));

  alerts.forEach((alert) => {
    const severity = document.createElement("span");
    severity.className = `severity ${alert.severity}`;
    severity.textContent = alert.severity;

    table.append(row([
      severity,
      alert.title,
      alert.deviceAlias,
      `${Math.round(alert.confidenceScore * 100)}%`,
      alert.status
    ]));
  });
}

function renderIncidents(incidents) {
  state.incidents = incidents;
  state.selectedIncidentId = state.selectedIncidentId || incidents[0]?.id || null;
  text("incident-count", incidents.length);

  const list = document.getElementById("incident-list");
  list.innerHTML = "";

  incidents.forEach((incident) => {
    const item = document.createElement("article");
    item.className = `incident-card${incident.id === state.selectedIncidentId ? " active" : ""}`;
    item.tabIndex = 0;
    item.innerHTML = `
      <p class="item-title">${escapeHtml(incident.id)} - ${escapeHtml(incident.confidence)}</p>
      <p class="item-meta">${escapeHtml(incident.summary)}</p>
      <p class="item-meta">${escapeHtml(incident.deviceAlias)} | ${escapeHtml(incident.status)} | ${Math.round(incident.confidenceScore * 100)}%</p>
    `;
    item.addEventListener("click", () => selectIncident(incident.id));
    item.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") selectIncident(incident.id);
    });
    list.append(item);
  });

  if (state.selectedIncidentId) selectIncident(state.selectedIncidentId, false);
}

async function selectIncident(incidentId, rerender = true) {
  state.selectedIncidentId = incidentId;
  if (rerender) renderIncidents(state.incidents);

  text("timeline-id", incidentId);
  const timeline = await getJson(`/api/v1/incidents/${encodeURIComponent(incidentId)}/timeline`);
  const list = document.getElementById("timeline-list");
  list.innerHTML = "";

  timeline.items.forEach((item) => {
    const li = document.createElement("li");
    li.innerHTML = `
      <p class="item-title">${escapeHtml(item.title)}</p>
      <p class="item-meta">${formatTime(item.occurredAt)} | ${escapeHtml(item.eventType)} | ${escapeHtml(item.severity)}</p>
    `;
    list.append(li);
  });
}

function renderRecovery(recovery) {
  text("restore-rate", `${Math.round(recovery.restoreSuccessRate * 100)}%`);

  document.getElementById("recovery-grid").innerHTML = [
    metric("Integrity score", `${Math.round(recovery.integrityScore * 100)}%`),
    metric("Valid points", recovery.validRecoveryPoints),
    metric("Degraded points", recovery.degradedRecoveryPoints),
    metric("Active jobs", recovery.activeRestoreJobs)
  ].join("");

  const jobs = document.getElementById("recovery-jobs");
  jobs.innerHTML = "";
  recovery.jobs.forEach((job) => {
    const item = document.createElement("article");
    item.className = "recovery-card";
    item.innerHTML = `
      <p class="item-title">${escapeHtml(job.id)} - ${escapeHtml(job.status)}</p>
      <p class="item-meta">Incident ${escapeHtml(job.incidentId)} | restored ${job.restoredFiles}/${job.estimatedFiles} | failed ${job.failedFiles} | skipped ${job.skippedFiles}</p>
    `;
    jobs.append(item);
  });
}

function renderIntelligence(intelligence) {
  text("feed-version", `v${intelligence.feedVersion}`);

  document.getElementById("intel-summary").innerHTML = [
    metric("Privacy", intelligence.privacyLevel),
    metric("Fingerprints", intelligence.behavioralFingerprints),
    metric("Infra indicators", intelligence.infrastructureIndicators),
    metric("Campaigns", intelligence.campaignsTracked)
  ].join("");

  const families = document.getElementById("threat-families");
  families.innerHTML = "";
  intelligence.threatFamilies.forEach((family) => {
    const item = document.createElement("article");
    item.className = "intel-card";
    item.innerHTML = `
      <p class="item-title">${escapeHtml(family.name)}</p>
      <p class="item-meta">${family.reports} reports | ${escapeHtml(family.confidence)} confidence</p>
    `;
    families.append(item);
  });
}

function renderHealth(items) {
  text("health-count", items.length);

  const list = document.getElementById("health-list");
  list.innerHTML = "";
  items.forEach((item) => {
    const card = document.createElement("article");
    card.className = "health-card";
    card.innerHTML = `
      <div class="section-heading compact">
        <p class="item-title">${escapeHtml(item.module)}</p>
        <span class="severity ${escapeHtml(item.status)}">${escapeHtml(item.status)}</span>
      </div>
      <p class="item-meta">${escapeHtml(item.detail)}</p>
      <p class="item-meta">${formatTime(item.observedAt)}</p>
    `;
    list.append(card);
  });
}

function renderTrends(items) {
  const max = Math.max(...items.map((item) => item.alerts + item.recovered), 1);
  const chart = document.getElementById("trend-chart");
  chart.innerHTML = "";

  items.forEach((item) => {
    const bar = document.createElement("div");
    bar.className = "trend-bar";
    const height = Math.max(8, ((item.alerts + item.recovered) / max) * 130);
    bar.innerHTML = `
      <div class="trend-stack" style="height:${height}px" title="${item.alerts} alerts, ${item.incidents} incidents, ${item.recovered} recovered"></div>
      <span>${escapeHtml(item.label)}</span>
    `;
    chart.append(bar);
  });
}

async function updateScore() {
  const form = document.getElementById("score-form");
  const body = Object.fromEntries([...new FormData(form).entries()].map(([key, value]) => [key, Number(value)]));
  const score = await fetch(endpoints.score, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body)
  }).then((response) => response.json());

  const level = document.getElementById("score-level");
  level.textContent = score.level;
  level.className = `severity ${score.level}`;
  document.getElementById("score-bar").style.width = `${Math.round(score.score * 100)}%`;
  text("score-reasoning", `Score ${Math.round(score.score * 100)}%. ${score.reasoning}.`);
}

function activateTab(id) {
  document.querySelectorAll(".tab").forEach((tab) => tab.classList.toggle("active", tab.dataset.tab === id));
  document.querySelectorAll(".tab-panel").forEach((panel) => panel.classList.toggle("active", panel.id === id));
}

function row(cells, className = "") {
  const element = document.createElement("div");
  element.className = `table-row ${className}`.trim();
  cells.forEach((cell) => {
    const div = document.createElement("div");
    if (cell instanceof HTMLElement) div.append(cell);
    else div.textContent = cell;
    element.append(div);
  });
  return element;
}

function metric(label, value) {
  return `
    <article class="metric">
      <span class="metric-label">${escapeHtml(label)}</span>
      <strong>${escapeHtml(String(value))}</strong>
    </article>
  `;
}

function text(id, value) {
  document.getElementById(id).textContent = value;
}

function setRuntimeStatus(value, status) {
  const element = document.getElementById("runtime-status");
  element.textContent = value;
  element.className = `status-pill severity ${status}`;
}

function formatTime(value) {
  return new Intl.DateTimeFormat(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    month: "short",
    day: "numeric"
  }).format(new Date(value));
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

