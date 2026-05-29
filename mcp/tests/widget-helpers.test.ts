import assert from "node:assert/strict";
import { test } from "node:test";

import {
  createRefreshArgs,
  createRefreshErrorResult,
  inferActiveTool,
  isRecord,
} from "../widget/src/widget-helpers.js";

const householdId = "00000000-0000-4000-8000-000000000001";

test("isRecord rejects arrays", () => {
  assert.equal(isRecord({ id: householdId }), true);
  assert.equal(isRecord([]), false);
  assert.equal(isRecord(null), false);
});

test("shopping context results can be refreshed with the current household", () => {
  const activeTool = inferActiveTool({
    household: { id: householdId },
    shoppingLists: [{ id: "00000000-0000-4000-8000-000000000002", name: "Weekly Groceries" }],
  });

  assert.equal(activeTool, "convy_get_shopping_context");
  assert.deepEqual(createRefreshArgs(activeTool, { household: { id: householdId } }, {}), { householdId });
});

test("refresh failures become visible widget errors", () => {
  const result = createRefreshErrorResult(new Error("network unavailable"));

  assert.equal(result.isError, true);
  assert.deepEqual(result.content, [{
    type: "text",
    text: "Convy refresh failed: network unavailable",
  }]);
});
