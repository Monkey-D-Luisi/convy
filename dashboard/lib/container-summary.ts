// One current product capture only; historical scopes and process/host resources are excluded.
type Service = { containerName: string; containerId: string | null; state: string; health: string; cpuPercent: number | null; memoryBytes: number | null; restartCount: number | null };
type Capture = { schemaVersion: number; scope: string; product: string; capturedAtUtc: string; status: string; services: Service[] };
export const serviceNames = ["convy-api", "convy-worker", "convy-auth", "convy-dashboard", "convy-mcp", "convy-db"];
const measured = (value: unknown): value is number => typeof value === "number" && Number.isFinite(value) && value >= 0;
export function summarizeContainers(metrics: Capture | null | undefined, now = Date.now(), unavailable = false) {
  const age = now - Date.parse(metrics?.capturedAtUtc?.startsWith("0001-") ? "" : metrics?.capturedAtUtc ?? "");
  const current = !unavailable && metrics?.schemaVersion === 1 && metrics.scope === "container" && metrics.product === "convy" && metrics.status === "available" && Number.isFinite(age) && age >= -30000 && age <= 180000;
  const rows = current && Array.isArray(metrics.services) ? metrics.services : [];
  const services = serviceNames.map(name => rows.filter(row => row?.containerName === name)).filter(matches => matches.length === 1).map(matches => matches[0]).filter(row => /^[a-f0-9]{64}$/.test(row.containerId ?? "") && rows.filter(other => other?.containerId === row.containerId).length === 1);
  const running = services.filter(row => row.state === "running");
  const sum = (key: "cpuPercent" | "memoryBytes") => {
    const values = running.map(row => row[key]).filter(measured);
    return { value: values.length ? values.reduce((total, value) => total + value, 0) : null, count: values.length };
  };
  const healthy = running.filter(row => row.health === "healthy").length;
  const unhealthy = running.filter(row => row.health === "unhealthy").length;
  const restarts = services.map(row => row.restartCount).filter((value): value is number => measured(value) && Number.isSafeInteger(value));
  return { current, ageSeconds: Number.isFinite(age) ? Math.max(0, Math.floor(age / 1000)) : null, expected: serviceNames.length, identified: services.length, running: running.length, healthy, unhealthy, unknown: serviceNames.length - healthy - unhealthy, cpu: sum("cpuPercent"), memory: sum("memoryBytes"), restarts: restarts.length ? restarts.reduce((total, value) => total + value, 0) : null, restartCoverage: restarts.length, services };
}
