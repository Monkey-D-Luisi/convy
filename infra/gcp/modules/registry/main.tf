resource "google_artifact_registry_repository" "api" {
  repository_id = "convy-${var.environment}"
  project       = var.project_id
  location      = var.region
  format        = "DOCKER"
  description   = "Docker images for Convy ${var.environment} backend API"

  cleanup_policies {
    id     = "keep-last-5"
    action = "KEEP"
    most_recent_versions {
      keep_count = 5
    }
  }
}
