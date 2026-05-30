import { useEffect, useState, type ReactNode } from "react";
import {
  arrayOfRecords,
  formatValue,
  inferActiveTool,
  isRecord,
  normalizeStructuredContent,
  normalizeToolResult,
  type ConvyStructuredContent,
  type ToolResultNotification,
} from "./widget-helpers.js";

type OpenAiRuntime = {
  toolOutput?: unknown;
  toolResponseMetadata?: unknown;
};

type OpenAiGlobalsEvent = CustomEvent<{
  globals?: {
    toolOutput?: unknown;
  };
}>;

declare global {
  interface Window {
    openai?: OpenAiRuntime;
  }
}

export function ConvySummaryWidget() {
  const [result, setResult] = useState<ToolResultNotification>(() => readInitialResult());
  const structuredContent = normalizeStructuredContent(result.structuredContent);
  const data = structuredContent.data ?? {};
  const meta = structuredContent.meta ?? {};
  const activeTool = inferActiveTool(data);
  const hasError = result.isError === true;
  const showDebug = meta?.debug === true || debugEnabled(window.openai?.toolResponseMetadata);

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

  return (
    <main className="shell">
      <header className="header">
        <div>
          <p className="eyebrow">Convy</p>
          <h1>{headingFor(activeTool, hasError)}</h1>
        </div>
      </header>

      {hasError ? <ErrorState result={result} /> : <ResultView data={data} meta={meta} showDebug={showDebug} />}
    </main>
  );
}

function ResultView({
  data,
  meta,
  showDebug,
}: {
  data: Record<string, unknown>;
  meta: ConvyStructuredContent["meta"];
  showDebug: boolean;
}) {
  const households = arrayOfRecords(data.households);
  const shoppingLists = arrayOfRecords(data.shoppingLists);
  const pendingItems = arrayOfRecords(data.pendingItems);
  const completedItems = arrayOfRecords(data.completedItems);
  const pendingTasks = arrayOfRecords(data.pendingTasks);
  const completedTasks = arrayOfRecords(data.completedTasks);
  const activity = arrayOfRecords(data.activity);

  if (data.selectionRequired === true || meta?.selectionRequired === true) {
    return households.length > 0 ? (
      <Section title="Choose a household">
        <EntityList
          values={households}
          primaryKey="name"
          secondaryKeys={["createdAt"]}
          debugKeys={["id"]}
          showDebug={showDebug}
        />
      </Section>
    ) : <EmptyState />;
  }

  if (households.length > 0) {
    return (
      <Section title="Households">
        <SummaryLine label="Available" value={String(data.householdCount ?? households.length)} />
        <EntityList
          values={households}
          primaryKey="name"
          secondaryKeys={["createdAt"]}
          debugKeys={["id"]}
          showDebug={showDebug}
        />
      </Section>
    );
  }

  if (shoppingLists.length > 0) {
    return (
      <Section title="Shopping lists">
        <EntityList
          values={shoppingLists}
          primaryKey="name"
          secondaryKeys={["type"]}
          debugKeys={["id", "householdId"]}
          showDebug={showDebug}
        />
      </Section>
    );
  }

  if (pendingItems.length > 0 || completedItems.length > 0) {
    return (
      <div className="stack">
        {pendingItems.length > 0 ? (
          <Section title="Pending items">
            <EntityList
              values={pendingItems}
              primaryKey="title"
              secondaryKeys={["quantity", "unit", "note"]}
              debugKeys={["id", "listId"]}
              showDebug={showDebug}
            />
          </Section>
        ) : null}
        {completedItems.length > 0 ? (
          <Section title="Completed items">
            <EntityList
              values={completedItems}
              primaryKey="title"
              secondaryKeys={["completedByName", "completedAt"]}
              debugKeys={["id", "listId"]}
              showDebug={showDebug}
            />
          </Section>
        ) : null}
        <TruncationNotice truncated={meta?.truncated} />
      </div>
    );
  }

  if (pendingTasks.length > 0 || completedTasks.length > 0) {
    return (
      <div className="stack">
        {pendingTasks.length > 0 ? (
          <Section title="Pending tasks">
            <EntityList
              values={pendingTasks}
              primaryKey="title"
              secondaryKeys={["note", "createdByName"]}
              debugKeys={["id", "listId", "assignedToUserId"]}
              showDebug={showDebug}
            />
          </Section>
        ) : null}
        {completedTasks.length > 0 ? (
          <Section title="Completed tasks">
            <EntityList
              values={completedTasks}
              primaryKey="title"
              secondaryKeys={["completedByName", "completedAt"]}
              debugKeys={["id", "listId", "assignedToUserId"]}
              showDebug={showDebug}
            />
          </Section>
        ) : null}
        <TruncationNotice truncated={meta?.truncated} />
      </div>
    );
  }

  if (activity.length > 0) {
    return (
      <Section title="Recent activity">
        <EntityList
          values={activity}
          primaryKey="actionType"
          secondaryKeys={["entityType", "performedByName", "createdAt"]}
          debugKeys={["id", "householdId", "entityId"]}
          showDebug={showDebug}
        />
        <TruncationNotice truncated={meta?.truncated} />
      </Section>
    );
  }

  return <EmptyState />;
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
  debugKeys,
  showDebug,
}: {
  values: Record<string, unknown>[];
  primaryKey: string;
  secondaryKeys: string[];
  debugKeys: string[];
  showDebug: boolean;
}) {
  const visibleSecondaryKeys = showDebug ? [...secondaryKeys, ...debugKeys] : secondaryKeys;

  return (
    <ul className="entity-list">
      {values.map((value, index) => (
        <li key={String(value.id ?? `${primaryKey}-${index}`)}>
          <strong>{formatValue(value[primaryKey])}</strong>
          <span>{visibleSecondaryKeys.map((key) => formatValue(value[key])).filter(Boolean).join(" | ")}</span>
        </li>
      ))}
    </ul>
  );
}

function EmptyState() {
  return (
    <section className="empty">
      <h2>Convy result</h2>
      <p>The latest Convy tool returned data that can be summarized in the chat response.</p>
    </section>
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

function debugEnabled(value: unknown) {
  return isRecord(value) && value.debug === true;
}

function headingFor(activeTool: string | null, hasError: boolean) {
  if (hasError) return "Needs attention";
  if (activeTool === "convy_get_shopping_context") return "Shopping lists";
  if (activeTool === "convy_get_shopping_list") return "Shopping list";
  if (activeTool === "convy_get_task_list") return "Task list";
  if (activeTool === "convy_get_recent_activity") return "Recent activity";
  return "Household context";
}
