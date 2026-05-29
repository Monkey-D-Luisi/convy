# ChatGPT OAuth Flow

Convy owns the OAuth broker for the ChatGPT MCP integration. Firebase authenticates the Convy user during consent; Convy issues MCP-specific access and refresh tokens.

## Endpoints

| Endpoint | URL |
| --- | --- |
| MCP endpoint | `https://mcp.convyapp.com/mcp` |
| Protected resource metadata | `https://mcp.convyapp.com/.well-known/oauth-protected-resource` |
| Authorization | `https://auth.convyapp.com/oauth/authorize` |
| Token | `https://auth.convyapp.com/oauth/token` |
| Revocation | `https://auth.convyapp.com/oauth/revoke` |
| Authorization server metadata | `https://auth.convyapp.com/.well-known/oauth-authorization-server` |

## Discovery

1. ChatGPT calls the MCP endpoint without a valid bearer token.
2. The MCP service returns `401` with a `WWW-Authenticate` challenge.
3. The challenge points to protected resource metadata.
4. Protected resource metadata advertises the resource URL and authorization server.
5. Authorization server metadata advertises authorization, token, revocation, and supported scopes.

## Authorization Code Flow

1. ChatGPT starts authorization at `auth.convyapp.com/oauth/authorize`.
2. Request includes `client_id`, exact `redirect_uri`, `resource`, `scope`, `state`, `code_challenge`, and `code_challenge_method=S256`.
3. The auth app validates request shape and displays Firebase login.
4. User signs in with Firebase email/password or Google Sign-In.
5. Consent page displays requested scopes and explicitly excluded capabilities.
6. Auth app sends the Firebase ID token and OAuth request details to `POST /api/v1/mcp/oauth/authorize/approve`.
7. API validates Firebase token, client metadata, exact client ID, exact redirect URI, resource, scopes, and PKCE method.
8. API stores only a hash of the authorization code.
9. API redirects ChatGPT to the validated redirect URI with `code` and `state`.
10. ChatGPT exchanges the code at `/oauth/token` with the PKCE verifier.

## Token Rules

- Authorization codes are single-use and expire.
- Refresh tokens are stored as hashes.
- Refresh token grants rotate refresh tokens.
- Revocation hashes the presented token and revokes the matching active refresh token.
- Access tokens are short-lived RS256 JWTs.
- Access token audience is `https://mcp.convyapp.com`.
- Access token issuer is `https://auth.convyapp.com`.
- Access tokens include `token_use=mcp_access`, `auth_source=mcp`, `sub`, `client_id`, and space-delimited `scope`.
- API signs with the private key.
- API and MCP validate with the public key.

## Firebase Requirements

Firebase Authentication authorized domains must include:

- `auth.convyapp.com`
- `admin.convyapp.com`

Google Sign-In must remain enabled for users who registered with Google in the mobile app.

## Failure Modes

| Failure | Expected behavior |
| --- | --- |
| Missing/invalid MCP token | MCP returns 401 and an OAuth challenge. |
| Unsupported scope | Authorization request is rejected. |
| Redirect URI mismatch | Authorization approval fails. |
| Invalid client metadata | Authorization approval fails. |
| PKCE missing or not S256 | Authorization approval or token exchange fails. |
| Reused authorization code | Token exchange fails. |
| Revoked refresh token | Refresh fails. |
