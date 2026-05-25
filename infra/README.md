# Infrastructure

Infrastructure is split by cloud provider:

- `gcp/`: Legacy inactive Google Cloud Terraform stack for Cloud Run, Cloud SQL, Artifact Registry, Secret Manager, and networking. It is not part of the active staging deployment.
- `oci/`: Oracle Cloud Infrastructure Always Free Terraform stack for a possible future production target.
- `hetzner/`: Hetzner Cloud Terraform stack for the active staging VPS.

Each provider directory is an independent Terraform root. Run `terraform init`, `terraform plan`, and `terraform apply` from the provider directory you intend to manage.
