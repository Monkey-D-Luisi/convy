# MCP Tools

All tools use strict Zod input schemas and call the Convy API. The MCP service must never query PostgreSQL directly.

| Tool | Required scopes | Purpose |
| --- | --- | --- |
| `convy_get_context` | `convy.households.read` | Returns households visible to the connected user and whether selection is required. |
| `convy_get_household_overview` | `convy.households.read`, `convy.lists.read`, `convy.activity.read` | Returns household summary, member names and roles, list counts, and latest activity. |
| `convy_get_lists` | `convy.households.read`, `convy.lists.read` | Returns household shopping and task lists. |
| `convy_get_shopping_items` | `convy.items.read` | Returns shopping items for a list. |
| `convy_get_tasks` | `convy.tasks.read` | Returns tasks for a list. |
| `convy_get_recent_activity` | `convy.households.read`, `convy.activity.read` | Returns recent household activity. |
| `convy_create_shopping_item` | `convy.items.write` | Creates one shopping item. Requires an idempotency key. |
| `convy_complete_shopping_item` | `convy.items.write` | Marks one shopping item completed. Requires an idempotency key. |
| `convy_uncomplete_shopping_item` | `convy.items.write` | Marks one shopping item pending again. Requires an idempotency key. |
| `convy_create_task` | `convy.tasks.write` | Creates one task. Requires an idempotency key. |
| `convy_complete_task` | `convy.tasks.write` | Marks one task completed. Requires an idempotency key. |
| `convy_uncomplete_task` | `convy.tasks.write` | Marks one task pending again. Requires an idempotency key. |

## Household Selection

Tools that operate on a household accept optional `householdId`.

- If the user has one household, tools can use it implicitly.
- If the user has multiple households and no `householdId` is provided, the tool returns `selectionRequired=true` with compact household choices.
- The API still enforces membership on every household, list, item, task, and activity request.

## Output Policy

- Outputs are concise JSON in `structuredContent`.
- The MCP service does not interpret, rewrite, or prompt over user data.
- Household overview excludes member email addresses.
- Audit logs contain tool name, user ID, optional household ID, status, latency, and error type only.
- Write tools are idempotent, non-destructive, closed-world tools.
- ChatGPT cannot edit, delete, archive, invite, leave, view admin metrics, access backups, manage lists, or manage household membership through MCP.
