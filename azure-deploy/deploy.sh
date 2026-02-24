#!/bin/bash

# NexusPM Azure Deploy Script
set -e

echo "🚀 NexusPM Azure Deploy başlayır..."

# Variables
RESOURCE_GROUP="NexusPM-RG"
LOCATION="westeurope"
APP_NAME="nexus-pm-api"
SQL_SERVER="nexus-pm-sql"
SQL_ADMIN="nexusadmin"
SQL_PASSWORD="Nexus@2024!Strong"
SKU="B1"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check Azure CLI
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI tapılmadı. Yükləyin: https://aka.ms/installazurecli"
    exit 1
fi

# Login check
echo "🔐 Azure login yoxlanılır..."
az account show &> /dev/null || az login

# Create Resource Group
echo -e "${BLUE}📦 Resource Group yaradılır...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION --output none
echo -e "${GREEN}✅ Resource Group hazır${NC}"

# Deploy ARM Template
echo -e "${BLUE}☁️ Azure resursları deploy olunur...${NC}"
echo "⏳ Bu 5-10 dəqiqə çəkə bilər..."

az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file azuredeploy.json \
    --parameters \
        appName=$APP_NAME \
        sqlServerName=$SQL_SERVER \
        sqlAdminLogin=$SQL_ADMIN \
        sqlAdminPassword=$SQL_PASSWORD \
        sku=$SKU \
    --output none

echo -e "${GREEN}✅ Deploy tamamlandı!${NC}"

# Output URLs
echo ""
echo "═══════════════════════════════════════════"
echo "🎉 NEXUS PM UĞURLA DEPLOY OLUNDU!"
echo "═══════════════════════════════════════════"
echo ""
echo -e "🌐 API URL: ${BLUE}https://$APP_NAME.azurewebsites.net${NC}"
echo -e "📚 Swagger: ${BLUE}https://$APP_NAME.azurewebsites.net/swagger${NC}"
echo -e "🗄️ SQL Server: ${BLUE}$SQL_SERVER.database.windows.net${NC}"
echo ""
echo "⚙️ Admin Panel:"
echo "   Username: $SQL_ADMIN"
echo "   Password: $SQL_PASSWORD"
echo ""
echo "═══════════════════════════════════════════"
echo ""
echo "💡 Növbəti addımlar:"
echo "   1. Database migration işlədin"
echo "   2. API test edin"
echo "   3. Mobile app config update edin"
echo ""
