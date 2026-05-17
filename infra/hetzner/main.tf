locals {
  resource_prefix = "${var.project_name}-${var.environment}"
}

resource "hcloud_ssh_key" "deploy" {
  name       = "${local.resource_prefix}-deploy"
  public_key = file(var.ssh_public_key_path)
}

resource "hcloud_firewall" "main" {
  name = "${local.resource_prefix}-firewall"

  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "22"
    source_ips = [var.allowed_ssh_cidr]
  }

  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "80"
    source_ips = ["0.0.0.0/0", "::/0"]
  }

  rule {
    direction  = "in"
    protocol   = "tcp"
    port       = "443"
    source_ips = ["0.0.0.0/0", "::/0"]
  }
}

resource "hcloud_server" "api" {
  name        = "${local.resource_prefix}-api"
  image       = var.image
  server_type = var.server_type
  location    = var.location
  ssh_keys    = [hcloud_ssh_key.deploy.id]
  user_data   = file("${path.module}/cloud-init.yaml")
  firewall_ids = [
    hcloud_firewall.main.id
  ]

  labels = {
    project     = var.project_name
    environment = var.environment
    role        = "api"
  }
}
