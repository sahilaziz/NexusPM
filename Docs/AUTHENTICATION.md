# NexusPM Autentifikasiya Sistemi

## Ümumi Baxış

NexusPM iki autentifikasiya rejimini dəstəkləyir:

1. **Local Authentication** - Email + Şifrə + 2FA
2. **Active Directory Authentication** - Windows Domain inteqrasiyası

## Rejimlər

### 1. Local Mode (`appsettings.json`)

```json
"Authentication": {
  "Mode": "Local",
  "Local": {
    "RequireEmailConfirmation": true,
    "EnableTwoFactor": true,
    "AllowUserTwoFactorSetup": true
  }
}
```

**Xüsusiyyətlər:**
- ✅ İstifadəçi özü qeydiyyatdan keçə bilər (Register)
- ✅ Email təsdiqi (zəruri)
- ✅ 2FA dəstəyi (Google/Microsoft Authenticator)
- ✅ Şifrə sıfırlama (Forgot Password)

**Giriş səhifəsi:**
- Email və şifrə ilə giriş
- "Qeydiyyat" düyməsi
- "Şifrəmi unutdum" linki

### 2. Active Directory Mode

```json
"Authentication": {
  "Mode": "ActiveDirectory",
  "ActiveDirectory": {
    "Domain": "COMPANY.COM",
    "LdapServer": "dc.company.com",
    "AdminGroups": ["Domain Admins", "NexusPM_Admins"],
    "UserGroups": ["NexusPM_Users"]
  }
}
```

**Xüsusiyyətlər:**
- ❌ İstifadəçi özü qeydiyyatdan keçə bilməz (Admin əlavə edir)
- ✅ Recovery Email dəstəyi
- ✅ Şifrə sıfırlama (Recovery email vasitəsilə)
- ✅ Backup şifrə (AD offline olduqda)

**Giriş səhifəsi:**
- Yalnız istifadəçi adı və şifrə
- "Şifrəmi unutdum" linki (recovery email varsa)
- "Qeydiyyat" yoxdur

### 3. Mixed Mode

```json
"Authentication": {
  "Mode": "Mixed"
}
```

Hər iki rejim eyni vaxtda aktivdir. İstifadəçi seçim edə bilər.

## Active Directory İstifadəçi Axını

### İlk Login

```
1. AD istifadəçisi sistemə daxil olur
   ↓
2. AD autentifikasiyası uğurlu olur
   ↓
3. Database-də istifadəçi yoxdur (ilk dəfə)
   ↓
4. Profil yaratma səhifəsi göstərilir
   ↓
5. Recovery email tələb olunur
   ↓
6. Təsdiq linki göndərilir
   ↓
7. Email təsdiqləndikdən sonra sistemə giriş
```

### Növbəti Login-lər

```
1. AD istifadəçisi sistemə daxil olur
   ↓
2. AD autentifikasiyası uğurlu olur
   ↓
3. Database-də istifadəçi tapılır
   ↓
4. Recovery email təsdiqlənibsə → Giriş
   ↓
5. Recovery email yoxdursa → Xəbərdarlıq + Giriş
```

### Şifrə Sıfırlama (Forgot Password)

```
1. "Şifrəmi unutdum" klikləyir
   ↓
2. Username daxil edir
   ↓
3. Recovery email-ə sıfırlama linki göndərilir
   ↓
4. İstifadəçi linkə klikləyir
   ↓
5. Yeni şifrə təyin edir (local backup şifrə)
   ↓
6. AD ilə və ya backup şifrə ilə giriş mümkündür
```

## Recovery Email Nədir?

Recovery email - AD istifadəçiləri üçün alternativ email ünvanıdır:

- **Təyin edən:** Admin və ya istifadəçi özü (ilk login-dən sonra)
- **Məqsəd:** Şifrə sıfırlama və hesab bərpası
- **Nümunələr:** şəxsi Gmail, Yahoo, şirkətin digər domain-i

### Recovery Email Axını

```
1. Admin istifadəçiyə recovery email əlavə edir
   VƏ YA
   İstifadəçi profilində özü əlavə edir
   ↓
2. Təsdiq linki recovery email-ə göndərilir
   ↓
3. İstifadəçi linkə klikləyir
   ↓
4. Recovery email təsdiqlənir
   ↓
5. Artıq "Forgot Password" işləyir
```

## API Endpoint-ləri

### Local Authentication

| Endpoint | Təsvir |
|----------|--------|
| `POST /api/auth/register` | Yeni qeydiyyat |
| `POST /api/auth/login` | Giriş |
| `GET /api/auth/confirm-email` | Email təsdiqi |
| `POST /api/auth/verify-2fa` | 2FA kodu yoxlama |
| `POST /api/auth/forgot-password` | Şifrə sıfırlama tələbi |
| `POST /api/auth/reset-password` | Yeni şifrə təyin et |

### Active Directory Authentication

| Endpoint | Təsvir |
|----------|--------|
| `POST /api/auth/ad-login` | AD ilə giriş |
| `POST /api/auth/ad-complete-profile` | İlk login profili tamamla |
| `GET /api/auth/ad-confirm-recovery-email` | Recovery email təsdiqi |
| `POST /api/auth/ad-set-recovery-email` | Recovery email əlavə et |
| `POST /api/auth/ad-forgot-password` | Şifrə sıfırlama tələbi |
| `POST /api/auth/ad-reset-password` | Yeni şifrə təyin et |
| `POST /api/auth/ad-change-password` | AD şifrəsini dəyiş |
| `POST /api/auth/ad-set-backup-password` | Backup şifrə yarat |

### Common

| Endpoint | Təsvir |
|----------|--------|
| `GET /api/auth/mode` | Sistem rejimini göstər |
| `GET /api/auth/me` | Cari istifadəçi məlumatları |

## Təhlükəsizlik

### Şifrə Siyasəti (Local)

- Minimum 8 simvol
- Ən azı 1 böyük hərf
- Ən azı 1 kiçik hərf
- Ən azı 1 rəqəm
- Ən azı 1 xüsusi simvol

### Hesab Bloklanması

- 5 uğursuz cəhd → 30 dəqiqəlik bloklama
- Email təsdiqi tələb olunur (local)

### Recovery Email Təhlükəsliyi

- Token 24 saat etibarlıdır
- Təsdiq olunmayan recovery email ilə şifrə sıfırlama mümkün deyil
- Hər dəfə yeni recovery email əlavə edildikdə təsdiq tələb olunur

## Konfiqurasiya Nümunələri

### Kiçik Şirkət (Local Mode)

```json
"Authentication": {
  "Mode": "Local",
  "Local": {
    "RequireEmailConfirmation": true,
    "EnableTwoFactor": false,
    "MaxFailedAccessAttempts": 5
  }
}
```

### Korporativ (AD Mode)

```json
"Authentication": {
  "Mode": "ActiveDirectory",
  "ActiveDirectory": {
    "Domain": "COMPANY.COM",
    "LdapServer": "dc.company.com",
    "AdminGroups": ["Domain Admins"],
    "UserGroups": ["NexusPM_Users"]
  }
}
```

### Hibrid (Mixed Mode)

```json
"Authentication": {
  "Mode": "Mixed"
}
```

## Admin Guide: AD İstifadəçisi Əlavə Etmə

1. **Active Directory-də** istifadəçini `NexusPM_Users` qrupuna əlavə edin
2. **NexusPM** sistemində istifadəçi avtomatik görünəcək (ilk login-də)
3. Və ya admin əl ilə recovery email təyin edə bilər

```sql
-- Admin recovery email əlavə edir
UPDATE Users 
SET RecoveryEmail = 'user@gmail.com',
    IsRecoveryEmailConfirmed = 1
WHERE Username = 'john.doe' AND AuthenticationType = 'ActiveDirectory';
```

## Troubleshooting

### AD Giriş Problemləri

**Problem:** "AD authentication failed"
- Domain controller əlçatandır?
- İstifadəçi adı formatı düzgündür? (DOMAIN\username və ya username@domain)
- İstifadəçi `NexusPM_Users` qrupundadır?

**Problem:** "Recovery email not set"
- İstifadəçi ilk login-də recovery email əlavə etməyib
- Admin əl ilə əlavə edə bilər

**Problem:** "Forgot password not working for AD user"
- Recovery email təsdiqlənibmi?
- `IsRecoveryEmailConfirmed = 1` olmalıdır
