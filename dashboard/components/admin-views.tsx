"use client";

import dynamic from "next/dynamic";
import { ReactNode, useEffect, useMemo, useState } from "react";
import { useAdminToken } from "@/components/admin-shell";
import type { BackupRun, McpOverview, OpenAiMetrics, Overview, SystemHealth, UsageMetrics, VoiceMetrics } from "@/lib/types";

const numberFormatter = new Intl.NumberFormat("en-US");
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

const DailyUsageChart = dynamic(() => import("@/components/admin-charts").then((module) => module.DailyUsageChart), { loading: ChartFallback, ssr: false });
const CompletionSourceChart = dynamic(() => import("@/components/admin-charts").then((module) => module.CompletionSourceChart), { loading: ChartFallback, ssr: false });
const AiCallsChart = dynamic(() => import("@/components/admin-charts").then((module) => module.AiCallsChart), { loading: ChartFallback, ssr: false });
const VoiceActivityChart = dynamic(() => import("@/components/admin-charts").then((module) => module.VoiceActivityChart), { loading: ChartFallback, ssr: false });
const DailyMcpCallsChart = dynamic(() => import("@/components/admin-charts").then((module) => module.DailyMcpCallsChart), { loading: ChartFallback, ssr: false });

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

function formatRatio(value: number) {
  return `${Math.round(value * 100)}%`;
}

function formatLatency(value: number | null) {
  return value === null ? "Unknown" : `${Math.round(value)} ms`;
}

function formatNullableDateTime(value: string | null) {
  return value ? new Date(value).toLocaleString() : "None";
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

function ChartFallback() {
  return <div className="flex h-full items-center justify-center text-sm text-muted">Loading chart</div>;
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

      <div className="grid gap-4 lg:grid-cols-4">
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
          <h2 className="text-lg font-semibold text-ink">MCP</h2>
          <div className="mt-4 flex flex-wrap gap-2">
            <StatusPill ok={data.mcp.mcpHealthy} label="MCP" />
            <StatusPill ok={data.mcp.authHealthy} label="Auth" />
          </div>
          <dl className="mt-4 grid grid-cols-3 gap-4">
            <Stat label="Calls" value={data.mcp.toolCalls24h} />
            <Stat label="Success" value={data.mcp.toolSuccesses24h} />
            <Stat label="Failed" value={data.mcp.toolFailures24h} />
          </dl>
          <p className="mt-4 text-sm text-muted">Success rate: {formatRatio(data.mcp.successRate24h)}</p>
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
        <DailyUsageChart days={data.days} />
      </ChartPanel>
      <ChartPanel title="Completion Source">
        <CompletionSourceChart days={data.days} />
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
        <AiCallsChart days={data.days} />
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
        <VoiceActivityChart days={voiceData.days} />
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

export function McpView() {
  const range = useMemo(() => dateRange(14), []);
  const path = useMemo(() => `mcp/overview?${range.query}`, [range.query]);
  const { data, error, lastUpdatedAt, loading, refresh } = useAdminResource<unknown>(path);
  const payloadError = data && !isMcpOverviewPayload(data) ? "Unexpected MCP admin payload." : "";
  const state = <PageState loading={loading} error={error || payloadError} />;
  if (loading || error || payloadError || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="MCP"
          description="ChatGPT MCP runtime, OAuth adoption, tool usage, and publication readiness."
          rangeLabel={range.label}
          lastUpdatedAt={lastUpdatedAt}
          loading={loading}
          onRefresh={refresh}
        />
        {state}
      </div>
    );
  }
  const overview = data as McpOverview;

  return (
    <div className="space-y-6">
      <ViewHeader
        title="MCP"
        description="ChatGPT MCP runtime, OAuth adoption, tool usage, and publication readiness."
        rangeLabel={range.label}
        lastUpdatedAt={lastUpdatedAt}
        loading={loading}
        onRefresh={refresh}
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Tool calls" value={numberFormatter.format(overview.usage.invocations)} />
        <MetricCard label="Success rate" value={formatRatio(overview.usage.successRate)} tone={overview.usage.failures === 0 ? "good" : "warn"} />
        <MetricCard label="Failures" value={numberFormatter.format(overview.usage.failures)} tone={overview.usage.failures > 0 ? "warn" : "default"} />
        <MetricCard label="Avg latency" value={formatLatency(overview.usage.averageLatencyMs)} />
        <MetricCard label="P95 latency" value={overview.usage.p95LatencyMs === null ? "Unknown" : `${overview.usage.p95LatencyMs} ms`} />
        <MetricCard label="Active consents" value={numberFormatter.format(overview.oauth.activeConsents)} />
        <MetricCard label="Active refresh tokens" value={numberFormatter.format(overview.oauth.activeRefreshTokens)} />
        <MetricCard label="Expiring 7d" value={numberFormatter.format(overview.oauth.refreshTokensExpiring7d)} tone={overview.oauth.refreshTokensExpiring7d > 0 ? "warn" : "default"} />
        <MetricCard label="MCP health" value={overview.runtime.mcpHealthHealthy ? "Healthy" : "Unhealthy"} tone={overview.runtime.mcpHealthHealthy ? "good" : "warn"} />
        <MetricCard label="Auth health" value={overview.runtime.authHealthHealthy ? "Healthy" : "Unhealthy"} tone={overview.runtime.authHealthHealthy ? "good" : "warn"} />
      </div>

      <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
        <h2 className="text-lg font-semibold text-ink">Runtime</h2>
        <dl className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <Stat label="MCP URL" value={overview.runtime.mcpUrl} />
          <Stat label="Auth URL" value={overview.runtime.authUrl} />
          <Stat label="Issuer" value={overview.runtime.issuer} />
          <Stat label="Audience" value={overview.runtime.audience} />
        </dl>
        <div className="mt-4 flex flex-wrap gap-2">
          <StatusPill ok={overview.runtime.mcpMetadataHealthy} label="MCP metadata" />
          <StatusPill ok={overview.runtime.authMetadataHealthy} label="Auth metadata" />
          {overview.runtime.scopes.map((scope) => (
            <span className="rounded-full bg-surface px-3 py-1 text-sm font-semibold text-muted" key={scope}>
              {scope}
            </span>
          ))}
        </div>
      </section>

      <ChartPanel title="Daily MCP Calls">
        <DailyMcpCallsChart days={overview.days} />
      </ChartPanel>

      {overview.tools.length === 0 ? (
        <EmptyState title="No MCP tool usage yet" description="Tool metrics appear after ChatGPT invokes Convy MCP tools against this environment." />
      ) : (
        <section className="overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
          <table className="w-full min-w-[920px] border-collapse text-left text-sm">
            <thead className="bg-surface text-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Tool</th>
                <th className="px-4 py-3 font-semibold">Calls</th>
                <th className="px-4 py-3 font-semibold">Success</th>
                <th className="px-4 py-3 font-semibold">Failed</th>
                <th className="px-4 py-3 font-semibold">Avg latency</th>
                <th className="px-4 py-3 font-semibold">P95 latency</th>
                <th className="px-4 py-3 font-semibold">Last invocation</th>
              </tr>
            </thead>
            <tbody>
              {overview.tools.map((tool) => (
                <tr className="border-t border-line" key={tool.toolName}>
                  <td className="px-4 py-3 font-mono text-xs">{tool.toolName}</td>
                  <td className="px-4 py-3">{numberFormatter.format(tool.invocations)}</td>
                  <td className="px-4 py-3">{numberFormatter.format(tool.successes)}</td>
                  <td className="px-4 py-3">{numberFormatter.format(tool.failures)}</td>
                  <td className="px-4 py-3">{formatLatency(tool.averageLatencyMs)}</td>
                  <td className="px-4 py-3">{tool.p95LatencyMs === null ? "Unknown" : `${tool.p95LatencyMs} ms`}</td>
                  <td className="px-4 py-3">{formatNullableDateTime(tool.lastInvocationAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
        <h2 className="text-lg font-semibold text-ink">OAuth</h2>
        <dl className="mt-4 grid gap-4 md:grid-cols-4">
          <Stat label="Revoked consents" value={overview.oauth.revokedConsents} />
          <Stat label="Revoked refresh tokens" value={overview.oauth.revokedRefreshTokens} />
          <Stat label="Last consent" value={formatNullableDateTime(overview.oauth.lastConsentAt)} />
          <Stat label="Last token use" value={formatNullableDateTime(overview.oauth.lastTokenUsedAt)} />
        </dl>
      </section>

      <section className="overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
        <div className="p-5">
          <h2 className="text-lg font-semibold text-ink">Recent Invocations</h2>
        </div>
        {overview.recentInvocations.length === 0 ? (
          <div className="px-5 pb-5">
            <EmptyState title="No recent invocations" description="Recent MCP calls will appear here without prompts or full tool arguments." />
          </div>
        ) : (
          <table className="w-full min-w-[960px] border-collapse text-left text-sm">
            <thead className="bg-surface text-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Time</th>
                <th className="px-4 py-3 font-semibold">Tool</th>
                <th className="px-4 py-3 font-semibold">Status</th>
                <th className="px-4 py-3 font-semibold">Latency</th>
                <th className="px-4 py-3 font-semibold">Error</th>
                <th className="px-4 py-3 font-semibold">User</th>
                <th className="px-4 py-3 font-semibold">Household</th>
              </tr>
            </thead>
            <tbody>
              {overview.recentInvocations.map((invocation) => (
                <tr className="border-t border-line" key={`${invocation.createdAt}-${invocation.toolName}-${invocation.latencyMs}`}>
                  <td className="px-4 py-3">{new Date(invocation.createdAt).toLocaleString()}</td>
                  <td className="px-4 py-3 font-mono text-xs">{invocation.toolName}</td>
                  <td className="px-4 py-3">
                    <StatusPill ok={invocation.status === "Success"} label={invocation.status} />
                  </td>
                  <td className="px-4 py-3">{invocation.latencyMs} ms</td>
                  <td className="px-4 py-3">{invocation.errorType ?? "None"}</td>
                  <td className="px-4 py-3 font-mono text-xs">{invocation.userId}</td>
                  <td className="px-4 py-3 font-mono text-xs">{invocation.householdId ?? "None"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
        <div className="p-5">
          <h2 className="text-lg font-semibold text-ink">Tool Catalog</h2>
        </div>
        <table className="w-full min-w-[920px] border-collapse text-left text-sm">
          <thead className="bg-surface text-muted">
            <tr>
              <th className="px-4 py-3 font-semibold">Tool</th>
              <th className="px-4 py-3 font-semibold">Title</th>
              <th className="px-4 py-3 font-semibold">Scopes</th>
              <th className="px-4 py-3 font-semibold">Annotations</th>
            </tr>
          </thead>
          <tbody>
            {overview.toolCatalog.map((tool) => (
              <tr className="border-t border-line" key={tool.name}>
                <td className="px-4 py-3 font-mono text-xs">{tool.name}</td>
                <td className="px-4 py-3">{tool.title}</td>
                <td className="px-4 py-3">{tool.requiredScopes.join(", ")}</td>
                <td className="px-4 py-3">
                  readOnly={String(tool.readOnlyHint)}, destructive={String(tool.destructiveHint)}, idempotent={String(tool.idempotentHint)}, openWorld={String(tool.openWorldHint)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
        <h2 className="text-lg font-semibold text-ink">Publication Readiness</h2>
        <div className="mt-4 grid gap-3 md:grid-cols-2">
          {overview.readinessChecks.map((check) => (
            <div className="rounded-md border border-line p-4" key={check.key}>
              <div className="flex items-center justify-between gap-3">
                <h3 className="text-sm font-semibold text-ink">{check.label}</h3>
                <StatusPill ok={check.status === "Pass"} label={check.status} />
              </div>
              <p className="mt-2 text-sm text-muted">{check.details}</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

function isMcpOverviewPayload(value: unknown): value is McpOverview {
  if (!isRecord(value)) {
    return false;
  }

  return isRecord(value.runtime)
    && isRecord(value.oauth)
    && isRecord(value.usage)
    && Array.isArray(value.days)
    && Array.isArray(value.tools)
    && Array.isArray(value.recentInvocations)
    && Array.isArray(value.toolCatalog)
    && Array.isArray(value.readinessChecks)
    && typeof value.oauth.activeConsents === "number"
    && typeof value.oauth.activeRefreshTokens === "number"
    && typeof value.usage.invocations === "number"
    && typeof value.runtime.mcpUrl === "string"
    && Array.isArray(value.runtime.scopes);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
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
        <MetricCard label="MCP" value={data.mcpHealthy ? "Healthy" : "Unhealthy"} tone={data.mcpHealthy ? "good" : "warn"} />
        <MetricCard label="Auth" value={data.authHealthy ? "Healthy" : "Unhealthy"} tone={data.authHealthy ? "good" : "warn"} />
        <MetricCard label="MCP metadata" value={data.mcpMetadataHealthy ? "Reachable" : "Unreachable"} tone={data.mcpMetadataHealthy ? "good" : "warn"} />
        <MetricCard label="Auth metadata" value={data.authMetadataHealthy ? "Reachable" : "Unreachable"} tone={data.authMetadataHealthy ? "good" : "warn"} />
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
      <StatusPill ok={health.mcpHealthy} label="MCP" />
      <StatusPill ok={health.authHealthy} label="Auth" />
    </div>
  );
}

function sum<T extends Record<string, unknown>>(items: T[], key: keyof T) {
  return numberFormatter.format(
    items.reduce((total, item) => total + (typeof item[key] === "number" ? item[key] : 0), 0),
  );
}
