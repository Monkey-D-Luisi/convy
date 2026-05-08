# Convy OCI Always Free Infrastructure

This Terraform stack provisions the free hosting target for Convy:

- One `VM.Standard.A1.Flex` Ubuntu instance in `eu-madrid-1`
- Public subnet with ports `22`, `80`, and `443`
- One attached block volume mounted later at `/opt/convy`
- One private Object Storage bucket for encrypted PostgreSQL backups

The stack lives under `infra/oci`. The existing GCP stack lives under `infra/gcp`; keep it running until the data migration and mobile smoke tests pass.

## Preconditions

- OCI API key configured in `~/.oci/config`
- SSH key at `~/.ssh/convy_oci_deploy.pub`
- Free Tier strict account with home region `eu-madrid-1`

## First Apply

```powershell
cd infra/oci
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
terraform apply
```

Madrid currently exposes a single availability domain for this tenancy. If OCI returns `Out of host capacity`, keep `VM.Standard.A1.Flex` and retry the free shape with:

```powershell
..\..\ops\oci\retry-a1-provision.ps1
```

The retry script attempts default placement and `FAULT-DOMAIN-1` through `FAULT-DOMAIN-3` with 1 OCPU and 6 GB RAM. Do not switch to a paid shape.
