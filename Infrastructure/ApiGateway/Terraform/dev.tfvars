# dev.tfvars — checked into source control (no secrets here)
environment          = "dev"
location             = "eastus"
resource_group_name  = "eshopping-dev-rg"
apim_name            = "eshopping-apim-dev"
apim_sku             = "Developer"
apim_sku_capacity    = 1
publisher_name       = "eShopping Platform"
publisher_email      = "platform@eshopping.com"

vnet_name           = "eshopping-dev-vnet"
vnet_address_space  = ["10.0.0.0/16"]
apim_subnet_prefix  = "10.0.1.0/24"
aks_subnet_prefix   = "10.0.2.0/24"

catalog_backend_url  = "http://catalog-api.default.svc.cluster.local"
basket_backend_url   = "http://basket-api.default.svc.cluster.local"
ordering_backend_url = "http://ordering-api.default.svc.cluster.local"
discount_grpc_url    = "http://discount-api.default.svc.cluster.local:8080"
identity_server_url  = "https://identity-dev.eshopping.com"
