output "cloud_run_url" {
  description = "URL of the Cloud Run service"
  value       = module.cloud_run.service_url
}

output "cloud_sql_connection_name" {
  description = "Cloud SQL instance connection name"
  value       = module.database.connection_name
}

output "artifact_registry_url" {
  description = "Artifact Registry repository URL"
  value       = module.registry.repository_url
}

output "cloud_sql_private_ip" {
  description = "Private IP of the Cloud SQL instance"
  value       = module.database.private_ip
}
