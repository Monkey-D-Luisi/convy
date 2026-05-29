# Security

This document describes implemented security boundaries and known controlled-release limitations. It is not a substitute for a formal security review.

## Authentication

| Surface | Authentication |
| --- | --- |
| Mobile app | Firebase Auth ID tokens sent to the backend as bearer tokens. |
| Dashboard | Caddy Basic Auth, Firebase login, and backend `AdminOnly` email allowlist. |
| Auth app | Firebase login for OAuth consent. |
| MCP service | Short-lived Convy MCP JWT access tokens issued by the backend OAuth broker. |
| MCP audit ingestion | Service key header from MCP service to API. |

## Authorization

- Backend endpoints require authentication unless explicitly public.
- Household, list, item, task, activity, invite, and device access is scoped to the authenticated user.
- `AdminOnly` uses Firebase authentication and rejects MCP-origin tokens.
- MCP scopes are enforced by API endpoint policies and by MCP tool handlers.

## ChatGPT MCP OAuth

Implemented controls:

- OAuth authorization code flow with PKCE S256.
- Firebase login before consent.
- Strict redirect URI and resource validation.
- CIMD client metadata validation with HTTPS, timeout, size, client ID, and redirect URI checks.
- Authorization codes and refresh tokens stored as hashes, not raw values.
- RS256 access tokens signed by the API private key.
- MCP service validates issuer, audience, `token_use=mcp_access`, subject, and scopes.
- Refresh token grants rotate refresh tokens.
- Revocation invalidates the presented refresh token.

Current MCP scopes:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`
- `convy.items.write`
- `convy.tasks.write`

MCP does not expose admin metrics, backups, list management, household management, invites, archive, delete, or destructive tools.

## MCP Write Safety

- Write tools are limited to smart-batch item/task creation and item/task status changes.
- API write paths require an `Idempotency-Key` for MCP tokens.
- The MCP service generates an idempotency key when ChatGPT does not provide one.
- Idempotency records store hashed keys, request hash, action name, status code, optional location, compact response JSON, and expiry metadata.
- Smart writes normalize titles and reuse exact normalized matches instead of creating safe duplicates.

## Prompt Injection And Output Policy

- MCP tool definitions are static.
- Convy data returned by tools is treated as data, not instructions.
- The MCP service does not create dynamic tools from household content, item titles, notes, or activity metadata.
- Tool responses use concise structured content and short summaries.
- MCP audit logs do not store prompts or full tool arguments.

## Voice And OpenAI Data Handling

- Voice audio is sent to OpenAI for transcription and parsing only when users use voice input.
- Convy records operational metrics such as status, latency, approximate duration, token counts, parsed item count, and estimated cost when configured.
- Voice metrics should not store audio, transcripts, prompts, or detected product names.

## Secrets

Do not commit:

- Firebase admin JSON
- Firebase API keys outside documented placeholder examples
- OpenAI API keys
- MCP RSA private keys
- MCP audit service keys
- Caddy Basic Auth hashes
- PostgreSQL passwords
- GitHub tokens
- SSH private keys

Use placeholders in docs. Store runtime secrets on the VPS under `/opt/convy/shared` and push them with `ops/vps/push-secrets.ps1`.

## Backups

- Backups are local PostgreSQL custom-format dumps on the VPS.
- Dumps include checksums and metadata.
- Catalog verification and scheduled restore verification are implemented.
- Admin dashboard download is limited to registered successful backup files resolved under the configured backup root.
- Encrypted offsite backups are a required future step before broader public onboarding.

## Rate Limits And Availability

The MCP service has a fixed-window request limit in `mcp/src/server.ts`. Broader rate limiting, external alerting, and production monitoring should be treated as future hardening unless implemented in code.

## Incident Checklist

1. Disable the affected public route or service.
2. Rotate exposed secrets or keys.
3. Revoke affected OAuth refresh tokens where applicable.
4. Review API, Caddy, and MCP logs.
5. Review `mcp_tool_invocations`, `mcp_idempotency_records`, and relevant admin metrics.
6. Restore from the latest verified backup only after confirming the incident scope.
