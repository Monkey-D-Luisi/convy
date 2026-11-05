"use client";

import { useEffect, useState } from "react";
import type { ContainerMetrics } from "@/lib/types";
import { summarizeContainers } from "../lib/container-summary";

export function ContainerServices({ metrics }: { metrics: ContainerMetrics | null | undefined }) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => { const timer = setInterval(() => setNow(Date.now()), 30000); return () => clearInterval(timer); }, []);
  const summary = summarizeContainers(metrics, now);
  const available = summary.current;
  const coverage = (count: number) => `${count}/${summary.expected}`;
  const number = (value: number | null) => value === null ? "Unknown" : value.toLocaleString("en", { maximumFractionDigits: 2 });
  const names: Record<string, string> = { "convy-api": "API", "convy-worker": "Worker", "convy-auth": "Authentication", "convy-dashboard": "Admin dashboard", "convy-mcp": "MCP", "convy-db": "PostgreSQL" };
  return <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
    <h2 className="text-lg font-semibold text-ink">Convy Resource Summary</h2>
    <p className="mt-2 text-sm text-muted">{available ? "Current capture" : "Telemetry unavailable or stale"} · Collection time: {metrics?.capturedAtUtc && Number.isFinite(Date.parse(metrics.capturedAtUtc)) && !metrics.capturedAtUtc.startsWith("0001-") ? new Date(metrics.capturedAtUtc).toLocaleString() : "Unknown"}{summary.ageSeconds !== null ? ` · ${summary.ageSeconds}s old` : ""}</p>
    <dl className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {[["Expected / Running", `${summary.expected} / ${summary.current ? summary.running : "Unknown"}`], ["Identified containers", coverage(summary.identified)], ["CPU aggregate · 100% = one core", `${number(summary.cpu.value)}% · coverage ${coverage(summary.cpu.count)}`], ["Memory aggregate · Docker working set", `${number(summary.memory.value === null ? null : summary.memory.value / 2 ** 20)} MiB · coverage ${coverage(summary.memory.count)}`], ["Health · Healthy / Unhealthy / Unknown", `${summary.healthy} / ${summary.unhealthy} / ${summary.unknown}`], ["Restarts · container lifetime", `${number(summary.restarts)} · coverage ${coverage(summary.restartCoverage)}`]].map(([label, value]) => <div key={label}><dt className="text-sm text-muted">{label}</dt><dd className="mt-1 font-semibold text-ink">{value}</dd></div>)}
    </dl>
    <p className="mt-3 text-sm text-muted">{summary.cpu.count === summary.expected && summary.memory.count === summary.expected ? "Complete resource coverage." : "Partial coverage: aggregates are measured subtotals; missing containers are excluded."} One capture; restarts reset when a container is recreated.</p>
    <h3 className="mt-5 font-semibold text-ink">Convy containers</h3>
    <p className="mt-2 text-sm text-muted">Read-only Docker telemetry · CPU: 100% equals one core · Memory: Docker working set · Health is unknown when no healthcheck is configured.</p>
    <p className="mt-2 text-sm text-muted">Telemetry: {metrics?.status ?? "unknown"}{metrics?.capturedAtUtc && !metrics.capturedAtUtc.startsWith("0001-") ? ` · ${new Date(metrics.capturedAtUtc).toLocaleString()}` : ""}</p>
    <div className="mt-4 overflow-x-auto"><table className="w-full text-left text-sm"><caption className="sr-only">Convy service resources and container identity</caption>
      <thead><tr>{["Service", "Container", "State", "Health", "CPU", "Memory", "Restarts"].map(label => <th key={label} scope="col" className="p-2">{label}</th>)}</tr></thead>
      <tbody>{Object.entries(names).map(([name, label]) => {
        const service = summary.services.find(row => row.containerName === name);
        return <tr key={name} className="border-t border-line"><th scope="row" className="p-2">{label}</th>
          <td className="p-2" title={service?.containerId ?? "Unknown"}>{name}<span className="block text-xs text-muted">{service?.containerId?.slice(0, 12) ?? "Unknown identity"}</span></td>
          <td className="p-2">{available ? service?.state ?? "unknown" : "unknown"}</td><td className="p-2">{available ? service?.health ?? "unknown" : "unknown"}</td>
          <td className="p-2">{available && service?.state === "running" && service?.cpuPercent != null && Number.isFinite(service.cpuPercent) && service.cpuPercent >= 0 ? `${service.cpuPercent.toFixed(2)}%` : "Unknown"}</td>
          <td className="p-2">{available && service?.state === "running" && service?.memoryBytes != null && Number.isFinite(service.memoryBytes) && service.memoryBytes >= 0 ? `${(service.memoryBytes / 2 ** 20).toFixed(1)} MiB` : "Unknown"}</td>
          <td className="p-2">{available && Number.isSafeInteger(service?.restartCount) && (service?.restartCount ?? -1) >= 0 ? service?.restartCount : "Unknown"}</td></tr>;
      })}</tbody></table></div>
    <p className="mt-3 text-sm text-muted">VPS CPU, RAM, filesystem, load and uptime are available in <a className="underline" href="https://ops.monkeydluisi.com" target="_blank" rel="noreferrer">Infrastructure Ops</a>.</p>
  </section>;
}
