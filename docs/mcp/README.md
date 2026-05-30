# Convy ChatGPT MCP

This directory documents the Convy ChatGPT MCP integration for the current controlled release.

The ChatGPT MCP integration lets an authorized ChatGPT client read a user's Convy household context, lists, shopping items, tasks, and recent activity, plus perform limited confirmed writes for shopping items and tasks.

The MCP server also registers a shared React Apps SDK widget at `ui://widget/convy-summary-v1.html`. Normal tools are data-first and do not render the widget automatically; dedicated render tools use the widget only when the user explicitly asks for a panel, card, widget, or visual component.

For MCP servers used by development agents, see [AI development MCP setup](../ai-tooling/mcp-setup.md).

## Components

| Component | Responsibility |
| --- | --- |
| `auth/` | Public OAuth authorization and Firebase consent UI at `https://auth.convyapp.com`. |
| `backend/src/Convy.API` | OAuth broker, token endpoint, revoke endpoint, scope policies, idempotency, audit ingestion. |
| `mcp/` | MCP resource server and Convy API tool adapter at `https://mcp.convyapp.com`. |
| `dashboard/` | Admin MCP metrics and runtime status view. |
| `docker/Caddyfile.vps` | Public routing for auth, MCP, API, dashboard, public, legal, and legacy hosts. |

## Public URLs

- MCP endpoint: `https://mcp.convyapp.com/mcp`
- Protected resource metadata: `https://mcp.convyapp.com/.well-known/oauth-protected-resource`
- Authorization endpoint: `https://auth.convyapp.com/oauth/authorize`
- Token endpoint: `https://auth.convyapp.com/oauth/token`
- Revocation endpoint: `https://auth.convyapp.com/oauth/revoke`
- Authorization server metadata: `https://auth.convyapp.com/.well-known/oauth-authorization-server`
- Privacy: `https://legal.convyapp.com/privacy`
- Terms: `https://legal.convyapp.com/terms`

## MCP Scopes

Read scopes:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`

Limited write scopes:

- `convy.items.write`
- `convy.tasks.write`

Write tools can create shopping items/tasks and mark existing shopping items/tasks completed or pending. They cannot edit, delete, archive, invite, leave, manage lists, manage household membership, view admin metrics, access backups, or perform destructive actions.

## Tools

Current tools:

- `convy_get_context`
- `convy_get_shopping_context`
- `convy_get_shopping_list`
- `convy_get_task_list`
- `convy_get_recent_activity`
- `convy_render_context`
- `convy_render_shopping_context`
- `convy_render_shopping_list`
- `convy_render_task_list`
- `convy_render_recent_activity`
- `convy_add_shopping_items`
- `convy_update_shopping_items_status`
- `convy_add_tasks`
- `convy_update_tasks_status`

See [tools.md](tools.md) for schemas, scopes, API paths, output policy, and error behavior.

## Security Model

High-level controls:

- Firebase login before OAuth consent.
- OAuth authorization code flow with PKCE S256.
- Hashed authorization codes and refresh tokens.
- Short-lived RS256 access tokens.
- API and MCP scope enforcement.
- MCP-only API policies for smart writes.
- No direct PostgreSQL access from MCP.
- Idempotency for MCP writes.
- Allowlisted OAuth client metadata hosts.
- Redacted MCP audit logs.
- Static tools and prompt-injection-resistant output policy.

See [security-model.md](security-model.md).

## Operating And Testing

- [OAuth flow](oauth-flow.md)
- [Manual ChatGPT test plan](manual-chatgpt-test-plan.md)
- [Domain cutover](domain-cutover.md)
- [Public submission prep](public-submission.md)
- [Smart tool behavior](smart-tool-behavior.md)
- [Operations runbook](../operations/mcp-runbook.md)

## Backlog

The current integration has fixed, closed-world tools. Additional "smart" behavior beyond exact normalized duplicate handling should be documented as future/backlog unless it is implemented in `mcp/src/tools/definitions.ts` or the backend command handlers.
