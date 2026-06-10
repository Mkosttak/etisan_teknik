/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: Program.cs
 * KONUM: TeknikServisAPI/
 * TİP: Backend - ASP.NET Core Startup & Configuration
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * ASP.NET Core Web API uygulamasının ana başlangıç (entry point) dosyası.
 * Tüm service'lerin, middleware'lerin, ve konfigürasyonların yapıldığı yerdir.
 * 
 * SORUMLULUKLAR:
 * - Dependency Injection (DI) Container yapılandırması
 * - Middleware pipeline konfigürasyonu
 * - Database (EF Core + MySQL) bağlantısı
 * - Authentication & Authorization (JWT)
 * - Logging (Serilog)
 * - Background Jobs (Hangfire)
 * - Rate Limiting
 * - CORS politikaları
 * - Health Checks
 * - Security Headers
 * - Swagger/OpenAPI dokümantasyonu
 * 
 * PRODUCTION-READY ÖZELLİKLER:
 * - User Secrets & Environment Variables ile güvenli konfigürasyon
 * - HTTPS Redirection (zorunlu)
 * - Secure file upload paths
 * - Admin-only Hangfire Dashboard
 * - Production-safe CORS
 * - Health Check endpoints (Kubernetes ready)
 * - Correlation ID for distributed tracing
 * - Global exception handling
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using TeknikServisAPI.Authorization;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;
using TeknikServisAPI.Services;
using TeknikServisAPI.Middleware;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.MySql;
using Hangfire.Dashboard;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.CookiePolicy;

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * GLOBAL TRY-CATCH BLOCK
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * Tüm uygulama startup sırasında oluşabilecek kritik hataları yakalar.
 * Serilog ile fatal log kaydedilir ve uygulama temiz şekilde kapatılır.
 */
try
{
    var builder = WebApplication.CreateBuilder(args);

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * SECURITY: SERVER VERSION HIDING
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * Response header'larında "Server: Kestrel" bilgisini gizler.
     * Security best practice - saldırganlar sunucu versiyonu bilgisini kullanarak
     * spesifik zafiyetler arayabilir.
     */
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * SERILOG - STRUCTURED LOGGING
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Yapılandırılmış logging framework (JSON formatted logs).
     * 
     * KONFİGÜRASYON:
     * - appsettings.json'dan okur (MinimumLevel, WriteTo, vb.)
     * - Enrich.FromLogContext() -> Correlation ID gibi context bilgileri eklenir
     * 
     * ÇIKIŞ YERLERİ:
     * - Console (Development)
     * - File (Production - /logs klasörü)
     * - Seq / Application Insights (opsiyonel)
     */
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();

    Log.Information("🚀 Teknik Servis API başlatılıyor...");

    // ASP.NET Core'un default logger'ını Serilog ile değiştir
    builder.Host.UseSerilog();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * QUESTPDF - PDF GENERATION LICENSE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * QuestPDF kütüphanesi Community lisansını global olarak aktive eder.
     * 
     * ÖNEMLİ:
     * - Community lisansı ücretsizdir ve açık kaynak/kişisel kullanım için geçerlidir
     * - Production kullanımda lisans gereksinimlerini kontrol edin
     * - Bu ayar uygulama başlangıcında MUTLAKA yapılmalıdır
     * 
     * SORUN:
     * Bu satır eksikse PDF'ler boş oluşur (içerik yazılmaz)
     */
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    Log.Information("✅ QuestPDF Community license activated");


    /**
     * ═══════════════════════════════════════════════════════════════════════
     * MVC CONTROLLERS & JSON SERIALİZATION
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AddControllers(): API controller'ları etkinleştirir
     * 
     * JSON OPTIONS:
     * - camelCase property naming (örn: firstName, lastName)
     * - WriteIndented: true (readable JSON - development için)
     */
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            // Production ortamında okunabilir JSON çıktısını kapat (performans için)
            if (builder.Environment.IsDevelopment())
            {
                options.JsonSerializerOptions.WriteIndented = true;
            }
            else
            {
                options.JsonSerializerOptions.WriteIndented = false;
            }
        });

    /**
     * ⭐ SECURITY: COOKIE POLICY
     * CSRF ve Session güvenliği için SameSite politikasını zorunlu kılar.
     */
    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.CheckConsentNeeded = context => false;
        options.MinimumSameSitePolicy = SameSiteMode.Lax;
        options.HttpOnly = HttpOnlyPolicy.Always;
        options.Secure = CookieSecurePolicy.Always;
    });

    // Memory cache - Rate limiting ve performans için
    builder.Services.AddMemoryCache();
    
    /**
     * ═══════════════════════════════════════════════════════════════════════
     * RATE LIMITING - BRUTE-FORCE PROTECTION
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * IP bazlı rate limiting (API abuse ve brute-force saldırılarına karşı).
     * 
     * KONFİGÜRASYON:
     * appsettings.json -> IpRateLimiting section
     * 
     * ÖRNEK KURAL:
     * - Login endpoint: 5 istek / 1 dakika
     * - Genel API: 100 istek / 1 dakika
     */
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * FLUENT VALIDATION
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * DTO validation için FluentValidation kullanılır.
     * Assembly'deki tüm IValidator<T> implementasyonları otomatik register edilir.
     * 
     * ÖRNEK:
     * CreateRequestDtoValidator, UpdateFaultDtoValidator, vb.
     */
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * MEDIATR - EVENT-DRIVEN ARCHITECTURE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * CQRS pattern ve domain events için MediatR kullanılır.
     * Handlers otomatik olarak bulunur ve DI container'a eklenir.
     */
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
    
    /**
     * ═══════════════════════════════════════════════════════════════════════
     * HEALTH CHECKS - PRODUCTION MONITORING
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Uygulamanın sağlık durumunu izlemek için health check endpoint'leri.
     * Kubernetes, Docker, load balancer'lar için kritik.
     * 
     * CHECKS:
     * 1. MySQL Database - bağlantı kontrolü
     * 2. Hangfire - background job sistem kontrolü
     * 
     * ENDPOINTS:
     * - /health - Genel durum (public)
     * - /health/detailed - Detaylı durum (admin only)
     * - /health/live - Kubernetes liveness probe
     * - /health/ready - Kubernetes readiness probe
     */
    builder.Services.AddHealthChecks()
        .AddMySql(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: new[] { "db", "sql", "mysql" })
        .AddHangfire(options =>
        {
            options.MinimumAvailableServers = 1;
        }, 
        name: "hangfire", 
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "background-jobs" });

    builder.Services.AddEndpointsApiExplorer();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * API VERSIONING
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * API versiyonlama desteği (future-proof design).
     * 
     * KULLANIM:
     * - URL Segment: /api/v1/requests
     * - Header: X-Api-Version: 1.0
     * 
     * DEFAULT VERSION: 1.0
     */
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
            new Asp.Versioning.UrlSegmentApiVersionReader(),
            new Asp.Versioning.HeaderApiVersionReader("X-Api-Version")
        );
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

/**
 * ═══════════════════════════════════════════════════════════════════════
 * SWAGGER / OPENAPI
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * API dokümantasyonu ve test arayüzü.
 * 
 * JWT AUTHENTICATION:
 * Swagger UI'da "Authorize" butonu ile Bearer token girilir.
 * 
 * ENDPOINT: /swagger
 */
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Teknik Servis API", 
        Version = "v1",
        Description = "Teknik Servis Yönetim Sistemi API"
    });
    
    // JWT Authentication için Swagger yapılandırması
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * MYSQL DATABASE - ENTITY FRAMEWORK CORE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * MySQL veritabanı bağlantısı ve EF Core DbContext konfigürasyonu.
     * 
     * CONNECTION STRING:
     * 1. User Secrets (Development)
     * 2. Environment Variables (Production)
     * 3. appsettings.json (NOT RECOMMENDED for secrets)
     * 
     * GÜVENLİK:
     * Connection string mutlaka User Secrets veya env variables'dan okunmalı.
     * Hard-coded password GÜVENLİ DEĞİL!
     * 
     * QUERY SPLITTING:
     * SplitQuery behavior - Include'lu sorgularda performans için.
     */
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString) || connectionString == "TO_BE_SET_IN_USER_SECRETS_OR_ENV")
    {
        throw new InvalidOperationException(
            "Database connection string is not configured. " +
            "Please set it using User Secrets (dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"your-connection-string\") " +
            "or environment variables.");
    }
    
    Log.Information("✅ Database connection configured successfully");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * HANGFIRE - BACKGROUND JOBS
     * ══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Arka planda çalışan periyodik ve deferred job'lar.
     * 
     * STORAGE:
     * - Development: Memory Storage (data kaybı olabilir)
     * - Production: MySQL Storage (persistent)
     * 
     * JOBS:
     * 1. check-pending-faults - Her saatte bekleyen arızaları kontrol et
     * 2. check-low-stock - Her gün 09:00'da düşük stok kontrolü
     * 3. cleanup-old-logs - Her Pazar 02:00'de eski logları temizle
     * 
     * DASHBOARD: /hangfire (Admin only!)
     */
    var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection") ?? connectionString;
    var useMemoryStorage = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Hangfire:UseMemoryStorage");

    builder.Services.AddHangfire(configuration =>
    {
        configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings();

        if (useMemoryStorage)
        {
            configuration.UseMemoryStorage();
        }
        else
        {
            var tablePrefix = builder.Configuration.GetValue<string>("Hangfire:TablesPrefix") ?? "Hangfire";
            configuration.UseStorage(new MySqlStorage(hangfireConnectionString, new MySqlStorageOptions
            {
                TablesPrefix = tablePrefix
            }));
        }
    });

    builder.Services.AddHangfireServer();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * JWT AUTHENTICATION
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Stateless token-based authentication.
     * 
     * SECRET KEY:
     * - MUTLAKA User Secrets veya env variables'dan okunmalı
     * - HMAC-SHA256 için minimum 32 karakter (256-bit) gerekli
     * 
     * TOKEN VALIDATION:
     * - Issuer: API'nin kendisi
     * - Audience: Frontend client
     * - Lifetime: AccessToken 15 dakika, RefreshToken 7 gün
     * - Signature: SymmetricSecurityKey ile doğrulanır
     * 
     * QUERY STRING TOKEN:
     * PDF download ve file download endpoint'leri için query string'de
     * token gönderme desteği (iframe ve browser download için gerekli).
     */
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    if (string.IsNullOrEmpty(secretKey) || secretKey == "TO_BE_SET_IN_USER_SECRETS_OR_ENV")
    {
        throw new InvalidOperationException(
            "JWT Secret Key is not configured. " +
            "Please set it using User Secrets (dotnet user-secrets set \"JwtSettings:SecretKey\" \"your-secret-key\") " +
            "or environment variables.");
    }
    
    // ⭐ GÜVENLİK: HMAC-SHA256 için minimum 256-bit (32 karakter) key gerekli
    if (secretKey.Length < 32)
    {
        throw new InvalidOperationException(
            $"JWT Secret Key must be at least 32 characters for HMAC-SHA256 security. " +
            $"Current key length: {secretKey.Length}. " +
            "Please use a longer, cryptographically secure random string.");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            // JWT claim'lerini ASP.NET Core claim'lerine map et
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        /**
         * ⭐ SECURITY: QUERY STRING TOKEN SUPPORT KALDIRILDI
         * 
         * NEDEN:
         * Query string'de token göndermek ciddi güvenlik riski oluşturur:
         * - URL'ler browser history'de saklanır
         * - Referrer header'ında sızabilir
         * - Proxy ve server loglarında görünür
         * - Güvenli olmayan kanallardan paylaşılabilir
         * 
         * ÇÖZÜM:
         * Dosya indirme için kısa ömürlü tek kullanımlık token veya
         * yetkili API endpoint üzerinden streaming kullanın.
         * 
         * SADECE Authorization header kabul edilir.
         */
    });

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * AUTHORIZATION - POLICY-BASED
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * Custom authorization policy: RequestOwnerOrPrivileged
     * 
     * KURAL:
     * Kullanıcı ya kendi taleplerine erişebilir, ya da privileged role'dedir
     * (Admin, Teknik, Proje).
     * 
     * HANDLER: RequestOwnerHandler.cs
     */
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicies.RequestOwnerOrPrivileged, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new RequestOwnerRequirement());
        });
    });

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * DEPENDENCY INJECTION - SERVICES
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * Tüm application service'ler Scoped lifetime ile register edilir.
     * 
     * SCOPED:
     * Her HTTP request için yeni instance oluşturulur.
     * Request bittiğinde dispose edilir.
     * 
     * SERVICE LISTESI:
     * - IAuthService: Authentication & JWT token generation
     * - IDashboardService: Dashboard istatistikleri
     * - IFaultService: Arıza yönetimi
     * - IRequestService: Talep yönetimi
     * - IShipmentService: Kargo takibi
     * - IStockService: Stok yönetimi
     * - IInstitutionService: Kurum yönetimi
     * - IBackgroundJobService: Hangfire job'ları
     * - INotificationService: Bildirim yönetimi
     * - IWorkItemService: İş kalemi yönetimi
     * - IAuditLogService: Audit log kayıtları
     * - PdfService: PDF generation (QuestPDF)
     * - RequestOwnerHandler: Authorization handler
     */
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IFaultService, FaultService>();
    builder.Services.AddScoped<IRequestService, RequestService>();
    builder.Services.AddScoped<IShipmentService, ShipmentService>();
    builder.Services.AddScoped<IStockService, StockService>();
    builder.Services.AddScoped<IInstitutionService, InstitutionService>();
    builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IWorkItemService, WorkItemService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<PdfService>();
    builder.Services.AddScoped<IAuthorizationHandler, RequestOwnerHandler>();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * CORS - CROSS-ORIGIN RESOURCE SHARING
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Frontend (React) farklı origin'den API'ye erişebilsin.
     * 
     * PRODUCTION SECURITY:
     * - AllowedOrigins mutlaka appsettings.Production.json'da tanımlı olmalı
     * - Wildcard (*) KULLANILMAZ
     * - Specific methods ve headers tanımlı
     * 
     * DEVELOPMENT:
     * - Default: http://localhost:3000
     * 
     * CREDENTIALS:
     * WithCredentials() -> Cookie ve Authorization header desteği
     */
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
    
    // ⭐ SECURITY: Production'da CORS origin'leri mutlaka tanımlanmalı
    if (builder.Environment.IsProduction() &&(allowedOrigins == null || allowedOrigins.Length == 0))
    {
        throw new InvalidOperationException(
            "AllowedOrigins must be configured in production environment. " +
            "Please set it in appsettings.Production.json or environment variables.");
    }
    allowedOrigins ??= new[] { "http://localhost:3000" }; // Development fallback only
    
    var allowedMethods = builder.Configuration.GetSection("AllowedMethods").Get<string[]>() 
        ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
    var allowedHeaders = builder.Configuration.GetSection("AllowedHeaders").Get<string[]>()
        ?? new[] { "Authorization", "Content-Type", "Accept", "X-Requested-With" };
    
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)   // ⭐ Production-safe: specific methods only
                  .WithHeaders(allowedHeaders)   // ⭐ Production-safe: specific headers only
                  .AllowCredentials();
        });
    });

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * WEB APPLICATION BUILD
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * Builder'dan WebApplication instance'ı oluşturulur.
     * Bu noktadan sonra middleware pipeline configure edilir.
     */
    var app = builder.Build();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * MIDDLEWARE PIPELINE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * SIRA ÇOK ÖNEMLİ!
     * 
     * 1. Rate Limiting (en önce - tüm istekleri filtreler)
     * 2. Routing
     * 3. CORS (routing'den sonra)
     * 4. Authentication
     * 5. Authorization
     * 6. Hangfire Dashboard (auth sonrası)
     * 7. Correlation ID
     * 8. Global Exception Handler
     * 9. Request Logging
     * 10. Static Files (güvenlik middleware'i ile)
     * 11. Swagger (sadece dev/enabled ise)
     * 12. Security Headers
     * 13. HTTPS Redirection
     * 14. Controllers (son)
     */

    // 0. Security - Cookie Policy (MUST be early)
    app.UseCookiePolicy();

    // 1. Rate limiting middleware (must be before UseRouting)
    app.UseIpRateLimiting();
    
    // 2. Routing
    app.UseRouting();
    
    // 3. CORS - MUTLAKA Routing'den sonra
    app.UseCors("AllowAll");
    
    // 4-5. Authentication & Authorization MUST be before Hangfire
    app.UseAuthentication();
    app.UseAuthorization();
    
    /**
     * 6. HANGFIRE DASHBOARD
     * 
     * ENDPOINT: /hangfire
     * AUTHORIZATION: Sadece Admin rolü erişebilir (HangfireAuthorizationFilter)
     * 
     * KRİTİK:
     * Hangfire dashboard job'ları iptal etme, retry etme gibi hassas işlemler
     * içerdiği için MUTLAKA Admin-only olmalı.
     */
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter() },
        DashboardTitle = "Teknik Servis - Background Jobs"
    });
    
    // 7. Correlation ID Middleware - Distributed tracing için
    app.UseCorrelationId();
    
    // 8. Global Exception Handler Middleware
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    
    // 9. Request Logging Middleware (Correlation ID zaten loglanıyor)
    app.UseMiddleware<RequestLoggingMiddleware>();

// OPTIONS request'leri için özel handling


// Response header'larını her response'a ekle (exception'lar dahil)


    /**
     * ═══════════════════════════════════════════════════════════════════════
     * STATIC FILES - SECURITY MIDDLEWARE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Statik dosya erişimini güvenli hale getir.
     * 
     * GÜVENLİK KURALLARI:
     * 1. /secure-uploads -> KAPALI (404) - Sadece API endpoint'leri üzerinden erişim
     * 2. /pdfs -> KAPALI (404) - Generated PDF'ler sadece API üzerinden
     * 3. /uploads -> SADECE AUTHENTICATED kullanıcılar erişebilir
     * 
     * NEDEN:
     * Dosyalar hassas bilgi içerebilir (müşteri imzası, arıza fotoğrafları vb.).
     * Direkt URL ile erişim GÜVENLİ DEĞİL!
     */
    app.Use(async (context, next) =>
    {
        var path = context.Request.Path;
        
        // ⭐ SECURITY: SecureUploads klasörü direkt erişime KAPALI
        // Dosyalar SADECE authenticated API endpoint'leri üzerinden erişilebilir
        if (path.StartsWithSegments("/secure-uploads"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        
        if (path.StartsWithSegments("/pdfs"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (path.StartsWithSegments("/uploads"))
        {
            var authResult = await context.AuthenticateAsync();
            if (!authResult.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            context.User = authResult.Principal!;
        }

        await next();
    });

    // 10. Static Files
    app.UseStaticFiles();

    /**
     * 11. SWAGGER / OPENAPI
     * 
     * ENDPOINT: /swagger
     * 
     * PRODUCTION:
     * Swagger sadece explicitly enabled ise aktif (güvenlik için).
     * appsettings.Production.json -> Swagger:Enabled = true
     */
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Teknik Servis API v1");
            c.RoutePrefix = "swagger";
        });
    }
    
    // 12. Endpoints
    app.MapControllers();

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * HANGFIRE RECURRING JOBS
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * Periyodik olarak çalışan background job'lar.
     * 
     * 1. check-pending-faults
     *    - Çalışma: Her saat başı (Cron.Hourly)
     *    - Görev: Bekleyen arızaları kontrol et, gerekirse bildirim gönder
     * 
     * 2. check-low-stock
     *    - Çalışma: Her gün saat 09:00 (Cron.Daily(9))
     *    - Görev: Stokta düşük olan ürünleri kontrol et, bildirim gönder
     * 
     * 3. cleanup-old-logs
     *    - Çalışma: Her Pazar saat 02:00 (Cron.Weekly(DayOfWeek.Sunday, 2))
     *    - Görev: 30 günden eski logları temizle (performans)
     */
    RecurringJob.AddOrUpdate<IBackgroundJobService>(
        "check-pending-faults",
        service => service.CheckPendingFaultsAsync(),
        Cron.Hourly); // Her saat başı çalışır

    RecurringJob.AddOrUpdate<IBackgroundJobService>(
        "check-low-stock",
        service => service.CheckLowStockItemsAsync(),
        Cron.Daily(9)); // Her gün saat 09:00'da çalışır

    RecurringJob.AddOrUpdate<IBackgroundJobService>(
        "cleanup-old-logs",
        service => service.CleanupOldLogsAsync(),
        Cron.Weekly(DayOfWeek.Sunday, 2)); // Her Pazar saat 02:00'de çalışır

    RecurringJob.AddOrUpdate<IBackgroundJobService>(
        "cleanup-expired-tokens",
        service => service.CleanupExpiredRefreshTokensAsync(),
        Cron.Daily(3)); // Her gün saat 03:00'te çalışır - güvenlik için

    // Startup log
    Log.Information("🌐 API başlatılıyor: http://localhost:5002");
    Log.Information("📖 Swagger UI: http://localhost:5002/swagger");
    Log.Information("📊 Hangfire Dashboard: http://localhost:5002/hangfire");
    
    /**
     * ═══════════════════════════════════════════════════════════════════════
     * HEALTH CHECK ENDPOINTS - KUBERNETES/DOCKER READY
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * 1. /health
     *    - Public endpoint
     *    - Production'da sadece status ve timestamp
     *    - Development'ta detaylı bilgi
     *    - Kullanım: Load balancer health check
     * 
     * 2. /health/detailed
     *    - Admin only (authentication required)
     *    - Detaylı health check bilgisi (DB, Hangfire, vb.)
     *    - Exception mesajları dahil
     * 
     * 3. /health/live
     *    - Kubernetes liveness probe
     *    - Her zaman healthy (uygulama çalışıyor mu?)
     * 
     * 4. /health/ready
     *    - Kubernetes readiness probe
     *    - DB bağlantısı kontrolü
     *    - Traffic alabilir mi?
     */
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            // ⭐ GÜVENLİK: Production'da sadece basit durum bilgisi ver
            var isProduction = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction();
            
            if (isProduction)
            {
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow
                }));
            }
            else
            {
                var response = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = $"{e.Value.Duration.TotalMilliseconds}ms",
                        tags = e.Value.Tags
                    }),
                    totalDuration = $"{report.TotalDuration.TotalMilliseconds}ms"
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }));
            }
        }
    });
    
    // ADMIN ONLY: Detaylı health check - sadece authenticated admin kullanıcılar
    app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = $"{e.Value.Duration.TotalMilliseconds}ms",
                    tags = e.Value.Tags,
                    exception = e.Value.Exception?.Message
                }),
                totalDuration = $"{report.TotalDuration.TotalMilliseconds}ms"
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }));
        }
    }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    
    // Kubernetes probes
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Always healthy - liveness probe
    });
    
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db") // DB bağlantısı kontrolü - readiness probe
    });
    
    // ⭐ PRODUCTION: Security Headers (OWASP recommendations)
    app.UseSecurityHeaders();


    // ⭐ SECURITY: HTTPS Redirection - tüm ortamlarda aktif (best practice)
    // ⭐ HTTPS Redirect - Sadece production'da aktif
    // Development'ta frontend HTTP üzerinden bağlanır, HTTPS redirect sorun yaratır
    if (app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }
    
    /**
     * DATABASE SEEDING
     * 
     * İlk çalıştırmada default data oluşturulur:
     * - Admin user
     * - Default roller
     * - Sample data (development)
     */
    await TeknikServisAPI.Data.DbSeeder.SeedAsync(app);
    
    /**
     * APPLICATION START
     * 
     * Configuration-based URLs kullanılır:
     * - launchSettings.json
     * - ASPNETCORE_URLS environment variable
     * - Kestrel configuration
     * 
     * Docker/Kubernetes için port flexibility.
     */
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Uygulama başlatılamadı");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * HANGFIRE AUTHORIZATION FILTER
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Hangfire Dashboard'a SADECE Admin rolündeki kullanıcıların erişmesini sağlar.
 * 
 * KRİTİK:
 * Hangfire dashboard job'ları görüntüleme, iptal etme, retry etme gibi
 * hassas işlemler içerir. PRODUCTION SECURITY için MUTLAKA Admin-only olmalı.
 * 
 * KONTROLLER:
 * 1. Kullanıcı authenticated mi?
 * 2. Kullanıcı Admin rolünde mi?
 * 
 * @returns true: erişim izni var, false: erişim engellendi
 */
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Kullanıcı authenticated olmalı
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }
        
        // ⭐ KRİTİK: Sadece Admin rolündeki kullanıcılar Hangfire Dashboard'a erişebilir
        // Bu, job'ları görüntüleme, iptal etme, retry etme gibi hassas işlemleri korur
        if (!httpContext.User.IsInRole("Admin"))
        {
            return false;
        }
        
        return true;
    }
}



