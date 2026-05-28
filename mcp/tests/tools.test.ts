import assert from "node:assert/strict";
import { test } from "node:test";

import { toolDefinitions } from "../src/tools/definitions.js";

test("MCP exposes read tools and limited idempotent write tools", () => {
  const toolNames = toolDefinitions.map((tool) => tool.name).sort();

  assert.deepEqual(toolNames, [
    "convy_complete_shopping_item",
    "convy_complete_task",
    "convy_create_shopping_item",
    "convy_create_task",
    "convy_get_context",
    "convy_get_household_overview",
    "convy_get_lists",
    "convy_get_recent_activity",
    "convy_get_shopping_items",
    "convy_get_tasks",
    "convy_uncomplete_shopping_item",
    "convy_uncomplete_task",
  ]);

  for (const tool of toolDefinitions.filter((definition) => definition.name.startsWith("convy_get_"))) {
    assert.equal(tool.annotations.readOnlyHint, true);
    assert.equal(tool.annotations.destructiveHint, false);
    assert.ok(!tool.requiredScopes.some((scope) => scope.includes(".write")));
  }

  for (const tool of toolDefinitions.filter((definition) => !definition.name.startsWith("convy_get_"))) {
    assert.equal(tool.annotations.readOnlyHint, false);
    assert.equal(tool.annotations.destructiveHint, false);
    assert.equal(tool.annotations.openWorldHint, false);
    assert.equal(tool.annotations.idempotentHint, true);
    assert.ok(tool.requiredScopes.some((scope) => scope.includes(".write")));
    assert.deepEqual(Object.keys(tool.inputSchema.shape).includes("idempotencyKey"), true);
  }
});

test("write tool input schemas keep optional fields non-null for ChatGPT compatibility", () => {
  const createShoppingItem = toolDefinitions.find((tool) => tool.name === "convy_create_shopping_item");
  const createTask = toolDefinitions.find((tool) => tool.name === "convy_create_task");
  assert.ok(createShoppingItem);
  assert.ok(createTask);

  const listId = "11111111-1111-4111-8111-111111111111";
  const idempotencyKey = "debug-key-123";

  assert.equal(createShoppingItem.inputSchema.safeParse({
    listId,
    title: "Milk",
    quantity: null,
    idempotencyKey,
  }).success, false);
  assert.equal(createShoppingItem.inputSchema.safeParse({
    listId,
    title: "Milk",
    note: null,
    idempotencyKey,
  }).success, false);
  assert.equal(createTask.inputSchema.safeParse({
    listId,
    title: "Clean kitchen",
    note: null,
    idempotencyKey,
  }).success, false);
});
