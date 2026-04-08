# ─── Resource Group ──────────────────────────────────────────────────────────
resource "azurerm_resource_group" "eshopping" {
  name     = var.resource_group_name
  location = var.location

  tags = local.common_tags
}

# ─── Virtual Network ─────────────────────────────────────────────────────────
resource "azurerm_virtual_network" "eshopping" {
  name                = var.vnet_name
  location            = azurerm_resource_group.eshopping.location
  resource_group_name = azurerm_resource_group.eshopping.name
  address_space       = var.vnet_address_space

  tags = local.common_tags
}

# Subnet dedicated to APIM (must be /27 or larger; /24 preferred)
resource "azurerm_subnet" "apim" {
  name                 = "apim-subnet"
  resource_group_name  = azurerm_resource_group.eshopping.name
  virtual_network_name = azurerm_virtual_network.eshopping.name
  address_prefixes     = [var.apim_subnet_prefix]
}

# Subnet for AKS node pool (backends live here)
resource "azurerm_subnet" "aks" {
  name                 = "aks-subnet"
  resource_group_name  = azurerm_resource_group.eshopping.name
  virtual_network_name = azurerm_virtual_network.eshopping.name
  address_prefixes     = [var.aks_subnet_prefix]
}

# ─── NSG: allow APIM → AKS backend traffic ───────────────────────────────────
# APIM requires specific management ports — these are mandatory for VNet mode.
resource "azurerm_network_security_group" "apim" {
  name                = "apim-nsg"
  location            = azurerm_resource_group.eshopping.location
  resource_group_name = azurerm_resource_group.eshopping.name

  # Required: APIM management endpoint (Azure platform → APIM)
  security_rule {
    name                       = "AllowAPIMManagementInbound"
    priority                   = 100
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "3443"
    source_address_prefix      = "ApiManagement"
    destination_address_prefix = "VirtualNetwork"
  }

  # Required: APIM load balancer health probe
  security_rule {
    name                       = "AllowAzureLoadBalancerInbound"
    priority                   = 110
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "6390"
    source_address_prefix      = "AzureLoadBalancer"
    destination_address_prefix = "VirtualNetwork"
  }

  # Allow HTTPS traffic from the internet (gateway endpoint)
  security_rule {
    name                       = "AllowHTTPSInbound"
    priority                   = 120
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "443"
    source_address_prefix      = "Internet"
    destination_address_prefix = "VirtualNetwork"
  }

  # APIM → AKS backends (HTTP:80)
  security_rule {
    name                       = "AllowAPIMToAKSBackends"
    priority                   = 200
    direction                  = "Outbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_ranges    = ["80", "443", "8080"]
    source_address_prefix      = var.apim_subnet_prefix
    destination_address_prefix = var.aks_subnet_prefix
  }

  # Required: APIM outbound to Azure Storage, SQL, Event Hub (monitoring)
  security_rule {
    name                       = "AllowAPIMOutboundAzureServices"
    priority                   = 210
    direction                  = "Outbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_ranges    = ["443", "445", "1433", "5671", "5672"]
    source_address_prefix      = "VirtualNetwork"
    destination_address_prefix = "Storage"
  }

  tags = local.common_tags
}

resource "azurerm_subnet_network_security_group_association" "apim" {
  subnet_id                 = azurerm_subnet.apim.id
  network_security_group_id = azurerm_network_security_group.apim.id
}

# ─── Application Insights ────────────────────────────────────────────────────
resource "azurerm_log_analytics_workspace" "eshopping" {
  name                = "eshopping-law"
  location            = azurerm_resource_group.eshopping.location
  resource_group_name = azurerm_resource_group.eshopping.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.common_tags
}

resource "azurerm_application_insights" "eshopping" {
  name                = "eshopping-appinsights"
  location            = azurerm_resource_group.eshopping.location
  resource_group_name = azurerm_resource_group.eshopping.name
  workspace_id        = azurerm_log_analytics_workspace.eshopping.id
  application_type    = "web"

  tags = local.common_tags
}

# ─── Key Vault (storing backend secrets / cert thumbprints) ──────────────────
data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "eshopping" {
  name                       = "eshopping-kv-${var.environment}"
  location                   = azurerm_resource_group.eshopping.location
  resource_group_name        = azurerm_resource_group.eshopping.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = true

  # Allow APIM managed identity to read secrets
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = azurerm_api_management.eshopping.identity[0].principal_id

    secret_permissions = ["Get", "List"]
  }

  # Allow the deploying principal to manage secrets during CI/CD
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = ["Get", "List", "Set", "Delete"]
  }

  tags = local.common_tags
}

# ─── APIM Instance ───────────────────────────────────────────────────────────
resource "azurerm_api_management" "eshopping" {
  name                = var.apim_name
  location            = azurerm_resource_group.eshopping.location
  resource_group_name = azurerm_resource_group.eshopping.name
  publisher_name      = var.publisher_name
  publisher_email     = var.publisher_email

  sku_name = "${var.apim_sku}_${var.apim_sku_capacity}"

  # VNet injection — required to reach AKS private backends
  virtual_network_type = "Internal"
  virtual_network_configuration {
    subnet_id = azurerm_subnet.apim.id
  }

  # System-assigned managed identity → used to access Key Vault
  identity {
    type = "SystemAssigned"
  }

  # Security hardening: disable legacy TLS and SSL
  security {
    enable_backend_tls10      = false
    enable_backend_tls11      = false
    enable_backend_ssl30      = false
    enable_frontend_tls10     = false
    enable_frontend_tls11     = false
    enable_frontend_ssl30     = false
    tls_ecdhe_ecdsa_with_aes128_cbc_sha_ciphers_enabled = false
    tls_ecdhe_ecdsa_with_aes256_cbc_sha_ciphers_enabled = false
    tls_ecdhe_rsa_with_aes128_cbc_sha_ciphers_enabled   = false
    tls_ecdhe_rsa_with_aes256_cbc_sha_ciphers_enabled   = false
    tls_rsa_with_aes128_cbc_sha256_ciphers_enabled       = false
    tls_rsa_with_aes128_cbc_sha_ciphers_enabled          = false
    tls_rsa_with_aes256_cbc_sha256_ciphers_enabled       = false
    tls_rsa_with_aes256_cbc_sha_ciphers_enabled          = false
  }

  tags = local.common_tags

  depends_on = [
    azurerm_subnet_network_security_group_association.apim
  ]
}

# ─── APIM Logger (Application Insights) ──────────────────────────────────────
resource "azurerm_api_management_logger" "appinsights" {
  name                = "appinsights-logger"
  api_management_name = azurerm_api_management.eshopping.name
  resource_group_name = azurerm_resource_group.eshopping.name
  resource_id         = azurerm_application_insights.eshopping.id

  application_insights {
    instrumentation_key = azurerm_application_insights.eshopping.instrumentation_key
  }
}

# ─── APIM Diagnostic (tie logger to APIM) ────────────────────────────────────
resource "azurerm_api_management_diagnostic" "appinsights" {
  identifier               = "applicationinsights"
  resource_group_name      = azurerm_resource_group.eshopping.name
  api_management_name      = azurerm_api_management.eshopping.name
  api_management_logger_id = azurerm_api_management_logger.appinsights.id

  always_log_errors         = true
  log_client_ip             = true
  verbosity                 = "information"
  http_correlation_protocol = "W3C"

  sampling_percentage = 100

  frontend_request {
    headers_to_log = ["X-Correlation-ID", "X-Forwarded-For", "X-User-Id"]
  }

  frontend_response {
    headers_to_log = ["X-Correlation-ID", "X-Response-Time-ms"]
  }

  backend_request {
    headers_to_log = ["X-Correlation-ID"]
    body_bytes     = 256
  }

  backend_response {
    headers_to_log = ["Content-Type"]
    body_bytes     = 256
  }
}

# ─── Named Values (backend URLs — single source of truth) ────────────────────
resource "azurerm_api_management_named_value" "catalog_backend_url" {
  name                = "catalog-backend-url"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "catalog-backend-url"
  value               = var.catalog_backend_url
  secret              = false
}

resource "azurerm_api_management_named_value" "basket_backend_url" {
  name                = "basket-backend-url"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "basket-backend-url"
  value               = var.basket_backend_url
  secret              = false
}

resource "azurerm_api_management_named_value" "ordering_backend_url" {
  name                = "ordering-backend-url"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "ordering-backend-url"
  value               = var.ordering_backend_url
  secret              = false
}

resource "azurerm_api_management_named_value" "discount_grpc_url" {
  name                = "discount-grpc-url"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "discount-grpc-url"
  value               = var.discount_grpc_url
  secret              = false
}

resource "azurerm_api_management_named_value" "identity_server_url" {
  name                = "identity-server-url"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "identity-server-url"
  value               = var.identity_server_url
  secret              = false
}

# ─── API Version Sets ─────────────────────────────────────────────────────────
resource "azurerm_api_management_api_version_set" "catalog" {
  name                = "catalog-version-set"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "Catalog API"
  versioning_scheme   = "Segment" # /v1/catalog, /v2/catalog, …
}

resource "azurerm_api_management_api_version_set" "basket" {
  name                = "basket-version-set"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "Basket API"
  versioning_scheme   = "Segment"
}

resource "azurerm_api_management_api_version_set" "ordering" {
  name                = "ordering-version-set"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  display_name        = "Ordering API"
  versioning_scheme   = "Segment"
}

# ─── Catalog API ─────────────────────────────────────────────────────────────
resource "azurerm_api_management_api" "catalog_v1" {
  name                = "catalog-v1"
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  revision            = "1"
  display_name        = "Catalog API"
  path                = "catalog"
  protocols           = ["https"]
  service_url         = var.catalog_backend_url
  subscription_required = true

  api_version     = "v1"
  api_version_set_id = azurerm_api_management_api_version_set.catalog.id
  is_current      = true

  # Import OpenAPI spec directly from the running service (run once after backend is up)
  # Uncomment and set correct URL when importing
  # import {
  #   content_format = "openapi+json-link"
  #   content_value  = "${var.catalog_backend_url}/swagger/v1/swagger.json"
  # }
}

resource "azurerm_api_management_api_policy" "catalog_v1" {
  api_name            = azurerm_api_management_api.catalog_v1.name
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  xml_content         = file("${path.module}/../Policies/catalog-api-policy.xml")

  depends_on = [azurerm_api_management_named_value.catalog_backend_url]
}

# ─── Basket API ──────────────────────────────────────────────────────────────
resource "azurerm_api_management_api" "basket_v1" {
  name                  = "basket-v1"
  resource_group_name   = azurerm_resource_group.eshopping.name
  api_management_name   = azurerm_api_management.eshopping.name
  revision              = "1"
  display_name          = "Basket API"
  path                  = "basket"
  protocols             = ["https"]
  service_url           = var.basket_backend_url
  subscription_required = true

  api_version        = "v1"
  api_version_set_id = azurerm_api_management_api_version_set.basket.id
  is_current         = true
}

resource "azurerm_api_management_api_policy" "basket_v1" {
  api_name            = azurerm_api_management_api.basket_v1.name
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  xml_content         = file("${path.module}/../Policies/basket-api-policy.xml")

  depends_on = [azurerm_api_management_named_value.basket_backend_url]
}

# Basket v2 — maps to /api/v2/Basket/* on backend
resource "azurerm_api_management_api" "basket_v2" {
  name                  = "basket-v2"
  resource_group_name   = azurerm_resource_group.eshopping.name
  api_management_name   = azurerm_api_management.eshopping.name
  revision              = "1"
  display_name          = "Basket API"
  path                  = "basket"
  protocols             = ["https"]
  service_url           = var.basket_backend_url
  subscription_required = true

  api_version        = "v2"
  api_version_set_id = azurerm_api_management_api_version_set.basket.id
  is_current         = false
}

resource "azurerm_api_management_api_policy" "basket_v2" {
  api_name            = azurerm_api_management_api.basket_v2.name
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  xml_content         = file("${path.module}/../Policies/basket-v2-api-policy.xml")

  depends_on = [azurerm_api_management_named_value.basket_backend_url]
}

# ─── Ordering API ─────────────────────────────────────────────────────────────
resource "azurerm_api_management_api" "ordering_v1" {
  name                  = "ordering-v1"
  resource_group_name   = azurerm_resource_group.eshopping.name
  api_management_name   = azurerm_api_management.eshopping.name
  revision              = "1"
  display_name          = "Ordering API"
  path                  = "order"
  protocols             = ["https"]
  service_url           = var.ordering_backend_url
  subscription_required = true

  api_version        = "v1"
  api_version_set_id = azurerm_api_management_api_version_set.ordering.id
  is_current         = true
}

resource "azurerm_api_management_api_policy" "ordering_v1" {
  api_name            = azurerm_api_management_api.ordering_v1.name
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
  xml_content         = file("${path.module}/../Policies/ordering-api-policy.xml")

  depends_on = [azurerm_api_management_named_value.ordering_backend_url]
}

# ─── Products (subscription tiers) ───────────────────────────────────────────
resource "azurerm_api_management_product" "public" {
  product_id            = "public"
  resource_group_name   = azurerm_resource_group.eshopping.name
  api_management_name   = azurerm_api_management.eshopping.name
  display_name          = "Public"
  description           = "Read-only access to Catalog. No JWT required."
  subscription_required = true
  approval_required     = false
  published             = true
}

resource "azurerm_api_management_product_api" "public_catalog" {
  api_name            = azurerm_api_management_api.catalog_v1.name
  product_id          = azurerm_api_management_product.public.product_id
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
}

resource "azurerm_api_management_product" "authenticated" {
  product_id            = "authenticated"
  resource_group_name   = azurerm_resource_group.eshopping.name
  api_management_name   = azurerm_api_management.eshopping.name
  display_name          = "Authenticated"
  description           = "Full access to all micro-services. JWT + subscription key required."
  subscription_required = true
  approval_required     = true
  published             = true
}

resource "azurerm_api_management_product_api" "authenticated_catalog" {
  api_name            = azurerm_api_management_api.catalog_v1.name
  product_id          = azurerm_api_management_product.authenticated.product_id
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
}

resource "azurerm_api_management_product_api" "authenticated_basket" {
  api_name            = azurerm_api_management_api.basket_v1.name
  product_id          = azurerm_api_management_product.authenticated.product_id
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
}

resource "azurerm_api_management_product_api" "authenticated_ordering" {
  api_name            = azurerm_api_management_api.ordering_v1.name
  product_id          = azurerm_api_management_product.authenticated.product_id
  resource_group_name = azurerm_resource_group.eshopping.name
  api_management_name = azurerm_api_management.eshopping.name
}

# ─── Global (all-APIs) policy ─────────────────────────────────────────────────
resource "azurerm_api_management_policy" "global" {
  api_management_id = azurerm_api_management.eshopping.id
  xml_content       = file("${path.module}/../Policies/global-policy.xml")
}

# ─── Locals ──────────────────────────────────────────────────────────────────
locals {
  common_tags = {
    Project     = "eShopping"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
