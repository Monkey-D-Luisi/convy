locals {
  resource_prefix     = "${var.project_name}-${var.environment}"
  availability_domain = data.oci_identity_availability_domains.available.availability_domains[var.availability_domain_index].name
  backup_bucket_name  = coalesce(var.backup_bucket_name, "${local.resource_prefix}-backups")
}

data "oci_identity_availability_domains" "available" {
  compartment_id = var.compartment_ocid
}

data "oci_objectstorage_namespace" "current" {
  compartment_id = var.compartment_ocid
}

data "oci_core_images" "ubuntu" {
  compartment_id           = var.compartment_ocid
  operating_system         = "Canonical Ubuntu"
  operating_system_version = "24.04"
  shape                    = "VM.Standard.A1.Flex"
  sort_by                  = "TIMECREATED"
  sort_order               = "DESC"
}

resource "oci_core_vcn" "main" {
  compartment_id = var.compartment_ocid
  display_name   = "${local.resource_prefix}-vcn"
  cidr_blocks    = ["10.20.0.0/16"]
  dns_label      = "convy"
}

resource "oci_core_internet_gateway" "main" {
  compartment_id = var.compartment_ocid
  display_name   = "${local.resource_prefix}-igw"
  vcn_id         = oci_core_vcn.main.id
  enabled        = true
}

resource "oci_core_route_table" "public" {
  compartment_id = var.compartment_ocid
  display_name   = "${local.resource_prefix}-public-rt"
  vcn_id         = oci_core_vcn.main.id

  route_rules {
    destination       = "0.0.0.0/0"
    destination_type  = "CIDR_BLOCK"
    network_entity_id = oci_core_internet_gateway.main.id
  }
}

resource "oci_core_security_list" "public" {
  compartment_id = var.compartment_ocid
  display_name   = "${local.resource_prefix}-public-sl"
  vcn_id         = oci_core_vcn.main.id

  ingress_security_rules {
    protocol = "6"
    source   = var.allowed_ssh_cidr

    tcp_options {
      min = 22
      max = 22
    }
  }

  ingress_security_rules {
    protocol = "6"
    source   = "0.0.0.0/0"

    tcp_options {
      min = 80
      max = 80
    }
  }

  ingress_security_rules {
    protocol = "6"
    source   = "0.0.0.0/0"

    tcp_options {
      min = 443
      max = 443
    }
  }

  ingress_security_rules {
    protocol = "1"
    source   = "0.0.0.0/0"

    icmp_options {
      type = 3
      code = 4
    }
  }

  egress_security_rules {
    protocol    = "all"
    destination = "0.0.0.0/0"
  }
}

resource "oci_core_subnet" "public" {
  compartment_id             = var.compartment_ocid
  display_name               = "${local.resource_prefix}-public-subnet"
  vcn_id                     = oci_core_vcn.main.id
  cidr_block                 = "10.20.1.0/24"
  dns_label                  = "public"
  prohibit_public_ip_on_vnic = false
  route_table_id             = oci_core_route_table.public.id
  security_list_ids          = [oci_core_security_list.public.id]
}

resource "oci_core_instance" "api" {
  availability_domain = local.availability_domain
  compartment_id      = var.compartment_ocid
  display_name        = "${local.resource_prefix}-api"
  fault_domain        = var.fault_domain
  shape               = "VM.Standard.A1.Flex"

  shape_config {
    ocpus         = var.instance_ocpus
    memory_in_gbs = var.instance_memory_gb
  }

  create_vnic_details {
    assign_public_ip = true
    display_name     = "${local.resource_prefix}-vnic"
    hostname_label   = "api"
    subnet_id        = oci_core_subnet.public.id
  }

  metadata = {
    ssh_authorized_keys = file(var.ssh_public_key_path)
    user_data           = base64encode(file("${path.module}/cloud-init.yaml"))
  }

  source_details {
    source_type             = "image"
    source_id               = data.oci_core_images.ubuntu.images[0].id
    boot_volume_size_in_gbs = var.boot_volume_size_gb
  }
}

resource "oci_core_volume" "data" {
  availability_domain = local.availability_domain
  compartment_id      = var.compartment_ocid
  display_name        = "${local.resource_prefix}-data"
  size_in_gbs         = var.data_volume_size_gb
}

resource "oci_core_volume_attachment" "data" {
  attachment_type                     = "paravirtualized"
  device                              = "/dev/oracleoci/oraclevdb"
  display_name                        = "${local.resource_prefix}-data-attachment"
  instance_id                         = oci_core_instance.api.id
  is_pv_encryption_in_transit_enabled = true
  volume_id                           = oci_core_volume.data.id
}

resource "oci_objectstorage_bucket" "backups" {
  compartment_id = var.compartment_ocid
  namespace      = data.oci_objectstorage_namespace.current.namespace
  name           = local.backup_bucket_name
  access_type    = "NoPublicAccess"
  storage_tier   = "Standard"
  versioning     = "Enabled"
}
