# Convy OCI Always Free Infrastructure

This Terraform stack provisions the free hosting target for Convy:

- One `VM.Standard.A1.Flex` Ubuntu instance in `eu-madrid-1`
- Public subnet with ports `22`, `80`, and `443`
- One attached block volume mounted later at `/opt/convy`
- One private Object Storage bucket for encrypted PostgreSQL backups

The stack intentionally lives next to the existing GCP Terraform instead of replacing it. Keep GCP running until the data migration and mobile smoke tests pass.

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

If OCI returns an Ampere capacity error, update `availability_domain_index` to `1` or `2` and retry. Do not change the shape to a paid shape.
