# Public Endpoint Checks

Checked on 2026-05-29 at approximately 17:51 Europe/Warsaw from the Codex workspace.

| Check | Command | Expected | Observed |
| --- | --- | --- | --- |
| MCP health | `curl.exe -i https://mcp.convyapp.com/health` | `200 OK` JSON health response | `200 OK`, `{"status":"ok"}` |
| MCP protected resource metadata | `curl.exe -i https://mcp.convyapp.com/.well-known/oauth-protected-resource` | `200 OK` JSON metadata | `200 OK`; resource `https://mcp.convyapp.com`; auth server `https://auth.convyapp.com`; only Convy read/write scopes advertised. |
| Auth server metadata | `curl.exe -i https://auth.convyapp.com/.well-known/oauth-authorization-server` | `200 OK` JSON metadata | `200 OK`; authorization, token, revocation, PKCE S256, and Convy scopes advertised. |
| Privacy page | `curl.exe -i https://legal.convyapp.com/privacy` | `200 OK` HTML | `200 OK` HTML with static CSP. |
| Terms page | `curl.exe -i https://legal.convyapp.com/terms` | `200 OK` HTML | `200 OK` HTML with static CSP. |
| Landing page | `curl.exe -i https://convyapp.com` | `200 OK` HTML | `200 OK` HTML with static CSP. |
| MCP OAuth challenge | `curl.exe -i -X POST https://mcp.convyapp.com/mcp -H "content-type: application/json" -d "{}"` | `401 Unauthorized` with `WWW-Authenticate` challenge | `401 Unauthorized`; `resource_metadata="https://mcp.convyapp.com/.well-known/oauth-protected-resource"`. |

## Notes

- `GET https://mcp.convyapp.com/mcp` returns `404 Not Found`; use `POST /mcp` for the MCP OAuth challenge smoke test.
- Public legal and landing pages must be redeployed after this branch is merged so the live wording matches the submission copy.
