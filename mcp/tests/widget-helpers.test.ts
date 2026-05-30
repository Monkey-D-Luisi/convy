import assert from "node:assert/strict";
import { test } from "node:test";

import {
  formatValue,
  inferActiveTool,
  isRecord,
  normalizeToolResult,
} from "../widget/src/widget-helpers.js";

const householdId = "00000000-0000-4000-8000-000000000001";

test("isRecord rejects arrays", () => {
  assert.equal(isRecord({ id: householdId }), true);
  assert.equal(isRecord([]), false);
  assert.equal(isRecord(null), false);
});

test("shopping context results still infer the current active tool", () => {
  const activeTool = inferActiveTool({
    household: { id: householdId },
    shoppingLists: [{ id: "00000000-0000-4000-8000-000000000002", name: "Weekly Groceries" }],
  });

  assert.equal(activeTool, "convy_get_shopping_context");
});

test("normalizing structured content keeps data-first tool output renderable", () => {
  const result = normalizeToolResult({
    data: {
      pendingItems: [{ id: householdId, title: "Milk" }],
      completedItems: [],
    },
    meta: { source: "convy_api" },
  });

  assert.deepEqual(result, {
    structuredContent: {
      data: {
        pendingItems: [{ id: householdId, title: "Milk" }],
        completedItems: [],
      },
      meta: { source: "convy_api" },
    },
  });
});

test("formatValue hides empty metadata values", () => {
  assert.equal(formatValue(undefined), "");
  assert.equal(formatValue(null), "");
  assert.equal(formatValue(""), "");
  assert.equal(formatValue("Milk"), "Milk");
});
