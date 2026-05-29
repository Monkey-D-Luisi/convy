"use client";

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
import type { McpOverview, OpenAiMetrics, UsageMetrics, VoiceMetrics } from "@/lib/types";

const compactFormatter = new Intl.NumberFormat("en-US", { notation: "compact", maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat("en-US", { month: "short", day: "numeric" });

export function DailyUsageChart({ days }: { days: UsageMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Bar dataKey="itemsCreated" fill="#0b7a5f" name="Items created" />
        <Bar dataKey="itemsCompleted" fill="#246b8f" name="Items completed" />
        <Bar dataKey="itemsUncompleted" fill="#7c3aed" name="Items reopened" />
        <Bar dataKey="itemsDeleted" fill="#b91c1c" name="Items deleted" />
        <Bar dataKey="tasksCompleted" fill="#b57b11" name="Tasks completed" />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function CompletionSourceChart({ days }: { days: UsageMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Bar dataKey="itemCompletionsCreatedSameDay" fill="#0b7a5f" name="Created same day" />
        <Bar dataKey="itemCompletionsFromBacklog" fill="#246b8f" name="From backlog" />
      </BarChart>
    </ResponsiveContainer>
  );
}

export function AiCallsChart({ days }: { days: OpenAiMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart data={days}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Line type="monotone" dataKey="requests" stroke="#0b7a5f" strokeWidth={2} name="Requests" />
        <Line type="monotone" dataKey="failures" stroke="#b91c1c" strokeWidth={2} name="Failures" />
      </LineChart>
    </ResponsiveContainer>
  );
}

export function VoiceActivityChart({ days }: { days: VoiceMetrics["days"] }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <LineChart data={days}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Line type="monotone" dataKey="requests" stroke="#0b7a5f" strokeWidth={2} name="Requests" />
        <Line type="monotone" dataKey="voiceItemsCreated" stroke="#246b8f" strokeWidth={2} name="Items created" />
        <Line type="monotone" dataKey="failures" stroke="#b91c1c" strokeWidth={2} name="Failures" />
      </LineChart>
    </ResponsiveContainer>
  );
}

export function DailyMcpCallsChart({ days }: { days: McpOverview["days"] }) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={days}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tickFormatter={formatDateTick} />
        <YAxis tickFormatter={formatCompactTick} />
        <Tooltip />
        <Bar dataKey="successes" fill="#0b7a5f" name="Successes" />
        <Bar dataKey="failures" fill="#b91c1c" name="Failures" />
      </BarChart>
    </ResponsiveContainer>
  );
}

function formatDateTick(value: string) {
  return dateFormatter.format(new Date(value));
}

function formatCompactTick(value: number) {
  return compactFormatter.format(value);
}
