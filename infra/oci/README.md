# Convy OCI Always Free Infrastructure

This Terraform stack is a fallback/reference path, not the active beta/staging deployment.

It provisions:

- one `VM.Standard.A1.Flex` Ubuntu instance in `eu-madrid-1`
- public subnet with ports `22`, `80`, and `443`
- attached block volume for `/opt/convy`
- optional Object Storage bucket for encrypted backup experiments

## Preconditions

- OCI API key configured in `~/.oci/config`
- SSH key at `~/.ssh/convy_oci_deploy.pub`
- strict Free Tier account

## Plan

```powershell
cd infra/oci
Copy-Item terraform.tfvars.example terraform.tfvars
terraform init
terraform plan
```

If OCI returns `Out of host capacity`, keep the free shape and retry placement:

```powershell
..\..\ops\oci\retry-a1-provision.ps1
```

The retry script attempts default placement and `FAULT-DOMAIN-1` through `FAULT-DOMAIN-3` with 1 OCPU and 6 GB RAM. Do not switch to a paid shape.

Before using OCI for any live environment, validate parity with the active Hetzner stack. See [Oracle fallback runbook](../../docs/operations/oracle-free-tier-runbook.md).
