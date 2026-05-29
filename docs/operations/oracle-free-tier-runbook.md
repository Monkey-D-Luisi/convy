# Oracle Always Free Fallback Runbook

OCI is a fallback/reference target, not the active beta/staging deployment. The active path is Hetzner VPS.

Use this runbook only when intentionally testing or reviving the OCI path. Do not document OCI as production unless Compose, Caddy, auth, MCP, dashboard, legal, public site, backups, and deploy scripts have been validated for parity.

## Current Scope

The OCI Terraform stack provisions:

- one `VM.Standard.A1.Flex` Ubuntu instance in `eu-madrid-1`
- public subnet with ports `22`, `80`, and `443`
- block volume for `/opt/convy`
- optional Object Storage bucket for encrypted backup future use

The existing OCI Compose/deploy files may lag behind the Hetzner stack. Validate before use.

## Preconditions

- OCI CLI config in `~/.oci/config`
- SSH key at `~/.ssh/convy_oci_deploy.pub`
- strict Free Tier account in `eu-madrid-1`
- accepted risk that OCI A1 capacity may be unavailable

## Provision

```powershell
cd infra/oci
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply
```

If OCI reports `Out of host capacity`, retry free placement options:

```powershell
..\..\ops\oci\retry-a1-provision.ps1
```

The retry script keeps `VM.Standard.A1.Flex`, 1 OCPU, and 6 GB RAM, then tries default placement and `FAULT-DOMAIN-1` through `FAULT-DOMAIN-3`. Do not switch to paid shapes.

## Bootstrap And Secrets

```powershell
$ip = terraform output -raw public_ip
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" ..\..\ops\oci\bootstrap-server.sh "ubuntu@${ip}:/tmp/bootstrap-server.sh"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo bash /tmp/bootstrap-server.sh"
..\..\ops\oci\push-secrets.ps1 -HostName $ip -ConvyHostname "$(terraform output -raw sslip_hostname)"
```

Review `ops/oci/push-secrets.ps1` before use. It may not cover the full current Hetzner auth/MCP/dashboard secret set.

## Deploy

```powershell
git archive --format=tar.gz -o "$env:TEMP\convy-release.tar.gz" HEAD
scp -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "$env:TEMP\convy-release.tar.gz" "ubuntu@${ip}:/tmp/convy-release.tar.gz"
ssh -i "$env:USERPROFILE\.ssh\convy_oci_deploy" "ubuntu@${ip}" "sudo mkdir -p /opt/convy/releases/manual && sudo tar -xzf /tmp/convy-release.tar.gz -C /opt/convy/releases/manual && sudo /opt/convy/releases/manual/ops/oci/deploy-release.sh manual"
```

## Parity Checklist Before Use

- Compose includes all required active services.
- Caddy routes public, API, admin, auth, MCP, and legal hosts.
- Firebase authorized domains are correct.
- MCP issuer/audience/resource URLs match the target host.
- API, auth, MCP, dashboard, legal, and public health checks pass.
- Backups and restore verification work.
- CD workflow target is intentionally configured for OCI.

## Backups

The OCI path has a daily backup timer. It does not replace the Hetzner backup/restore runbook until validated for current schema and services.

```bash
sudo /opt/convy/current/ops/oci/install-backup-timer.sh
```

If OCI becomes active, create a dedicated backup and restore validation pass before moving users.
