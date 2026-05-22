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
import type { BackupRun, OpenAiMetrics, Overview, SystemHealth, UsageMetrics, VoiceMetrics } from "@/lib/types";

const numberFormatter = new Intl.NumberFormat("en-US");
const compactFormatter = new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

function useAdminResource<T>(path: string) {
  const token = useAdminToken();
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const [reloadIndex, setReloadIndex] = useState(0);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      setLoading(true);
      setError("");
      try {
        const response = await fetch(`/api/admin/${path}`, {
          headers: { "x-firebase-id-token": token },
          cache: "no-store",
        });

        if (!response.ok) {
          throw new Error(`${response.status} ${response.statusText}`);
        }

        const payload = (await response.json()) as T;
        if (!cancelled) {
          setData(payload);
          setLastUpdatedAt(new Date());
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
  }, [path, reloadIndex, token]);

  return {
    data,
    error,
    loading,
    lastUpdatedAt,
    refresh: () => setReloadIndex((current) => current + 1),
  };
}

function dateRange(days: number): { query: string; label: string } {
  const to = new Date();
  const from = new Date();
  from.setDate(to.getDate() - (days - 1));

  return {
    query: `from=${formatDate(from)}&to=${formatDate(to)}`,
    label: `${dateFormatter.format(from)} - ${dateFormatter.format(to)}`,
  };
}

function formatDate(value: Date) {
  return value.toISOString().slice(0, 10);
}

function formatMicros(value: number | null) {
  if (value === null) {
    return "Cost unavailable";
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

function formatDuration(value: number | null) {
  if (value === null) {
    return "Unknown";
  }

  const days = Math.floor(value / 86400);
  const hours = Math.floor((value % 86400) / 3600);
  return days > 0 ? `${days}d ${hours}h` : `${hours}h`;
}

function formatPercent(value: number, total: number) {
  if (total === 0) {
    return "0%";
  }

  return `${Math.round((value / total) * 100)}%`;
}

function PageState({ loading, error }: { loading: boolean; error: string }) {
  if (loading) {
    return <div className="rounded-lg border border-line bg-white p-6 text-sm text-muted">Loading data</div>;
  }

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-sm text-red-800">
        <p className="font-semibold">Could not load this view.</p>
        <p className="mt-1">{error}</p>
      </div>
    );
  }

  return null;
}

function ViewHeader({
  title,
  description,
  rangeLabel,
  lastUpdatedAt,
  loading,
  onRefresh,
}: {
  title: string;
  description: string;
  rangeLabel?: string;
  lastUpdatedAt: Date | null;
  loading: boolean;
  onRefresh: () => void;
}) {
  return (
    <header className="flex flex-col gap-3 rounded-lg border border-line bg-white p-5 shadow-sm md:flex-row md:items-center md:justify-between">
      <div>
        <h1 className="text-xl font-semibold text-ink">{title}</h1>
        <p className="mt-1 text-sm text-muted">{description}</p>
        <div className="mt-2 flex flex-wrap gap-2 text-xs font-medium text-muted">
          {rangeLabel ? <span className="rounded-full bg-surface px-2 py-1">Range: {rangeLabel}</span> : null}
          <span className="rounded-full bg-surface px-2 py-1">
            Updated: {lastUpdatedAt ? lastUpdatedAt.toLocaleTimeString() : "Not yet"}
          </span>
        </div>
      </div>
      <button
        className="inline-flex items-center justify-center rounded-md border border-line bg-white px-3 py-2 text-sm font-semibold text-ink disabled:cursor-not-allowed disabled:opacity-50"
        disabled={loading}
        onClick={onRefresh}
        type="button"
      >
        {loading ? "Refreshing" : "Refresh"}
      </button>
    </header>
  );
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
      <p className="mt-2 break-words text-3xl font-semibold text-ink">{value}</p>
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

function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <section className="rounded-lg border border-dashed border-line bg-white p-8 text-sm shadow-sm">
      <h2 className="text-base font-semibold text-ink">{title}</h2>
      <p className="mt-2 max-w-2xl text-muted">{description}</p>
    </section>
  );
}

export function OverviewView() {
  const { data, error, lastUpdatedAt, loading, refresh } = useAdminResource<Overview>("metrics/overview");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="Overview"
          description="Current operational health, household activity, AI usage, and backup status."
          lastUpdatedAt={lastUpdatedAt}
          loading={loading}
          onRefresh={refresh}
        />
        {state}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ViewHeader
        title="Overview"
        description="Current operational health, household activity, AI usage, and backup status."
        lastUpdatedAt={lastUpdatedAt}
        loading={loading}
        onRefresh={refresh}
      />
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
          <h2 className="text-lg font-semibold text-ink">AI</h2>
          <dl className="mt-4 grid grid-cols-3 gap-4">
            <Stat label="Requests" value={data.aiRequests7d} />
            <Stat label="Success" value={data.aiSuccesses7d} />
            <Stat label="Failed" value={data.aiFailures7d} />
          </dl>
          <p className="mt-4 text-sm text-muted">Estimated cost: {formatMicros(data.estimatedAiCostMicros7d)}</p>
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
  const range = useMemo(() => dateRange(14), []);
  const path = useMemo(() => `metrics/usage?${range.query}`, [range.query]);
  const { data, error, lastUpdatedAt, loading, refresh } = useAdminResource<UsageMetrics>(path);
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="Usage"
          description="Daily list and task activity across the active reporting window."
          rangeLabel={range.label}
          lastUpdatedAt={lastUpdatedAt}
          loading={loading}
          onRefresh={refresh}
        />
        {state}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ViewHeader
        title="Usage"
        description="Daily list and task activity across the active reporting window."
        rangeLabel={range.label}
        lastUpdatedAt={lastUpdatedAt}
        loading={loading}
        onRefresh={refresh}
      />
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Items created" value={sum(data.days, "itemsCreated")} />
        <MetricCard label="Items completed" value={sum(data.days, "itemsCompleted")} />
        <MetricCard label="Items reopened" value={sum(data.days, "itemsUncompleted")} />
        <MetricCard label="Items deleted" value={sum(data.days, "itemsDeleted")} />
        <MetricCard label="Completed same day" value={sum(data.days, "itemCompletionsCreatedSameDay")} />
        <MetricCard label="Completed from backlog" value={sum(data.days, "itemCompletionsFromBacklog")} />
        <MetricCard label="Tasks completed" value={sum(data.days, "tasksCompleted")} />
        <MetricCard label="Tasks deleted" value={sum(data.days, "tasksDeleted")} />
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
            <Bar dataKey="itemsUncompleted" fill="#7c3aed" name="Items reopened" />
            <Bar dataKey="itemsDeleted" fill="#b91c1c" name="Items deleted" />
            <Bar dataKey="tasksCompleted" fill="#b57b11" name="Tasks completed" />
          </BarChart>
        </ResponsiveContainer>
      </ChartPanel>
      <ChartPanel title="Completion Source">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data.days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={(value: string) => dateFormatter.format(new Date(value))} />
            <YAxis tickFormatter={(value: number) => compactFormatter.format(value)} />
            <Tooltip />
            <Bar dataKey="itemCompletionsCreatedSameDay" fill="#0b7a5f" name="Created same day" />
            <Bar dataKey="itemCompletionsFromBacklog" fill="#246b8f" name="From backlog" />
          </BarChart>
        </ResponsiveContainer>
      </ChartPanel>
    </div>
  );
}

export function OpenAiView() {
  const range = useMemo(() => dateRange(14), []);
  const openAiPath = useMemo(() => `metrics/openai?${range.query}`, [range.query]);
  const voicePath = useMemo(() => `metrics/voice?${range.query}`, [range.query]);
  const openAi = useAdminResource<OpenAiMetrics>(openAiPath);
  const voice = useAdminResource<VoiceMetrics>(voicePath);
  const loading = openAi.loading || voice.loading;
  const error = openAi.error || voice.error;
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !openAi.data || !voice.data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="AI"
          description="OpenAI costs, failures, latency, token usage, and voice funnel health."
          rangeLabel={range.label}
          lastUpdatedAt={openAi.lastUpdatedAt ?? voice.lastUpdatedAt}
          loading={loading}
          onRefresh={() => {
            openAi.refresh();
            voice.refresh();
          }}
        />
        {state}
      </div>
    );
  }

  const data = openAi.data;
  const voiceData = voice.data;

  return (
    <div className="space-y-6">
      <ViewHeader
        title="AI"
        description="OpenAI costs, failures, latency, token usage, and voice funnel health."
        rangeLabel={range.label}
        lastUpdatedAt={openAi.lastUpdatedAt ?? voice.lastUpdatedAt}
        loading={loading}
        onRefresh={() => {
          openAi.refresh();
          voice.refresh();
        }}
      />
      <section className="space-y-4">
        <h2 className="text-lg font-semibold text-ink">AI Usage</h2>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          <MetricCard label="Requests" value={data.requests} />
          <MetricCard label="Failed" value={data.failures} tone={data.failures > 0 ? "warn" : "default"} />
          <MetricCard label="Input tokens" value={numberFormatter.format(data.inputTokens)} />
          <MetricCard label="Output tokens" value={numberFormatter.format(data.outputTokens)} />
          <MetricCard label="Estimated cost" value={formatMicros(data.estimatedCostMicros)} />
          <MetricCard label="Cached tokens" value={numberFormatter.format(data.cachedTokens)} />
          <MetricCard label="Reasoning tokens" value={numberFormatter.format(data.reasoningTokens)} />
          <MetricCard label="Audio tokens" value={numberFormatter.format(data.audioTokens)} />
          <MetricCard label="Audio seconds" value={data.audioDurationSeconds.toFixed(1)} />
          <MetricCard label="Avg latency" value={data.averageLatencyMs === null ? "Unknown" : `${Math.round(data.averageLatencyMs)} ms`} />
        </div>
      </section>
      <ChartPanel title="AI Calls">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data.days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={(value: string) => dateFormatter.format(new Date(value))} />
            <YAxis tickFormatter={(value: number) => compactFormatter.format(value)} />
            <Tooltip />
            <Line type="monotone" dataKey="requests" stroke="#0b7a5f" strokeWidth={2} name="Requests" />
            <Line type="monotone" dataKey="failures" stroke="#b91c1c" strokeWidth={2} name="Failures" />
          </LineChart>
        </ResponsiveContainer>
      </ChartPanel>
      <section className="space-y-4">
        <h2 className="text-lg font-semibold text-ink">Voice Funnel</h2>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
          <MetricCard label="Voice requests" value={voiceData.requests} />
          <MetricCard
            label="Success rate"
            value={formatPercent(voiceData.successes, voiceData.requests)}
            tone={voiceData.requests > 0 && voiceData.failures === 0 ? "good" : voiceData.failures > 0 ? "warn" : "default"}
          />
          <MetricCard label="Voice failures" value={voiceData.failures} tone={voiceData.failures > 0 ? "warn" : "default"} />
          <MetricCard label="Parsed items" value={voiceData.parsedItems} />
          <MetricCard label="Items created" value={voiceData.voiceItemsCreated} />
        </div>
      </section>
      <ChartPanel title="Voice Activity">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={voiceData.days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tickFormatter={(value: string) => dateFormatter.format(new Date(value))} />
            <YAxis tickFormatter={(value: number) => compactFormatter.format(value)} />
            <Tooltip />
            <Line type="monotone" dataKey="requests" stroke="#0b7a5f" strokeWidth={2} name="Requests" />
            <Line type="monotone" dataKey="voiceItemsCreated" stroke="#246b8f" strokeWidth={2} name="Items created" />
            <Line type="monotone" dataKey="failures" stroke="#b91c1c" strokeWidth={2} name="Failures" />
          </LineChart>
        </ResponsiveContainer>
      </ChartPanel>
      {data.operations.length === 0 ? (
        <EmptyState title="No AI operation rows" description="No OpenAI usage was recorded in this range. Metrics above will update after the first tracked request." />
      ) : (
        <section className="overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
          <table className="w-full min-w-[960px] border-collapse text-left text-sm">
            <thead className="bg-surface text-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Feature</th>
                <th className="px-4 py-3 font-semibold">Operation</th>
                <th className="px-4 py-3 font-semibold">Model</th>
                <th className="px-4 py-3 font-semibold">Requests</th>
                <th className="px-4 py-3 font-semibold">Failures</th>
                <th className="px-4 py-3 font-semibold">Input</th>
                <th className="px-4 py-3 font-semibold">Output</th>
                <th className="px-4 py-3 font-semibold">Cost</th>
                <th className="px-4 py-3 font-semibold">Latency</th>
              </tr>
            </thead>
            <tbody>
              {data.operations.map((operation) => (
                <tr className="border-t border-line" key={`${operation.feature}-${operation.operation}-${operation.model ?? "none"}`}>
                  <td className="px-4 py-3">{operation.feature}</td>
                  <td className="px-4 py-3">{operation.operation}</td>
                  <td className="max-w-48 truncate px-4 py-3" title={operation.model ?? "Unknown"}>
                    {operation.model ?? "Unknown"}
                  </td>
                  <td className="px-4 py-3">{numberFormatter.format(operation.requests)}</td>
                  <td className="px-4 py-3">{numberFormatter.format(operation.failures)}</td>
                  <td className="px-4 py-3">{numberFormatter.format(operation.inputTokens)}</td>
                  <td className="px-4 py-3">{numberFormatter.format(operation.outputTokens)}</td>
                  <td className="px-4 py-3">{formatMicros(operation.estimatedCostMicros)}</td>
                  <td className="px-4 py-3">{operation.averageLatencyMs === null ? "Unknown" : `${Math.round(operation.averageLatencyMs)} ms`}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </div>
  );
}

export function BackupsView() {
  const token = useAdminToken();
  const { data, error, lastUpdatedAt, loading, refresh } = useAdminResource<BackupRun[]>("backups/runs?limit=30");
  const [downloadError, setDownloadError] = useState("");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="Backups"
          description="Recent backup runs, verification state, size, hash, and manual download actions."
          lastUpdatedAt={lastUpdatedAt}
          loading={loading}
          onRefresh={refresh}
        />
        {state}
      </div>
    );
  }

  async function downloadBackup(backup: BackupRun) {
    setDownloadError("");
    const response = await fetch(`/api/admin/backups/runs/${backup.id}/download`, {
      headers: { "x-firebase-id-token": token },
      cache: "no-store",
    });

    if (!response.ok) {
      setDownloadError(`${response.status} ${response.statusText}`);
      return;
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = backup.fileName ?? "convy-backup.dump";
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  }

  return (
    <section className="space-y-6">
      <ViewHeader
        title="Backups"
        description="Recent backup runs, verification state, size, hash, and manual download actions."
        lastUpdatedAt={lastUpdatedAt}
        loading={loading}
        onRefresh={refresh}
      />
      {downloadError ? <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">{downloadError}</div> : null}
      {data.length === 0 ? (
        <EmptyState
          title="No backup runs yet"
          description="The backup table will populate after the first scheduled or manual backup. Until then there is nothing to download or verify."
        />
      ) : (
        <div className="overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
          <table className="w-full min-w-[980px] border-collapse text-left text-sm">
            <thead className="bg-surface text-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Started</th>
                <th className="px-4 py-3 font-semibold">Type</th>
                <th className="px-4 py-3 font-semibold">Status</th>
                <th className="px-4 py-3 font-semibold">Verification</th>
                <th className="px-4 py-3 font-semibold">Size</th>
                <th className="px-4 py-3 font-semibold">File</th>
                <th className="px-4 py-3 font-semibold">SHA256</th>
                <th className="px-4 py-3 font-semibold">Download</th>
              </tr>
            </thead>
            <tbody>
              {data.map((backup) => (
                <tr className="border-t border-line" key={backup.id}>
                  <td className="px-4 py-3">{new Date(backup.startedAt).toLocaleString()}</td>
                  <td className="px-4 py-3">{backup.backupType}</td>
                  <td className="px-4 py-3">
                    <StatusPill ok={backup.status === "Success"} label={backup.status} />
                  </td>
                  <td className="px-4 py-3">{backup.verificationStatus}</td>
                  <td className="px-4 py-3">{formatBytes(backup.sizeBytes)}</td>
                  <td className="max-w-sm truncate px-4 py-3" title={backup.fileName ?? "None"}>
                    {backup.fileName ?? "None"}
                  </td>
                  <td className="max-w-[14rem] truncate px-4 py-3 font-mono text-xs" title={backup.sha256 ?? "None"}>
                    {backup.sha256 ?? "None"}
                  </td>
                  <td className="px-4 py-3">
                    <button
                      className="rounded-md border border-line bg-white px-3 py-2 text-sm font-semibold text-ink disabled:cursor-not-allowed disabled:opacity-50"
                      disabled={backup.status !== "Success" || !backup.fileName}
                      onClick={() => void downloadBackup(backup)}
                      type="button"
                    >
                      Download
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export function SystemView() {
  const { data, error, lastUpdatedAt, loading, refresh } = useAdminResource<SystemHealth>("system/health");
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="System"
          description="Runtime, database, host, deploy, and resource health."
          lastUpdatedAt={lastUpdatedAt}
          loading={loading}
          onRefresh={refresh}
        />
        {state}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ViewHeader
        title="System"
        description="Runtime, database, host, deploy, and resource health."
        lastUpdatedAt={lastUpdatedAt}
        loading={loading}
        onRefresh={refresh}
      />
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="API" value={data.apiHealthy ? "Healthy" : "Unhealthy"} tone={data.apiHealthy ? "good" : "warn"} />
        <MetricCard label="Database" value={data.databaseHealthy ? "Healthy" : "Unhealthy"} tone={data.databaseHealthy ? "good" : "warn"} />
        <MetricCard label="Disk free" value={formatBytes(data.diskFreeBytes)} />
        <MetricCard label="Disk total" value={formatBytes(data.diskTotalBytes)} />
        <MetricCard label="Memory available" value={formatBytes(data.memoryAvailableBytes)} />
        <MetricCard label="Memory total" value={formatBytes(data.memoryTotalBytes)} />
        <MetricCard label="CPU cores" value={data.processorCount} />
        <MetricCard label="Load 1m" value={data.loadAverage1m === null ? "Unknown" : data.loadAverage1m.toFixed(2)} />
        <MetricCard label="Uptime" value={formatDuration(data.uptimeSeconds)} />
        <MetricCard label="PostgreSQL data" value={formatBytes(data.postgresDataSizeBytes)} />
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm md:col-span-2 xl:col-span-4">
          <h2 className="text-lg font-semibold text-ink">Versions</h2>
          <dl className="mt-4 grid gap-4 md:grid-cols-4">
            <Stat label="Backend" value={data.backendVersion ?? "Unknown"} />
            <Stat label="Android" value={data.androidVersion ?? "Unknown"} />
            <Stat label="Release" value={data.releaseSha ? `${data.releaseSha.slice(0, 12)}...` : "Unknown"} />
            <Stat label="Last deploy" value={data.lastDeployAt ? new Date(data.lastDeployAt).toLocaleString() : "Unknown"} />
          </dl>
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm md:col-span-2 xl:col-span-4">
          <h2 className="text-lg font-semibold text-ink">Host</h2>
          <dl className="mt-4 grid gap-4 md:grid-cols-3">
            <Stat label="OS" value={data.operatingSystem ?? "Unknown"} />
            <Stat label="Architecture" value={data.architecture ?? "Unknown"} />
            <Stat label="CPU" value={data.cpuModel ?? "Unknown"} />
          </dl>
        </section>
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string | number }) {
  const formatted = typeof value === "number" ? numberFormatter.format(value) : value;

  return (
    <div className="min-w-0">
      <dt className="text-sm font-medium text-muted">{label}</dt>
      <dd className="mt-1 min-w-0 break-words text-xl font-semibold text-ink" title={String(formatted)}>
        {formatted}
      </dd>
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
