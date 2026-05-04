variable "project_id" {
  type = string
}

variable "region" {
  type = string
}

variable "environment" {
  type = string
}

variable "image" {
  type = string
}

variable "vpc_connector_id" {
  type = string
}

variable "db_connection_name" {
  type = string
}

variable "db_name" {
  type = string
}

variable "db_user" {
  type = string
}

variable "db_password_secret_id" {
  type = string
}

variable "openai_key_secret_id" {
  type = string
}

variable "firebase_project_id" {
  type = string
}

variable "min_instances" {
  type    = number
  default = 0
}

variable "max_instances" {
  type    = number
  default = 2
}

variable "db_private_ip" {
  type = string
}
