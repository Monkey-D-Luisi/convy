"use client";

import { ReactNode, useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { useAdminToken } from "@/components/admin-shell";
import type { BackupRun, Overview, SystemHealth, UsageMetrics, VoiceMetrics } from "@/lib/types";

const numberFormatter = new Intl.NumberFormat("en-US");
const compactFormatter = new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

function useAdminResource<T>(path: string) {
  const token = useAdminToken();
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError("");
      try {
        const response = await fetch(`/api/admin/${path}`, {
          headers: { Authorization: `Bearer ${token}` },
          cache: "no-store",
        });

        if (!response.ok) {
          throw new Error(`${response.status} ${response.statusText}`);
        }

        const payload = (await response.json()) as T;
        if (!cancelled) {
          setData(payload);
        }
      } catch (nextError) {
        if (!cancelled) {
          setError(nextError instanceof Error ? nextError.message : "Request failed.");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      cancelled = true;
    };
  }, [path, token]);

  return { data, error, loading };
}

function dateRange(days: number) {
  const to = new Date();
  const from = new Date();
  from.setDate(to.getDate() - (days - 1));

  return `from=${formatDate(from)}&to=${formatDate(to)}`;
}

function formatDate(value: Date) {
  return value.toISOString().slice(0, 10);
}

function formatMicros(value: number | null) {
  if (value === null) {
    return "Not configured";
  }

  return `$${(value / 1_000_000).toFixed(4)}`;
}

function formatBytes(value: number | null) {
  if (value === null) {
    return "Unknown";
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  let next = value;
  let index = 0;
  while (next >= 1024 && index < units.length - 1) {
    next /= 1024;
    index += 1;
  }

  return `${next.toFixed(index === 0 ? 0 : 1)} ${units[index]}`;
}

function PageState({ loading, error }: { loading: boolean; error: string }) {
  if (loading) {
    return <div className="rounded-lg border border-line bg-white p-6 text-sm text-muted">Loading</div>;
  }

  if (error) {
    return <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-sm text-red-800">{error}</div>;
  }

  return null;
}

function MetricCard({ label, value, tone = "default" }: { label: string; value: string | number; tone?: "default" | "good" | "warn" }) {
  const toneClass =
    tone === "good"
      ? "border-brand/30 bg-emerald-50"
      : tone === "warn"
        ? "border-amber-300 bg-amber-50"
        : "border-line bg-white";

  return (
    <section className={`rounded-lg border p-5 shadow-sm ${toneClass}`}>
      <p className="text-sm font-medium text-muted">{label}</p>
      <p className="mt-2 text-3xl font-semibold text-ink">{value}</p>
    </section>
  );
}

function StatusPill({ ok, label }: { ok: boolean; label: string }) {
  return (
    <span className={`inline-flex rounded-full px-3 py-1 text-sm font-semibold ${ok ? "bg-emerald-100 text-emerald-800" : "bg-red-100 text-red-800"}`}>
      {label}
    </span>
  );
}

function ChartPanel({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
      <h2 className="text-lg font-semibold text-ink">{title}</h2>
      <div className="mt-4 h-72">{children}</div>
    </section>
  );
}

export function OverviewView() {
  const { data, error, loading } = useAdminResource<Overview>("metrics/overview");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return state;
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Users" value={numberFormatter.format(data.usersTotal)} />
        <MetricCard label="Households" value={numberFormatter.format(data.householdsTotal)} />
        <MetricCard label="Active households 7d" value={numberFormatter.format(data.householdsActive7d)} tone="good" />
        <MetricCard label="Active households 30d" value={numberFormatter.format(data.householdsActive30d)} />
        <MetricCard label="Items created 7d" value={numberFormatter.format(data.itemsCreated7d)} />
        <MetricCard label="Items completed 7d" value={numberFormatter.format(data.itemsCompleted7d)} />
        <MetricCard label="Tasks completed 7d" value={numberFormatter.format(data.tasksCompleted7d)} />
        <MetricCard label="Voice items 7d" value={numberFormatter.format(data.voiceItemsCreated7d)} />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Voice</h2>
          <dl className="mt-4 grid grid-cols-3 gap-4">
            <Stat label="Requests" value={data.voiceParseRequests7d} />
            <Stat label="Success" value={data.voiceParseSuccess7d} />
            <Stat label="Failed" value={data.voiceParseFailures7d} />
          </dl>
          <p className="mt-4 text-sm text-muted">Estimated cost: {formatMicros(data.estimatedOpenAiCostMicros7d)}</p>
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Backup</h2>
          <BackupSummary backup={data.lastBackup} />
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">System</h2>
          <HealthSummary health={data.health} />
        </section>
      </div>
    </div>
  );
}

export function UsageView() {
  const path = useMemo(() => `metrics/usage?${dateRange(14)}`, []);
  const { data, error, loading } = useAdminResource<UsageMetrics>(path);
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return state;
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Items created" value={sum(data.days, "itemsCreated")} />
        <MetricCard label="Items completed" value={sum(data.days, "itemsCompleted")} />
        <MetricCard label="Tasks created" value={sum(data.days, "tasksCreated")} />
        <MetricCard label="Tasks completed" value={sum(data.days, "tasksCompleted")} />
      </div>
      <ChartPanel title="Daily Usage">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data.days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={(value: string) => dateFormatter.format(new Date(value))} />
            <YAxis tickFormatter={(value: number) => compactFormatter.format(value)} />
            <Tooltip />
            <Bar dataKey="itemsCreated" fill="#0b7a5f" name="Items created" />
            <Bar dataKey="itemsCompleted" fill="#246b8f" name="Items completed" />
            <Bar dataKey="tasksCompleted" fill="#b57b11" name="Tasks completed" />
          </BarChart>
        </ResponsiveContainer>
      </ChartPanel>
    </div>
  );
}

export function VoiceView() {
  const path = useMemo(() => `metrics/voice?${dateRange(14)}`, []);
  const { data, error, loading } = useAdminResource<VoiceMetrics>(path);
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return state;
  }

  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Requests" value={data.requests} />
        <MetricCard label="Success" value={data.successes} tone="good" />
        <MetricCard label="Failed" value={data.failures} tone={data.failures > 0 ? "warn" : "default"} />
        <MetricCard label="Parsed items" value={data.parsedItems} />
        <MetricCard label="Items created" value={data.voiceItemsCreated} />
      </div>
      <ChartPanel title={`Voice Activity (${formatMicros(data.estimatedCostMicros)})`}>
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data.days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={(value: string) => dateFormatter.format(new Date(value))} />
            <YAxis tickFormatter={(value: number) => compactFormatter.format(value)} />
            <Tooltip />
            <Line type="monotone" dataKey="requests" stroke="#0b7a5f" strokeWidth={2} name="Requests" />
            <Line type="monotone" dataKey="voiceItemsCreated" stroke="#246b8f" strokeWidth={2} name="Items created" />
            <Line type="monotone" dataKey="failures" stroke="#b57b11" strokeWidth={2} name="Failures" />
          </LineChart>
        </ResponsiveContainer>
      </ChartPanel>
    </div>
  );
}

export function BackupsView() {
  const { data, error, loading } = useAdminResource<BackupRun[]>("backups/runs?limit=30");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return state;
  }

  return (
    <section className="overflow-hidden rounded-lg border border-line bg-white shadow-sm">
      <table className="w-full min-w-[860px] border-collapse text-left text-sm">
        <thead className="bg-surface text-muted">
          <tr>
            <th className="px-4 py-3 font-semibold">Started</th>
            <th className="px-4 py-3 font-semibold">Type</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Verification</th>
            <th className="px-4 py-3 font-semibold">Size</th>
            <th className="px-4 py-3 font-semibold">File</th>
          </tr>
        </thead>
        <tbody>
          {data.map((backup) => (
            <tr className="border-t border-line" key={backup.id}>
              <td className="px-4 py-3">{new Date(backup.startedAt).toLocaleString()}</td>
              <td className="px-4 py-3">{backup.backupType}</td>
              <td className="px-4 py-3">{backup.status}</td>
              <td className="px-4 py-3">{backup.verificationStatus}</td>
              <td className="px-4 py-3">{formatBytes(backup.sizeBytes)}</td>
              <td className="max-w-sm truncate px-4 py-3">{backup.fileName ?? "None"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}

export function SystemView() {
  const { data, error, loading } = useAdminResource<SystemHealth>("system/health");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return state;
  }

  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <MetricCard label="API" value={data.apiHealthy ? "Healthy" : "Unhealthy"} tone={data.apiHealthy ? "good" : "warn"} />
      <MetricCard label="Database" value={data.databaseHealthy ? "Healthy" : "Unhealthy"} tone={data.databaseHealthy ? "good" : "warn"} />
      <MetricCard label="Disk free" value={formatBytes(data.diskFreeBytes)} />
      <MetricCard label="PostgreSQL data" value={formatBytes(data.postgresDataSizeBytes)} />
      <section className="rounded-lg border border-line bg-white p-5 shadow-sm md:col-span-2 xl:col-span-4">
        <h2 className="text-lg font-semibold text-ink">Versions</h2>
        <dl className="mt-4 grid gap-4 md:grid-cols-3">
          <Stat label="Backend" value={data.backendVersion ?? "Unknown"} />
          <Stat label="Android" value={data.androidVersion ?? "Unknown"} />
          <Stat label="Last deploy" value={data.lastDeployAt ? new Date(data.lastDeployAt).toLocaleString() : "Unknown"} />
        </dl>
      </section>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string | number }) {
  return (
    <div>
      <dt className="text-sm font-medium text-muted">{label}</dt>
      <dd className="mt-1 text-xl font-semibold text-ink">{typeof value === "number" ? numberFormatter.format(value) : value}</dd>
    </div>
  );
}

function BackupSummary({ backup }: { backup: BackupRun | null }) {
  if (!backup) {
    return <p className="mt-4 text-sm text-muted">No backup run recorded.</p>;
  }

  return (
    <div className="mt-4 space-y-3 text-sm">
      <StatusPill ok={backup.status === "Success"} label={backup.status} />
      <p className="text-muted">{new Date(backup.startedAt).toLocaleString()}</p>
      <p className="text-muted">{formatBytes(backup.sizeBytes)}</p>
    </div>
  );
}

function HealthSummary({ health }: { health: SystemHealth }) {
  return (
    <div className="mt-4 flex flex-wrap gap-2">
      <StatusPill ok={health.apiHealthy} label="API" />
      <StatusPill ok={health.databaseHealthy} label="Database" />
    </div>
  );
}

function sum<T extends Record<string, unknown>>(items: T[], key: keyof T) {
  return numberFormatter.format(
    items.reduce((total, item) => total + (typeof item[key] === "number" ? item[key] : 0), 0),
  );
}
