variable "oci_profile" {
  description = "OCI CLI config profile to use."
  type        = string
  default     = "DEFAULT"
}

variable "compartment_ocid" {
  description = "Compartment OCID where Convy resources will be created. For a Free Tier-only tenancy this can be the tenancy OCID."
  type        = string
}

variable "region" {
  description = "OCI region."
  type        = string
  default     = "eu-madrid-1"
}

variable "project_name" {
  description = "Project name used in OCI resource names."
  type        = string
  default     = "convy"
}

variable "environment" {
  description = "Environment name."
  type        = string
  default     = "production"
}

variable "ssh_public_key_path" {
  description = "Path to the SSH public key authorized for the Ubuntu user."
  type        = string
}

variable "availability_domain_index" {
  description = "Zero-based availability domain index. Change this if A1 capacity is unavailable in the first AD."
  type        = number
  default     = 0
}

variable "fault_domain" {
  description = "Optional OCI fault domain for A1 placement retries. Leave null for OCI default placement."
  type        = string
  default     = null

  validation {
    condition = (
      var.fault_domain == null ||
      contains(["FAULT-DOMAIN-1", "FAULT-DOMAIN-2", "FAULT-DOMAIN-3"], var.fault_domain)
    )
    error_message = "fault_domain must be null, FAULT-DOMAIN-1, FAULT-DOMAIN-2, or FAULT-DOMAIN-3."
  }
}

variable "allowed_ssh_cidr" {
  description = "CIDR allowed to reach SSH. Keep key-only SSH enabled on the host."
  type        = string
  default     = "0.0.0.0/0"
}

variable "instance_ocpus" {
  description = "Ampere A1 OCPUs for the API VM. Always Free pool allows up to 4 OCPUs total."
  type        = number
  default     = 2
}

variable "instance_memory_gb" {
  description = "Ampere A1 memory in GB for the API VM. Always Free pool allows up to 24 GB total."
  type        = number
  default     = 8
}

variable "boot_volume_size_gb" {
  description = "Boot volume size in GB."
  type        = number
  default     = 50
}

variable "data_volume_size_gb" {
  description = "Block volume size in GB for Docker, PostgreSQL data, releases, and backups."
  type        = number
  default     = 100
}

variable "backup_bucket_name" {
  description = "Optional OCI Object Storage bucket name for encrypted database backups."
  type        = string
  default     = null
}
