# Provider Layout and OCI Retry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make cloud infrastructure layout explicit by provider and make OCI Always Free provisioning retry across valid free-capacity placement options.

**Architecture:** Keep GCP and OCI Terraform roots independent under `infra/gcp` and `infra/oci`. OCI remains strict Always Free and uses Terraform variables to vary placement without introducing paid shapes.

**Tech Stack:** Terraform, Oracle OCI provider, Google Terraform provider, GitHub Actions, PowerShell automation.

---

### Task 1: Move GCP Terraform Root

**Files:**
- Move: `infra/main.tf` to `infra/gcp/main.tf`
- Move: `infra/variables.tf` to `infra/gcp/variables.tf`
- Move: `infra/outputs.tf` to `infra/gcp/outputs.tf`
- Move: `infra/terraform.tfvars.example` to `infra/gcp/terraform.tfvars.example`
- Move: `infra/modules` to `infra/gcp/modules`

- [ ] Create `infra/gcp`.
- [ ] Move the existing GCP Terraform root and modules into `infra/gcp`.
- [ ] Verify module source paths remain `./modules/...`.
- [ ] Leave `infra/oci` untouched except for OCI-specific changes.

### Task 2: Add OCI Fault Domain Placement

**Files:**
- Modify: `infra/oci/variables.tf`
- Modify: `infra/oci/main.tf`
- Modify: `infra/oci/terraform.tfvars.example`
- Modify: `infra/oci/README.md`
- Create: `ops/oci/retry-a1-provision.ps1`

- [ ] Add nullable `fault_domain` variable with validation for `FAULT-DOMAIN-1`, `FAULT-DOMAIN-2`, and `FAULT-DOMAIN-3`.
- [ ] Pass `fault_domain` to `oci_core_instance.api`.
- [ ] Create a PowerShell retry script that attempts default placement and all three fault domains.
- [ ] Document that retries can vary fault domain while staying on `VM.Standard.A1.Flex`.
- [ ] Keep the retry shape at 1 OCPU and 6 GB RAM unless explicitly changed.

### Task 3: Update Operational Docs and Automation

**Files:**
- Modify: `docs/operations/oracle-free-tier-runbook.md`
- Update automation: `retry-oci-a1`

- [ ] Update the runbook with the new `infra/gcp` and `infra/oci` layout.
- [ ] Update retry guidance to try default placement and all three OCI fault domains.
- [ ] Update the heartbeat automation prompt so each run attempts those placements in order.

### Task 4: Verify

**Commands:**
- `terraform fmt -check -recursive infra/gcp infra/oci`
- `terraform -chdir=infra/gcp init -backend=false`
- `terraform -chdir=infra/gcp validate`
- `terraform -chdir=infra/oci validate`
- `git status --short --branch`

- [ ] Run the commands from the worktree root.
- [ ] Fix any validation or formatting failure before committing.
- [ ] Confirm no Terraform plan files are left untracked.
