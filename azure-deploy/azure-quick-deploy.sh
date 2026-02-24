#!/bin/bash

# NexusPM Azure Quick Deploy Script
# Usage: bash azure-quick-deploy.sh

set -e

echo "🚀 NexusPM Azure Deploy başlayır..."
echo ""

# Variables
RESOURCE_GROUP="NexusPM-RG"
LOCATION="westeurope"
APP_NAME="nexus-pm-api"
SQL_SERVER="nexus-pm-sql"
SQL_ADMIN="nexusadmin"
SQL_PASSWORD="Nexus@2024!Strong"

echo "📦 1/5 Resource Group yaradılır..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --output none

echo "🗄️  2/5 SQL Server yaradılır..."
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN \
  --admin-password "$SQL_PASSWORD" \
  --output none

echo "🔥 3/5 SQL Firewall açılır..."
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name AllowAll \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 255.255.255.255 \
  --output none

echo "💾 4/5 Database yaradılır..."
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name NexusPM \
  --service-objective S0 \
  --output none

echo "⚙️  5/5 App Service Plan yaradılır..."
az appservice plan create \
  --name nexus-pm-plan \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux \
  --output none

echo "🌐 Web App yaradılır..."
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan nexus-pm-plan \
  --runtime "DOTNETCORE:9.0" \
  --output none

echo "🔗 Connection String əlavə edilir..."
az webapp config connection-string set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings DefaultConnection="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=NexusPM;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;" \
  --output none

echo "📥 GitHub deploy başlayır..."
az webapp deployment source config \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --repo-url https://github.com/sahilaziz/NexusPM \
  --branch main \
  --manual-integration \
  --output none

echo ""
echo "═══════════════════════════════════════════════════"
echo "✅ DEPLOY UĞURLU OLDU!"
echo "═══════════════════════════════════════════════════"
echo ""
echo "🌐 API URL: https://$APP_NAME.azurewebsites.net"
echo "📚 Swagger: https://$APP_NAME.azurewebsites.net/swagger"
echo "🗄️  SQL Server: $SQL_SERVER.database.windows.net"
echo ""
echo "⏳ Deploy 5-10 dəqiqə çəkəcək..."
echo "   Yoxlamaq üçün: az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "═══════════════════════════════════════════════════"
