# Oracle Always Free Hosting Runbook

This runbook moves Convy from GCP Cloud Run and Cloud SQL to Oracle Cloud Infrastructure Always Free without removing current app capabilities.

## Architecture

- OCI `VM.Standard.A1.Flex` in `eu-madrid-1`
- Docker Compose runs PostgreSQL, the ASP.NET Core API, and Caddy
- Caddy terminates HTTPS for the `sslip.io` hostname
- Firebase Auth and FCM remain enabled through a Firebase Admin JSON mounted into the API container
- OpenAI voice parsing remains enabled through `OPENAI_API_KEY`

## Provision

```powershell
cd infra/oci
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply
```

Use the tenancy OCID as `compartment_ocid` for a strict Free Tier account. If OCI reports no A1 capacity, set `availability_domain_index` to another value and retry.

After apply:

```powershell
$ip = terraform output -raw public_ip
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" ..\..\ops\oci\bootstrap-server.sh "ubuntu@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo bash /tmp/bootstrap-server.sh"
..\..\ops\oci\push-secrets.ps1 -HostName $ip -ConvyHostname "$(terraform output -raw sslip_hostname)"
```

## GitHub Secrets

Set these repository secrets before merging the CD workflow:

- `OCI_DEPLOY_HOST`: public IP from Terraform output
- `OCI_PUBLIC_HOSTNAME`: hostname from Terraform output, for example `1.2.3.4.sslip.io`
- `OCI_SSH_PRIVATE_KEY`: contents of `~/.ssh/convy_oci_deploy`

## First Deploy

From GitHub Actions, the CD workflow uploads the checked-out commit to the VM and runs `ops/oci/deploy-release.sh`. For a manual first deploy:

```powershell
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "$env:TEMP\convy-release.tar.gz" "ubuntu@${ip}:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo mkdir -p /opt/convy/releases/manual && sudo tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/manual && sudo /opt/convy/releases/manual/ops/oci/deploy-release.sh manual"
```

## Data Migration

Use a short write-freeze window:

1. Confirm the OCI deployment is healthy with an empty database.
2. Disable writes or scale down the GCP Cloud Run production service.
3. Export Cloud SQL PostgreSQL with `pg_dump`.
4. Copy the dump to the OCI VM.
5. Restore into `convy-db`.
6. Compare row counts for users, households, lists, items, invites, activity logs, and device tokens.
7. Validate login, household loading, item creation, SignalR updates, FCM push, and voice parsing.
8. Keep GCP resources for rollback until the mobile app has used OCI successfully.

## Backups

After first deploy, install the daily backup timer:

```bash
sudo /opt/convy/current/ops/oci/install-backup-timer.sh
```

Backups are stored under `/opt/convy/backups/postgres` with local retention. Copy encrypted dumps to OCI Object Storage while staying under the Always Free storage limit.
