# MCP Tools

All tools are registered statically in `mcp/src/tools/definitions.ts`. Inputs use strict Zod schemas. The MCP service calls the Convy API and never connects directly to PostgreSQL.

Tool responses return concise `structuredContent` with:

```json
{
  "data": {},
  "meta": {
    "source": "convy_api",
    "householdId": null,
    "truncated": false,
    "selectionRequired": false
  }
}
```

The text response is a short summary only. Consumers should use structured content. Normal read and write tools are data-first: they do not include `openai/outputTemplate` or `ui.resourceUri`, so ChatGPT can answer in text without automatically rendering a component.

Only render tools reference the shared Apps SDK widget resource `ui://widget/convy-summary-v1.html`. Render tools are read-only, model-only, and should be used only when the user explicitly asks for a panel, card, widget, visual component, tarjeta, or componente visual.

## Read Tools

| Tool | Scopes | Input | API path used | Output | Expected errors |
| --- | --- | --- | --- | --- | --- |
| `convy_get_context` | `convy.households.read` | `{}` | `GET /api/v1/households` | Household count, default household ID when exactly one exists, compact household choices. | `401`, `403`, API connectivity failures. |
| `convy_get_shopping_context` | `convy.households.read`, `convy.lists.read` | optional `householdId` UUID | `GET /api/v1/households`, `GET /api/v1/households/{householdId}/lists` | Active shopping lists for the selected household and selection hints. | `401`, `403`, `404`, selection required. |
| `convy_get_shopping_list` | `convy.items.read` | `listId` UUID, optional `includeCompleted`, optional `limit` 1-100 | `GET /api/v1/lists/{listId}/items?status=Pending`, optionally `status=Completed` | Pending items and optionally completed items, compacted and truncated by limit. | `401`, `403`, `404`, validation errors. |
| `convy_get_task_list` | `convy.tasks.read` | `listId` UUID, optional `includeCompleted`, optional `limit` 1-100 | `GET /api/v1/lists/{listId}/tasks?status=Pending`, optionally `status=Completed` | Pending tasks and optionally completed tasks, including compact assignment, due date, reminder, and priority metadata. | `401`, `403`, `404`, validation errors. |
| `convy_get_recent_activity` | `convy.households.read`, `convy.activity.read` | optional `householdId` UUID, optional `limit` 1-50 | `GET /api/v1/households/{householdId}/activity` | Recent household activity with compact entity/action metadata. | `401`, `403`, `404`, selection required. |

Read annotations:

```json
{
  "readOnlyHint": true,
  "destructiveHint": false,
  "idempotentHint": true,
  "openWorldHint": false
}
```

Read tools return pending shopping items and tasks by default. Completed items/tasks are fetched only when `includeCompleted=true`.

## Render Tools

Render tools reuse the same API paths and output schemas as their matching read tools, but they attach widget metadata so ChatGPT can render a compact Convy panel on explicit visual requests.

| Tool | Matching data tool | Scopes | Input |
| --- | --- | --- | --- |
| `convy_render_context` | `convy_get_context` | `convy.households.read` | `{}` |
| `convy_render_shopping_context` | `convy_get_shopping_context` | `convy.households.read`, `convy.lists.read` | optional `householdId` UUID |
| `convy_render_shopping_list` | `convy_get_shopping_list` | `convy.items.read` | `listId` UUID, optional `includeCompleted`, optional `limit` 1-100 |
| `convy_render_task_list` | `convy_get_task_list` | `convy.tasks.read` | `listId` UUID, optional `includeCompleted`, optional `limit` 1-100 |
| `convy_render_recent_activity` | `convy_get_recent_activity` | `convy.households.read`, `convy.activity.read` | optional `householdId` UUID, optional `limit` 1-50 |

Render tool annotations match read tools. Render tool descriptors are the only tool descriptors that include:

```json
{
  "_meta": {
    "ui": {
      "resourceUri": "ui://widget/convy-summary-v1.html",
      "visibility": ["model"]
    },
    "openai/outputTemplate": "ui://widget/convy-summary-v1.html"
  }
}
```

The widget is display-only. It has no refresh button, no write controls, no empty completed sections, no technical IDs unless debug metadata is enabled, and internally scrolls long lists after roughly eight visible rows.

## Write Tools

| Tool | Scopes | Input | API path used | Output | Idempotency | Expected errors |
| --- | --- | --- | --- | --- | --- | --- |
| `convy_add_shopping_items` | `convy.items.write` | `listId`, `items[]` with `title`, optional integer `quantity`, optional `unit`, optional `note`, optional `idempotencyKey` | `POST /api/v1/lists/{listId}/items/smart-batch` | Created, reused, uncompleted, duplicate, and warning summaries from the API. | MCP sends/generates an idempotency key. API stores hashed key and request hash. | `400`, `401`, `403`, `404`, `409`, API errors. |
| `convy_update_shopping_items_status` | `convy.items.write` | `listId`, `itemIds[]`, `status` of `Pending` or `Completed`, optional `idempotencyKey` | `POST /api/v1/lists/{listId}/items/status-batch` | Updated item status summary. | MCP sends/generates an idempotency key. | `400`, `401`, `403`, `404`, `409`, API errors. |
| `convy_add_tasks` | `convy.tasks.write` | `listId`, `tasks[]` with `title`, optional `note`, optional `assignedToUserId`, optional `dueDate` as `YYYY-MM-DD`, optional `reminderAtUtc` ISO instant, optional `priority` of `Low`, `Normal`, or `High`, optional `idempotencyKey` | `POST /api/v1/lists/{listId}/tasks/smart-batch` | Created, reused, uncompleted, duplicate, and warning summaries from the API. | MCP sends/generates an idempotency key. Expired keys return `409 idempotency_key_expired` without executing. | `400`, `401`, `403`, `404`, `409`, API errors. |
| `convy_update_tasks_status` | `convy.tasks.write` | `listId`, `taskIds[]`, `status` of `Pending` or `Completed`, optional `idempotencyKey` | `POST /api/v1/lists/{listId}/tasks/status-batch` | Updated task status summary. | MCP sends/generates an idempotency key. | `400`, `401`, `403`, `404`, `409`, API errors. |

Write annotations:

```json
{
  "readOnlyHint": false,
  "destructiveHint": false,
  "idempotentHint": true,
  "openWorldHint": false
}
```

## Selection Rules

- Tools that operate on a household accept optional `householdId`.
- If the user has one household, tools can use it implicitly.
- If the user has multiple households and no `householdId` is provided, the tool returns `selectionRequired=true` with compact household choices.
- If more than one active shopping list is available, ChatGPT must ask the user which list to use before writing.
- The API enforces membership on every household, list, item, task, and activity request.

## Smart Write Rules

Implemented backend behavior:

- Titles are trimmed, internal whitespace is collapsed, display capitalization is normalized, and `normalized_title` is stored.
- Exact normalized pending matches are reused instead of duplicated.
- Exact normalized completed matches are returned to pending instead of duplicated.
- Quantity, unit, or note conflicts are reported as warnings and are not silently edited.
- Task assignment, due date, reminder, and priority are accepted when explicitly provided and are echoed in compact task read responses.
- Duplicate entries inside one batch create one record and report later duplicates.
- The same pattern exists for shopping items and tasks.

Not implemented unless added in code:

- fuzzy matching
- translation
- semantic item merging
- automatic list selection when multiple lists are plausible
- destructive cleanup or deletion

## Output And Audit Policy

- Outputs are minimized and structured.
- Tool definitions do not change based on user data.
- Item titles, notes, and activity metadata are treated as data, not instructions.
- Audit logs contain tool name, user ID, optional household ID, optional OAuth client ID, status, latency, and error type.
- Audit logs do not store prompts or full tool arguments.
