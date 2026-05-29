# Infrastructure

Infrastructure is split by provider. Each provider directory is an independent Terraform root.

| Directory | Status | Purpose |
| --- | --- | --- |
| `hetzner/` | Active beta/staging | Hetzner Cloud VPS used by the current Docker Compose deployment. |
| `oci/` | Fallback/reference | Oracle Cloud Infrastructure Always Free experiment/fallback path. |
| `gcp/` | Legacy inactive | Previous Google Cloud stack kept for reference and migration history. |

Run Terraform from the provider directory you intend to manage.

```bash
cd infra/hetzner
terraform init
terraform plan
```

Do not run `apply` until the provider, cost, region, resource replacement, and deployment target have been reviewed.

Operational docs:

- [Deployment](../docs/DEPLOYMENT.md)
- [Operations](../docs/OPERATIONS.md)
- [Hetzner VPS runbook](../docs/operations/hetzner-vps-runbook.md)
- [Oracle fallback runbook](../docs/operations/oracle-free-tier-runbook.md)
