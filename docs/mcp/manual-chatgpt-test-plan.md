# Manual ChatGPT MCP Test Plan

Run this against staging before private beta access.

## Setup

1. Deploy the release with `auth`, `mcp`, and API services.
2. Verify:
   - `https://api.convyapp.com/health/ready`
   - `https://auth.convyapp.com/health`
   - `https://mcp.convyapp.com/health`
3. Confirm metadata:
   - `https://auth.convyapp.com/.well-known/oauth-authorization-server`
   - `https://mcp.convyapp.com/.well-known/oauth-protected-resource`
4. Confirm `scopes_supported` contains the five read scopes plus `convy.items.write` and `convy.tasks.write`, and does not contain list, household, admin, backup, invite, or delete scopes.

## ChatGPT Developer Mode

1. Add the MCP server URL `https://mcp.convyapp.com/mcp`.
2. Confirm ChatGPT receives a 401 challenge with protected resource metadata.
3. Start OAuth authorization.
4. Sign in with Firebase at `auth.convyapp.com`.
5. Confirm the consent page says ChatGPT can add shopping items/tasks and mark existing shopping items/tasks completed or pending, and cannot edit, delete, archive, invite, leave, view admin metrics, access backups, or manage lists.
6. Approve the requested scopes.
7. Query household context, shopping context, one shopping list, one task list, and recent activity.
8. Ask ChatGPT: `Añade leche, pan y huevos a la compra.` Confirm it uses one `convy_add_shopping_items` call.
9. Ask ChatGPT to add an already pending item. Confirm the API returns `reused` and does not duplicate it.
10. Complete an item in Convy, then ask ChatGPT to add it again. Confirm the API returns it to pending and reports `uncompleted`.
11. Ask ChatGPT to add one task and then mark it completed through the task status-batch tool.
12. Revoke access through ChatGPT.
13. Confirm refresh no longer works and ChatGPT loses access.

## Google Sign-In Check

Use Chrome or Edge for Google Sign-In validation because the Codex in-app browser blocks popups.

1. Open ChatGPT Developer Mode in Chrome or Edge.
2. Start OAuth against `https://mcp.convyapp.com/mcp`.
3. Click "Sign in with Google" on `https://auth.convyapp.com`.
4. If the browser blocks the login, allow popups for `auth.convyapp.com` and `accounts.google.com`.
5. Confirm the consent page returns to ChatGPT after approval.

## Negative Checks

- A token without any supported Convy scope returns 403 from MCP.
- A read-only token cannot invoke smart-batch or status-batch write tools.
- A direct API write call without `Idempotency-Key` returns 400 for MCP tokens.
- Reusing the same idempotency key with a different request returns 409.
- Missing or invalid token returns 401 with `WWW-Authenticate`.
- Direct MCP token calls to single create, single complete/uncomplete, PUT, DELETE, archive, invite, leave, admin, backup, and device endpoints fail.
- User A cannot read User B household, list, item, task, or activity.
- Multi-household users receive `selectionRequired=true` when no household is selected.
