# MCP Security Model

## Trust Boundaries

```text
ChatGPT client
  -> auth.convyapp.com for OAuth consent
  -> mcp.convyapp.com for MCP tool calls
  -> api.convyapp.com through MCP service only
  -> PostgreSQL never directly
```

Roles:

- ChatGPT is an OAuth client.
- `auth/` is the public authorization and consent surface.
- API is the OAuth broker and policy enforcement point.
- `mcp/` is the resource server and tool adapter.
- PostgreSQL is reachable only by the API.

## Implemented Controls

- Firebase authenticates the Convy user during consent.
- CIMD client metadata must use HTTPS, fit timeout/size limits, match `client_id`, and contain the exact redirect URI.
- OAuth authorization codes and refresh tokens are stored as hashes.
- PKCE S256 is required.
- Access tokens are short-lived RS256 JWTs.
- The API signs with the private key; API and MCP validate with the public key.
- API endpoints enforce MCP scopes in addition to MCP tool handlers.
- `AdminOnly` requires Firebase auth and rejects `auth_source=mcp`.
- MCP write API endpoints require `Idempotency-Key`.
- MCP audit ingestion uses a service key.
- Audit records omit prompts and full arguments.

## Scope Boundaries

MCP read scopes:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`

MCP write scopes:

- `convy.items.write`
- `convy.tasks.write`

Not exposed through MCP:

- admin metrics
- backups
- device token management
- invites
- household membership management
- list creation/edit/archive/delete
- item/task edit/delete
- destructive or irreversible tools

## Idempotency

MCP write calls store idempotency records with:

- user ID
- OAuth client ID
- hashed idempotency key
- action name
- request hash
- status code
- optional location
- compact response JSON
- created and expiry timestamps

Reusing the same key with the same request returns the stored outcome. Reusing the same key with a different request returns conflict.

## Prompt Injection Policy

- Convy data returned by tools is data, not instructions.
- Tool definitions are static.
- Titles, notes, activity metadata, and household names cannot create new tools or change behavior.
- The MCP service does not interpret prompts or rewrite user data.
- ChatGPT must ask the user to disambiguate household/list choices before writing when the tool output requires selection.

## Output Minimization

MCP responses include compact household/list/item/task/activity fields needed for the requested task. They should not include admin metrics, backups, raw tokens, secrets, internal logs, prompts, full arguments, or unrelated household data.

## Key Rotation

1. Generate a new RSA key pair.
2. Update `McpAuth__PrivateKeyPemBase64` and `McpAuth__PublicKeyPemBase64`.
3. Redeploy API and MCP together.
4. Existing access tokens signed by the previous key stop validating after deployment.
5. Refresh tokens can mint new access tokens unless revoked.

## Revocation

ChatGPT calls `POST /oauth/revoke` with the refresh token. The API hashes the presented token and revokes the matching active refresh token. Revoked refresh tokens cannot rotate or mint new access tokens.

## Incident Response

- Suspected client abuse: revoke refresh token and review `mcp_tool_invocations`.
- Public key exposure: rotate only if private key exposure is possible.
- Private key exposure: rotate RSA pair immediately and restart API/MCP.
- Audit key exposure: rotate `McpAudit__ApiKey` and restart API/MCP.
- Unexpected writes: inspect idempotency records, audit logs, and affected list/task activity.
