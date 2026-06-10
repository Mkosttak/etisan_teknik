# Teknik Servis Yönetim Sistemi

> **Kapsamlı Teknik Servis, Arıza Takibi ve Stok Yönetimi Platformu**

Modern, ölçeklenebilir ve güvenli bir teknik servis yönetim sistemi. ASP.NET Core Web API backend ve React frontend ile geliştirilmiştir.

---

## 📋 İçindekiler

- [Proje Hakkında](#-proje-hakkında)
- [Teknoloji Stack](#-teknoloji-stack)
- [Mimari Yapı](#-mimari-yapı)
- [Ana Özellikler](#-ana-özellikler)
- [Kurulum](#-kurulum)
- [Kullanıcı Rolleri ve Yetkiler](#-kullanıcı-rolleri-ve-yetkiler)
- [API Dokümantasyonu](#-api-dokümantasyonu)
- [Sayfalar ve İşlevleri](#-sayfalar-ve-i̇şlevleri)
- [Deployment](#-deployment)
- [Geliştirici Notları](#-geliştirici-notları)

---

## 🎯 Proje Hakkında

**Teknik Servis Yönetim Sistemi**, teknik servis hizmetleri sunan kuruluşlar için geliştirilmiş, end-to-end bir iş yönetim platformudur. Sistem, müşteri talebi alımından arıza çözümüne, kargo takibinden servis formlarına kadar tüm teknik servis operasyonlarını dijital ortamda yönetmenizi sağlar.

### Temel Kullanım Senaryoları

1. **Talep Yönetimi**: Müşterilerden gelen teknik servis taleplerini kaydetme, takip etme ve yönetme
2. **Arıza Takibi**: Cihaz arızalarını lifecycle boyunca takip etme (Müşteride → Tamirde → Üreticede → Teslim)
3. **Stok ve Parça Yönetimi**: Teknik servis için gerekli yedek parça stoklarını yönetme
4. **Kargo Operasyonları**: Müşteriye, üreticiye veya teknisyene gönderilecek ürünlerin takibi
5. **Servis Formları**: Müşteri onaylı servis formlarını dijital ortamda oluşturma ve PDF olarak arşivleme
6. **İş Akışı (Workflow)**: Teknik ekip için görev yönetimi ve proje takibi
7. **Bildirim Sistemi**: Kullanıcıya ve role özel gerçek zamanlı bildirimler

---

## 🛠 Teknoloji Stack

### Backend (TeknikServisAPI)

| Teknoloji | Versiyon | Kullanım Alanı |
|-----------|----------|----------------|
| **ASP.NET Core** | 8.0 | Web API Framework |
| **Entity Framework Core** | 8.0 | ORM (Object-Relational Mapping) |
| **MySQL** | 8.0+ | İlişkisel Veritabanı (Pomelo.EntityFrameworkCore.MySql) |
| **JWT Bearer** | 8.0 | Stateless Authentication |
| **BCrypt.Net** | 4.0.3 | Şifre Hashleme (Güvenlik) |
| **Serilog** | 8.0 | Yapılandırılmış Logging (JSON, File, Console) |
| **Hangfire** | 1.8.6 | Background Jobs & Recurring Tasks |
| **FluentValidation** | 11.3 | DTO Validation |
| **MediatR** | 12.4 | CQRS Pattern & Event-Driven Architecture |
| **QuestPDF** | 2025.12 | PDF Generation (Servis Formları) |
| **ImageSharp** | 3.1.12 | Resim İşleme ve Optimizasyon |
| **AspNetCoreRateLimit** | 5.0 | IP-Based Rate Limiting (Brute-Force Protection) |
| **Swagger/OpenAPI** | 6.5 | API Dokümantasyonu |
| **HtmlSanitizer** | 8.0.865 | XSS Koruması |

### Frontend (teknik-servis-frontend)

| Teknoloji | Versiyon | Kullanım Alanı |
|-----------|----------|----------------|
| **React** | 18.2 | UI Framework |
| **Vite** | 7.3 | Modern Build Tool (HMR, Fast Refresh) |
| **Ant Design** | 5.12 | Enterprise UI Component Library |
| **React Router** | 6.20 | Client-Side Routing |
| **TanStack React Query** | 5.17 | Server State Management & Caching |
| **Axios** | 1.6.2 | HTTP Client |
| **Day.js** | 1.11 | Tarih Manipülasyonu |
| **Recharts** | 2.10 | Grafikler ve Dashboard |
| **Framer Motion** | 12.27 | Animasyonlar |
| **ExcelJS** | 4.4 | Excel Export İşlemleri |
| **Vitest** | 2.0 | Unit Testing |

---

## 🏗 Mimari Yapı

### Backend Mimarisi (N-Tier Architecture)

```
TeknikServisAPI/
│
├── Controllers/          # API Endpoints (HTTP Request Handling)
│   ├── AuthController          → Kimlik doğrulama (Login, Refresh Token)
│   ├── RequestsController      → Talep yönetimi
│   ├── FaultsController        → Arıza yönetimi
│   ├── ShipmentsController     → Kargo takibi
│   ├── StockController         → Stok yönetimi
│   ├── ServiceFormsController  → Servis formları
│   ├── WorkItemsController     → İş akışı (Kanban)
│   ├── InstitutionsController  → Kurum yönetimi
│   ├── UsersController         → Kullanıcı yönetimi
│   ├── DashboardController     → Dashboard istatistikleri
│   ├── NotificationsController → Bildirimler
│   └── AuditLogsController     → Audit kayıtları
│
├── Services/             # Business Logic Layer
│   ├── IAuthService / AuthService
│   ├── IRequestService / RequestService
│   ├── IFaultService / FaultService
│   ├── IShipmentService / ShipmentService
│   ├── IStockService / StockService
│   ├── INotificationService / NotificationService
│   ├── IWorkItemService / WorkItemService
│   ├── IInstitutionService / InstitutionService
│   ├── IDashboardService / DashboardService
│   ├── IBackgroundJobService / BackgroundJobService
│   ├── IAuditLogService / AuditLogService
│   └── PdfService (QuestPDF)
│
├── Models/               # Database Entities
│   ├── User, RefreshToken
│   ├── Request, RequestStockItem
│   ├── Fault, FaultHistory
│   ├── Shipment, ShipmentItem
│   ├── StockItem, StockMovement
│   ├── ServiceForm
│   ├── WorkItem, WorkItemComment, WorkItemAttachment, WorkItemHistory
│   ├── Institution, InstitutionAddress
│   ├── Notification, NotificationReceipt
│   ├── AuditLog
│   └── Enums (FaultStatus, Priority, UserRole, vb.)
│
├── DTOs/                 # Data Transfer Objects
│   └── Create/Update/Response DTO'ları (FluentValidation ile validate edilir)
│
├── Data/                 # EF Core DbContext
│   └── ApplicationDbContext (Fluent API ile ilişki tanımları)
│
├── Validators/           # FluentValidation Rules
│   └── DTO validator'ları (CreateRequestDtoValidator, vb.)
│
├── Middleware/           # Custom Middlewares
│   ├── GlobalExceptionHandlerMiddleware  → Merkezi hata yakalama
│   ├── RequestLoggingMiddleware          → İstek/Yanıt loglama
│   ├── CorrelationIdMiddleware           → Distributed tracing için Correlation ID
│   └── HangfireAuthorizationFilter       → Hangfire Dashboard güvenliği
│
├── Authorization/        # Custom Authorization Handlers
│   └── RequestOwnerHandler → Policy-based authorization (kendi talebine veya privileged role)
│
├── Events/               # Domain Events
│   └── FaultStatusChangedEvent, WorkItemStatusChangedEvent (MediatR ile işlenir)
│
├── EventHandlers/        # MediatR Event Handlers
│   └── Domain event'leri dinler ve gerekli işlemleri yapar (bildirim gönderme vb.)
│
├── Utilities/            # Helper Fonksiyonları
├── Configuration/        # Application Configuration
├── Exceptions/           # Custom Exception Types
└── Program.cs            # Startup & Dependency Injection
```

### Frontend Mimarisi (Component-Based)

```
frontend/src/
│
├── pages/                # Route-Level Pages (Lazy Loaded)
│   ├── Login.jsx              → Giriş sayfası
│   ├── Dashboard.jsx          → Ana sayfa & İstatistikler
│   ├── Requests.jsx           → Talepler listesi
│   ├── RequestDetail.jsx      → Talep detayı
│   ├── RequestForm.jsx        → Talep oluştur/düzenle
│   ├── Faults.jsx             → Arızalar listesi
│   ├── FaultDetail.jsx        → Arıza detayı
│   ├── FaultForm.jsx          → Arıza oluştur/düzenle
│   ├── Shipments.jsx          → Kargolar (Kanban görünümü)
│   ├── Stock.jsx              → Stok yönetimi
│   ├── StockForm.jsx          → Stok ürün ekle/düzenle
│   ├── ServiceForms.jsx       → Servis formları listesi
│   ├── ServiceFormWizard.jsx  → Servis formu wizard (multi-step)
│   ├── ServiceFormDetail.jsx  → Servis formu görüntüleme
│   ├── WorkFlow.jsx           → İş akışı (Kanban board)
│   ├── WorkItemForm.jsx       → İş kalemi ekle/düzenle
│   ├── Institutions.jsx       → Kurum listesi ve detayları
│   ├── InstitutionManagement.jsx → Kurum CRUD (Admin only)
│   ├── Users.jsx              → Kullanıcı listesi (Admin only)
│   ├── UserForm.jsx           → Kullanıcı ekle/düzenle (Admin only)
│   └── ChangePassword.jsx     → Şifre değiştirme
│
├── components/           # Reusable Components
│   ├── MainLayout.jsx         → Sidebar, Header, Content wrapper
│   ├── LoadingSpinner.jsx     → Global loading animasyonu
│   ├── ScrollToTop.jsx        → Route değişiminde scroll reset
│   └── InstitutionSelect.jsx  → Kurum seçim dropdown'u (global)
│
├── context/              # React Context Providers
│   ├── AuthContext.jsx        → Kullanıcı authentication state
│   ├── ThemeContext.jsx       → Dark/Light mode
│   └── NotificationContext.jsx → Real-time bildirim yönetimi
│
├── services/             # API Service Layer
│   └── api.js                 → Axios instance & HTTP interceptors
│
├── hooks/                # Custom React Hooks
│   └── useDebounce.js
│
├── constants/            # Constants & Enums
│   └── roles.js               → Kullanıcı rolleri (backend ile senkron)
│
├── utils/                # Utility Functions
│   └── formatters.js          → Tarih, para birimi formatlama
│
├── lib/                  # Third-party Library Configs
│   └── queryClient.js         → React Query configuration
│
├── App.jsx               # Root Component (Routes & Provider Hierarchy)
├── main.jsx              # React DOM Entry Point
└── index.css             # Global CSS
```

---

## ✨ Ana Özellikler

### 1. 🎟 Talep Yönetimi (Requests)

- Müşteri taleplerini kaydetme ve takip etme
- Talep durumu: `Created` → `InProgress` → `Completed` / `Cancelled`
- Talebe stok parça bağlama (`RequestStockItem`)
- Öncelik seviyeleri: Low, Normal, High, Urgent
- Timeline görünümü ile talep geçmişi
- Dosya ekleme desteği (resim, PDF, dokümanlar)

### 2. 🔧 Arıza Yönetimi (Faults)

- Cihaz arızalarını yaşam döngüsü boyunca takip
- Arıza durumları:
  - `Created` → Müşteriden alındı
  - `ReceivedByTechnician` → Teknikde
  - `InProgress` → Tamir devam ediyor
  - `WaitingForParts` → Parça bekleniyor
  - `Completed` → Tamamlandı
  - `Cancelled` → İptal edildi
- Cihaz konumu takibi:
  - `InCustomer` → Müşteride
  - `InRepair` → Serviste tamirde
  - `AtManufacturer` → Üreticiye garanti ile gönderildi
  - `Completed` → Tamir tamamlandı
  - `Delivered` → Müşteriye teslim edildi
- Timeline ile arıza geçmişi ve yapılan işlemler
- Arıza fotoğrafları ve dokümanları

### 3. 📦 Kargo Yönetimi (Shipments)

- Kanban board ile kargo durumu görselleştirme
- Kargo türleri:
  - `ShippingToCustomer` → Müşteriye gönderiliyor
  - `ShippingToManufacturer` → Üreticiye gönderiliyor (garanti)
  - `ShippingToTechnician` → Teknisyene gönderiliyor
  - `ReceivedFromManufacturer` → Üreticiden alındı
- Kargo durumları: `Preparing` → `Shipped` → `Delivered` / `Cancelled`
- Kargo takip numarası (tracking number) kaydetme
- Drag & drop ile kargo durumu güncelleme

### 4. 📊 Stok Yönetimi (Stock)

- Yedek parça ve malzeme stok takibi
- Stok hareketleri (In/Out) ile giriş/çıkış kayıtları
- Düşük stok uyarıları (kritik eşik)
- Stok hareketleri geçmişi
- Hangfire job ile günlük düşük stok kontrolü

### 5. 📝 Servis Formları (Service Forms)

- Multi-step wizard ile servis formu oluşturma
- Müşteri bilgileri, cihaz bilgileri, yapılan işlemler
- Müşteri imza alanı (mobil fotoğraf veya file upload)
- **PDF Generation** (QuestPDF) - Müşteri için yazdırılabilir form
- Durum: `DRAFT` (taslak) → `SUBMITTED` (gönderildi, PDF oluşturuldu)

### 6. 🗂 İş Akışı / Workflow (Kanban Board)

- Görev yönetimi (WorkItem)
- Kanban görevleri: Todo → In Progress → Done
- Öncelik, teslim tarihi, atanan kişi
- Yorumlar, dosya ekleri, checklist
- Timeline ile iş kalemi geçmişi
- Teslim tarihi yaklaşan/geçen görevler için görsel uyarı

### 7. 🔔 Bildirim Sistemi

- Gerçek zamanlı bildirimler (Role-based & User-specific)
- Bildirim tipleri: Info, Success, Warning, Error
- Bildirim okundu/okunmadı takibi (`NotificationReceipt`)
- Header'da bildirim badge'i (okunmamış sayısı)
- Bildirim tetikleyicileri:
  - Talep/arıza durumu değişimi
  - İş kalemi atandığında
  - Düşük stok uyarısı
  - Kargo durumu değişimi

### 8. 🏢 Kurum Yönetimi (Institutions)

- Müşteri kurumları ve adreslerini kaydetme
- Kurum detayları (telefon, e-posta, adres)
- Kuruma bağlı cihazlar (`Device`)
- Kurum bazlı filtreleme (Talep, Arıza, Servis Formları)

### 9. 📈 Dashboard & Analytics

- Özet istatistikler (toplam talep, arıza, sevkiyat)
- Son 7 günlük talep/arıza grafikleri (Recharts)
- Aktif arızalar, bekleyen talepler
- Düşük stokta olan ürünler
- Rollere göre özelleştirilmiş dashboard

### 10. 🔐 Kimlik Doğrulama & Yetkilendirme

- **JWT Bearer Authentication** (Access Token + Refresh Token)
- Access Token: 30 dakika, Refresh Token: 7 gün
- Refresh token rotation (güvenlik)
- BCrypt ile şifre hashleme
- **Role-Based Access Control (RBAC)** - 5 kullanıcı rolü
- **Policy-Based Authorization** (örn: `RequestOwnerOrPrivileged`)
- Rate limiting (brute-force protection): Login endpoint 5 istek/dakika

### 11. 🎨 Modern UI/UX

- Ant Design component library
- Dark/Light mode desteği
- Responsive tasarım (mobil uyumlu)
- Lazy loading ile performans optimizasyonu
- Framer Motion animasyonları
- Türkçe yerelleştirme (tam destek)
- Custom loading spinner (branded)

### 12. 🔄 Background Jobs (Hangfire)

- **check-pending-faults**: Her saat başı bekleyen arızaları kontrol et
- **check-low-stock**: Her gün 09:00'da düşük stok kontrolü
- **cleanup-old-logs**: Her Pazar 02:00'de eski logları temizle
- **cleanup-expired-tokens**: Her gün 03:00'te süresi geçmiş refresh token'ları temizle
- Hangfire Dashboard (`/hangfire`) - Admin only

### 13. 🛡 Güvenlik Özellikleri

- HTTPS redirection (zorunlu)
- CORS politikaları (production-safe, wildcard yok)
- XSS koruması (HtmlSanitizer)
- SQL Injection koruması (EF Core parameterized queries)
- Secure file uploads (`SecureUploads/` klasörü direkt erişime kapalı)
- JWT secret key validation (minimum 256-bit)
- Cookie policy (HttpOnly, Secure, SameSite)
- Security headers (X-Frame-Options, X-Content-Type-Options, vb.)
- Rate limiting (IP-based)

### 14. 📊 Logging & Monitoring

- **Serilog** ile yapılandırılmış logging
- Console ve File log output'ları
- Correlation ID ile distributed tracing
- Request/Response logging middleware
- Health Check endpoints:
  - `/health` - Genel durum
  - `/health/detailed` - Detaylı (admin only)
  - `/health/live` - Kubernetes liveness probe
  - `/health/ready` - Kubernetes readiness probe

### 15. 📂 Dosya Yönetimi

- Güvenli dosya upload (`ImageSharp` ile resim validasyonu)
- Desteklenen formatlar: JPG, PNG, PDF, Excel, Word
- Dosya tipi bazlı işleme:
  - Resimler: Yeni tab'da açılır
  - PDF'ler: Yeni tab'da açılır
  - Diğerleri: Direkt download
- Timeline'da dosya görüntüleme

---

## 🚀 Kurulum

### Gereksinimler

- **Backend**: .NET 8 SDK, MySQL 8.0+
- **Frontend**: Node.js 18+, npm
- **Opsiyonel**: Docker, Git

### 1. Depoyu Klonlayın

```bash
git clone <repository-url>
cd <project-directory>
```

### 2. Backend Kurulumu

#### 2.1. Veritabanı Bağlantısını Yapılandırın

**Seçenek 1: User Secrets (Önerilen - Development)**

```bash
cd TeknikServisAPI

# Connection String
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=teknikservis;User=root;Password=yourpassword;"

# Hangfire Connection (opsiyonel, yoksa DefaultConnection kullanılır)
dotnet user-secrets set "ConnectionStrings:HangfireConnection" "Server=localhost;Port=3306;Database=teknikservis_hangfire;User=root;Password=yourpassword;"

# JWT Secret Key (minimum 32 karakter)
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-jwt-key-min-32-chars-long-example-123456"
```

**Seçenek 2: Environment Variables (Production)**

```bash
# Linux/Mac
export ConnectionStrings__DefaultConnection="Server=prod-db;Port=3306;Database=teknikservis;User=appuser;Password=prodpassword;"
export JwtSettings__SecretKey="your-production-jwt-secret-key-min-32-chars"

# Windows
set ConnectionStrings__DefaultConnection=Server=prod-db;Port=3306;Database=teknikservis;User=appuser;Password=prodpassword;
set JwtSettings__SecretKey=your-production-jwt-secret-key-min-32-chars
```

#### 2.2. Migration ve Veritabanını Oluşturun

```bash
# EF Core migrations'ı uygula
dotnet ef database update

# VEYA manuel SQL ile institution seed data'sını yükle
mysql -u root -p teknikservis < add_institutions.sql
```

#### 2.3. Backend'i Çalıştırın

```bash
dotnet run
```

Backend şu adreslerde çalışacak:
- API: `http://localhost:5002`
- Swagger: `http://localhost:5002/swagger`
- Hangfire Dashboard: `http://localhost:5002/hangfire`

### 3. Frontend Kurulumu

```bash
cd frontend
npm install

# Development sunucusunu başlat
npm run dev
```

Frontend şu adreste çalışacak:
- Frontend: `http://localhost:3000`

### 4. İlk Admin Kullanıcısını Oluşturun

> ⚠️ **GÜVENLİK UYARISI**: Aşağıdaki endpoint sadece **bir kere** kullanılmalıdır! Production'da mutlaka kapatın.

```bash
# 1. appsettings.json dosyasında AdminBootstrap ayarlarını yapın
{
  "AdminBootstrap": {
    "Enabled": true,               # SADECE İLK KURULUMDA true yapın!
    "SeedEnabled": false,
    "Secret": "your-bootstrap-secret",  # User Secrets'tan okunmalı
    "Password": "YourAdminPassword123!"  # User Secrets'tan okunmalı
  }
}

# 2. User secrets ile ayarlayın (ÖNERİLEN)
dotnet user-secrets set "AdminBootstrap:Secret" "YourSecretBootstrapKey123"
dotnet user-secrets set "AdminBootstrap:Password" "Admin123!StrongPassword"

# 3. API çağrısı yapın
POST http://localhost:5002/api/auth/create-admin
Content-Type: application/json

{
  "secret": "YourSecretBootstrapKey123",
  "username": "admin",
  "email": "admin@example.com",
  "fullName": "Sistem Yöneticisi",
  "password": "Admin123!StrongPassword"
}

# 4. MUTLAKA Admin oluşturduktan sonra AdminBootstrap:Enabled'ı false yapın!
```

### 5. Giriş Yapın

- URL: `http://localhost:3000/login`
- Kullanıcı adı: `admin`
- Şifre: Yukarıda belirlediğiniz şifre

---

## 👥 Kullanıcı Rolleri ve Yetkiler

Sistem 5 farklı kullanıcı rolüne sahiptir. Her rolün erişim yetkileri farklıdır.

### Rol Tanımları

| Rol | Açıklama | Erişim Kapsamı |
|-----|----------|----------------|
| **Admin** | Sistem yöneticisi | **Tüm yetkiler** - Kullanıcı yönetimi, kurum yönetimi, tüm modüller |
| **Proje** | Proje yöneticisi | Yönetimsel yetkiler - Dashboard, talepler, arızalar, servis formları, sevkiyat, stok, kurumlar, workflow |
| **Teknik** | Teknik personel | Teknik yetkiler - Dashboard, talepler, arızalar, servis formları, sevkiyat, stok, kurumlar, workflow |
| **Kullanici** | Standart kullanıcı | Kullanıcı yetkileri - Dashboard, talepler, arızalar, servis formları, sevkiyat, stok, kurumlar, workflow |
| **SahaElemani** | Saha personeli | Kısıtlı yetkiler - **Sadece** talepler, arızalar, servis formları ve şifre değiştirme |

### Sayfa Bazlı Yetki Matrisi

| Sayfa | Admin | Proje | Teknik | Kullanici | SahaElemani |
|-------|:-----:|:-----:|:------:|:---------:|:-----------:|
| Dashboard | ✅ | ✅ | ✅ | ✅ | ❌ |
| Talepler (Requests) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Arızalar (Faults) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Sevkiyat (Shipments) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Stok (Stock) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Servis Formları | ✅ | ✅ | ✅ | ✅ | ✅ |
| Kurumlar (Institutions) | ✅ | ✅ | ✅ | ✅ | ❌ |
| İş Akışı (Workflow) | ✅ | ✅ | ✅ | ✅ | ❌ |
| Kullanıcı Yönetimi | ✅ | ❌ | ❌ | ❌ | ❌ |
| Kurum Yönetimi (CRUD) | ✅ | ❌ | ❌ | ❌ | ❌ |
| Hangfire Dashboard | ✅ | ❌ | ❌ | ❌ | ❌ |
| Şifre Değiştir | ✅ | ✅ | ✅ | ✅ | ✅ |

### Authorization Policy

Sistemde **Policy-Based Authorization** kullanılır:

- **RequestOwnerOrPrivileged**: Kullanıcı ya kendi talebine/arızasına erişebilir, ya da `Admin` / `Proje` rolündedir.
  - Privileged Roller: `Admin`, `Proje`, `Teknik`, `Kullanici`
  - Non-Privileged: `SahaElemani` (sadece kendi kayıtlarına erişebilir)

---

## 📡 API Dokümantasyonu

### Base URL

- Development: `http://localhost:5002/api`
- Production: `https://your-domain.com/api`

### Swagger UI

API dokümantasyonu Swagger/OpenAPI ile sunulur:
- URL: `http://localhost:5002/swagger`
- Production'da varsayılan olarak kapalıdır. Açmak için `appsettings.Production.json`:
  ```json
  {
    "Swagger": {
      "Enabled": true
    }
  }
  ```

### Authentication

Tüm endpoint'ler (Login hariç) JWT Bearer authentication gerektirir.

**Token Alma:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "YourPassword123!"
}
```

**Response:**

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abc123...",
  "expiresIn": 1800,
  "user": {
    "id": 1,
    "username": "admin",
    "fullName": "Sistem Yöneticisi",
    "email": "admin@example.com",
    "role": "Admin"
  }
}
```

**Token Kullanımı:**

```http
GET /api/requests
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Refresh Token:**

```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

### Temel Endpoint'ler

#### 1. Authentication (`/api/auth`)

| Method | Endpoint | Açıklama | Auth |
|--------|----------|----------|------|
| POST | `/login` | Kullanıcı girişi | ❌ |
| POST | `/refresh-token` | Access token yenileme | ❌ |
| POST | `/logout` | Çıkış (refresh token iptal) | ✅ |
| POST | `/change-password` | Şifre değiştirme | ✅ |
| POST | `/create-admin` | İlk admin oluşturma (bootstrap) | ❌ |

#### 2. Requests (`/api/requests`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Talep listesi | Tümü |
| GET | `/{id}` | Talep detayı | Owner veya Privileged |
| POST | `/` | Yeni talep | Tümü |
| PUT | `/{id}` | Talep güncelle | Owner veya Privileged |
| DELETE | `/{id}` | Talep sil | Admin, Proje |

#### 3. Faults (`/api/faults`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Arıza listesi | Tümü |
| GET | `/{id}` | Arıza detayı | Tümü |
| POST | `/` | Yeni arıza | Tümü |
| PUT | `/{id}` | Arıza güncelle | Admin, Proje, Teknik, Kullanici |
| PATCH | `/{id}/status` | Arıza durumu güncelle | Admin, Proje, Teknik |
| DELETE | `/{id}` | Arıza sil | Admin |
| GET | `/{id}/history` | Arıza geçmişi | Tümü |

#### 4. Shipments (`/api/shipments`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Sevkiyat listesi | Admin, Proje, Teknik, Kullanici |
| GET | `/{id}` | Sevkiyat detayı | Admin, Proje, Teknik, Kullanici |
| POST | `/` | Yeni sevkiyat | Admin, Proje, Teknik |
| PUT | `/{id}` | Sevkiyat güncelle | Admin, Proje, Teknik |
| PATCH | `/{id}/status` | Sevkiyat durumu güncelle | Admin, Proje, Teknik |

#### 5. Stock (`/api/stock`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Stok listesi | Admin, Proje, Teknik, Kullanici |
| GET | `/{id}` | Stok detayı | Admin, Proje, Teknik, Kullanici |
| POST | `/` | Yeni stok ürün | Admin, Proje, Teknik |
| PUT | `/{id}` | Stok güncelle | Admin, Proje, Teknik |
| POST | `/movement` | Stok hareketi (giriş/çıkış) | Admin, Proje, Teknik |
| GET | `/low-stock` | Düşük stokta olanlar | Admin, Proje, Teknik |

#### 6. Service Forms (`/api/service-forms`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Servis formu listesi | Tümü |
| GET | `/{id}` | Servis formu detayı | Tümü |
| POST | `/` | Yeni servis formu (wizard) | Tümü |
| PUT | `/{id}` | Servis formu güncelle | Admin, Proje, Teknik |
| POST | `/{id}/submit` | Formu gönder ve PDF oluştur | Tümü |
| GET | `/{id}/pdf` | PDF indir | Tümü |

#### 7. WorkItems (`/api/workitems`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | İş kalemi listesi | Admin, Proje, Teknik, Kullanici |
| GET | `/{id}` | İş kalemi detayı | Admin, Proje, Teknik, Kullanici |
| POST | `/` | Yeni iş kalemi | Admin, Proje, Teknik, Kullanici |
| PUT | `/{id}` | İş kalemi güncelle | Admin, Proje, Teknik, Kullanici |
| PATCH | `/{id}/status` | Durum güncelle (Kanban) | Admin, Proje, Teknik, Kullanici |
| POST | `/{id}/comment` | Yorum ekle | Admin, Proje, Teknik, Kullanici |

#### 8. Institutions (`/api/institutions`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Kurum listesi | Admin, Proje, Teknik, Kullanici |
| GET | `/{id}` | Kurum detayı | Admin, Proje, Teknik, Kullanici |
| POST | `/` | Yeni kurum | Admin |
| PUT | `/{id}` | Kurum güncelle | Admin |
| DELETE | `/{id}` | Kurum sil | Admin |
| GET | `/{id}/devices` | Kuruma ait cihazlar | Admin, Proje, Teknik, Kullanici |

#### 9. Notifications (`/api/notifications`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Bildirim listesi | Tümü |
| GET | `/unread-count` | Okunmamış sayısı | Tümü |
| POST | `/{id}/read` | Bildirimi okundu işaretle | Tümü |
| POST | `/read-all` | Tümünü okundu işaretle | Tümü |

#### 10. Users (`/api/users`)

| Method | Endpoint | Açıklama | Roller |
|--------|----------|----------|--------|
| GET | `/` | Kullanıcı listesi | Admin |
| GET | `/{id}` | Kullanıcı detayı | Admin |
| POST | `/` | Yeni kullanıcı | Admin |
| PUT | `/{id}` | Kullanıcı güncelle | Admin |
| DELETE | `/{id}` | Kullanıcı sil | Admin |

---

## 📄 Sayfalar ve İşlevleri

### 1. **Login** (`/login`)
- Kullanıcı adı ve şifre ile giriş
- JWT token alımı
- Otomatik yönlendirme (role göre)
- Hatalı giriş durumunda kullanıcı dostu hata mesajı (sayfa yenilenmez)

### 2. **Dashboard** (`/`)
**Roller**: Admin, Proje, Teknik, Kullanici

**İçerik**:
- Özet kartlar (toplam talep, arıza, bekleyen işler, sevkiyat)
- Son 7 günlük talep/arıza grafikleri (çizgi grafik)
- Aktif arızalar tablosu
- Bekleyen talepler tablosu
- Düşük stokta ürünler tablosu
- Hoş geldiniz modalı (oturum başına bir kez gösterilir)

### 3. **Talepler** (`/requests`)
**Roller**: Tümü

**İçerik**:
- Talep listesi (tablo görünümü)
- Filtreleme: Durum, öncelik, tarih aralığı
- Arama: Talep numarası, müşteri adı
- Yeni talep oluştur butonu
- Talep detayına git
- Toplu Excel export

**Alt Sayfalar**:
- `/requests/new` - Yeni talep oluştur
- `/requests/:id` - Talep detayı (timeline, dosyalar)
- `/requests/:id/edit` - Talep düzenle

### 4. **Arızalar** (`/faults`)
**Roller**: Tümü

**İçerik**:
- Arıza listesi (tablo görünümü)
- Filtreleme: Durum, cihaz durumu, öncelik
- Arama: Arıza numarası, müşteri
- Yeni arıza oluştur butonu
- Arıza detayına git

**Alt Sayfalar**:
- `/faults/new` - Yeni arıza oluştur
- `/faults/:id` - Arıza detayı (timeline, fotoğraflar, geçmiş)
- `/faults/:id/edit` - Arıza düzenle

### 5. **Sevkiyat** (`/shipments`)
**Roller**: Admin, Proje, Teknik, Kullanici

**İçerik**:
- **Kanban Board Görünümü** (Preparing → Shipped → Delivered)
- Drag & drop ile kargo durumu güncelleme
- Kart üzerinde kargo tipi, ürünler, tracking number
- Filtreleme: Kargo tipi, durum
- Yeni sevkiyat oluştur modal

### 6. **Stok** (`/stock`)
**Roller**: Admin, Proje, Teknik, Kullanici

**İçerik**:
- Stok ürünleri tablosu
- Düşük stok uyarısı (kırmızı badge)
- Stok hareketi ekleme (giriş/çıkış)
- Stok hareketleri geçmişi
- Yeni stok ürün ekleme (Admin, Proje, Teknik)

**Alt Sayfalar**:
- `/stock/new` - Yeni stok ürün ekle
- `/stock/:id/edit` - Stok ürün düzenle

### 7. **Servis Formları** (`/service-forms`)
**Roller**: Tümü

**İçerik**:
- Servis formu listesi (tablo)
- Filtreleme: Durum (Draft/Submitted), tarih
- Yeni servis formu oluştur (wizard)
- PDF indirme (submitted formlar için)

**Alt Sayfalar**:
- `/service-forms/new` - Yeni servis formu wizard (multi-step)
- `/service-forms/:id` - Mevcut formu düzenle (wizard)
- `/service-forms/:id/view` - Forma bak (read-only)

**Wizard Adımları**:
1. Müşteri ve Kurum Bilgileri
2. Cihaz Bilgileri
3. Yapılan İşlemler
4. Müşteri İmzası (fotoğraf veya dosya upload)
5. Özet ve Gönder

### 8. **Kurumlar** (`/institutions`)
**Roller**: Admin, Proje, Teknik, Kullanici

**İçerik**:
- Kurum listesi ve detayları (collapse/expand)
- Kurum arama
- Kuruma ait cihazlar
- Kurum adresi
- Kuruma ait talepler/arızalar (dashboard)

### 9. **İş Akışı** (`/workflow`)
**Roller**: Admin, Proje, Teknik, Kullanici

**İçerik**:
- **Kanban Board** (Todo → In Progress → Done)
- Görev kartları (başlık, öncelik, atanan kişi, teslim tarihi)
- Teslim tarihi yaklaşan/geçen görevler için uyarı badge'i (turuncu/kırmızı)
- Drag & drop ile görev durumu değiştirme
- Görev detay modalı (yorumlar, dosyalar, checklist, tarihçe)

**Alt Sayfalar**:
- `/workflow/new` - Yeni iş kalemi oluştur
- `/workflow/:id/edit` - İş kalemi düzenle

### 10. **Kullanıcı Yönetimi** (`/users`)
**Roller**: Sadece Admin

**İçerik**:
- Kullanıcı listesi (tablo)
- Kullanıcı ekleme, düzenleme, silme
- Rol atama
- Kullanıcı arama

**Alt Sayfalar**:
- `/users/new` - Yeni kullanıcı ekle
- `/users/:id/edit` - Kullanıcı düzenle

### 11. **Kurum Yönetimi** (`/institution-management`)
**Roller**: Sadece Admin

**İçerik**:
- Kurum CRUD işlemleri (ekleme, düzenleme, silme)
- Adres yönetimi
- Toplu işlemler

### 12. **Şifre Değiştir** (`/change-password`)
**Roller**: Tümü

**İçerik**:
- Mevcut şifre doğrulama
- Yeni şifre girişi
- Şifre politikası kontrolü

---

## 🚢 Deployment

### Production Checklist

#### Backend (TeknikServisAPI)

1. **Environment Variables**: Production'da tüm hassas bilgiler environment variables'dan okunmalı:
   ```bash
   ConnectionStrings__DefaultConnection=Server=prod-server;...
   JwtSettings__SecretKey=production-secret-min-32-chars
   AdminBootstrap__Enabled=false  # MUTLAKA false!
   Swagger__Enabled=false         # Opsiyonel: API dokümantasyonu gerekiyorsa true
   ```

2. **CORS**: `appsettings.Production.json` içinde production domain'leri ekleyin:
   ```json
   {
     "AllowedOrigins": [
       "https://yourdomain.com",
       "https://www.yourdomain.com"
     ]
   }
   ```

3. **HTTPS**: Production'da HTTPS zorunlu. SSL sertifikası gerekli.

4. **Database Migration**: Production veritabanında migration'ları uygulayın:
   ```bash
   dotnet ef database update --project TeknikServisAPI
   ```

5. **Logs**: Serilog log dosyalarını `/Logs` klasöründe saklar. Log rotation aktif (30 gün).

6. **Hangfire**: Production'da Hangfire MySQL storage kullanır (persistent). `HangfireConnection` connection string'ini ayarlayın.

#### Frontend (React)

1. **Environment Variables**: `.env.production` dosyasında production API URL:
   ```
   VITE_API_URL=https://api.yourdomain.com
   ```

2. **Build**:
   ```bash
   npm run build
   ```

3. **Deploy**: `dist/` klasörünü static hosting'e yükleyin (Nginx, Apache, Vercel, Netlify, vb.).

4. **Nginx Konfigürasyon Örneği**:
   ```nginx
   server {
       listen 80;
       server_name yourdomain.com;
       
       location / {
           root /var/www/frontend/dist;
           try_files $uri $uri/ /index.html;  # SPA routing için
       }
       
       location /api {
           proxy_pass http://localhost:5002;
           proxy_set_header Host $host;
           proxy_set_header X-Real-IP $remote_addr;
       }
   }
   ```

### Docker Deployment (Opsiyonel)

Backend için `Dockerfile` mevcuttur:

```bash
# Build
docker build -t teknik-servis-api:latest ./TeknikServisAPI

# Run
docker run -d \
  -p 5002:8080 \
  -e ConnectionStrings__DefaultConnection="Server=db;..." \
  -e JwtSettings__SecretKey="your-secret-key" \
  --name teknik-servis-api \
  teknik-servis-api:latest
```

### Health Checks (Kubernetes Ready)

Kubernetes deployment için health check endpoint'leri:

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

---

## 🔧 Geliştirici Notları

### Backend Development

#### Yeni Service Ekleme

1. Interface tanımla (`Services/IYourService.cs`)
2. Implementation oluştur (`Services/YourService.cs`)
3. `Program.cs`'de DI container'a ekle:
   ```csharp
   builder.Services.AddScoped<IYourService, YourService>();
   ```

#### Yeni Controller Ekleme

1. `Controllers/YourController.cs` oluştur
2. Controller'ı `[Authorize]` ile koru
3. Role-based yetkilendirme için `[Authorize(Roles = "Admin,Proje")]` kullan

#### Validation Kuralı Ekleme

1. `Validators/YourDtoValidator.cs` oluştur
2. `AbstractValidator<YourDto>` sınıfından türet
3. FluentValidation otomatik olarak DTO'yu validate eder

#### Event-Driven İşlemler (MediatR)

1. Event tanımla (`Events/YourEvent.cs`)
2. Event handler oluştur (`EventHandlers/YourEventHandler.cs`)
3. Service'te event publish et:
   ```csharp
   await _mediator.Publish(new YourEvent { ... });
   ```

### Frontend Development

#### Yeni Sayfa Ekleme

1. `pages/YourPage.jsx` oluştur
2. `App.jsx`'te lazy load ile import et:
   ```jsx
   const YourPage = lazy(() => import('./pages/YourPage'))
   ```
3. Route ekle:
   ```jsx
   <Route path="your-path" element={<RoleProtectedRoute allowedRoles={[...]}><YourPage /></RoleProtectedRoute>} />
   ```
4. Sidebar'a menü ekle (`components/MainLayout.jsx`)

#### API Çağrısı (React Query)

```jsx
import { useQuery, useMutation } from '@tanstack/react-query'
import api from '../services/api'

// GET
const { data, isLoading } = useQuery({
  queryKey: ['requests'],
  queryFn: async () => {
    const response = await api.get('/requests')
    return response.data
  }
})

// POST
const mutation = useMutation({
  mutationFn: async (newData) => {
    const response = await api.post('/requests', newData)
    return response.data
  },
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['requests'] })
  }
})
```

#### State Management

- **Global State**: React Context kullanılır (`AuthContext`, `ThemeContext`, `NotificationContext`)
- **Server State**: React Query (cache, refetch, invalidation)
- **Local State**: `useState` / `useReducer`

### Testing

#### Backend Tests (xUnit)

```bash
cd TeknikServisAPI.Tests
dotnet test
```

#### Frontend Tests (Vitest)

```bash
cd frontend
npm run test
npm run test:coverage
```

### Code Style

- **Backend**: C# naming conventions (PascalCase for public, camelCase for private)
- **Frontend**: ESLint + Prettier (otomatik format)
- **Commit Messages**: Conventional Commits (örn: `feat:`, `fix:`, `docs:`)

### Veritabanı Değişiklikleri

Yeni migration oluşturma:

```bash
cd TeknikServisAPI
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Loglama

Backend'de `ILogger` kullanın:

```csharp
_logger.LogInformation("İşlem başarılı: {RequestId}", requestId);
_logger.LogWarning("Düşük stok uyarısı: {StockItemId}", stockItemId);
_logger.LogError(exception, "Hata oluştu: {Message}", exception.Message);
```

Correlation ID otomatik olarak loglanır.

---

## 📞 Destek ve Katkı

### Hata Bildirimi

Bir hata bulduysanız lütfen şu bilgileri paylaşın:
- Hatanın adımları (reproducible steps)
- Beklenen davranış vs gerçekleşen
- Log kayıtları (`Logs/log-*.txt`)
- Ekran görüntüsü (varsa)

### Geliştirme Ortamı

- IDE: Visual Studio 2022 / VS Code
- Backend: C# IntelliSense, ReSharper (opsiyonel)
- Frontend: ESLint, Prettier, Vite DevTools
- Database: MySQL Workbench, DBeaver

---

## 📝 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

---

## 🌟 Highlights

- ✅ **Production-Ready**: Security best practices, health checks, logging, monitoring
- ✅ **Scalable**: N-tier architecture, DI, async/await patterns
- ✅ **Secure**: JWT, BCrypt, rate limiting, CORS, XSS protection
- ✅ **Modern UI**: Ant Design, responsive, dark mode, animations
- ✅ **Developer-Friendly**: Swagger, clear code structure, comprehensive documentation
- ✅ **Maintainable**: SOLID principles, separation of concerns, testable code

---

**Son Güncelleme**: Ocak 2026

Bu README dosyası, projenin tüm teknik detaylarını, kurulum adımlarını ve kullanım senaryolarını kapsamaktadır. Herhangi bir sorunuz varsa lütfen teknik ekiple iletişime geçin.

# etisan_teknik
#   e t i s a n _ t e k n i k  
 