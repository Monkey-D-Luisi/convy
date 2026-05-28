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
5. Confirm the consent page says ChatGPT can create and complete shopping items and tasks, and cannot edit, delete, archive, invite, leave, view admin metrics, access backups, or manage lists.
6. Approve the requested scopes.
7. Query household context, lists, shopping items, tasks, and recent activity.
8. Create one shopping item and one task from ChatGPT. Confirm ChatGPT requests write confirmation before each write.
9. Complete and uncomplete the created shopping item and task.
10. Revoke access through ChatGPT.
11. Confirm refresh no longer works and ChatGPT loses access.

## Google Sign-In Check

Use Chrome or Edge for Google Sign-In validation because the Codex in-app browser blocks popups.

1. Open ChatGPT Developer Mode in Chrome or Edge.
2. Start OAuth against `https://mcp.convyapp.com/mcp`.
3. Click "Sign in with Google" on `https://auth.convyapp.com`.
4. If the browser blocks the login, allow popups for `auth.convyapp.com` and `accounts.google.com`.
5. Confirm the consent page returns to ChatGPT after approval.

## Negative Checks

- A token without any supported Convy scope returns 403 from MCP.
- A read-only token cannot invoke create/complete/uncomplete write tools.
- A write call without `Idempotency-Key` returns 400.
- Reusing the same idempotency key with a different request returns 409.
- Missing or invalid token returns 401 with `WWW-Authenticate`.
- Direct MCP token calls to POST, PUT, DELETE, archive, invite, leave, admin, backup, and device endpoints fail.
- User A cannot read User B household, list, item, task, or activity.
- Multi-household users receive `selectionRequired=true` when no household is selected.
