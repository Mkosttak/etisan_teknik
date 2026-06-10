# Production Deployment Script
# ⚠️ Bu script PRODUCTION sunucuda çalıştırılmalıdır
# İLK DEPLOYMENT için gerekli tüm adımların otomasyonu

param(
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipAdminCreation = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipMigration = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Teknik Servis Production Deployment" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. ENVIRONMENT VARIABLES VALIDATION
# ============================================
Write-Host "[1/7] Environment Variables kontrolü..." -ForegroundColor Yellow

$requiredEnvVars = @(
    "ConnectionStrings__DefaultConnection",
    "JwtSettings__SecretKey",
    "AdminBootstrap__Secret",
    "AdminBootstrap__Password"
)

$missingVars = @()
foreach ($varName in $requiredEnvVars) {
    $value = [Environment]::GetEnvironmentVariable($varName, "Process")
    if ([string]::IsNullOrEmpty($value) -or $value -eq "TO_BE_SET_IN_USER_SECRETS_OR_ENV") {
        $missingVars += $varName
        Write-Host "  ❌ EKSIK: $varName" -ForegroundColor Red
    } else {
        Write-Host "  ✅ OK: $varName" -ForegroundColor Green
    }
}

if ($missingVars.Count -gt 0) {
    Write-Host ""
    Write-Host "HATA: Eksik environment variables!" -ForegroundColor Red
    Write-Host "Lütfen şu değişkenleri IIS Application Settings'e ekleyin:" -ForegroundColor Yellow
    foreach ($var in $missingVars) {
        Write-Host "  - $var" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "Örnek IIS ayarı (web.config):" -ForegroundColor Cyan
    Write-Host @"
<environmentVariables>
  <environmentVariable name="ConnectionStrings__DefaultConnection" 
    value="Server=YOUR_SERVER;Database=TeknikServis;Uid=USER;Pwd=PASS;SslMode=Required" />
  <environmentVariable name="JwtSettings__SecretKey" 
    value="YOUR_64_CHAR_CRYPTOGRAPHICALLY_SECURE_KEY" />
</environmentVariables>
"@ -ForegroundColor Gray
    exit 1
}

# JWT Secret Key uzunluk kontrolü
$jwtSecret = [Environment]::GetEnvironmentVariable("JwtSettings__SecretKey", "Process")
if ($jwtSecret.Length -lt 32) {
    Write-Host ""
    Write-Host "HATA: JwtSettings__SecretKey en az 32 karakter olmalı!" -ForegroundColor Red
    Write-Host "Mevcut uzunluk: $($jwtSecret.Length)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Güvenli key oluşturmak için:" -ForegroundColor Cyan
    Write-Host "  -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | % {[char]`$_})" -ForegroundColor Gray
    exit 1
}

Write-Host "  ✅ Tüm environment variables doğru!" -ForegroundColor Green
Write-Host ""

# ============================================
# 2. DATABASE MIGRATION
# ============================================
if (-not $SkipMigration) {
    Write-Host "[2/7] Database migration çalıştırılıyor..." -ForegroundColor Yellow
    
    $apiPath = Get-Location
    Push-Location $apiPath
    
    try {
        Write-Host "  dotnet ef database update --environment $Environment" -ForegroundColor Gray
        dotnet ef database update --environment $Environment
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ Migration başarılı!" -ForegroundColor Green
        } else {
            throw "Migration failed with exit code $LASTEXITCODE"
        }
    } catch {
        Write-Host "  ❌ Migration hatası: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Pop-Location
    Write-Host ""
} else {
    Write-Host "[2/7] Database migration ATLANDI" -ForegroundColor DarkYellow
    Write-Host ""
}

# ============================================
# 3. ADMIN USER CREATION (İLK KURULUM)
# ============================================
if (-not $SkipAdminCreation) {
    Write-Host "[3/7] Admin kullanıcısı oluşturuluyor..." -ForegroundColor Yellow
    
    $apiUrl = "https://etisan.muratkostak.com/api/auth/create-admin"
    $adminSecret = [Environment]::GetEnvironmentVariable("AdminBootstrap__Secret", "Process")
    
    $headers = @{
        "X-Admin-Secret" = $adminSecret
        "Accept" = "application/json"
    }
    
    try {
        Write-Host "  POST $apiUrl" -ForegroundColor Gray
        $response = Invoke-RestMethod -Uri $apiUrl -Method POST -Headers $headers -ErrorAction Stop
        
        if ($response.exists) {
            Write-Host "  ℹ️  Admin kullanıcısı zaten mevcut" -ForegroundColor Cyan
        } else {
            Write-Host "  ✅ Admin kullanıcısı oluşturuldu!" -ForegroundColor Green
            Write-Host "     Email: $($response.user.email)" -ForegroundColor Gray
        }
    } catch {
        if ($_.Exception.Response.StatusCode -eq 403) {
            Write-Host "  ℹ️  Admin creation production'da disabled (beklenen durum)" -ForegroundColor Cyan
        } else {
            Write-Host "  ⚠️  Admin oluşturulamadı (manuel oluşturmanız gerekebilir): $_" -ForegroundColor Yellow
        }
    }
    Write-Host ""
} else {
    Write-Host "[3/7] Admin user creation ATLANDI" -ForegroundColor DarkYellow
    Write-Host ""
}

# ============================================
# 4. CONFIGURATION VALIDATION
# ============================================
Write-Host "[4/7] Production configuration kontrol ediliyor..." -ForegroundColor Yellow

$appSettingsPath = "appsettings.Production.json"
if (Test-Path $appSettingsPath) {
    $config = Get-Content $appSettingsPath | ConvertFrom-Json
    
    # AdminBootstrap disabled olmalı
    if ($config.AdminBootstrap.Enabled -eq $true) {
        Write-Host "  ❌ UYARI: AdminBootstrap:Enabled = true! Production'da false olmalı!" -ForegroundColor Red
        Write-Host "     Güvenlik riski! Lütfen appsettings.Production.json düzeltin." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ AdminBootstrap disabled" -ForegroundColor Green
    }
    
    # Swagger disabled olmalı
    if ($config.Swagger.Enabled -eq $true) {
        Write-Host "  ⚠️  UYARI: Swagger enabled! Production'da kapalı olması önerilir." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ Swagger disabled" -ForegroundColor Green
    }
    
    # AllowedOrigins kontrolü
    if ($config.AllowedOrigins -and $config.AllowedOrigins.Count -gt 0) {
        Write-Host "  ✅ AllowedOrigins configured:" -ForegroundColor Green
        foreach ($origin in $config.AllowedOrigins) {
            Write-Host "     - $origin" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ❌ AllowedOrigins boş! CORS çalışmayacak!" -ForegroundColor Red
    }
} else {
    Write-Host "  ⚠️  appsettings.Production.json bulunamadı!" -ForegroundColor Yellow
}
Write-Host ""

# ============================================
# 5. HEALTH CHECK
# ============================================
Write-Host "[5/7] Health check çalıştırılıyor..." -ForegroundColor Yellow

$healthUrl = "https://etisan.muratkostak.com/health"
try {
    Write-Host "  GET $healthUrl" -ForegroundColor Gray
    $health = Invoke-RestMethod -Uri $healthUrl -Method GET -TimeoutSec 10 -ErrorAction Stop
    
    if ($health.status -eq "Healthy") {
        Write-Host "  ✅ Uygulama sağlıklı!" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  Status: $($health.status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ Health check başarısız: $_" -ForegroundColor Red
    Write-Host "  Uygulama çalışmıyor olabilir!" -ForegroundColor Yellow
}
Write-Host ""

# ============================================
# 6. DIRECTORY STRUCTURE
# ============================================
Write-Host "[6/7] Gerekli klasörler oluşturuluyor..." -ForegroundColor Yellow

$directories = @(
    "Logs",
    "SecureUploads",
    "SecureUploads\workitems",
    "wwwroot",
    "wwwroot\uploads"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  ✅ Created: $dir" -ForegroundColor Green
    } else {
        Write-Host "  ℹ️  Exists: $dir" -ForegroundColor Gray
    }
}
Write-Host ""

# ============================================
# 7. IIS APP POOL RESTART
# ============================================
Write-Host "[7/7] IIS Application Pool restart..." -ForegroundColor Yellow

$appPoolName = "TeknikServisAppPool"
try {
    if (Get-Command "Restart-WebAppPool" -ErrorAction SilentlyContinue) {
        Restart-WebAppPool -Name $appPoolName -ErrorAction Stop
        Write-Host "  ✅ App pool '$appPoolName' restarted" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  IIS modülü bulunamadı, manuel restart gerekli" -ForegroundColor Yellow
        Write-Host "     IIS Manager > Application Pools > $appPoolName > Restart" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ⚠️  App pool restart başarısız (manuel yapılabilir): $_" -ForegroundColor Yellow
}
Write-Host ""

# ============================================
# DEPLOYMENT ÖZET
# ============================================
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DEPLOYMENT TAMAMLANDI!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
Write-Host "  1. Admin user ile login test edin: https://etisan.muratkostak.com" -ForegroundColor White
Write-Host "  2. Hangfire dashboard kontrol edin: https://etisan.muratkostak.com/hangfire" -ForegroundColor White
Write-Host "  3. File upload/download test edin" -ForegroundColor White
Write-Host "  4. Logs klasörünü izleyin: .\Logs\" -ForegroundColor White
Write-Host ""
Write-Host "Monitoring URLs:" -ForegroundColor Yellow
Write-Host "  - Health: https://etisan.muratkostak.com/health" -ForegroundColor Gray
Write-Host "  - Swagger (if enabled): https://etisan.muratkostak.com/swagger" -ForegroundColor Gray
Write-Host ""
