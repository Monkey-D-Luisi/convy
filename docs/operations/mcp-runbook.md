# ChatGPT MCP Operations Runbook

## Services

- API: OAuth broker, token minting, scope enforcement, audit ingestion.
- Auth: Firebase login and OAuth consent UI.
- MCP: Streamable HTTP MCP endpoint and Convy API tool adapter.

## Health Checks

```bash
curl -fsS https://api.convyapp.com/health/ready
curl -fsS https://auth.convyapp.com/health
curl -fsS https://mcp.convyapp.com/health
```

## Required Environment

- `CONVY_AUTH_HOSTNAME`
- `CONVY_MCP_HOSTNAME`
- `McpAuth__Issuer`
- `McpAuth__Audience`
- `McpAuth__AuthorizationEndpoint`
- `McpAuth__PrivateKeyPemBase64`
- `McpAuth__PublicKeyPemBase64`
- `McpAudit__ApiKey`

`ops/vps/push-secrets.ps1` preserves existing MCP keys and audit key from `/opt/convy/shared/api.env` when present. If both MCP keys are missing, it generates a new 4096-bit RSA key pair.

## Deploy

```bash
ops/vps/push-secrets.ps1 -HostName <server>
ops/vps/deploy-release.sh <release-sha>
```

The deploy script builds `api`, `dashboard`, `auth`, and `mcp`, recreates services, and checks API, auth, and MCP health endpoints.

## Audit Logs

MCP tool invocations are recorded in `mcp_tool_invocations`. The table stores:

- user ID
- optional household ID
- tool name
- status
- latency
- error type
- timestamp

It does not store prompts or full tool arguments.

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

The table does not store raw idempotency keys, prompts, or full tool arguments. Records expire after 24 hours.

## Incident Response

1. If a ChatGPT client is suspected of abuse, revoke its refresh token through `/oauth/revoke` or remove matching rows from active refresh tokens after confirming hashes.
2. If the MCP public key is exposed, no minting capability is exposed. Rotate keys if the API private key may also be exposed.
3. If the API private key is exposed, rotate the RSA pair immediately and restart API and MCP.
4. If the audit service key is exposed, rotate `McpAudit__ApiKey` and restart API and MCP.
5. If idempotency records grow unexpectedly, delete only expired rows from `mcp_idempotency_records` after confirming the active deployment is healthy.
