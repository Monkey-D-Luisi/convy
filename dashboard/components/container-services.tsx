import type { ContainerMetrics } from "@/lib/types";

export function ContainerServices({ metrics }: { metrics: ContainerMetrics | null | undefined }) {
  const available = metrics?.status === "available";
  const names: Record<string, string> = { "convy-api": "API", "convy-worker": "Worker", "convy-auth": "Authentication", "convy-dashboard": "Admin dashboard", "convy-mcp": "MCP", "convy-db": "PostgreSQL" };
  return <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
    <h2 className="text-lg font-semibold text-ink">Convy containers</h2>
    <p className="mt-2 text-sm text-muted">Read-only Docker telemetry · CPU: 100% equals one core · Memory: Docker working set · Health is unknown when no healthcheck is configured.</p>
    <p className="mt-2 text-sm text-muted">Telemetry: {metrics?.status ?? "unknown"}{metrics?.capturedAtUtc && !metrics.capturedAtUtc.startsWith("0001-") ? ` · ${new Date(metrics.capturedAtUtc).toLocaleString()}` : ""}</p>
    <div className="mt-4 overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">Convy service resources and container identity</caption>
      <thead><tr>{["Service", "Container", "State", "Health", "CPU", "Memory", "Restarts"].map(label => <th key={label} scope="col" className="p-2">{label}</th>)}</tr></thead>
      <tbody>{Object.entries(names).map(([name, label]) => {
        const service = metrics?.services.find(row => row.containerName === name);
        return <tr key={name} className="border-t border-line"><th scope="row" className="p-2">{label}</th>
          <td className="p-2" title={service?.containerId ?? "Unknown"}>{name}<span className="block text-xs text-muted">{service?.containerId?.slice(0, 12) ?? "Unknown identity"}</span></td>
          <td className="p-2">{available ? service?.state ?? "unknown" : "unknown"}</td><td className="p-2">{available ? service?.health ?? "unknown" : "unknown"}</td>
          <td className="p-2">{available && service?.cpuPercent != null ? `${service.cpuPercent.toFixed(2)}%` : "Unknown"}</td>
          <td className="p-2">{available && service?.memoryBytes != null ? `${(service.memoryBytes / 2 ** 20).toFixed(1)} MiB` : "Unknown"}</td>
          <td className="p-2">{available ? service?.restartCount ?? "Unknown" : "Unknown"}</td></tr>;
      })}</tbody></table></div>
    <p className="mt-3 text-sm text-muted">VPS CPU, RAM, filesystem, load and uptime are available in <a className="underline" href="https://ops.monkeydluisi.com" target="_blank" rel="noreferrer">Infrastructure Ops</a>.</p>
  </section>;
}
