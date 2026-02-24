# 🚀 Azure Deploy - Addım-addım Təlimat

## Addım 1: Azure Portal (1 dəqiqə)

🔗 **Link:** https://portal.azure.com

**Gördüyünüz ekran:**
```
╔═══════════════════════════════════════════════════════╗
║  Azure Portal                                         ║
║                                                       ║
║  [Search resources...]              [Cloud Shell >_]  ║
║                                                       ║
║  + Create a resource                                  ║
║                                                       ║
║  Your subscriptions...                                ║
║                                                       ║
╚═══════════════════════════════════════════════════════╝
```

**Ediləcək:**
1. Hesaba daxil olun
2. Yuxarıda **"Cloud Shell"** iconuna ( >_ ) basın
3. **Bash** seçin
4. Gözləyin hazır olsun...

---

## Addım 2: Cloud Shell-də Deploy (3 dəqiqə)

**Cloud Shell pəncərəsində bu əmri yapışdırın:**

```bash
# 1. Repo klonla
cd ~
git clone https://github.com/sahilaziz/NexusPM.git
cd NexusPM

# 2. Azure resursları yarat
az group create --name NexusPM-RG --location westeurope

# 3. SQL Server və DB
az sql server create \
  --name nexus-pm-sql \
  --resource-group NexusPM-RG \
  --location westeurope \
  --admin-user nexusadmin \
  --admin-password "Nexus@2024!Strong"

# 4. Firewall aç
az sql server firewall-rule create \
  --resource-group NexusPM-RG \
  --server nexus-pm-sql \
  --name AllowAll \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 255.255.255.255

# 5. Database
az sql db create \
  --resource-group NexusPM-RG \
  --server nexus-pm-sql \
  --name NexusPM \
  --service-objective S0

# 6. App Service Plan
az appservice plan create \
  --name nexus-pm-plan \
  --resource-group NexusPM-RG \
  --sku B1 \
  --is-linux

# 7. Web App
az webapp create \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --plan nexus-pm-plan \
  --runtime "DOTNETCORE:9.0"

# 8. Connection String
az webapp config connection-string set \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --settings DefaultConnection="Server=tcp:nexus-pm-sql.database.windows.net,1433;Database=NexusPM;User ID=nexusadmin;Password=Nexus@2024!Strong;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"

# 9. GitHub deploy
az webapp deployment source config \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --repo-url https://github.com/sahilaziz/NexusPM \
  --branch main \
  --manual-integration

echo "✅ DEPLOY UĞURLU!"
echo "API: https://nexus-pm-api.azurewebsites.net"
```

**Enter basın** və gözləyin (5-10 dəqiqə)...

---

## Addım 3: Yoxlama (1 dəqiqə)

**Deploy bitəndən sonra:**

1. **Azure Portal** → **Resource groups** → **NexusPM-RG**
2. Görməlisiniz:
   - ✅ App Service: `nexus-pm-api`
   - ✅ SQL server: `nexus-pm-sql`
   - ✅ SQL database: `NexusPM`
   - ✅ App Service plan: `nexus-pm-plan`

3. **Brauzerdə açın:**
   ```
   https://nexus-pm-api.azurewebsites.net/swagger
   ```

**Görməlisiniz:** Swagger UI səhifəsi! 🎉

---

## ⚠️ Əgər Xəta Alsanız:

### Xəta 1: "subscription not found"
**Həll:** Azure hesabınızı yoxlayın və ya pulsuz yaradın:
https://azure.com/free

### Xəta 2: "sql server name exists"
**Həll:** Unikal ad yaradın:
```bash
az sql server create --name nexus-pm-sql-12345 --resource-group...
```

### Xəta 3: "webapp name exists"
**Həll:** Başqa ad seçin:
```bash
az webapp create --name nexus-pm-api-12345 --resource-group...
```

---

## 📱 Mobile App Build (Azure DevOps)

**Ayrıca olaraq:**

1. https://dev.azure.com açın
2. Yeni project yaradın: `NexusPM`
3. Pipelines → New pipeline → GitHub → NexusPM seçin
4. YAML faylı: `azure-pipelines.yml`
5. Run pipeline

**Nəticə:** APK avtomatik build olunacaq və yüklənəcək!

---

## 💰 Xərc (Aylıq)

| Resurs | Tier | Qiymət |
|--------|------|--------|
| App Service | B1 (Basic) | ~$13 |
| SQL Database | S0 (Standard) | ~$5 |
| Storage | LRS | ~$1 |
| **Ümumi** | | **~$19/ay** |

**Pulsuz alternativ:**
- App Service: F1 Free tier (1GB RAM, 1 saat/gün limit)
- SQL: Azure SQL Free (limitli)

---

## 🎯 Nəticə

**Deploy bitəndən sonra alacaqsınız:**

```
🌐 API URL:     https://nexus-pm-api.azurewebsites.net
📚 Swagger:     https://nexus-pm-api.azurewebsites.net/swagger
🗄️ SQL Server:  nexus-pm-sql.database.windows.net
📱 Mobile APK:  Azure DevOps artifacts
```

**Hazırsınız başlamağa?** 🚀

1. Azure portal açın
2. Cloud Shell açın
3. Yuxarıdakı əmrləri yapışdırın
4. Gözləyin...
5. ✅ Hazır!
