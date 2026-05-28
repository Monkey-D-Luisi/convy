# MCP Security Model

## Trust Boundaries

- ChatGPT is an OAuth client.
- `auth/` is the public user authorization surface.
- The API is the OAuth broker and policy enforcement point.
- `mcp/` is a resource server and tool adapter.
- PostgreSQL is only reachable by the API.

## Controls

- Firebase authenticates the Convy user during consent.
- CIMD client metadata must be HTTPS, fetch within timeout and size limits, match `client_id`, and contain the exact redirect URI.
- OAuth authorization codes and refresh tokens are stored as hashes.
- PKCE S256 is required for authorization code exchange.
- Access tokens are short-lived RS256 JWTs.
- The API signs with the private key. API and MCP validate with the public key.
- API endpoints enforce MCP scopes, not only MCP tool handlers.
- API write access for MCP is limited to creating and completing/uncompleting shopping items and tasks with `convy.items.write` or `convy.tasks.write`.
- Edit, delete, archive, invite, leave, admin metrics, backup, device, list-management, and household-management endpoints require Firebase auth and reject MCP tokens.
- `AdminOnly` requires Firebase auth and rejects `auth_source=mcp`.
- The MCP service validates issuer, audience, `token_use=mcp_access`, user ID subject, and supported Convy scopes, then checks required scopes per tool.
- MCP write endpoints require an `Idempotency-Key`; the API stores only the hashed key and request hash.
- MCP audit ingestion uses a service key and accepts no prompt or full argument fields.

## Key Rotation

1. Generate a new RSA key pair.
2. Update `McpAuth__PrivateKeyPemBase64` and `McpAuth__PublicKeyPemBase64`.
3. Redeploy API and MCP together.
4. Existing short-lived access tokens signed by the previous key will stop validating after deployment. Refresh tokens can still mint new access tokens unless revoked.

## Revocation

ChatGPT calls `POST /oauth/revoke` with the refresh token. The API hashes the presented token, finds the active refresh token, and revokes it. Revoked refresh tokens cannot rotate or mint new access tokens.
