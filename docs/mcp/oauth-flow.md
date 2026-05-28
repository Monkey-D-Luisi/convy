# ChatGPT OAuth Flow

## Endpoints

- Authorization: `https://auth.convyapp.com/oauth/authorize`
- Token: `https://auth.convyapp.com/oauth/token`
- Revocation: `https://auth.convyapp.com/oauth/revoke`
- Authorization server metadata: `https://auth.convyapp.com/.well-known/oauth-authorization-server`
- Protected resource metadata: `https://mcp.convyapp.com/.well-known/oauth-protected-resource`

## Flow

1. ChatGPT discovers the protected resource metadata from the MCP 401 challenge.
2. ChatGPT starts authorization at `auth.convyapp.com/oauth/authorize` with PKCE S256, exact redirect URI, resource, and requested Convy scopes.
3. The user signs in with Firebase on `auth/` using email/password or Google Sign-In.
4. The consent page shows read permissions, limited item/task write permissions, and excluded edit/delete/admin capabilities.
5. `auth/` sends the Firebase ID token and OAuth request to `POST /api/v1/mcp/oauth/authorize/approve`.
6. The API validates CIMD client metadata, exact `client_id`, exact redirect URI, resource, scopes, and PKCE method.
7. The API stores only a hash of the authorization code and redirects ChatGPT back to the validated redirect URI.
8. ChatGPT exchanges the code at `/oauth/token`.
9. The API validates single use, expiry, client, redirect URI, resource, and PKCE verifier before issuing a short-lived RS256 access token and refresh token.
10. Refresh token grants rotate refresh tokens. Revocation invalidates the current refresh token.

## Token Rules

- Access tokens are asymmetric JWTs signed by the API private key.
- The MCP service only receives the public key.
- Access tokens include `token_use=mcp_access`, `auth_source=mcp`, `sub`, `client_id`, and a space-delimited `scope`.
- Refresh tokens and authorization codes are never stored raw.

## Firebase Requirements

- `auth.convyapp.com` must be present in Firebase Authentication authorized domains.
- `admin.convyapp.com` must be present in Firebase Authentication authorized domains for the admin dashboard.
- The Google sign-in provider must remain enabled in Firebase Authentication for users who registered with Google in the mobile app.
