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

Existing MCP keys, audit key and OpenAI Apps challenge token are protected staging configuration. Ordinary releases preserve them. Credential provisioning or rotation requires a separate administrator operation; it is not part of CD.

## Deploy

Hetzner is shared **staging**; production does not exist. Follow [automatic staging CD](staging-cd.md) for the activation boundary and [isolated release and rollback](safe-release.md) for the trusted artifact, protected host profile, dry run and journal recovery commands.

CD is inactive until its separately approved first activation. After activation, a reviewed compatible merge to `master` with successful exact master CI automatically builds an immutable artifact, verifies provenance, acquires the common host lock, verifies backup/restore/retrieval, and applies only changed Convy applications and public content. Ordinary compatible releases need no additional human approval. The fixed installed broker never executes uploaded source or builds on Hetzner.

Acceptance checks cover API readiness, auth/MCP health and OAuth metadata, protected-resource discovery and challenge behavior. A failure restores the prior affected applications, `legal/` and `public-site/`, public release metadata, source pointer and CI ledger from the checksummed journal. Use the fixed installed controller's `rollback-plan` and approved exceptional `rollback` procedure from the recovery runbook; do not rebuild an old release or recreate the shared stack.

Caddy, PostgreSQL, credentials and Converso remain outside the application update. Automatic releases preserve the current parsing model even when Luna code is included; a model transition is a separate explicit configuration rollout. Android publication retains its own version-change, successful master-CI and protected-environment safeguards; the staging Android metadata is the source-declared version, not proof of Play publication.

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

Key rotation requires a separate approved administrator procedure. Ordinary CD preserves credentials and rejects unreviewed protected configuration drift; its application rollback is not a key-rotation transaction.

1. Prepare the private key-generation, revocation and recovery plan; prevent recovery from reintroducing a compromised key.
2. Coordinate all deployment writers under the common host lock from the staging runbook.
3. Update only the approved API/MCP credential configuration and reconcile the protected host profile through the reviewed rotation procedure. Do not invoke legacy secret-upload or deployment helpers as part of a normal release.
4. Apply and verify the coordinated API/MCP credential transition using that administrator procedure, preserving Caddy, PostgreSQL and Converso.
5. Re-run OAuth metadata, MCP health and ChatGPT Developer Mode tests, then resume ordinary CD only after the new protected baseline is verified.

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
