# ChatGPT App Submission Readiness Audit

Date: 2026-05-29
Commit reviewed before changes: `fec6c334cdf4695494613d336ad305dc89151985`
Repository status: `ready` after this submission-prep branch is merged and deployed
Submission status: `needs-changes` until the manual reviewer account, screenshots, OpenAI verification, and dashboard submission are completed

## Public Endpoints

| Endpoint | Expected URL | Status |
| --- | --- | --- |
| MCP server | `https://mcp.convyapp.com/mcp` | Public POST endpoint; returns OAuth challenge without token. |
| Protected resource metadata | `https://mcp.convyapp.com/.well-known/oauth-protected-resource` | Public JSON; advertises Convy MCP scopes only. |
| Auth app | `https://auth.convyapp.com` | Public OAuth consent surface. |
| Authorization server metadata | `https://auth.convyapp.com/.well-known/oauth-authorization-server` | Public JSON; advertises authorization, token, and revocation endpoints. |
| Privacy | `https://legal.convyapp.com/privacy` | Public static HTML. |
| Terms | `https://legal.convyapp.com/terms` | Public static HTML. |
| Company URL | `https://convyapp.com` | Public static landing page. |

## Tool Catalog

| Tool | Read-only | Destructive | Open world | Idempotent | Scopes |
| --- | --- | --- | --- | --- | --- |
| `convy_get_context` | true | false | false | true | `convy.households.read` |
| `convy_get_shopping_context` | true | false | false | true | `convy.households.read`, `convy.lists.read` |
| `convy_get_shopping_list` | true | false | false | true | `convy.items.read` |
| `convy_get_task_list` | true | false | false | true | `convy.tasks.read` |
| `convy_get_recent_activity` | true | false | false | true | `convy.households.read`, `convy.activity.read` |
| `convy_render_context` | true | false | false | true | `convy.households.read` |
| `convy_render_shopping_context` | true | false | false | true | `convy.households.read`, `convy.lists.read` |
| `convy_render_shopping_list` | true | false | false | true | `convy.items.read` |
| `convy_render_task_list` | true | false | false | true | `convy.tasks.read` |
| `convy_render_recent_activity` | true | false | false | true | `convy.households.read`, `convy.activity.read` |
| `convy_add_shopping_items` | false | false | false | true | `convy.items.write` |
| `convy_update_shopping_items_status` | false | false | false | true | `convy.items.write` |
| `convy_add_tasks` | false | false | false | true | `convy.tasks.write` |
| `convy_update_tasks_status` | false | false | false | true | `convy.tasks.write` |

## Scope Boundaries

- MCP calls the Convy API and does not connect directly to PostgreSQL.
- Read tools expose only household, list, item, task, and activity data authorized for the connected user.
- Write tools are limited to private Convy shopping item/task creation and status changes.
- The MCP surface does not expose delete, archive, invite, leave-household, account-management, admin metrics, backup, SQL, device-token, or configuration tools.
- Write tools generate or forward idempotency keys and the API enforces idempotency for MCP-origin writes.
- Tool annotations are explicit for `readOnlyHint`, `openWorldHint`, `destructiveHint`, and `idempotentHint`.
- Normal read/write tools are data-first and do not attach widget metadata.
- Render tools are read-only, model-only, and attach the shared widget only for explicit visual requests.
- The React Apps SDK widget is a compact display-only summary view. It has no write buttons and no refresh control.

## Widget Isolation

- The widget origin is currently `https://mcp.convyapp.com` because the deployed topology does not yet provision a dedicated `widgets.convyapp.com` host, certificate, and routing path.
- This still keeps the Apps review surface narrow: the widget is registered as a single Apps SDK resource, has no cookies, uses no browser storage, and declares empty connect, resource, and frame domains in its CSP.
- The widget is not granted tool-calling visibility. Write tools remain model-only and still require ChatGPT confirmation.
- If infrastructure adds `widgets.convyapp.com`, switch `CONVY_WIDGET_DOMAIN` to that dedicated origin before submission or in a follow-up deploy hardening PR.

## Risks And Required Manual Actions

- Create or verify the reviewer account and sample data outside the repository.
- Capture review screenshots after deploying this branch so public legal/landing wording matches the submission.
- Complete OpenAI identity verification and submit from a global-data-residency project with `api.apps.write`.
- Verify the OAuth login path in ChatGPT Developer Mode using the exact review account before submission.
