import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";

type OpenAiRuntime = {
  toolOutput?: unknown;
  toolResponseMetadata?: unknown;
  callTool?: (name: string, args: Record<string, unknown>) => Promise<unknown>;
};

type ToolResultNotification = {
  structuredContent?: unknown;
  content?: Array<{ type?: string; text?: string }>;
  isError?: boolean;
  _meta?: Record<string, unknown>;
};

type OpenAiGlobalsEvent = CustomEvent<{
  globals?: {
    toolOutput?: unknown;
  };
}>;

type ConvyStructuredContent = {
  data?: Record<string, unknown>;
  meta?: {
    householdId?: string | null;
    truncated?: boolean;
    selectionRequired?: boolean;
  };
};

declare global {
  interface Window {
    openai?: OpenAiRuntime;
  }
}

const refreshableReadTools = new Set([
  "convy_get_context",
  "convy_get_shopping_list",
  "convy_get_task_list",
  "convy_get_recent_activity",
]);

export function ConvySummaryWidget() {
  const [result, setResult] = useState<ToolResultNotification>(() => readInitialResult());
  const [refreshing, setRefreshing] = useState(false);
  const structuredContent = normalizeStructuredContent(result.structuredContent);
  const data = structuredContent.data ?? {};
  const meta = structuredContent.meta ?? {};
  const activeTool = inferActiveTool(data);
  const canRefresh = Boolean(window.openai?.callTool && activeTool && refreshableReadTools.has(activeTool));
  const hasError = result.isError === true;

  useEffect(() => {
    const handleMessage = (event: MessageEvent) => {
      if (event.source !== window.parent) {
        return;
      }

      const message = event.data as { jsonrpc?: string; method?: string; params?: ToolResultNotification } | undefined;
      if (!message || message.jsonrpc !== "2.0" || message.method !== "ui/notifications/tool-result") {
        return;
      }

      setResult(message.params ?? {});
    };

    const handleGlobals = (event: Event) => {
      const globals = (event as OpenAiGlobalsEvent).detail?.globals;
      if ("toolOutput" in (globals ?? {})) {
        setResult(normalizeToolResult(globals?.toolOutput ?? {}));
      }
    };

    window.addEventListener("message", handleMessage, { passive: true });
    window.addEventListener("openai:set_globals", handleGlobals, { passive: true });
    return () => {
      window.removeEventListener("message", handleMessage);
      window.removeEventListener("openai:set_globals", handleGlobals);
    };
  }, []);

  const refreshArgs = useMemo(() => createRefreshArgs(activeTool, data, meta), [activeTool, data, meta]);
  const refresh = useCallback(async () => {
    if (!activeTool || !window.openai?.callTool) {
      return;
    }

    setRefreshing(true);
    try {
      const nextResult = await window.openai.callTool(activeTool, refreshArgs);
      setResult(normalizeToolResult(nextResult));
    } finally {
      setRefreshing(false);
    }
  }, [activeTool, refreshArgs]);

  return (
    <main className="shell">
      <header className="header">
        <div>
          <p className="eyebrow">Convy</p>
          <h1>{headingFor(activeTool, hasError)}</h1>
        </div>
        {canRefresh ? (
          <button className="refresh" type="button" onClick={refresh} disabled={refreshing}>
            {refreshing ? "Refreshing" : "Refresh"}
          </button>
        ) : null}
      </header>

      {hasError ? <ErrorState result={result} /> : <ResultView data={data} meta={meta} />}
    </main>
  );
}

function ResultView({ data, meta }: { data: Record<string, unknown>; meta: ConvyStructuredContent["meta"] }) {
  if (data.selectionRequired === true || meta?.selectionRequired === true) {
    return (
      <Section title="Choose a household">
        <EntityList values={arrayOfRecords(data.households)} primaryKey="name" secondaryKeys={["id", "createdAt"]} />
      </Section>
    );
  }

  if (arrayOfRecords(data.households).length > 0) {
    return (
      <Section title="Households">
        <SummaryLine label="Available" value={String(data.householdCount ?? arrayOfRecords(data.households).length)} />
        <EntityList values={arrayOfRecords(data.households)} primaryKey="name" secondaryKeys={["id", "createdAt"]} />
      </Section>
    );
  }

  if (arrayOfRecords(data.shoppingLists).length > 0) {
    return (
      <Section title="Shopping lists">
        <EntityList values={arrayOfRecords(data.shoppingLists)} primaryKey="name" secondaryKeys={["id", "type"]} />
      </Section>
    );
  }

  if (arrayOfRecords(data.pendingItems).length > 0 || arrayOfRecords(data.completedItems).length > 0) {
    return (
      <div className="stack">
        <Section title="Pending items">
          <EntityList values={arrayOfRecords(data.pendingItems)} primaryKey="title" secondaryKeys={["quantity", "unit", "note"]} />
        </Section>
        <Section title="Completed items">
          <EntityList values={arrayOfRecords(data.completedItems)} primaryKey="title" secondaryKeys={["completedByName", "completedAt"]} emptyText="No completed items returned." />
        </Section>
        <TruncationNotice truncated={meta?.truncated} />
      </div>
    );
  }

  if (arrayOfRecords(data.pendingTasks).length > 0 || arrayOfRecords(data.completedTasks).length > 0) {
    return (
      <div className="stack">
        <Section title="Pending tasks">
          <EntityList values={arrayOfRecords(data.pendingTasks)} primaryKey="title" secondaryKeys={["note", "createdByName"]} />
        </Section>
        <Section title="Completed tasks">
          <EntityList values={arrayOfRecords(data.completedTasks)} primaryKey="title" secondaryKeys={["completedByName", "completedAt"]} emptyText="No completed tasks returned." />
        </Section>
        <TruncationNotice truncated={meta?.truncated} />
      </div>
    );
  }

  if (arrayOfRecords(data.activity).length > 0) {
    return (
      <Section title="Recent activity">
        <EntityList values={arrayOfRecords(data.activity)} primaryKey="actionType" secondaryKeys={["entityType", "performedByName", "createdAt"]} />
        <TruncationNotice truncated={meta?.truncated} />
      </Section>
    );
  }

  return (
    <section className="empty">
      <h2>Convy result</h2>
      <p>The latest Convy tool returned data that can be summarized in the chat response.</p>
    </section>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="section">
      <h2>{title}</h2>
      {children}
    </section>
  );
}

function SummaryLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="summary-line">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function EntityList({
  values,
  primaryKey,
  secondaryKeys,
  emptyText = "No records returned.",
}: {
  values: Record<string, unknown>[];
  primaryKey: string;
  secondaryKeys: string[];
  emptyText?: string;
}) {
  if (values.length === 0) {
    return <p className="muted">{emptyText}</p>;
  }

  return (
    <ul className="entity-list">
      {values.map((value, index) => (
        <li key={String(value.id ?? `${primaryKey}-${index}`)}>
          <strong>{formatValue(value[primaryKey])}</strong>
          <span>{secondaryKeys.map((key) => formatValue(value[key])).filter(Boolean).join(" | ")}</span>
        </li>
      ))}
    </ul>
  );
}

function ErrorState({ result }: { result: ToolResultNotification }) {
  const message = result.content?.find((item) => item.type === "text")?.text ?? "Convy could not return this result.";
  return (
    <section className="empty error">
      <h2>Action unavailable</h2>
      <p>{message}</p>
    </section>
  );
}

function TruncationNotice({ truncated }: { truncated?: boolean }) {
  return truncated ? <p className="notice">Some Convy records were omitted because the result was truncated.</p> : null;
}

function readInitialResult(): ToolResultNotification {
  return normalizeToolResult(window.openai?.toolOutput ?? {});
}

function normalizeToolResult(value: unknown): ToolResultNotification {
  if (isRecord(value) && ("structuredContent" in value || "content" in value || "isError" in value)) {
    return value as ToolResultNotification;
  }

  if (isRecord(value) && ("data" in value || "meta" in value)) {
    return { structuredContent: value };
  }

  return {};
}

function normalizeStructuredContent(value: unknown): ConvyStructuredContent {
  return isRecord(value) ? value as ConvyStructuredContent : {};
}

function inferActiveTool(data: Record<string, unknown>) {
  if ("households" in data && !("shoppingLists" in data)) return "convy_get_context";
  if ("pendingItems" in data || "completedItems" in data) return "convy_get_shopping_list";
  if ("pendingTasks" in data || "completedTasks" in data) return "convy_get_task_list";
  if ("activity" in data) return "convy_get_recent_activity";
  return null;
}

function createRefreshArgs(activeTool: string | null, data: Record<string, unknown>, meta: ConvyStructuredContent["meta"]) {
  if (activeTool === "convy_get_shopping_list" || activeTool === "convy_get_task_list") {
    const list = isRecord(data.list) ? data.list : {};
    return { listId: list.id, includeCompleted: true, limit: 50 };
  }

  if (activeTool === "convy_get_recent_activity") {
    return { householdId: meta?.householdId, limit: 20 };
  }

  return {};
}

function headingFor(activeTool: string | null, hasError: boolean) {
  if (hasError) return "Needs attention";
  if (activeTool === "convy_get_shopping_list") return "Shopping list";
  if (activeTool === "convy_get_task_list") return "Task list";
  if (activeTool === "convy_get_recent_activity") return "Recent activity";
  return "Household context";
}

function arrayOfRecords(value: unknown) {
  return Array.isArray(value) ? value.filter(isRecord) : [];
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return String(value);
}
