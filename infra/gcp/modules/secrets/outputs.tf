output "db_password_secret_id" {
  value = google_secret_manager_secret.db_password.secret_id
}

output "openai_key_secret_id" {
  value = google_secret_manager_secret.openai_key.secret_id
}
