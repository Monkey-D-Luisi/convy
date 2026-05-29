# MCP Tools

All tools use strict Zod input schemas and call the Convy API. The MCP service never queries PostgreSQL directly.

| Tool | Required scopes | Purpose |
| --- | --- | --- |
| `convy_get_context` | `convy.households.read` | Returns households visible to the connected user and whether household selection is required. |
| `convy_get_shopping_context` | `convy.households.read`, `convy.lists.read` | Returns active shopping lists for the selected or only household. |
| `convy_get_shopping_list` | `convy.items.read` | Returns pending shopping items and optionally completed items for one shopping list. |
| `convy_get_task_list` | `convy.tasks.read` | Returns pending tasks and optionally completed tasks for one task list. |
| `convy_get_recent_activity` | `convy.households.read`, `convy.activity.read` | Returns recent household activity. |
| `convy_add_shopping_items` | `convy.items.write` | Adds one or more shopping items through the API smart-batch endpoint. |
| `convy_update_shopping_items_status` | `convy.items.write` | Marks existing shopping items `Completed` or `Pending` through the API status-batch endpoint. |
| `convy_add_tasks` | `convy.tasks.write` | Adds one or more tasks through the API smart-batch endpoint. |
| `convy_update_tasks_status` | `convy.tasks.write` | Marks existing tasks `Completed` or `Pending` through the API status-batch endpoint. |

## Selection

- Tools that operate on a household accept optional `householdId`.
- If the user has one household, tools can use it implicitly.
- If the user has multiple households and no `householdId` is provided, the tool returns `selectionRequired=true` with compact household choices.
- If more than one active shopping list is available, ChatGPT must ask the user which list to use before writing.
- The API enforces membership on every household, list, item, task, and activity request.

## Smart Writes

- Smart batch endpoints normalize visible titles and store `normalized_title`.
- Exact normalized matches are reused instead of duplicated.
- Completed matches are returned to pending instead of creating a duplicate.
- Quantity, unit, or note conflicts are reported as warnings; the API does not update details silently.
- Duplicate entries inside the same batch create only one record and report later duplicates.
- MCP may omit `idempotencyKey`; the MCP service generates one and sends it as an API idempotency header.

## Output Policy

- Outputs are concise JSON in `structuredContent`.
- Tool result text is only a short summary; consumers should use `structuredContent`.
- The MCP service does not interpret, rewrite, or prompt over user data.
- Audit logs contain tool name, user ID, optional household ID, optional OAuth client ID, status, latency, and error type only.
- Write tools are idempotent, non-destructive, closed-world tools.
- ChatGPT cannot edit, delete, archive, invite, leave, view admin metrics, access backups, manage lists, or manage household membership through MCP.
