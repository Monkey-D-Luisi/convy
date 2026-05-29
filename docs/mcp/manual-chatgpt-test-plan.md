# Manual ChatGPT MCP Test Plan

Run this plan against staging before granting private beta access or submitting for public review.

## Setup

1. Deploy a release that includes API, auth, MCP, dashboard, Caddy, public, and legal services.
2. Verify health:

```bash
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
```

3. Verify metadata:

```bash
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://auth.convyapp.com/.well-known/oauth-authorization-server
```

4. Confirm `scopes_supported` includes only:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`
- `convy.items.write`
- `convy.tasks.write`

It must not advertise admin, backup, delete, invite, household-management, or list-management scopes.

## Positive ChatGPT Flow

1. In ChatGPT Developer Mode, add MCP server URL `https://mcp.convyapp.com/mcp`.
2. Confirm ChatGPT receives a 401 challenge with protected resource metadata.
3. Start OAuth authorization.
4. Sign in at `https://auth.convyapp.com`.
5. Confirm consent text says ChatGPT can read Convy data and perform limited item/task writes.
6. Confirm consent text says ChatGPT cannot edit, delete, archive, invite, leave, manage lists, view admin metrics, or access backups.
7. Approve requested scopes.
8. Ask ChatGPT to show Convy households.
9. Ask ChatGPT to show shopping context.
10. Ask ChatGPT to show one shopping list, including completed items.
11. Ask ChatGPT to show one task list, including completed tasks.
12. Ask ChatGPT to show recent household activity.
13. Ask: `Anade leche, pan y huevos a la compra.` Confirm one `convy_add_shopping_items` call.
14. Ask ChatGPT to add an already pending item. Confirm the API reports reuse and no duplicate item appears in the app.
15. Complete an item in Convy, then ask ChatGPT to add it again. Confirm the API returns it to pending and reports that state.
16. Ask ChatGPT to create one task.
17. Ask ChatGPT to mark that task completed through the task status-batch tool.
18. Ask ChatGPT to mark the task pending again.
19. Revoke access through ChatGPT.
20. Confirm refresh no longer works and ChatGPT loses access.

## Google Sign-In Check

Use Chrome or Edge because embedded/in-app browsers may block popups.

1. Open ChatGPT Developer Mode in Chrome or Edge.
2. Start OAuth against `https://mcp.convyapp.com/mcp`.
3. Click "Sign in with Google" on `https://auth.convyapp.com`.
4. Allow popups for `auth.convyapp.com` and `accounts.google.com` if required.
5. Confirm approval returns to ChatGPT.

## Negative Checks

- Missing token returns 401 with `WWW-Authenticate`.
- Invalid token returns 401.
- Token without supported Convy scopes returns 403 from MCP.
- Read-only token cannot invoke write tools.
- Direct API MCP write without `Idempotency-Key` returns 400.
- Reusing the same idempotency key with a different request returns 409.
- Direct MCP token calls to single create/complete/uncomplete, PUT, DELETE, archive, invite, leave, admin, backup, and device endpoints fail.
- User A cannot read User B household, list, item, task, or activity.
- Multi-household users receive `selectionRequired=true` when no household is selected.

## Audit Checks

In the dashboard or database, confirm MCP tool invocations record:

- user ID
- optional household ID
- optional OAuth client ID
- tool name
- status
- latency
- error type for failures

Confirm they do not record:

- ChatGPT prompts
- full tool arguments
- access tokens
- refresh tokens
- raw idempotency keys
