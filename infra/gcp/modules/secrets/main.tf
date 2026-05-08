resource "google_secret_manager_secret" "db_password" {
  secret_id = "convy-${var.environment}-db-password"
  project   = var.project_id

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "db_password" {
  secret      = google_secret_manager_secret.db_password.id
  secret_data = var.db_password
}

resource "google_secret_manager_secret" "openai_key" {
  secret_id = "convy-${var.environment}-openai-key"
  project   = var.project_id

  replication {
    auto {}
  }
}

resource "google_secret_manager_secret_version" "openai_key" {
  secret      = google_secret_manager_secret.openai_key.id
  secret_data = var.openai_key
}
