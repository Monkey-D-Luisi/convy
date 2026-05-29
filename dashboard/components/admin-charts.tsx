"use client";

import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import type { McpOverview, OpenAiMetrics, SystemHistory, UsageMetrics, VoiceMetrics } from "@/lib/types";

const compactFormatter = new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });
const bytesFormatter = new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 });

export const ChartPalette = {
  good: "#0b7a5f",
  compare: "#246b8f",
  caution: "#b57b11",
  danger: "#b91c1c",
  purple: "#6d41a1",
  neutral: "#64746f",
};

export function DailyUsageChart({ days }: { days: UsageMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Legend />
        <Bar dataKey="itemsCreated" fill={ChartPalette.good} isAnimationActive={false} name="Items created" />
        <Bar dataKey="itemsCompleted" fill={ChartPalette.compare} isAnimationActive={false} name="Items completed" />
        <Bar dataKey="itemsUncompleted" fill={ChartPalette.purple} isAnimationActive={false} name="Items reopened" />
        <Bar dataKey="itemsDeleted" fill={ChartPalette.danger} isAnimationActive={false} name="Items deleted" />
        <Bar dataKey="tasksCompleted" fill={ChartPalette.caution} isAnimationActive={false} name="Tasks completed" />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function CompletionSourceChart({ days }: { days: UsageMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Legend />
        <Bar dataKey="itemCompletionsCreatedSameDay" fill={ChartPalette.good} isAnimationActive={false} name="Created same day" />
        <Bar dataKey="itemCompletionsFromBacklog" fill={ChartPalette.compare} isAnimationActive={false} name="From backlog" />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function AiCallsChart({ days }: { days: OpenAiMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <LineChart data={days}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Legend />
        <Line type="monotone" dataKey="requests" isAnimationActive={false} stroke={ChartPalette.good} strokeWidth={2} name="Requests" />
        <Line type="monotone" dataKey="failures" isAnimationActive={false} stroke={ChartPalette.danger} strokeWidth={2} name="Failures" />
      </LineChart>
    </ResponsiveContainer>
  );
}

export function VoiceActivityChart({ days }: { days: VoiceMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <LineChart data={days}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Legend />
        <Line type="monotone" dataKey="requests" isAnimationActive={false} stroke={ChartPalette.good} strokeWidth={2} name="Requests" />
        <Line type="monotone" dataKey="voiceItemsCreated" isAnimationActive={false} stroke={ChartPalette.compare} strokeWidth={2} name="Items created" />
        <Line type="monotone" dataKey="failures" isAnimationActive={false} stroke={ChartPalette.danger} strokeWidth={2} name="Failures" />
      </LineChart>
    </ResponsiveContainer>
  );
}

export function DailyMcpCallsChart({ days }: { days: McpOverview["days"] }) {
  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Legend />
        <Bar dataKey="successes" fill={ChartPalette.good} isAnimationActive={false} name="Successes" />
        <Bar dataKey="failures" fill={ChartPalette.danger} isAnimationActive={false} name="Failures" />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function SystemTrendsChart({ samples }: { samples: SystemHistory["samples"] }) {
  const data = samples.map((sample) => ({
    ...sample,
    capturedAtLabel: dateFormatter.format(new Date(sample.capturedAt)),
    diskFreeGb: toGib(sample.diskFreeBytes),
    memoryAvailableGb: toGib(sample.memoryAvailableBytes),
    postgresDataMb: toMib(sample.postgresDataSizeBytes),
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <LineChart data={data}>
        <CartesianGrid strokeDasharray="3 3" stroke="#dce7e2" />
        <XAxis dataKey="capturedAt" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={(value) => bytesFormatter.format(Number(value))} />
        <Tooltip labelFormatter={(value) => new Date(String(value)).toLocaleString()} />
        <Legend />
        <Line type="monotone" dataKey="diskFreeGb" dot={false} isAnimationActive={false} stroke={ChartPalette.good} strokeWidth={2} name="Disk free GB" />
        <Line type="monotone" dataKey="memoryAvailableGb" dot={false} isAnimationActive={false} stroke={ChartPalette.compare} strokeWidth={2} name="Memory available GB" />
        <Line type="monotone" dataKey="postgresDataMb" dot={false} isAnimationActive={false} stroke={ChartPalette.caution} strokeWidth={2} name="PostgreSQL MB" />
      </LineChart>
    </ResponsiveContainer>
  );
}

function formatDateTick(value: string) {
  return dateFormatter.format(new Date(value));
}

function formatCompactTick(value: number) {
  return compactFormatter.format(value);
}

function toGib(value: number | null) {
  return value === null ? null : value / 1024 / 1024 / 1024;
}

function toMib(value: number | null) {
  return value === null ? null : value / 1024 / 1024;
}
