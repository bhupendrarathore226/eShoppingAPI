# ─── Globals ─────────────────────────────────────────────────────────────────
variable "environment" {
  type        = string
  description = "Deployment environment (dev / staging / prod)"
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  type        = string
  description = "Azure region"
  default     = "eastus"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group that contains all eShopping resources"
  default     = "eshopping-rg"
}

# ─── Networking ──────────────────────────────────────────────────────────────
variable "vnet_name" {
  type        = string
  description = "Name of the Virtual Network for VNet-injected APIM"
  default     = "eshopping-vnet"
}

variable "vnet_address_space" {
  type        = list(string)
  default     = ["10.0.0.0/16"]
}

variable "apim_subnet_prefix" {
  type        = string
  description = "CIDR for the APIM-dedicated subnet (/27 minimum for Developer; /24 recommended for Premium)"
  default     = "10.0.1.0/24"
}

variable "aks_subnet_prefix" {
  type        = string
  description = "CIDR for the AKS node pool subnet (backends live here)"
  default     = "10.0.2.0/24"
}

# ─── APIM ─────────────────────────────────────────────────────────────────────
variable "apim_name" {
  type        = string
  description = "Globally unique name for the APIM instance"
  default     = "eshopping-apim"
}

variable "apim_sku" {
  type        = string
  description = "APIM SKU. Use Developer for non-prod, Premium for prod (VNet injection requires Developer or Premium)"
  default     = "Developer"

  validation {
    condition     = contains(["Developer", "Premium"], var.apim_sku)
    error_message = "apim_sku must be Developer or Premium for VNet injection."
  }
}

variable "apim_sku_capacity" {
  type        = number
  description = "Number of scale units (1 is fine for Developer; start with 1 for Premium)"
  default     = 1
}

variable "publisher_name" {
  type        = string
  description = "Organisation / publisher name shown in the developer portal"
  default     = "eShopping Platform"
}

variable "publisher_email" {
  type        = string
  description = "Publisher e-mail address for APIM notifications"
  default     = "platform@eshopping.com"
}

# ─── Backend microservice URLs (AKS internal ClusterIP DNS) ──────────────────
variable "catalog_backend_url" {
  type        = string
  description = "Internal URL of the Catalog API (e.g. AKS ClusterIP service DNS)"
  default     = "http://catalog-api.default.svc.cluster.local"
}

variable "basket_backend_url" {
  type        = string
  description = "Internal URL of the Basket API"
  default     = "http://basket-api.default.svc.cluster.local"
}

variable "ordering_backend_url" {
  type        = string
  description = "Internal URL of the Ordering API"
  default     = "http://ordering-api.default.svc.cluster.local"
}

variable "discount_grpc_url" {
  type        = string
  description = "Internal gRPC URL for the Discount service (h2c inside cluster)"
  default     = "http://discount-api.default.svc.cluster.local:8080"
}

variable "identity_server_url" {
  type        = string
  description = "Base URL of the IdentityServer4 issuer (used in JWT validate-jwt policy)"
  default     = "https://identity.eshopping.com"
}

# ─── Security ─────────────────────────────────────────────────────────────────
variable "apim_subscription_key_header" {
  type    = string
  default = "Ocp-Apim-Subscription-Key"
}
