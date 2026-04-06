terraform {
  required_version = ">= 1.5"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

# Enable required GCP APIs
resource "google_project_service" "apis" {
  for_each = toset([
    "run.googleapis.com",
    "sqladmin.googleapis.com",
    "artifactregistry.googleapis.com",
    "secretmanager.googleapis.com",
    "vpcaccess.googleapis.com",
    "compute.googleapis.com",
    "servicenetworking.googleapis.com",
  ])

  project            = var.project_id
  service            = each.value
  disable_on_destroy = false
}

# Networking
module "networking" {
  source = "./modules/networking"

  project_id  = var.project_id
  region      = var.region
  environment = var.environment

  depends_on = [google_project_service.apis]
}

# Artifact Registry
module "registry" {
  source = "./modules/registry"

  project_id  = var.project_id
  region      = var.region
  environment = var.environment

  depends_on = [google_project_service.apis]
}

# Secrets
module "secrets" {
  source = "./modules/secrets"

  project_id  = var.project_id
  region      = var.region
  environment = var.environment
  db_password = var.db_password
  openai_key  = var.openai_api_key

  depends_on = [google_project_service.apis]
}

# Cloud SQL
module "database" {
  source = "./modules/database"

  project_id         = var.project_id
  region             = var.region
  environment        = var.environment
  db_tier            = var.db_tier
  db_password        = var.db_password
  private_network_id = module.networking.network_id

  depends_on = [
    google_project_service.apis,
    module.networking,
  ]
}

# Cloud Run
module "cloud_run" {
  source = "./modules/cloud-run"

  project_id            = var.project_id
  region                = var.region
  environment           = var.environment
  image                 = var.image
  vpc_connector_id      = module.networking.vpc_connector_id
  db_connection_name    = module.database.connection_name
  db_name               = module.database.database_name
  db_user               = module.database.database_user
  db_password_secret_id = module.secrets.db_password_secret_id
  openai_key_secret_id  = module.secrets.openai_key_secret_id
  firebase_project_id   = var.project_id
  min_instances         = var.cloud_run_min_instances
  max_instances         = var.cloud_run_max_instances
  db_private_ip         = module.database.private_ip

  depends_on = [
    module.database,
    module.secrets,
    module.networking,
  ]
}
