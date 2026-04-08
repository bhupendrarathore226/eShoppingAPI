environment          = "prod"
location             = "eastus"
resource_group_name  = "eshopping-prod-rg"
apim_name            = "eshopping-apim"
apim_sku             = "Premium"
apim_sku_capacity    = 1
publisher_name       = "eShopping Platform"
publisher_email      = "platform@eshopping.com"

vnet_name           = "eshopping-prod-vnet"
vnet_address_space  = ["10.1.0.0/16"]
apim_subnet_prefix  = "10.1.1.0/24"
aks_subnet_prefix   = "10.1.2.0/24"

catalog_backend_url  = "http://catalog-api.eshopping.svc.cluster.local"
basket_backend_url   = "http://basket-api.eshopping.svc.cluster.local"
ordering_backend_url = "http://ordering-api.eshopping.svc.cluster.local"
discount_grpc_url    = "http://discount-api.eshopping.svc.cluster.local:8080"
identity_server_url  = "https://identity.eshopping.com"
