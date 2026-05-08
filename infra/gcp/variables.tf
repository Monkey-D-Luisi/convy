variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region for all resources"
  type        = string
  default     = "europe-southwest1"
}

variable "environment" {
  description = "Environment name (staging or production)"
  type        = string
  validation {
    condition     = contains(["staging", "production"], var.environment)
    error_message = "Environment must be 'staging' or 'production'."
  }
}

variable "db_password" {
  description = "Password for the Cloud SQL database user"
  type        = string
  sensitive   = true
}

variable "openai_api_key" {
  description = "OpenAI API key for transcription/parsing features"
  type        = string
  sensitive   = true
}

variable "db_tier" {
  description = "Cloud SQL machine tier"
  type        = string
  default     = "db-g1-small"
}

variable "image" {
  description = "Full Docker image URI for the backend API (e.g. europe-southwest1-docker.pkg.dev/convy-6520d/convy-staging/api:latest)"
  type        = string
}

variable "cloud_run_min_instances" {
  description = "Minimum number of Cloud Run instances"
  type        = number
  default     = 0
}

variable "cloud_run_max_instances" {
  description = "Maximum number of Cloud Run instances"
  type        = number
  default     = 2
}
