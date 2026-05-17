output "public_ip" {
  description = "Public IPv4 address for the Convy API VM."
  value       = oci_core_instance.api.public_ip
}

output "sslip_hostname" {
  description = "Free TLS-capable hostname derived from the public IP."
  value       = "${oci_core_instance.api.public_ip}.sslip.io"
}

output "ssh_command" {
  description = "SSH command for the Ubuntu user."
  value       = "ssh -i ~/.ssh/convy_oci_deploy ubuntu@${oci_core_instance.api.public_ip}"
}

output "backup_bucket_name" {
  description = "OCI Object Storage bucket for encrypted backups."
  value       = oci_objectstorage_bucket.backups.name
}

output "data_volume_device" {
  description = "Expected consistent device path for the attached data volume."
  value       = oci_core_volume_attachment.data.device
}
