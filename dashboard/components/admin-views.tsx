"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { ReactNode, useEffect, useMemo, useState } from "react";
import { useAdminToken } from "@/components/admin-shell";
import type { BackupRun, McpOverview, OpenAiMetrics, Overview, SystemHealth, SystemHistory, UsageMetrics, VoiceMetrics } from "@/lib/types";

const numberFormatter = new Intl.NumberFormat("en-US");
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

const DailyUsageChart = dynamic(() => import("@/components/admin-charts").then((module) => module.DailyUsageChart), { loading: ChartFallback, ssr: false });
const CompletionSourceChart = dynamic(() => import("@/components/admin-charts").then((module) => module.CompletionSourceChart), { loading: ChartFallback, ssr: false });
const AiCallsChart = dynamic(() => import("@/components/admin-charts").then((module) => module.AiCallsChart), { loading: ChartFallback, ssr: false });
const VoiceActivityChart = dynamic(() => import("@/components/admin-charts").then((module) => module.VoiceActivityChart), { loading: ChartFallback, ssr: false });
const DailyMcpCallsChart = dynamic(() => import("@/components/admin-charts").then((module) => module.DailyMcpCallsChart), { loading: ChartFallback, ssr: false });
const SystemTrendsChart = dynamic(() => import("@/components/admin-charts").then((module) => module.SystemTrendsChart), { loading: ChartFallback, ssr: false });

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

function useDateRange(defaultDays = 14): { days: number; query: string; label: string; setRangeDays: (days: number) => void } {
  const router = useRouter();
  const searchParams = useSearchParams();
  const days = normalizeRangeDays(searchParams.get("days"), defaultDays);

  return useMemo(() => {
    const range = createDateRange(days);

    return {
      days,
      query: range.query,
      label: range.label,
      setRangeDays: (nextDays: number) => {
        const nextParams = new URLSearchParams(searchParams.toString());
        nextParams.set("days", String(normalizeRangeDays(String(nextDays), defaultDays)));
        router.replace(`?${nextParams.toString()}`, { scroll: false });
      },
    };
  }, [days, defaultDays, router, searchParams]);
}

function normalizeRangeDays(value: string | null, fallback: number) {
  const parsed = value === null ? fallback : Number.parseInt(value, 10);
  return [7, 14, 30, 90].includes(parsed) ? parsed : fallback;
}

function createDateRange(days: number): { query: string; label: string } {
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

function formatPercent(value: number, total: number, emptyLabel = "N/A") {
  if (total === 0) {
    return emptyLabel;
  }

  return `${Math.round((value / total) * 100)}%`;
}

function formatRatio(value: number) {
  return `${Math.round(value * 100)}%`;
}

function formatRatioWithDenominator(value: number, denominator: number, emptyLabel: string) {
  return denominator === 0 ? emptyLabel : formatRatio(value);
}

function successRateTone(denominator: number, failures: number): "default" | "good" | "warn" {
  if (denominator === 0) {
    return "default";
  }

  return failures === 0 ? "good" : "warn";
}

function formatSignedRatio(value: number) {
  return `${Math.round(value * 100)}%`;
}

function formatLatency(value: number | null) {
  return value === null ? "Unknown" : `${Math.round(value)} ms`;
}

function formatNullableDateTime(value: string | null) {
  return value ? new Date(value).toLocaleString() : "None";
}

function formatBackupDate(value: string | null) {
  return value ? new Date(value).toLocaleString() : "None";
}

function isVerifiedBackup(backup: BackupRun) {
  return backup.status === "Success" && (backup.verificationStatus === "PgRestoreListOk" || backup.verificationStatus === "RestoreOk");
}

function compareBackupsNewestFirst(left: BackupRun, right: BackupRun) {
  return new Date(right.startedAt).getTime() - new Date(left.startedAt).getTime();
}

function getBackupPosture(backups: BackupRun[]) {
  const orderedBackups = [...backups].sort(compareBackupsNewestFirst);
  const latestBackup = orderedBackups[0] ?? null;
  const latestVerified = orderedBackups.find(isVerifiedBackup) ?? null;
  const lastFailure = orderedBackups.find((backup) => backup.status === "Failed") ?? null;
  const successfulRuns = backups.filter((backup) => backup.status === "Success").length;
  const failedRuns = backups.filter((backup) => backup.status === "Failed").length;
  const current = latestBackup !== null && isVerifiedBackup(latestBackup);

  return {
    current,
    failedRuns,
    latestBackup,
    latestVerified,
    lastFailure,
    postureLabel: current ? "Current" : "Attention needed",
    successfulRuns,
  };
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
  rangeControl,
}: {
  title: string;
  description: string;
  rangeLabel?: string;
  lastUpdatedAt: Date | null;
  loading: boolean;
  onRefresh: () => void;
  rangeControl?: ReactNode;
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
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        {rangeControl}
        <button
          className="inline-flex items-center justify-center rounded-md border border-line bg-white px-3 py-2 text-sm font-semibold text-ink disabled:cursor-not-allowed disabled:opacity-50"
          disabled={loading}
          onClick={onRefresh}
          type="button"
        >
          {loading ? "Refreshing" : "Refresh"}
        </button>
      </div>
    </header>
  );
}

function RangeControl({ selectedDays, setRangeDays }: { selectedDays: number; setRangeDays: (days: number) => void }) {
  return (
    <div className="inline-flex rounded-md border border-line bg-surface p-1">
      {[7, 14, 30, 90].map((days) => (
        <button
          className={`rounded px-2.5 py-1.5 text-sm font-semibold ${selectedDays === days ? "bg-white text-ink shadow-sm" : "text-muted"}`}
          key={days}
          onClick={() => setRangeDays(days)}
          type="button"
        >
          {days}d
        </button>
      ))}
    </div>
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

function HealthBadge({ ok, label }: { ok: boolean; label: string }) {
  return (
    <div className={`rounded-md border p-3 ${ok ? "border-emerald-200 bg-emerald-50" : "border-red-200 bg-red-50"}`}>
      <p className="text-sm font-semibold text-ink">{label}</p>
      <p className={`mt-1 text-sm ${ok ? "text-emerald-800" : "text-red-800"}`}>{ok ? "Healthy" : "Unhealthy"}</p>
    </div>
  );
}

function RiskItemList({ items }: { items: Overview["risk"]["items"] }) {
  if (items.length === 0) {
    return <p className="mt-4 text-sm text-muted">No action needed in the current risk window.</p>;
  }

  return (
    <ul className="mt-4 divide-y divide-line">
      {items.map((item) => (
        <li className="flex flex-col gap-3 py-3 sm:flex-row sm:items-start sm:justify-between" key={item.key}>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <span className={`rounded-full px-2 py-1 text-xs font-semibold ${item.severity === "Critical" ? "bg-red-100 text-red-800" : "bg-amber-100 text-amber-800"}`}>
                {item.severity}
              </span>
              <p className="font-semibold text-ink">{item.label}</p>
            </div>
            <p className="mt-1 text-sm text-muted">{item.detail}</p>
          </div>
          {item.targetPath ? <DeepLink href={item.targetPath} label="Open" /> : null}
        </li>
      ))}
    </ul>
  );
}

function DeepLink({ href, label }: { href: string; label: string }) {
  return (
    <Link className="mt-4 inline-flex text-sm font-semibold text-brand hover:underline" href={href}>
      {label}
    </Link>
  );
}

function ChartFallback() {
  return <div className="flex min-h-[18rem] items-center justify-center text-sm text-muted">Loading chart</div>;
}

function ChartPanel({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
      <h2 className="text-lg font-semibold text-ink">{title}</h2>
      <div className="mt-4 min-h-[18rem] min-w-0">{children}</div>
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

type ResponsiveDataColumn<T> = {
  header: string;
  className?: string;
  render: (item: T) => ReactNode;
};

function ResponsiveDataTable<T>({
  items,
  columns,
  keyForItem,
  minWidth = "920px",
}: {
  items: T[];
  columns: ResponsiveDataColumn<T>[];
  keyForItem: (item: T) => string;
  minWidth?: string;
}) {
  return (
    <>
      <div className="hidden md:block overflow-x-auto rounded-lg border border-line bg-white shadow-sm">
        <table className="w-full border-collapse text-left text-sm" style={{ minWidth }}>
          <thead className="bg-surface text-muted">
            <tr>
              {columns.map((column, index) => (
                <th className={`px-4 py-3 font-semibold ${index === 0 ? "sticky left-0 z-10 bg-surface" : ""} ${column.className ?? ""}`} key={column.header}>
                  {column.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr className="border-t border-line" key={keyForItem(item)}>
                {columns.map((column, index) => (
                  <td className={`px-4 py-3 ${index === 0 ? "sticky left-0 z-10 bg-white" : ""} ${column.className ?? ""}`} key={column.header}>
                    {column.render(item)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="space-y-3 md:hidden">
        {items.map((item) => (
          <article className="rounded-lg border border-line bg-white p-4 shadow-sm" key={keyForItem(item)}>
            <dl className="space-y-3">
              {columns.map((column) => (
                <div className="grid grid-cols-[7rem_minmax(0,1fr)] gap-3 text-sm" key={column.header}>
                  <dt className="font-medium text-muted">{column.header}</dt>
                  <dd className="min-w-0 overflow-hidden break-all text-ink">{column.render(item)}</dd>
                </div>
              ))}
            </dl>
          </article>
        ))}
      </div>
    </>
  );
}

export function OverviewView() {
  const range = useDateRange(14);
  const overview = useAdminResource<Overview>("metrics/overview");
  const systemHistory = useAdminResource<SystemHistory>(`system/history?${range.query}`);
  const { data, error, lastUpdatedAt, loading, refresh } = overview;
  const combinedLoading = loading || systemHistory.loading;
  const combinedLastUpdatedAt = lastUpdatedAt ?? systemHistory.lastUpdatedAt;
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="Overview"
          description="Operations risk, product activity, AI reliability, MCP reliability, backup posture, and system trends."
          rangeLabel={range.label}
          lastUpdatedAt={combinedLastUpdatedAt}
          loading={combinedLoading}
          onRefresh={() => {
            refresh();
            systemHistory.refresh();
          }}
          rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
        />
        {state}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <ViewHeader
        title="Overview"
        description="Operations risk, product activity, AI reliability, MCP reliability, backup posture, and system trends."
        rangeLabel={range.label}
        lastUpdatedAt={combinedLastUpdatedAt}
        loading={combinedLoading}
        onRefresh={() => {
          refresh();
          systemHistory.refresh();
        }}
        rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
      />
      <div className="grid gap-4 xl:grid-cols-[1.15fr_0.85fr]">
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-ink">Operations Risk</h2>
              <p className="mt-1 text-sm text-muted">Critical and warning signals across runtime, reliability, storage, and backups.</p>
            </div>
            <div className="flex gap-2">
              <span className="rounded-full bg-red-100 px-3 py-1 text-sm font-semibold text-red-800">
                {data.risk.criticalCount} critical
              </span>
              <span className="rounded-full bg-amber-100 px-3 py-1 text-sm font-semibold text-amber-800">
                {data.risk.warningCount} warnings
              </span>
            </div>
          </div>
          <div className="mt-5 grid gap-3 md:grid-cols-4">
            <HealthBadge ok={data.health.apiHealthy} label="API" />
            <HealthBadge ok={data.health.databaseHealthy} label="Database" />
            <HealthBadge ok={data.mcp.mcpHealthy} label="MCP" />
            <HealthBadge ok={data.mcp.authHealthy} label="Auth" />
          </div>
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Action Needed</h2>
          <RiskItemList items={data.risk.items} />
        </section>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Users" value={numberFormatter.format(data.usersTotal)} />
        <MetricCard label="New users 7d" value={numberFormatter.format(data.growth.newUsers7d)} />
        <MetricCard label="Households" value={numberFormatter.format(data.householdsTotal)} />
        <MetricCard label="Active households 7d" value={numberFormatter.format(data.householdsActive7d)} tone="good" />
        <MetricCard label="Active rate 7d" value={formatSignedRatio(data.growth.activeHouseholdRate7d)} />
        <MetricCard label="Items completed 7d" value={numberFormatter.format(data.engagement.itemsCompleted7d)} />
        <MetricCard label="AI failure rate 7d" value={formatRatio(data.aiReliability.failureRate7d)} tone={data.aiReliability.failures7d > 0 ? "warn" : "default"} />
        <MetricCard
          label="Voice success rate 7d"
          value={formatRatioWithDenominator(data.voiceReliability.successRate7d, data.voiceReliability.requests7d, "No voice requests")}
          tone={successRateTone(data.voiceReliability.requests7d, data.voiceReliability.failures7d)}
        />
      </div>

      <div className="grid gap-4 lg:grid-cols-4">
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">AI</h2>
          <dl className="mt-4 grid grid-cols-3 gap-4">
            <Stat label="Requests" value={data.aiReliability.requests7d} />
            <Stat label="Failures" value={data.aiReliability.failures7d} />
            <Stat label="Latency" value={formatLatency(data.aiReliability.averageLatencyMs7d)} />
          </dl>
          <p className="mt-4 text-sm text-muted">Estimated cost: {formatMicros(data.aiReliability.estimatedCostMicros7d)}</p>
          <DeepLink href="/openai" label="Open AI view" />
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
          <p className="mt-4 text-sm text-muted">Success rate: {formatRatioWithDenominator(data.mcp.successRate24h, data.mcp.toolCalls24h, "No MCP calls")}</p>
          <DeepLink href="/mcp" label="Open MCP view" />
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Backup</h2>
          <BackupSummary backup={data.lastBackup} />
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Stat label="Success 30d" value={data.backupHealth.successes30d} />
            <Stat label="Failed 30d" value={data.backupHealth.failures30d} />
          </dl>
          <DeepLink href="/backups" label="Open Backups view" />
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">System</h2>
          <HealthSummary health={data.health} />
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Stat label="Disk free" value={formatBytes(data.health.diskFreeBytes)} />
            <Stat label="Uptime" value={formatDuration(data.health.uptimeSeconds)} />
          </dl>
          <DeepLink href="/system" label="Open System view" />
        </section>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Growth</h2>
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Stat label="New households" value={data.growth.newHouseholds7d} />
            <Stat label="New lists" value={data.growth.newLists7d} />
            <Stat label="Active 7d" value={formatRatio(data.growth.activeHouseholdRate7d)} />
            <Stat label="Active 30d" value={formatRatio(data.growth.activeHouseholdRate30d)} />
          </dl>
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Engagement</h2>
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Stat label="Items created" value={data.engagement.itemsCreated7d} />
            <Stat label="Items completed" value={data.engagement.itemsCompleted7d} />
            <Stat label="Tasks created" value={data.engagement.tasksCreated7d} />
            <Stat label="Tasks completed" value={data.engagement.tasksCompleted7d} />
          </dl>
          <p className="mt-4 text-sm text-muted">Item completion ratio: {formatRatio(data.engagement.itemCompletionRatio7d)}</p>
        </section>
        <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
          <h2 className="text-lg font-semibold text-ink">Voice Reliability</h2>
          <dl className="mt-4 grid grid-cols-2 gap-4">
            <Stat label="Requests" value={data.voiceReliability.requests7d} />
            <Stat label="Failures" value={data.voiceReliability.failures7d} />
            <Stat label="Parsed items" value={data.voiceReliability.parsedItems7d} />
            <Stat label="Created items" value={data.voiceReliability.itemsCreated7d} />
          </dl>
        </section>
      </div>

      <ChartPanel title="System Trends">
        {systemHistory.data?.samples.length ? (
          <SystemTrendsChart samples={systemHistory.data.samples} />
        ) : (
          <EmptyState title="No system history yet" description="System history appears after the background sampler records its first 15-minute snapshot." />
        )}
      </ChartPanel>
    </div>
  );
}

export function UsageView() {
  const range = useDateRange(14);
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
          rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
        rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
  const range = useDateRange(14);
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
          rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
        rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
            value={formatPercent(voiceData.successes, voiceData.requests, "No voice requests")}
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
        <ResponsiveDataTable
          columns={[
            { header: "Feature", render: (operation) => operation.feature },
            { header: "Operation", render: (operation) => operation.operation },
            {
              header: "Model",
              className: "max-w-48 truncate",
              render: (operation) => (
                <span title={operation.model ?? "Unknown"}>
                  {operation.model ?? "Unknown"}
                </span>
              ),
            },
            { header: "Requests", render: (operation) => numberFormatter.format(operation.requests) },
            { header: "Failures", render: (operation) => numberFormatter.format(operation.failures) },
            { header: "Input", render: (operation) => numberFormatter.format(operation.inputTokens) },
            { header: "Output", render: (operation) => numberFormatter.format(operation.outputTokens) },
            { header: "Cost", render: (operation) => formatMicros(operation.estimatedCostMicros) },
            { header: "Latency", render: (operation) => formatLatency(operation.averageLatencyMs) },
          ]}
          items={data.operations}
          keyForItem={(operation) => `${operation.feature}-${operation.operation}-${operation.model ?? "none"}`}
          minWidth="960px"
        />
      )}
    </div>
  );
}

export function McpView() {
  const range = useDateRange(14);
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
          rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
        rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Tool calls" value={numberFormatter.format(overview.usage.invocations)} />
        <MetricCard
          label="Success rate"
          value={formatRatioWithDenominator(overview.usage.successRate, overview.usage.invocations, "No MCP calls")}
          tone={successRateTone(overview.usage.invocations, overview.usage.failures)}
        />
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
        <ResponsiveDataTable
          columns={[
            { header: "Tool", className: "font-mono text-xs", render: (tool) => tool.toolName },
            { header: "Calls", render: (tool) => numberFormatter.format(tool.invocations) },
            { header: "Success", render: (tool) => numberFormatter.format(tool.successes) },
            { header: "Failed", render: (tool) => numberFormatter.format(tool.failures) },
            { header: "Avg latency", render: (tool) => formatLatency(tool.averageLatencyMs) },
            { header: "P95 latency", render: (tool) => (tool.p95LatencyMs === null ? "Unknown" : `${tool.p95LatencyMs} ms`) },
            { header: "Last invocation", render: (tool) => formatNullableDateTime(tool.lastInvocationAt) },
          ]}
          items={overview.tools}
          keyForItem={(tool) => tool.toolName}
          minWidth="920px"
        />
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

      <section className="space-y-4">
        <h2 className="text-lg font-semibold text-ink">Recent Invocations</h2>
        {overview.recentInvocations.length === 0 ? (
          <EmptyState title="No recent invocations" description="Recent MCP calls will appear here without prompts or full tool arguments." />
        ) : (
          <ResponsiveDataTable
            columns={[
              { header: "Time", render: (invocation) => new Date(invocation.createdAt).toLocaleString() },
              { header: "Tool", className: "font-mono text-xs", render: (invocation) => invocation.toolName },
              { header: "Status", render: (invocation) => <StatusPill ok={invocation.status === "Success"} label={invocation.status} /> },
              { header: "Latency", render: (invocation) => `${invocation.latencyMs} ms` },
              { header: "Error", render: (invocation) => invocation.errorType ?? "None" },
              { header: "User", className: "font-mono text-xs", render: (invocation) => invocation.userId },
              { header: "Household", className: "font-mono text-xs", render: (invocation) => invocation.householdId ?? "None" },
            ]}
            items={overview.recentInvocations}
            keyForItem={(invocation) => `${invocation.createdAt}-${invocation.toolName}-${invocation.latencyMs}`}
            minWidth="960px"
          />
        )}
      </section>

      <section className="space-y-4">
        <h2 className="text-lg font-semibold text-ink">Tool Catalog</h2>
        <ResponsiveDataTable
          columns={[
            { header: "Tool", className: "font-mono text-xs", render: (tool) => tool.name },
            { header: "Title", render: (tool) => tool.title },
            { header: "Scopes", render: (tool) => tool.requiredScopes.join(", ") },
            {
              header: "Annotations",
              render: (tool) => (
                <>
                  readOnly={String(tool.readOnlyHint)}, destructive={String(tool.destructiveHint)}, idempotent={String(tool.idempotentHint)}, openWorld={String(tool.openWorldHint)}
                </>
              ),
            },
          ]}
          items={overview.toolCatalog}
          keyForItem={(tool) => tool.name}
          minWidth="920px"
        />
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
        <>
          <BackupPostureSummary backups={data} />
          <ResponsiveDataTable
            columns={[
              { header: "Started", render: (backup) => new Date(backup.startedAt).toLocaleString() },
              { header: "Type", render: (backup) => backup.backupType },
              { header: "Status", render: (backup) => <StatusPill ok={backup.status === "Success"} label={backup.status} /> },
              { header: "Verification", render: (backup) => backup.verificationStatus },
              { header: "Size", render: (backup) => formatBytes(backup.sizeBytes) },
              {
                header: "File",
                className: "max-w-sm truncate",
                render: (backup) => (
                  <span title={backup.fileName ?? "None"}>
                    {backup.fileName ?? "None"}
                  </span>
                ),
              },
              {
                header: "SHA256",
                className: "max-w-[14rem] truncate font-mono text-xs",
                render: (backup) => (
                  <span title={backup.sha256 ?? "None"}>
                    {backup.sha256 ?? "None"}
                  </span>
                ),
              },
              {
                header: "Download",
                render: (backup) => (
                  <button
                    className="rounded-md border border-line bg-white px-3 py-2 text-sm font-semibold text-ink disabled:cursor-not-allowed disabled:opacity-50"
                    disabled={backup.status !== "Success" || !backup.fileName}
                    onClick={() => void downloadBackup(backup)}
                    type="button"
                  >
                    Download
                  </button>
                ),
              },
            ]}
            items={data}
            keyForItem={(backup) => backup.id}
            minWidth="980px"
          />
        </>
      )}
    </section>
  );
}

export function SystemView() {
  const range = useDateRange(14);
  const health = useAdminResource<SystemHealth>("system/health");
  const systemHistory = useAdminResource<SystemHistory>(`system/history?${range.query}`);
  const { data, error, lastUpdatedAt, loading, refresh } = health;
  const combinedLoading = loading || systemHistory.loading;
  const combinedLastUpdatedAt = lastUpdatedAt ?? systemHistory.lastUpdatedAt;
  const state = <PageState loading={loading} error={error} />;
  if (loading || error || !data) {
    return (
      <div className="space-y-6">
        <ViewHeader
          title="System"
          description="Runtime, database, host, deploy, and resource health."
          rangeLabel={range.label}
          lastUpdatedAt={combinedLastUpdatedAt}
          loading={combinedLoading}
          onRefresh={() => {
            refresh();
            systemHistory.refresh();
          }}
          rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
        rangeLabel={range.label}
        lastUpdatedAt={combinedLastUpdatedAt}
        loading={combinedLoading}
        onRefresh={() => {
          refresh();
          systemHistory.refresh();
        }}
        rangeControl={<RangeControl selectedDays={range.days} setRangeDays={range.setRangeDays} />}
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
      <ChartPanel title="System Trends">
        {systemHistory.data?.samples.length ? (
          <SystemTrendsChart samples={systemHistory.data.samples} />
        ) : (
          <EmptyState title="No system history yet" description="System history appears after the background sampler records its first 15-minute snapshot." />
        )}
      </ChartPanel>
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

function BackupPostureSummary({ backups }: { backups: BackupRun[] }) {
  const posture = getBackupPosture(backups);

  return (
    <section className="rounded-lg border border-line bg-white p-5 shadow-sm">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold text-ink">Backup Posture</h2>
          <p className="mt-1 text-sm text-muted">Latest run status, restore verification, and recent backup outcomes.</p>
        </div>
        <StatusPill ok={posture.current} label={posture.postureLabel} />
      </div>
      <dl className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-6">
        <PostureMetric label="Posture" value={posture.postureLabel} tone={posture.current ? "good" : "warn"} />
        <PostureMetric label="Latest backup" value={formatBackupDate(posture.latestBackup?.startedAt ?? null)} />
        <PostureMetric
          label="Latest verified"
          value={formatBackupDate(posture.latestVerified?.finishedAt ?? posture.latestVerified?.startedAt ?? null)}
          tone={posture.latestVerified ? "good" : "warn"}
        />
        <PostureMetric label="Last failure" value={formatBackupDate(posture.lastFailure?.startedAt ?? null)} tone={posture.lastFailure ? "warn" : "good"} />
        <PostureMetric label="Successful runs" value={posture.successfulRuns} tone={posture.successfulRuns > 0 ? "good" : "default"} />
        <PostureMetric label="Failed runs" value={posture.failedRuns} tone={posture.failedRuns > 0 ? "warn" : "default"} />
      </dl>
    </section>
  );
}

function PostureMetric({ label, value, tone = "default" }: { label: string; value: string | number; tone?: "default" | "good" | "warn" }) {
  const formatted = typeof value === "number" ? numberFormatter.format(value) : value;
  const toneClass = tone === "good" ? "border-emerald-200 bg-emerald-50" : tone === "warn" ? "border-amber-200 bg-amber-50" : "border-line bg-surface";

  return (
    <div className={`min-w-0 rounded-md border p-4 ${toneClass}`}>
      <dt className="text-xs font-semibold uppercase tracking-wide text-muted">{label}</dt>
      <dd className="mt-2 min-w-0 break-words text-base font-semibold text-ink" title={String(formatted)}>
        {formatted}
      </dd>
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
