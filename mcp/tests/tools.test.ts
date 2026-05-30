import assert from "node:assert/strict";
import { test } from "node:test";

import { toolDefinitions } from "../src/tools/definitions.js";

test("MCP exposes read tools and limited idempotent write tools", () => {
  const toolNames = toolDefinitions.map((tool) => tool.name).sort();

  assert.deepEqual(toolNames, [
    "convy_add_shopping_items",
    "convy_add_tasks",
    "convy_get_context",
    "convy_get_recent_activity",
    "convy_get_shopping_context",
    "convy_get_shopping_list",
    "convy_get_task_list",
    "convy_update_shopping_items_status",
    "convy_update_tasks_status",
  ]);

  for (const tool of toolDefinitions.filter((definition) => definition.annotations.readOnlyHint)) {
    assert.equal(tool.annotations.destructiveHint, false);
    assert.ok(!tool.requiredScopes.some((scope) => scope.includes(".write")));
  }

  for (const tool of toolDefinitions.filter((definition) => !definition.annotations.readOnlyHint)) {
    assert.equal(tool.annotations.destructiveHint, false);
    assert.equal(tool.annotations.openWorldHint, false);
    assert.equal(tool.annotations.idempotentHint, true);
    assert.ok(tool.requiredScopes.some((scope) => scope.includes(".write")));
  }
});

test("smart write tool schemas are strict and idempotency keys are optional", () => {
  const addShoppingItems = toolDefinitions.find((tool) => tool.name === "convy_add_shopping_items");
  const updateShoppingStatus = toolDefinitions.find((tool) => tool.name === "convy_update_shopping_items_status");
  const addTasks = toolDefinitions.find((tool) => tool.name === "convy_add_tasks");
  assert.ok(addShoppingItems);
  assert.ok(updateShoppingStatus);
  assert.ok(addTasks);

  const listId = "11111111-1111-4111-8111-111111111111";
  assert.equal(addShoppingItems.inputSchema.safeParse({
    listId,
    items: [{ title: "Leche", quantity: 2, unit: "litros" }],
  }).success, true);
  assert.equal(addShoppingItems.inputSchema.safeParse({
    listId,
    items: Array.from({ length: 21 }, () => ({ title: "Leche" })),
  }).success, false);
  assert.equal(updateShoppingStatus.inputSchema.safeParse({
    listId,
    itemIds: ["22222222-2222-4222-8222-222222222222"],
    status: "Archived",
  }).success, false);
  assert.equal(addTasks.inputSchema.safeParse({
    listId,
    tasks: [{ title: "Limpiar cocina", unexpected: true }],
  }).success, false);
  assert.equal(addTasks.inputSchema.safeParse({
    listId,
    tasks: [{
      title: "Limpiar cocina",
      note: "Antes de cenar",
      assignedToUserId: "22222222-2222-4222-8222-222222222222",
      dueDate: "2026-05-30",
      reminderAtUtc: "2026-05-30T07:00:00Z",
      priority: "High",
    }],
  }).success, true);
  assert.equal(addTasks.inputSchema.safeParse({
    listId,
    tasks: [{
      title: "Limpiar cocina",
      reminderAtUtc: "2026-05-30T09:00:00+02:00",
    }],
  }).success, false);
  assert.equal(addTasks.inputSchema.safeParse({
    listId,
    tasks: [{ title: "Limpiar cocina", priority: "Urgent" }],
  }).success, false);
});

test("smart shopping guidance is present in tool descriptions", () => {
  const addShoppingItems = toolDefinitions.find((tool) => tool.name === "convy_add_shopping_items");
  assert.ok(addShoppingItems);
  assert.match(addShoppingItems.description, /^Use this when/);
  assert.match(addShoppingItems.description, /Do not invent quantities or units/);
  assert.match(addShoppingItems.description, /Do not include negated items/);
});

test("task list responses include smart task metadata", async () => {
  const getTaskList = toolDefinitions.find((tool) => tool.name === "convy_get_task_list");
  assert.ok(getTaskList);

  const result = await getTaskList.execute({
    listId: "11111111-1111-4111-8111-111111111111",
    includeCompleted: false,
    limit: 50,
  } as never, {
    auth: {
      token: "token",
      userId: "22222222-2222-4222-8222-222222222222",
      scopes: new Set(["convy.tasks.read"]),
    },
    apiClient: {
      getTasks: async () => [{
        id: "33333333-3333-4333-8333-333333333333",
        title: "Clean kitchen",
        assignedToUserId: "44444444-4444-4444-8444-444444444444",
        assignedToUserName: "Marina",
        dueDate: "2026-05-30",
        reminderAtUtc: "2026-05-30T07:00:00Z",
        reminderSentAtUtc: null,
        priority: "High",
      }],
    } as never,
  });

  assert.deepEqual((result.data as { pendingTasks: unknown[] }).pendingTasks[0], {
    id: "33333333-3333-4333-8333-333333333333",
    title: "Clean kitchen",
    assignedToUserId: "44444444-4444-4444-8444-444444444444",
    assignedToUserName: "Marina",
    dueDate: "2026-05-30",
    reminderAtUtc: "2026-05-30T07:00:00Z",
    reminderSentAtUtc: null,
    priority: "High",
  });
});
