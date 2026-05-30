export type ToolResultNotification = {
  structuredContent?: unknown;
  content?: Array<{ type?: string; text?: string }>;
  isError?: boolean;
  _meta?: Record<string, unknown>;
};

export type ConvyStructuredContent = {
  data?: Record<string, unknown>;
  meta?: {
    householdId?: string | null;
    truncated?: boolean;
    selectionRequired?: boolean;
    debug?: boolean;
  };
};

export function normalizeToolResult(value: unknown): ToolResultNotification {
  if (isRecord(value) && ("structuredContent" in value || "content" in value || "isError" in value)) {
    return value as ToolResultNotification;
  }

  if (isRecord(value) && ("data" in value || "meta" in value)) {
    return { structuredContent: value };
  }

  return {};
}

export function normalizeStructuredContent(value: unknown): ConvyStructuredContent {
  return isRecord(value) ? value as ConvyStructuredContent : {};
}

export function inferActiveTool(data: Record<string, unknown>) {
  if ("households" in data && !("shoppingLists" in data)) return "convy_get_context";
  if ("shoppingLists" in data) return "convy_get_shopping_context";
  if ("pendingItems" in data || "completedItems" in data) return "convy_get_shopping_list";
  if ("pendingTasks" in data || "completedTasks" in data) return "convy_get_task_list";
  if ("activity" in data) return "convy_get_recent_activity";
  return null;
}

export function arrayOfRecords(value: unknown) {
  return Array.isArray(value) ? value.filter(isRecord) : [];
}

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function formatValue(value: unknown) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return String(value);
}
