# Convy MCP Domain Cutover

This checklist moves staging from the temporary `178.105.70.69.nip.io` hosts to the `convyapp.com` public hostnames.

## DNS

Create A records pointing to `178.105.70.69`:

- `convyapp.com`
- `www.convyapp.com`
- `api.convyapp.com`
- `admin.convyapp.com`
- `auth.convyapp.com`
- `mcp.convyapp.com`
- `legal.convyapp.com`

Verify locally:

```powershell
Resolve-DnsName convyapp.com
Resolve-DnsName api.convyapp.com
Resolve-DnsName admin.convyapp.com
Resolve-DnsName auth.convyapp.com
Resolve-DnsName mcp.convyapp.com
Resolve-DnsName legal.convyapp.com
```

Each hostname must resolve to `178.105.70.69` before the VPS cutover can finish cleanly.

Keep the temporary `nip.io` hosts configured in Caddy during the cutover so previously installed Android staging builds continue to reach the API at `https://178.105.70.69.nip.io`. New Android staging builds default to `https://api.convyapp.com`.

## Firebase

Add these authorized domains in Firebase Authentication:

- `admin.convyapp.com`
- `auth.convyapp.com`

Keep Google Sign-In enabled for the project. The MCP authorization app supports both email/password and Google Sign-In, so users who registered with Google in the mobile app can authorize ChatGPT without creating a password.

Keep the Firebase project auth domain as `convy-6520d.firebaseapp.com`.

## VPS Secrets

Push hostnames after DNS is visible:

```powershell
$ip = "178.105.70.69"
.\ops\vps\push-secrets.ps1 `
  -HostName $ip `
  -ConvyHostname "convyapp.com" `
  -ConvyApiHostname "api.convyapp.com" `
  -ConvyAdminHostname "admin.convyapp.com" `
  -ConvyAuthHostname "auth.convyapp.com" `
  -ConvyMcpHostname "mcp.convyapp.com" `
  -ConvyLegalHostname "legal.convyapp.com"
```

The script also writes these temporary fallback hosts unless they are explicitly overridden:

- `CONVY_LEGACY_API_HOSTNAME=178.105.70.69.nip.io`
- `CONVY_LEGACY_ADMIN_HOSTNAME=admin.178.105.70.69.nip.io`
- `CONVY_LEGACY_AUTH_HOSTNAME=auth.178.105.70.69.nip.io`
- `CONVY_LEGACY_MCP_HOSTNAME=mcp.178.105.70.69.nip.io`
- `CONVY_LEGACY_LEGAL_HOSTNAME=legal.178.105.70.69.nip.io`

## Deploy And Smoke Test

Deploy the current release, then verify:

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

The admin response should be a Basic Auth challenge before Firebase login.

## ChatGPT Reconnect

After the domain health checks pass, recreate the ChatGPT Developer Mode connector with:

```text
https://mcp.convyapp.com/mcp
```

Authorize through `https://auth.convyapp.com`, then verify households, lists, shopping items, tasks, recent activity, and revocation.
