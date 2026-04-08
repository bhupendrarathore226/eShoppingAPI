output "apim_gateway_url" {
  description = "Public HTTPS gateway URL clients call"
  value       = "https://${azurerm_api_management.eshopping.gateway_url}"
}

output "apim_portal_url" {
  description = "Developer portal URL"
  value       = azurerm_api_management.eshopping.developer_portal_url
}

output "apim_management_api_url" {
  description = "REST Management API URL (used by CI/CD)"
  value       = azurerm_api_management.eshopping.management_api_url
}

output "apim_principal_id" {
  description = "System-assigned managed identity object ID (assign Key Vault roles to this)"
  value       = azurerm_api_management.eshopping.identity[0].principal_id
}

output "appinsights_connection_string" {
  description = "Application Insights connection string — paste into each microservice's appsettings"
  value       = azurerm_application_insights.eshopping.connection_string
  sensitive   = true
}

output "appinsights_instrumentation_key" {
  description = "Instrumentation key for legacy SDK setup"
  value       = azurerm_application_insights.eshopping.instrumentation_key
  sensitive   = true
}

output "key_vault_uri" {
  description = "Key Vault URI for storing secrets referenced by APIM Named Values"
  value       = azurerm_key_vault.eshopping.vault_uri
}

output "apim_subnet_id" {
  description = "Subnet ID of the APIM subnet (needed when deploying AKS with Private Link)"
  value       = azurerm_subnet.apim.id
}

output "aks_subnet_id" {
  description = "Subnet ID for the AKS node pool"
  value       = azurerm_subnet.aks.id
}

output "catalog_api_id" {
  value = azurerm_api_management_api.catalog_v1.id
}

output "basket_api_id" {
  value = azurerm_api_management_api.basket_v1.id
}

output "ordering_api_id" {
  value = azurerm_api_management_api.ordering_v1.id
}
