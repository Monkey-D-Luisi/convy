variable "project_name" {
  description = "Project name used in Hetzner resource names."
  type        = string
  default     = "convy"
}

variable "environment" {
  description = "Environment name."
  type        = string
  default     = "production"
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
  description = "CIDR allowed to reach SSH. Keep key-only SSH enabled on the host."
  type        = string
  default     = "0.0.0.0/0"
}
