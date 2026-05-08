output "public_ip" {
  description = "Public IPv4 address for the Convy API VM."
  value       = hcloud_server.api.ipv4_address
}

output "ipv6_address" {
  description = "Public IPv6 address for the Convy API VM."
  value       = hcloud_server.api.ipv6_address
}

output "public_hostname" {
  description = "Free TLS-capable hostname derived from the public IPv4 address."
  value       = "${hcloud_server.api.ipv4_address}.nip.io"
}

output "ssh_command" {
  description = "SSH command for the root user."
  value       = "ssh -i ~/.ssh/convy_vps_deploy root@${hcloud_server.api.ipv4_address}"
}

output "server_name" {
  description = "Hetzner Cloud server name."
  value       = hcloud_server.api.name
}
