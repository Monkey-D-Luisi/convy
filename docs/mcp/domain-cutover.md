# Convy MCP Domain Cutover

This checklist covers public hostname cutover from temporary `nip.io` hosts to `convyapp.com` hosts.

## DNS

Create A records pointing to the staging VPS IP:

- `convyapp.com`
- `www.convyapp.com`
- `api.convyapp.com`
- `admin.convyapp.com`
- `auth.convyapp.com`
- `mcp.convyapp.com`
- `legal.convyapp.com`

Verify:

```powershell
Resolve-DnsName convyapp.com
Resolve-DnsName www.convyapp.com
Resolve-DnsName api.convyapp.com
Resolve-DnsName admin.convyapp.com
Resolve-DnsName auth.convyapp.com
Resolve-DnsName mcp.convyapp.com
Resolve-DnsName legal.convyapp.com
```

Keep legacy `nip.io` hosts configured during cutover so installed staging Android builds can still reach the API.

## Firebase

Add authorized domains:

- `admin.convyapp.com`
- `auth.convyapp.com`

Keep the Firebase project auth domain as `convy-6520d.firebaseapp.com`. Keep Google Sign-In enabled for users who registered with Google in the mobile app.

## VPS Secrets

Push hostnames after DNS is visible:

```powershell
$ip = "<staging-vps-ip>"
.\ops\vps\push-secrets.ps1 `
  -HostName $ip `
  -ConvyHostname "convyapp.com" `
  -ConvyApiHostname "api.convyapp.com" `
  -ConvyAdminHostname "admin.convyapp.com" `
  -ConvyAuthHostname "auth.convyapp.com" `
  -ConvyMcpHostname "mcp.convyapp.com" `
  -ConvyLegalHostname "legal.convyapp.com"
```

The script writes public and legacy hostname variables into `/opt/convy/shared/api.env`.

## Deploy And Smoke Test

```powershell
curl.exe -fsS https://convyapp.com
curl.exe -fsS https://api.convyapp.com/health/ready
curl.exe -fsS https://auth.convyapp.com/health
curl.exe -fsS https://mcp.convyapp.com/health
curl.exe -fsS https://mcp.convyapp.com/.well-known/oauth-protected-resource
curl.exe -fsS https://auth.convyapp.com/.well-known/oauth-authorization-server
curl.exe -fsS https://legal.convyapp.com/privacy
curl.exe -fsS https://legal.convyapp.com/terms
curl.exe -I https://admin.convyapp.com
curl.exe -fsS https://178.105.70.69.nip.io/health/ready
```

Expected:

- Public landing page loads.
- Legal pages load.
- API, auth, and MCP health endpoints pass.
- MCP metadata and authorization metadata are reachable.
- Admin host returns a Basic Auth challenge before Firebase login.
- Legacy API health works until old staging builds are no longer relevant.

## ChatGPT Reconnect

After health checks pass, recreate the ChatGPT Developer Mode connector with:

```text
https://mcp.convyapp.com/mcp
```

Authorize through `https://auth.convyapp.com`, then verify households, shopping context, shopping items, tasks, recent activity, write tools, audit records, and revocation.

## Documentation Updates

When domains change, update:

- `README.md`
- `docs/DEPLOYMENT.md`
- `docs/OPERATIONS.md`
- `docs/mcp/*`
- `docs/operations/*`
- `legal/*`
- `public-site/index.html`
- Firebase authorized domain notes
