# Convy Hetzner Infrastructure

This Terraform stack provisions the active Convy beta/staging VPS.

## Token

Do not commit the Hetzner API token. The helper script reads it from a local file and exports `HCLOUD_TOKEN` only for Terraform.

Recommended path:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.config\convy\secrets"
Set-Content -Path "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt" -Value "<token>"
```

## Plan

```powershell
cd infra/hetzner
Copy-Item terraform.tfvars.example terraform.tfvars
..\..\ops\hetzner\invoke-terraform.ps1 init
..\..\ops\hetzner\invoke-terraform.ps1 plan
```

Do not apply until resource replacement, server type, location, SSH key, and monthly cost are accepted.

## Defaults

- Ubuntu 24.04
- Hetzner location from Terraform variables
- firewall for ports `22`, `80`, and `443`
- Docker Compose runtime installed by `ops/vps/bootstrap-server.sh`
- active services deployed by `ops/vps/deploy-release.sh`

See [Hetzner VPS runbook](../../docs/operations/hetzner-vps-runbook.md).
