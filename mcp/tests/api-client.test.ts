import assert from "node:assert/strict";
import { test } from "node:test";
import { ConvyApiClient } from "../src/convy-api-client.js";

test("Convy API client calls expected read-only endpoints", async () => {
  const urls: string[] = [];
  const client = new ConvyApiClient({
    baseUrl: "https://api.convyapp.com",
    fetchImpl: async (url) => {
      urls.push(String(url));
      return new Response("[]", { status: 200, headers: { "content-type": "application/json" } });
    },
  });

  await client.getHouseholds("token");
  await client.getLists("token", "11111111-1111-4111-8111-111111111111", false);
  await client.getShoppingItems("token", "22222222-2222-4222-8222-222222222222", "Pending");
  await client.getTasks("token", "33333333-3333-4333-8333-333333333333", "Completed");
  await client.getRecentActivity("token", "44444444-4444-4444-8444-444444444444", 10);

  assert.deepEqual(urls, [
    "https://api.convyapp.com/api/v1/households/",
    "https://api.convyapp.com/api/v1/households/11111111-1111-4111-8111-111111111111/lists/?includeArchived=false",
    "https://api.convyapp.com/api/v1/lists/22222222-2222-4222-8222-222222222222/items/?status=Pending",
    "https://api.convyapp.com/api/v1/lists/33333333-3333-4333-8333-333333333333/tasks/?status=Completed",
    "https://api.convyapp.com/api/v1/households/44444444-4444-4444-8444-444444444444/activity/?limit=10",
  ]);
});

test("audit logging does not send prompts or full arguments", async () => {
  let body: Record<string, unknown> | undefined;
  const client = new ConvyApiClient({
    baseUrl: "https://api.convyapp.com",
    auditApiKey: "audit-secret",
    fetchImpl: async (_url, init) => {
      body = JSON.parse(String(init?.body)) as Record<string, unknown>;
      return new Response(null, { status: 202 });
    },
  });

  await client.recordToolInvocation({
    userId: "11111111-1111-4111-8111-111111111111",
    householdId: "22222222-2222-4222-8222-222222222222",
    clientId: "https://chatgpt.com/aip/g-test/.well-known/oauth-client",
    toolName: "convy_get_shopping_context",
    status: "Success",
    latencyMs: 12,
  });

  assert.deepEqual(Object.keys(body ?? {}).sort(), [
    "clientId",
    "errorType",
    "householdId",
    "latencyMs",
    "status",
    "toolName",
    "userId",
  ]);
});

test("Convy API client sends smart write calls with idempotency keys", async () => {
  const requests: Array<{ url: string; method?: string; key?: string; body?: string }> = [];
  const client = new ConvyApiClient({
    baseUrl: "https://api.convyapp.com",
    fetchImpl: async (url, init) => {
      requests.push({
        url: String(url),
        method: init?.method,
        key: init?.headers instanceof Headers
          ? init.headers.get("Idempotency-Key") ?? undefined
          : (init?.headers as Record<string, string> | undefined)?.["Idempotency-Key"],
        body: typeof init?.body === "string" ? init.body : undefined,
      });
      return new Response(JSON.stringify({ id: "55555555-5555-4555-8555-555555555555" }), {
        status: 201,
        headers: { "content-type": "application/json" },
      });
    },
  });

  await client.addShoppingItems("token", "11111111-1111-4111-8111-111111111111", {
    items: [{ title: "Milk", quantity: 2, unit: "l", note: "Whole" }],
    idempotencyKey: "stable-key",
  });
  await client.updateShoppingItemsStatus("token", "11111111-1111-4111-8111-111111111111", {
    itemIds: ["22222222-2222-4222-8222-222222222222"],
    status: "Completed",
    idempotencyKey: "complete-key",
  });
  await client.updateTasksStatus("token", "33333333-3333-4333-8333-333333333333", {
    taskIds: ["44444444-4444-4444-8444-444444444444"],
    status: "Pending",
    idempotencyKey: "uncomplete-key",
  });
  await client.addTasks("token", "33333333-3333-4333-8333-333333333333", {
    tasks: [{
      title: "Clean kitchen",
      note: "Before dinner",
      assignedToUserId: "55555555-5555-4555-8555-555555555555",
      dueDate: "2026-05-30",
      reminderAtUtc: "2026-05-30T07:00:00Z",
      priority: "High",
    }],
    idempotencyKey: "task-key",
  });

  assert.deepEqual(requests.map((request) => [request.method, request.url, request.key]), [
    ["POST", "https://api.convyapp.com/api/v1/lists/11111111-1111-4111-8111-111111111111/items/smart-batch", "stable-key"],
    ["POST", "https://api.convyapp.com/api/v1/lists/11111111-1111-4111-8111-111111111111/items/status-batch", "complete-key"],
    ["POST", "https://api.convyapp.com/api/v1/lists/33333333-3333-4333-8333-333333333333/tasks/status-batch", "uncomplete-key"],
    ["POST", "https://api.convyapp.com/api/v1/lists/33333333-3333-4333-8333-333333333333/tasks/smart-batch", "task-key"],
  ]);
  assert.match(requests[0]?.body ?? "", /"title":"Milk"/);
  assert.match(requests[3]?.body ?? "", /"assignedToUserId":"55555555-5555-4555-8555-555555555555"/);
  assert.match(requests[3]?.body ?? "", /"dueDate":"2026-05-30"/);
  assert.match(requests[3]?.body ?? "", /"reminderAtUtc":"2026-05-30T07:00:00Z"/);
  assert.match(requests[3]?.body ?? "", /"priority":"High"/);
  assert.doesNotMatch(requests[0]?.body ?? "", /idempotencyKey/);
});
