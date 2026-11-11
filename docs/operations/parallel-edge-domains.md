# Parallel public domains

The new public addresses are `convy.monkeydluisi.com`, `api-convy.monkeydluisi.com`,
`admin-convy.monkeydluisi.com`, `auth-convy.monkeydluisi.com`,
`mcp-convy.monkeydluisi.com` and `legal-convy.monkeydluisi.com`.
Keep every `convyapp.com` and retained nip.io address operational with trusted TLS.
Do not redirect API, token or MCP requests to migrate clients.

Keep the issuer **https://auth.convyapp.com**, the RSA signing key, Firebase project,
client metadata validation, PKCE, callback allowlists and stored grants unchanged.
Set `McpAuth__AdditionalAudiences__0=https://mcp-convy.monkeydluisi.com` on the API,
and `MCP_PUBLIC_URL_ALIASES` and `MCP_JWT_AUDIENCE_ALIASES` to that origin on MCP.
Keep `MCP_PUBLIC_URL`, `MCP_JWT_AUDIENCE`, `AUTH_PUBLIC_URL` and `MCP_JWT_ISSUER`
at their existing legacy values. The alias advertises its own resource and the
existing authorization server. Both explicitly configured audiences are accepted;
each authorization code and refresh token remains bound to its original resource.
The issuer is deliberately stable throughout this compatibility phase. The auth
alias serves the same UI and canonical discovery, without claiming a new issuer.

The Android staging build now defaults to `api-convy.monkeydluisi.com`. Existing
installed builds continue to call `api.convyapp.com`; their Firebase identity and
FCM registration contracts are unchanged. `CONVY_STAGING_API_HOST` remains an
explicit build override. Verify login, households/lists, SignalR reconnect and FCM
on a real device before publishing an update; an API test is not device acceptance.
Publishing to Google Play remains an owner action.

SignalR resolves the authenticated connection principal inside each hub invocation
scope. The HTTP middleware's scoped user resolution does not carry into those
scopes. Keep the invocation filter enabled so registered household members can
join and leave their notification group; the existing membership checks continue
to reject outsiders for both legacy and alias connections.

Add the new public/auth/admin origins to Firebase authorized domains without
removing any existing entry. Verify existing OAuth callback metadata unchanged.
An existing ChatGPT connection keeps using its legacy URL and resource. To migrate
it, the owner reconnects using `https://mcp-convy.monkeydluisi.com/mcp`, completes
the existing OAuth consent flow, and checks authorized reads and SSE. Do not reuse
an old resource-bound refresh token as a grant for the new resource.

Runtime migration and public DNS require the infrastructure cutover acceptance;
this source change alone does not assert that those addresses are deployed.
