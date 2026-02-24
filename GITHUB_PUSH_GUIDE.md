# GitHub-a Push Etmə Qaydası

## 🚀 Addımlar

### 1. GitHub Repository Yarat (Əgər yoxdursa)

1. https://github.com/new keçin
2. Repository adı: `NexusPM`
3. Public və ya Private seçin
4. "Create repository" basın

### 2. Local-də Git İnitialize et

```bash
# Proqram qovluğuna keçin
cd "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement"

# Git initialize et
git init

# Bütün faylları əlavə et
git add .

# İlk commit
git commit -m "Initial commit: Nexus PM v1.0.0 - Full system implementation

- Backend API with 55+ endpoints
- Mobile app (Flutter) with 8 screens
- Database with 25+ tables
- CI/CD pipelines
- Complete documentation"

# Remote əlavə et (Sizin repo URL-niz)
git remote add origin https://github.com/sahilaziz/NexusPM.git

# Push et
git push -u origin main
```

### 3. Yoxlama

GitHub-da bu linkə keçin:
```
https://github.com/sahilaziz/NexusPM
```

Görməlisiniz:
- ✅ Bütün fayllar
- ✅ README.md
- ✅ .github/workflows/ qovluğu
- ✅ Backend və Mobile qovluqları

### 4. Actions Yoxlama

GitHub-da:
1. "Actions" tab-ına basın
2. Workflow-ların işlədiyini görəcəksiniz
3. Yaşıl checkmarklar ✅

## 🔧 Əgər xəta alsanız:

### Xəta 1: "fatal: not a git repository"
```bash
git init
```

### Xəta 2: "remote origin already exists"
```bash
git remote remove origin
git remote add origin https://github.com/sahilaziz/NexusPM.git
```

### Xəta 3: "failed to push some refs"
```bash
git pull origin main --rebase
git push origin main
```

### Xəta 4: Authentication failed
```bash
# Personal Access Token yaratmalısınız:
# 1. GitHub → Settings → Developer settings → Personal access tokens
# 2. Token generate et
# 3. Push edərkən token istifadə edin
```

## 📸 Screenshots (Nə görməlisiniz)

### GitHub Actions Tab:
```
┌─────────────────────────────────────┐
│  Actions                            │
├─────────────────────────────────────┤
│  ✅ Backend CI      - passing       │
│  ✅ Mobile CI       - passing       │
│  ✅ Docker Build    - passing       │
│  ✅ Code Coverage   - 75%           │
└─────────────────────────────────────┘
```

### README göstərişi:
```
┌─────────────────────────────────────┐
│  Nexus Project Management           │
│                                     │
│  [Backend CI: passing]              │
│  [Mobile CI: passing]               │
│  [Docker Build: passing]            │
│  [Coverage: 75%]                    │
└─────────────────────────────────────┘
```

## ✅ Tez Yoxlama (Quick Check)

```bash
# 1. Status yoxla
git status

# 2. Remote yoxla
git remote -v

# 3. Son commit-i gör
git log --oneline -1

# 4. Branch yoxla
git branch
```

## 🆘 Yardım lazımdırsa:

```bash
# Ətraflı log
git log --oneline --graph --all

# Son dəyişiklikləri gör
git diff HEAD~1

# Remote ilə əlaqəni yoxla
git remote -v
```
