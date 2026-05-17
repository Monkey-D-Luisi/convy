variable "project_id" {
  type = string
}

variable "region" {
  type = string
}

variable "environment" {
  type = string
}

variable "db_password" {
  type      = string
  sensitive = true
}

variable "openai_key" {
  type      = string
  sensitive = true
}
