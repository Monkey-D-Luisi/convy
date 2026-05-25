# Oracle Always Free Hosting Runbook

This runbook describes a possible future move to Oracle Cloud Infrastructure Always Free without removing current app capabilities. It is not the active staging deployment path.

## Architecture

- OCI `VM.Standard.A1.Flex` in `eu-madrid-1`
- Docker Compose runs PostgreSQL, the ASP.NET Core API, and Caddy
- Caddy terminates HTTPS for the `sslip.io` hostname
- Firebase Auth and FCM remain enabled through a Firebase Admin JSON mounted into the API container
- OpenAI voice parsing remains enabled through `OPENAI_API_KEY`

## Infrastructure Layout

- `infra/gcp`: legacy GCP Terraform stack for reference during migration.
- `infra/oci`: OCI Always Free Terraform stack for a possible future production host.

Run Terraform from the provider directory you intend to manage. Do not run OCI commands from `infra/gcp`, and do not run GCP commands from `infra/oci`.

## Provision

```powershell
cd infra/oci
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply
```

Use the tenancy OCID as `compartment_ocid` for a strict Free Tier account.

If OCI reports no A1 capacity, retry all free placement options:

```powershell
..\..\ops\oci\retry-a1-provision.ps1
```

The retry script keeps `VM.Standard.A1.Flex`, 1 OCPU, and 6 GB RAM, then tries default placement and `FAULT-DOMAIN-1` through `FAULT-DOMAIN-3`. Do not switch to paid shapes.

After apply:

```powershell
$ip = terraform output -raw public_ip
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" ..\..\ops\oci\bootstrap-server.sh "ubuntu@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo bash /tmp/bootstrap-server.sh"
..\..\ops\oci\push-secrets.ps1 -HostName $ip -ConvyHostname "$(terraform output -raw sslip_hostname)"
```

## GitHub CD

The active CD workflow currently deploys staging to Hetzner with `STAGING_*` secrets and variables. If OCI becomes the production target later, create a dedicated production CD workflow or intentionally retarget the shared workflow.

For a future production OCI workflow, set these repository secrets:

- `PRODUCTION_DEPLOY_HOST`: public IP from Terraform output.
- `PRODUCTION_PUBLIC_HOSTNAME`: hostname from Terraform output, for example `1.2.3.4.sslip.io`.
- `PRODUCTION_SSH_PRIVATE_KEY`: contents of `~/.ssh/convy_oci_deploy`.

For a future production OCI workflow, set these repository variables:

- `PRODUCTION_DEPLOY_USER`: `ubuntu` for OCI.
- `PRODUCTION_DEPLOY_SCRIPT`: `ops/oci/deploy-release.sh` for OCI.

## First Deploy

From GitHub Actions, a future production workflow can upload the checked-out commit to the VM and run `ops/oci/deploy-release.sh` when `PRODUCTION_DEPLOY_SCRIPT` is set as above. For a manual first deploy:

```powershell
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "$env:TEMP\convy-release.tar.gz" "ubuntu@${ip}:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo mkdir -p /opt/convy/releases/manual && sudo tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/manual && sudo /opt/convy/releases/manual/ops/oci/deploy-release.sh manual"
```

## Data Migration

Use a short write-freeze window:

1. Confirm the OCI deployment is healthy with an empty database.
2. Disable writes or scale down the previous live service.
3. Export Cloud SQL PostgreSQL with `pg_dump`.
4. Copy the dump to the OCI VM.
5. Restore into `convy-db`.
6. Compare row counts for `users`, `households`, `household_memberships`, `household_lists`, `list_items`, `task_items`, `invites`, `activity_logs`, `device_tokens`, and `notification_preferences`.
7. Validate login, household loading, item creation, task creation, notification preferences, SignalR updates, FCM push, and voice parsing.
8. Keep GCP resources for rollback until the mobile app has used OCI successfully.

## Backups

After first deploy, install the daily backup timer:

```bash
sudo /opt/convy/current/ops/oci/install-backup-timer.sh
```

Backups are stored under `/opt/convy/backups/postgres` with local retention. Copy encrypted dumps to OCI Object Storage while staying under the Always Free storage limit.
