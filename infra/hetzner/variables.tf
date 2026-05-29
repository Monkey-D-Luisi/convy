variable "project_name" {
  description = "Project name used in Hetzner resource names."
  type        = string
  default     = "convy"
}

variable "environment" {
  description = "Environment name."
  type        = string
  default     = "staging"
}

variable "server_type" {
  description = "Hetzner Cloud server type. cx23 is the low-cost x86 fallback for Convy."
  type        = string
  default     = "cx23"
}

variable "image" {
  description = "Hetzner Cloud image."
  type        = string
  default     = "ubuntu-24.04"
}

variable "location" {
  description = "Hetzner Cloud location."
  type        = string
  default     = "fsn1"
}

variable "ssh_public_key_path" {
  description = "Path to the SSH public key authorized for the root user."
  type        = string
}

variable "allowed_ssh_cidr" {
  description = "CIDR allowed to reach SSH. Set this to a trusted administrator IP or VPN range; do not use 0.0.0.0/0."
  type        = string

  validation {
    condition     = var.allowed_ssh_cidr != "0.0.0.0/0"
    error_message = "allowed_ssh_cidr must not expose SSH to the entire internet."
  }
}
