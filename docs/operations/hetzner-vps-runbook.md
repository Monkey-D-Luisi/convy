# Hetzner VPS Hosting Runbook

This runbook is the low-cost fallback while Oracle Always Free A1 capacity is unavailable. It keeps the same runtime shape as the OCI target: Docker Compose with PostgreSQL, the ASP.NET Core API, and Caddy.

## Infrastructure Layout

- `infra/gcp`: current Google Cloud Terraform stack for rollback and reference.
- `infra/oci`: Oracle Always Free Terraform stack and retry automation.
- `infra/hetzner`: Hetzner Cloud Terraform stack for the paid low-cost fallback.
- `ops/oci`: Oracle-specific retry and bootstrap helpers.
- `ops/vps`: provider-neutral VPS bootstrap, secret push, and deployment helpers.

## Token

Store the Hetzner API token outside the repo:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.config\convy\secrets"
Set-Content -Path "$env:USERPROFILE\.config\convy\secrets\hcloud-token.txt" -Value "<token>"
```

Do not paste the token into chat and do not put it in `terraform.tfvars`.
The Terraform helper also accepts the local fallback path `C:\Users\luiss\secrets\hetzner`, or any explicit path passed with `-TokenPath`.

## Provision

```powershell
cd infra/hetzner
Copy-Item terraform.tfvars.example terraform.tfvars
..\..\ops\hetzner\invoke-terraform.ps1 init
..\..\ops\hetzner\invoke-terraform.ps1 plan
```

Only run `apply` after confirming the planned server type:

```powershell
..\..\ops\hetzner\invoke-terraform.ps1 apply
```

## First Bootstrap

```powershell
$ip = terraform output -raw public_ip
scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" ..\..\ops\vps\bootstrap-server.sh "root@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "root@${ip}" "bash /tmp/bootstrap-server.sh"
..\..\ops\vps\push-secrets.ps1 -HostName $ip -ConvyHostname "$(terraform output -raw public_hostname)"
```

## First Deploy

```powershell
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "$env:TEMP\convy-release.tar.gz" "root@${ip}:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_vps_deploy" "root@${ip}" "mkdir -p /opt/convy/releases/manual && tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/manual && /opt/convy/releases/manual/ops/vps/deploy-release.sh manual"
```

## GitHub CD

The CD workflow is provider-neutral and deploys the commit that passed CI.

Set these repository secrets:

- `PRODUCTION_DEPLOY_HOST`: public IPv4 address of the VPS.
- `PRODUCTION_PUBLIC_HOSTNAME`: public HTTPS hostname, for example `178.105.70.69.nip.io`.
- `PRODUCTION_SSH_PRIVATE_KEY`: private deploy key matching the public key provisioned in Hetzner.

Set these repository variables:

- `PRODUCTION_DEPLOY_USER`: `root` for Hetzner.
- `PRODUCTION_DEPLOY_SCRIPT`: `ops/vps/deploy-release.sh` for Hetzner.

## Data Migration

Use the same short write-freeze process as OCI:

1. Confirm the Hetzner deployment is healthy with an empty database.
2. Disable writes or scale down the GCP Cloud Run production service.
3. Export Cloud SQL PostgreSQL with `pg_dump`.
4. Copy the dump to the VPS.
5. Restore into `convy-db`.
6. Compare row counts for `users`, `households`, `household_memberships`, `household_lists`, `list_items`, `task_items`, `invites`, `activity_logs`, `device_tokens`, and `notification_preferences`.
7. Validate login, household loading, item creation, task creation, notification preferences, SignalR updates, FCM push, and voice parsing.
8. Keep GCP resources for rollback until the mobile app has used Hetzner successfully.
