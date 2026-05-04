# Infrastructure

Infrastructure is split by cloud provider:

- `gcp/`: Existing Google Cloud Terraform stack for Cloud Run, Cloud SQL, Artifact Registry, Secret Manager, and networking.
- `oci/`: Oracle Cloud Infrastructure Always Free Terraform stack for the free production target.

Each provider directory is an independent Terraform root. Run `terraform init`, `terraform plan`, and `terraform apply` from the provider directory you intend to manage.
