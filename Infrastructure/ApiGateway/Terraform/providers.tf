terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.95"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.48"
    }
  }

  # Remote state — swap out for your storage account details
  backend "azurerm" {
    resource_group_name  = "eshopping-tfstate-rg"
    storage_account_name = "eshoppingtfstate"
    container_name       = "tfstate"
    key                  = "apim/terraform.tfstate"
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

provider "azuread" {}
