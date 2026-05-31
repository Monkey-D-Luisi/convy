# ChatGPT MCP Operations Runbook

## Services

- API: OAuth broker, token minting, token revocation, scope enforcement, write idempotency, audit ingestion.
- Auth: Firebase login and OAuth consent UI.
- MCP: Streamable HTTP MCP endpoint and Convy API tool adapter.
- Dashboard: admin MCP overview and runtime status.

## Health Checks

```bash
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
curl -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl -fsS https://auth.convyapp.com/.well-known/oauth-authorization-server
```

Expected OpenAI Apps domain challenge when `OPENAI_APPS_CHALLENGE_TOKEN` is configured:

```bash
curl -fsS https://mcp.convyapp.com/.well-known/openai-apps-challenge
```

Response should be `200 OK`, `text/plain`, and the exact configured token body. If the token is not configured, the endpoint intentionally returns `404` as `text/plain`.

Expected OAuth challenge:

```bash
curl -i -X POST https://mcp.convyapp.com/mcp -H "content-type: application/json" -d "{}"
```

Response should be `401` with `WWW-Authenticate` pointing to protected resource metadata.

## Required Environment

- `CONVY_AUTH_HOSTNAME`
- `CONVY_MCP_HOSTNAME`
- `McpAuth__Issuer`
- `McpAuth__Audience`
- `McpAuth__AuthorizationEndpoint`
- `McpAuth__PrivateKeyPemBase64`
- `McpAuth__PublicKeyPemBase64`
- `McpAuth__AllowedClientMetadataHosts__0=chat.openai.com`
- `McpAuth__AllowedClientMetadataHosts__1=chatgpt.com`
- `McpAudit__ApiKey`
- `MCP_PUBLIC_URL`
- `AUTH_PUBLIC_URL`
- `MCP_JWT_ISSUER`
- `MCP_JWT_AUDIENCE`
- `MCP_JWT_PUBLIC_KEY_BASE64`
- `CONVY_MCP_AUDIT_API_KEY`
- `OPENAI_APPS_CHALLENGE_TOKEN` when OpenAI Apps domain verification is pending or being rechecked

`ops/vps/push-secrets.ps1` preserves existing MCP keys, audit key, and OpenAI Apps challenge token from `/opt/convy/shared/api.env` when present. If both MCP keys are missing, it generates a new RSA key pair.

## Deploy

```bash
ops/vps/push-secrets.ps1 -HostName <server>
ops/vps/deploy-release.sh <release-sha>
```

The deploy script builds `api`, `worker`, `dashboard`, `auth`, and `mcp`, recreates services, and checks API, auth, and MCP health endpoints.

## Validate Scopes

```bash
curl -fsS https://auth.convyapp.com/.well-known/oauth-authorization-server
```

Confirm supported scopes are limited to:

- `convy.households.read`
- `convy.lists.read`
- `convy.items.read`
- `convy.tasks.read`
- `convy.activity.read`
- `convy.items.write`
- `convy.tasks.write`

## Audit Logs

MCP tool invocations are recorded in `mcp_tool_invocations` with:

- user ID
- optional household ID
- optional OAuth client ID
- tool name
- status
- latency
- error type
- timestamp

Audit records do not store prompts or full tool arguments.

## Write Idempotency

MCP write calls require `Idempotency-Key`. The API stores records in `mcp_idempotency_records` with:

- user ID
- OAuth client ID
- hashed idempotency key
- action name
- request hash
- status code
- optional location
- compact response JSON
- creation and expiry timestamps

Records expire after 24 hours.

## Revoke Refresh Tokens

Normal path: ChatGPT calls `POST /oauth/revoke`.

Incident path: identify affected active refresh token records by user/client/resource metadata and revoke them after confirming hashes and scope. Do not delete records blindly; retain forensic value when possible.

## Rotate MCP Keys

1. Generate a new RSA key pair.
2. Set `MCP_AUTH_PRIVATE_KEY_PEM_BASE64` and `MCP_AUTH_PUBLIC_KEY_PEM_BASE64`.
3. Run `ops/vps/push-secrets.ps1`.
4. Redeploy API and MCP together.
5. Re-run OAuth metadata, MCP health, and ChatGPT Developer Mode tests.

## Disable MCP

Options:

- stop `convy-mcp` container
- remove the MCP Caddy route and reload Caddy
- rotate keys to invalidate current access tokens
- revoke affected refresh tokens

After disabling, confirm:

```bash
curl -i -X POST https://mcp.convyapp.com/mcp -H "content-type: application/json" -d "{}"
```

The endpoint should be unavailable or intentionally blocked.

## Incident Response

1. Preserve relevant logs.
2. Disable MCP if active misuse is ongoing.
3. Rotate exposed keys/secrets.
4. Revoke affected refresh tokens.
5. Review `mcp_tool_invocations` and idempotency records.
6. Validate household/list/task/item state with the affected user if needed.
7. Re-enable MCP only after smoke tests pass.
