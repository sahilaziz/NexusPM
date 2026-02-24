# ☁️ Azure Deploy

## 🚀 Bir Kliklə Deploy

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fsahilaziz%2FNexusPM%2Fmain%2Fazure-deploy%2Fazuredeploy.json)

### Necə işləyir:
1. **Yuxarıdakı "Deploy to Azure" düyməsinə basın**
2. Azure hesabınıza daxil olun
3. Parametrləri doldurun (və ya default qalsın):
   - **Resource group**: Yeni yaradın (məs: `NexusPM-RG`)
   - **Region**: West Europe
   - **App Name**: `nexus-pm-api`
   - **SQL Server Name**: `nexus-pm-sql`
   - **SKU**: `F1` (Pulsuz) və ya `B1` (Basic $13/ay)
4. **Review + Create** → **Create**
5. Gözləyin (3-5 dəqiqə)
6. ✅ Hazır!

---

## 📋 Deploy-dən Sonra

| URL | Təsvir |
|-----|--------|
| `https://nexus-pm-api.azurewebsites.net` | API |
| `https://nexus-pm-api.azurewebsites.net/swagger` | API Docs |
| `https://nexus-pm-api.azurewebsites.net/health` | Health Check |

---

## 💰 Qiymət

| Tier | Qiymət | Limitlər |
|------|--------|----------|
| **F1 (Free)** | $0 | 1GB RAM, 1 saat/gün CPU |
| **B1 (Basic)** | ~$13/ay | 1.75GB RAM, limitsiz |

---

## 🔧 Manual Deploy (Cloud Shell)

Əgər düymə işləməsə:

```bash
# Azure Cloud Shell (Bash)
curl -fsSL https://raw.githubusercontent.com/sahilaziz/NexusPM/main/azure-deploy/deploy.sh | bash
```

Və ya addım-addım:

```bash
# 1. Login
az login

# 2. Resource Group
az group create --name NexusPM-RG --location westeurope

# 3. Deploy
az deployment group create \
  --resource-group NexusPM-RG \
  --template-file azuredeploy.json \
  --parameters sku=F1
```

---

## 📱 Mobile App Build

Azure DevOps ilə Flutter APK build:

```bash
# Azure DevOps portalında:
# Pipelines → New Pipeline → GitHub YAML
# Mobile/azure-pipelines.yml seçin
```

---

## 🔗 Faydalı Linklər

- [Azure Portal](https://portal.azure.com)
- [Azure Pricing](https://azure.microsoft.com/pricing/calculator/)
- [App Service Docs](https://docs.microsoft.com/azure/app-service/)

---

**Deploy uğurlu olsun! 🎉**
