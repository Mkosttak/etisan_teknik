# Production Deployment Checklist

**Destination:** Real production server (https://etisan.muratkostak.com)  
**Last Updated:** 2026-01-15

---

## 🔴 CRITICAL - ZOR

UNLU (Canlıya Çıkmadan Önce Mutlaka Yapılması Gerekenler)

### A. Environment Variables (IIS Application Settings)

- [ ] **ConnectionStrings__DefaultConnection**
  ```
  Server=YOUR_MYSQL_SERVER;Database=TeknikServis;Uid=YOUR_USER;Pwd=YOUR_PASSWORD;SslMode=Required;CharSet=utf8mb4
  ```
  - [ ] MySQL server erişilebilir
  - [ ] Database oluşturulmuş
  - [ ] User'ın yetkileri var (CREATE, ALTER, SELECT, INSERT, UPDATE, DELETE)

- [ ] **JwtSettings__SecretKey**
  - [ ] Minimum 32 karakter (önerilen 64 karakter)
  - [ ] Cryptographically secure random string
  - [ ] **ASLA** Git'e commit edilmemeli!
  
  **Oluşturma komutu:**
  ```powershell
  -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | % {[char]$_})
  ```

- [ ] **AdminBootstrap__Secret**
  - [ ] First-time admin creation için kullanılacak
  - [ ] Deployment sonrası silinebilir

- [ ] **AdminBootstrap__Password**
  - [ ] İlk admin kullanıcısının şifresi
  - [ ] En az 8 karakter, büyük harf, küçük harf, rakam içermeli

- [ ] **ASPNETCORE_ENVIRONMENT = Production**
  - [ ] web.config'de set edilmiş

### B. Database Migration

- [ ] **Backup Strategy**
  - [ ] Otomatik backup script kuruldu
  - [ ] Backup retention policy belirlendi (30 gün önerilir)
  - [ ] Restore testi yapıldı

- [ ] **Migration Execution**
  ```powershell
  cd TeknikServisAPI
  dotnet ef database update --environment Production
  ```
  - [ ] Tüm migration'lar başarıyla çalıştı
  - [ ] Tablo yapıları doğru
  - [ ] Index'ler oluşturulmuş

### C. IIS Configuration

- [ ] **Application Pool**
  - [ ] İsim: `TeknikServisAppPool`
  - [ ] .NET CLR Version: **No Managed Code**
  - [ ] Managed Pipeline Mode: Integrated
  - [ ] Start Mode: AlwaysRunning (önerilir)
  - [ ] Idle Timeout: 0 (önerilir, uygulamayı her zaman aktif tutar)

- [ ] **Site Bindings**
  - [ ] HTTPS (port 443)
  - [ ] SSL Certificate yüklü ve geçerli
  - [ ] HTTP -> HTTPS redirect aktif

- [ ] **AspNetCoreModuleV2**
  - [ ] Hosting Bundle yüklü
  - [ ] web.config doğru yapılandırılmış

### D. Frontend Build & Deployment

- [ ] **Environment Variables**
  ```bash
  # .env.production
  VITE_API_BASE_URL=/api
  ```

- [ ] **Build Execution**
  ```bash
  cd frontend
  npm ci  # Clean install
  npm run build
  ```
  - [ ] Build hatasız tamamlandı
  - [ ] `dist/` klasörü oluştu

- [ ] **Deployment**
  - [ ] `dist/` içeriği `TeknikServisAPI/wwwroot/` klasörüne kopyalandı
  - [ ] `index.html` root'ta var

### E. SSL/TLS Certificate

- [ ] **Certificate Installation**
  - [ ] Valid SSL certificate (Let's Encrypt, DigiCert, vb.)
  - [ ] Expiration date > 30 gün
  - [ ] Auto-renewal configured

- [ ] **HTTPS Enforcement**
  - [ ] `UseHttpsRedirection()` middleware aktif
  - [ ] HSTS header enabled

### F. File System Permissions

- [ ] **Directories Created**
  ```
  TeknikServisAPI/
  ├── Logs/                    (yazılabilir)
  ├── SecureUploads/          (yazılabilir)
  │   └── workitems/
  └── wwwroot/                (okunabilir)
      └── uploads/            (backward compat)
  ```

- [ ] **IIS AppPool User Permissions**
  - [ ] `Logs/` klasörüne WRITE izni
  - [ ] `SecureUploads/` klasörüne WRITE izni
  - [ ] `wwwroot/` klasörüne READ izni

---

## 🟠 IMPORTANT - Önemli (İlk Hafta İçinde)

### G. Initial Admin Setup

- [ ] **Admin User Creation**
  ```powershell
  .\Scripts\setup-production.ps1
  ```
  VEYA Manuel:
  ```powershell
  $headers = @{ "X-Admin-Secret" = "YOUR_SECRET" }
  Invoke-RestMethod -Uri "https://etisan.muratkostak.com/api/auth/create-admin" -Method POST -Headers $headers
  ```

- [ ] **Test Login**
  - [ ] Email: `admin@etisan.com`
  - [ ] Password: (AdminBootstrap:Password değeri)
  - [ ] Login başarılı

- [ ] **AdminBootstrap Disable**
  ```json
  // appsettings.Production.json
  "AdminBootstrap": {
    "Enabled": false  // ✅ Mutlaka false
  }
  ```

### H. Monitoring Setup

- [ ] **Health Check Endpoints**
  - [ ] `/health` - Public health check çalışıyor
  - [ ] `/health/detailed` - Admin-only çalışıyor
  - [ ] `/health/ready` - Database bağlantısı kontrol ediliyor

- [ ] **Hangfire Dashboard**
  - [ ] `https://etisan.muratkostak.com/hangfire` erişilebilir
  - [ ] Admin user ile login gerekiyor (güvenlik OK)
  - [ ] Recurring jobs çalışıyor:
    - [ ] `check-pending-faults` (Hourly)
    - [ ] `check-low-stock` (Daily 09:00)
    - [ ] `cleanup-old-logs` (Weekly Sunday 02:00)

- [ ] **Logging**
  - [ ] `Logs/` klasöründe log dosyaları oluşuyor
  - [ ] Log retention: 7 gün (Production)
  - [ ] Disk space monitoring kuruldu

### I. Security Validation

- [ ] **CORS Test**
  - [ ] Frontend'den API çağrıları çalışıyor
  - [ ] Başka domain'lerden engellenmiş

- [ ] **File Upload Security**
  - [ ] MIME spoofing testi (fake .jpg.exe upload)
  - [ ] Path traversal testi (`../../appsettings.json` download)
  - [ ] Dosyalar `SecureUploads/` klasörüne kaydediliyor

- [ ] **JWT Token**
  - [ ] Token expiration çalışıyor (30 min)
  - [ ] Refresh token rotation çalışıyor
  - [ ] Invalid token rejected

---

## 🟡 RECOMMENDED - Önerilen (İlk Ay İçinde)

### J. Performance Optimization

- [ ] **Connection Pooling**
  ```
  Server=...;MinimumPoolSize=5;MaximumPoolSize=50;ConnectionTimeout=30;
  ```

- [ ] **Response Caching**
  - [ ] Statik data için cache implementasyonu

- [ ] **CDN Integration** (Opsiyonel)
  - [ ] Static files CDN'e taşınabilir

### K. Advanced Monitoring

- [ ] **Application Insights / ELK Setup**
  - [ ] Structured logging sink eklendi
  - [ ] Dashboard kuruldu
  - [ ] Alerts configured

- [ ] **Performance Metrics**
  - [ ] Response time tracking
  - [ ] Error rate monitoring
  - [ ] Resource usage (CPU, RAM, Disk)

### L. Disaster Recovery

- [ ] **Backup Verification**
  - [ ] Weekly restore test schedule

- [ ] **Rollback Plan**
  - [ ] Previous version binary'leri saklanıyor
  - [ ] Database point-in-time recovery test edildi

---

## ✅ POST-DEPLOYMENT VERIFICATION

### İlk 15 Dakika İçinde

- [ ] **Health Check**: `GET /health` → Status 200
- [ ] **Login Test**: Admin login başarılı
- [ ] **File Upload Test**: Bir dosya yükle
- [ ] **File Download Test**: Yüklenen dosyayı indir
- [ ] **Hangfire Dashboard**: Admin ile erişilebilir

### İlk 1 Saat İçinde

- [ ] **API Endpoints**: Tüm CRUD işlemleri test edildi
- [ ] **Background Jobs**: İlk recurring job çalıştı
- [ ] **Logs**: Error log yok, Info loglar düzgün

### İlk 24 Saat İçinde

- [ ] **Performance**: Average response time < 200ms
- [ ] **Errors**: Zero 500 errors
- [ ] **Memory**: No memory leaks observed

---

## 🚨 ROLLBACK PROCEDURE

Eğer deployment başarısız olursa:

```powershell
# 1. IIS'i durdur
Stop-WebAppPool -Name "TeknikServisAppPool"

# 2. Eski version'ı geri yükle
Copy-Item -Path "C:\Backup\TeknikServisAPI\*" -Destination "C:\inetpub\TeknikServisAPI\" -Recurse -Force

# 3. Database rollback (eğer migration yapıldıysa)
dotnet ef database update PreviousMigrationName --environment Production

# 4. IIS'i başlat
Start-WebAppPool -Name "TeknikServisAppPool"

# 5. Health check
Invoke-RestMethod -Uri "https://etisan.muratkostak.com/health"
```

---

## 📞 SUPPORT CONTACTS

- **Database Admin**: [Database team contact]
- **Network/Infrastructure**: [IT team contact]
- **Security Team**: [Security team contact]

---

**Deployment Date**: _____________  
**Deployed By**: _____________  
**Verified By**: _____________  
**Sign-off**: _____________
