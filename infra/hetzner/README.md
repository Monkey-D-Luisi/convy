# Convy Hetzner Cloud Infrastructure

This Terraform stack provisions the low-cost VPS fallback for Convy while OCI Always Free capacity is unavailable.

## Token

Do not commit the Hetzner API token. The Terraform provider reads it from `HCLOUD_TOKEN`.

Recommended local token file:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.config\convy\secrets"
Set-Content -Path "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt" -Value "<token>"
```

The helper script `ops/hetzner/invoke-terraform.ps1` reads that file and exports `HCLOUD_TOKEN` only for the Terraform process. It also accepts the local fallback path `C:\Users\luiss\secrets\hetzner`, or any explicit path passed with `-TokenPath`.

## First Plan

```powershell
cd infra/hetzner
Copy-Item terraform.tfvars.example terraform.tfvars
..\..\ops\hetzner\invoke-terraform.ps1 plan
```

Do not run `apply` until the planned server type and monthly cost are accepted.

## Defaults

- `cx23`
- Ubuntu 24.04
- `fsn1`
- Ports `22`, `80`, and `443`
- Docker Compose stack with PostgreSQL, Convy API, and Caddy
