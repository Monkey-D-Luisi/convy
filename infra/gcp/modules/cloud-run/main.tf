# Service account for Cloud Run
resource "google_service_account" "cloud_run" {
  account_id   = "convy-${var.environment}-api"
  project      = var.project_id
  display_name = "Convy ${var.environment} API service account"
}

# Grant Cloud Run SA access to Secret Manager secrets
resource "google_secret_manager_secret_iam_member" "db_password" {
  project   = var.project_id
  secret_id = var.db_password_secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_secret_manager_secret_iam_member" "openai_key" {
  project   = var.project_id
  secret_id = var.openai_key_secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Grant Cloud Run SA access to Cloud SQL
resource "google_project_iam_member" "cloudsql_client" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Grant Cloud Run SA access to Firebase Cloud Messaging
resource "google_project_iam_member" "firebase_messaging" {
  project = var.project_id
  role    = "roles/firebase.sdkAdminServiceAgent"
  member  = "serviceAccount:${google_service_account.cloud_run.email}"
}

# Cloud Run service
resource "google_cloud_run_v2_service" "api" {
  name     = "convy-${var.environment}-api"
  project  = var.project_id
  location = var.region
  ingress  = "INGRESS_TRAFFIC_ALL"

  template {
    service_account = google_service_account.cloud_run.email

    scaling {
      min_instance_count = var.min_instances
      max_instance_count = var.max_instances
    }

    session_affinity = true # Required for SignalR WebSocket connections

    vpc_access {
      connector = var.vpc_connector_id
      egress    = "PRIVATE_RANGES_ONLY"
    }

    containers {
      image = var.image

      ports {
        container_port = 8080
      }

      resources {
        limits = {
          cpu    = "1"
          memory = "512Mi"
        }
      }

      # DB connection — Password is injected separately via DB_PASSWORD.
      # The app's Program.cs must compose the connection string at startup.
      env {
        name  = "DB_HOST"
        value = var.db_private_ip
      }

      env {
        name  = "DB_NAME"
        value = var.db_name
      }

      env {
        name  = "DB_USER"
        value = var.db_user
      }

      env {
        name = "DB_PASSWORD"
        value_source {
          secret_key_ref {
            secret  = var.db_password_secret_id
            version = "latest"
          }
        }
      }

      env {
        name  = "Firebase__ProjectId"
        value = var.firebase_project_id
      }

      env {
        name = "OpenAI__ApiKey"
        value_source {
          secret_key_ref {
            secret  = var.openai_key_secret_id
            version = "latest"
          }
        }
      }

      env {
        name  = "OpenAI__TranscriptionModel"
        value = "gpt-4o-mini-transcribe"
      }

      env {
        name  = "OpenAI__ParsingModel"
        value = "gpt-5.4-nano"
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "production" ? "Production" : "Staging"
      }

      startup_probe {
        http_get {
          path = "/health"
          port = 8080
        }
        initial_delay_seconds = 5
        period_seconds        = 10
        timeout_seconds       = 5
        failure_threshold     = 3
      }

      liveness_probe {
        http_get {
          path = "/health"
          port = 8080
        }
        period_seconds  = 30
        timeout_seconds = 5
      }
    }
  }

  depends_on = [
    google_secret_manager_secret_iam_member.db_password,
    google_secret_manager_secret_iam_member.openai_key,
    google_project_iam_member.cloudsql_client,
    google_project_iam_member.firebase_messaging,
  ]
}

# Allow unauthenticated access (API handles auth via Firebase JWT)
resource "google_cloud_run_v2_service_iam_member" "public" {
  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.api.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}
