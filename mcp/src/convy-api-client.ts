import type { McpToolInvocationStatus } from "./tool-status.js";

type Fetch = typeof fetch;

export type AuditEvent = {
  userId: string;
  householdId?: string | null;
  clientId?: string | null;
  toolName: string;
  status: McpToolInvocationStatus;
  latencyMs: number;
  errorType?: string | null;
};

export type AddShoppingItemsRequest = {
  items: Array<{
    title: string;
    quantity?: number;
    unit?: string;
    note?: string;
  }>;
  idempotencyKey: string;
};

export type UpdateShoppingItemsStatusRequest = {
  itemIds: string[];
  status: "Pending" | "Completed";
  idempotencyKey: string;
};

export type AddTasksRequest = {
  tasks: Array<{
    title: string;
    note?: string;
  }>;
  idempotencyKey: string;
};

export type UpdateTasksStatusRequest = {
  taskIds: string[];
  status: "Pending" | "Completed";
  idempotencyKey: string;
};

export class ConvyApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly errorType: string,
    message: string,
  ) {
    super(message);
    this.name = "ConvyApiError";
  }
}

export class ConvyApiClient {
  private readonly baseUrl: string;
  private readonly fetchImpl: Fetch;
  private readonly auditApiKey?: string;

  constructor(options: { baseUrl: string; fetchImpl?: Fetch; auditApiKey?: string }) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, "");
    this.fetchImpl = options.fetchImpl ?? fetch;
    this.auditApiKey = options.auditApiKey;
  }

  getHouseholds(token: string) {
    return this.getJson<unknown[]>("/api/v1/households/", token);
  }

  getHousehold(token: string, householdId: string) {
    return this.getJson<Record<string, unknown>>(`/api/v1/households/${householdId}`, token);
  }

  getLists(token: string, householdId: string, includeArchived: boolean) {
    const query = new URLSearchParams({ includeArchived: includeArchived ? "true" : "false" });
    return this.getJson<unknown[]>(`/api/v1/households/${householdId}/lists/?${query}`, token);
  }

  getShoppingItems(token: string, listId: string, status?: string) {
    const query = new URLSearchParams();
    if (status) {
      query.set("status", status);
    }

    const suffix = query.size > 0 ? `?${query}` : "";
    return this.getJson<unknown[]>(`/api/v1/lists/${listId}/items/${suffix}`, token);
  }

  getTasks(token: string, listId: string, status?: string) {
    const query = new URLSearchParams();
    if (status) {
      query.set("status", status);
    }

    const suffix = query.size > 0 ? `?${query}` : "";
    return this.getJson<unknown[]>(`/api/v1/lists/${listId}/tasks/${suffix}`, token);
  }

  getRecentActivity(token: string, householdId: string, limit: number) {
    const query = new URLSearchParams({ limit: String(limit) });
    return this.getJson<unknown[]>(`/api/v1/households/${householdId}/activity/?${query}`, token);
  }

  addShoppingItems(token: string, listId: string, request: AddShoppingItemsRequest) {
    return this.postJson<Record<string, unknown>>(
      `/api/v1/lists/${listId}/items/smart-batch`,
      token,
      request.idempotencyKey,
      { items: request.items },
    );
  }

  updateShoppingItemsStatus(token: string, listId: string, request: UpdateShoppingItemsStatusRequest) {
    return this.postJson<Record<string, unknown>>(
      `/api/v1/lists/${listId}/items/status-batch`,
      token,
      request.idempotencyKey,
      {
        itemIds: request.itemIds,
        status: request.status,
      },
    );
  }

  addTasks(token: string, listId: string, request: AddTasksRequest) {
    return this.postJson<Record<string, unknown>>(
      `/api/v1/lists/${listId}/tasks/smart-batch`,
      token,
      request.idempotencyKey,
      { tasks: request.tasks },
    );
  }

  updateTasksStatus(token: string, listId: string, request: UpdateTasksStatusRequest) {
    return this.postJson<Record<string, unknown>>(
      `/api/v1/lists/${listId}/tasks/status-batch`,
      token,
      request.idempotencyKey,
      {
        taskIds: request.taskIds,
        status: request.status,
      },
    );
  }

  async recordToolInvocation(event: AuditEvent) {
    if (!this.auditApiKey) {
      return;
    }

    const response = await this.fetchImpl(`${this.baseUrl}/api/v1/mcp/audit/tool-invocations`, {
      method: "POST",
      headers: {
        "content-type": "application/json",
        "x-convy-mcp-audit-key": this.auditApiKey,
      },
      body: JSON.stringify({
        userId: event.userId,
        householdId: event.householdId ?? null,
        clientId: event.clientId ?? null,
        toolName: event.toolName,
        status: event.status,
        latencyMs: event.latencyMs,
        errorType: event.errorType ?? null,
      }),
    });

    if (!response.ok) {
      throw new ConvyApiError(response.status, mapStatusToErrorType(response.status), "MCP audit logging failed.");
    }
  }

  private async getJson<T>(path: string, token: string): Promise<T> {
    const response = await this.fetchImpl(`${this.baseUrl}${path}`, {
      method: "GET",
      headers: {
        accept: "application/json",
        authorization: `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      throw new ConvyApiError(response.status, mapStatusToErrorType(response.status), await readError(response));
    }

    return (await response.json()) as T;
  }

  private async postJson<T>(path: string, token: string, idempotencyKey: string, body: Record<string, unknown>): Promise<T> {
    const response = await this.fetchImpl(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: {
        accept: "application/json",
        authorization: `Bearer ${token}`,
        "content-type": "application/json",
        "Idempotency-Key": idempotencyKey,
      },
      body: JSON.stringify(body),
    });

    if (!response.ok) {
      throw new ConvyApiError(response.status, mapStatusToErrorType(response.status), await readError(response));
    }

    return (await response.json()) as T;
  }

}

async function readError(response: Response) {
  const text = await response.text();
  if (!text) {
    return `Convy API returned ${response.status}.`;
  }

  try {
    const parsed = JSON.parse(text) as { message?: string; error?: string };
    return parsed.message ?? parsed.error ?? text;
  } catch {
    return text;
  }
}

function mapStatusToErrorType(status: number) {
  if (status === 400) return "ValidationError";
  if (status === 401) return "Unauthorized";
  if (status === 403) return "Forbidden";
  if (status === 404) return "NotFound";
  if (status === 409) return "Conflict";
  return status >= 500 ? "ProviderError" : "UnexpectedError";
}
