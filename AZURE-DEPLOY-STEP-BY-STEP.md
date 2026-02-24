# Azure Deploy - Addım-addım Təlimat

## ✅ Addım 1: Hesaba Daxil Oldunuz

## 🔧 Addım 2: Cloud Shell Açın

Azure Portal-da yuxarı sağda bu iconu tapın və basın:

```
┌─────────────────────────────────────────────────────────────┐
│  Azure Portal    [Qəribə icon] [🔔] [?] [⚙️] [>_] 👤        │
│                                             ↑               │
│                                      BUNA BASIN            │
└─────────────────────────────────────────────────────────────┘
```

**Basandan sonra:**
1. Aşağıda pəncərə açılacaq
2. **"Bash"** seçin (PowerShell yox)
3. **"Create storage"** düyməsinə basın
4. Gözləyin hazır olsun (~30 saniyə)

**Görməlisiniz:**
```
┌──────────────────────────────────────────────────────────────┐
│  Cloud Shell (Bash)                                          │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  sahil@Azure:~$ █                                       │  │
│  │                                                         │  │
│  │                                                         │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

---

## 📋 Addım 3: Əmrləri Yapışdırın

Aşağıdakı **BÜTÜN** kodu kopyalayın və Cloud Shell-ə **sağ klik** → **Paste** edin:

```bash
# ===== 1. REPOYU KLONLAYIN =====
cd ~
rm -rf NexusPM 2>/dev/null
git clone https://github.com/sahilaziz/NexusPM.git
cd NexusPM

# ===== 2. RESOURCE GROUP YARADIN =====
echo "📦 Resource Group yaradılır..."
az group create \
  --name NexusPM-RG \
  --location westeurope \
  --output none

# ===== 3. SQL SERVER YARADIN =====
echo "🗄️ SQL Server yaradılır..."
az sql server create \
  --name nexus-pm-sql \
  --resource-group NexusPM-RG \
  --location westeurope \
  --admin-user nexusadmin \
  --admin-password "Nexus@2024!Secure" \
  --output none

# ===== 4. FIREWALL AÇIN =====
echo "🔥 Firewall açılır..."
az sql server firewall-rule create \
  --resource-group NexusPM-RG \
  --server nexus-pm-sql \
  --name AllowAll \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 255.255.255.255 \
  --output none

# ===== 5. DATABASE YARADIN =====
echo "💾 Database yaradılır..."
az sql db create \
  --resource-group NexusPM-RG \
  --server nexus-pm-sql \
  --name NexusPM \
  --service-objective S0 \
  --output none

# ===== 6. APP SERVICE PLAN YARADIN =====
echo "⚙️ App Service Plan yaradılır..."
az appservice plan create \
  --name nexus-pm-plan \
  --resource-group NexusPM-RG \
  --sku B1 \
  --is-linux \
  --output none

# ===== 7. WEB APP YARADIN =====
echo "🌐 Web App yaradılır..."
az webapp create \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --plan nexus-pm-plan \
  --runtime "DOTNETCORE:9.0" \
  --output none

# ===== 8. CONNECTION STRING TƏYİN EDİN =====
echo "🔗 Connection String əlavə edilir..."
az webapp config connection-string set \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --settings DefaultConnection="Server=tcp:nexus-pm-sql.database.windows.net,1433;Database=NexusPM;User ID=nexusadmin;Password=Nexus@2024!Secure;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;" \
  --output none

# ===== 9. GITHUB-DAN DEPLOY =====
echo "📥 GitHub deploy başlayır..."
az webapp deployment source config \
  --name nexus-pm-api \
  --resource-group NexusPM-RG \
  --repo-url https://github.com/sahilaziz/NexusPM \
  --branch main \
  --manual-integration \
  --output none

# ===== 10. UĞUR MESAJI =====
echo ""
echo "═══════════════════════════════════════════════════"
echo "✅ DEPLOY UĞURLU OLDU!"
echo "═══════════════════════════════════════════════════"
echo ""
echo "🌐 API URL: https://nexus-pm-api.azurewebsites.net"
echo "📚 Swagger: https://nexus-pm-api.azurewebsites.net/swagger"
echo ""
echo "🗄️ SQL Info:"
echo "   Server: nexus-pm-sql.database.windows.net"
echo "   Database: NexusPM"
echo "   Username: nexusadmin"
echo "   Password: Nexus@2024!Secure"
echo ""
echo "═══════════════════════════════════════════════════"
```

**Bu kodu yapışdırdıqdan sonra:**
1. **Enter** basın
2. Gözləyin (~10 dəqiqə)
3. Yaşıl "✅ DEPLOY UĞURLU OLDU!" görəcəksiniz

---

## 🔍 Addım 4: Yoxlayın

Deploy bitəndən sonra brauzerdə açın:

```
https://nexus-pm-api.azurewebsites.net/swagger
```

**Görməlisiniz:**
```
╔═══════════════════════════════════════════════════════╗
║  Swagger UI                                           ║
║                                                       ║
║  Nexus PM API v1.0                                    ║
║                                                       ║
║  [Authorize]                                          ║
║                                                       ║
║  POST   /api/auth/login                               ║
║  POST   /api/auth/register                            ║
║  GET    /api/projects                                 ║
║  POST   /api/projects                                 ║
║  ...                                                  ║
╚═══════════════════════════════════════════════════════╝
```

🎉 **TƏBRİKLƏR! API işləyir!**

---

## ❌ Əgər Xəta Alsanız:

### Xəta: "sql server name exists"
```bash
# Başqa ad istifadə edin:
az sql server create --name nexus-pm-sql-12345 ...
```

### Xəta: "webapp name exists"
```bash
# Başqa ad istifadə edin:
az webapp create --name nexus-pm-api-12345 ...
```

### Xəta: "Resource group already exists"
```bash
# Problem deyil, davam edin, artıq var
```

---

## 📱 Növbəti Addım: Mobile App

API hazırdır! İndi Flutter app-da API URL-ni dəyişin:

```dart
// Mobile/lib/core/constants/api_constants.dart
static const String baseUrl = 'https://nexus-pm-api.azurewebsites.net/api';
```

Və APK build edin!

---

## 💰 Xərc (Aylıq)

| Resurs | Qiymət |
|--------|--------|
| App Service (B1) | ~$13 |
| SQL Database (S0) | ~$5 |
| **Ümumi** | **~$18/ay** |

**Pulsuz istəyirsinizsə?** B1 yerinə F1 yazın (amma 1 saat/gün limit)

---

**Hazırsınız Cloud Shell açmağa?** 🚀
