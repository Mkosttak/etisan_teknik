/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: Enums.cs
 * KONUM: TeknikServisAPI/Models/
 * TİP: Backend - Enum Definitions
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ: Uygulama genelinde kullanılan enum tanımları
 * 
 * ENUM LİSTESİ:
 * - FaultStatus: Arıza durumları (Created → Completed/Cancelled)
 * - Priority: Öncelik seviyeleri (Low → Urgent)
 * - DeviceStatus: Cihaz konumu/durumu
 * - ShipmentType: Sevkiyat türü (Müşteri/Üretici/Teknik)
 * - ShipmentStatus: Sevkiyat durumu
 * - StockMovementType: Stok hareketleri (In/Out)
 * - NotificationType: Bildirim tipleri
 * - UserRole: Kullanıcı rolleri
 * - ServiceFormStatus: Servis formu durumu
 * 
 * NOT: Frontend'deki roles.js ile senkron tutulmalı
 * ═══════════════════════════════════════════════════════════════════════════
 */

namespace TeknikServisAPI.Models
{
    /// <summary>
    /// Arıza durumları - Lifecycle: Created → InProgress → Completed
    /// </summary>
    public enum FaultStatus
    {
        Created,              // Oluşturuldu - İlk durum
        ReceivedByTechnician, // Teknikde - Teknik aldı
        InProgress,           // İşlemde - Tamir devam ediyor
        WaitingForParts,      // Parça Bekleniyor - Stok yoksa
        Completed,            // Tamamlandı - Başarılı son durum
        Cancelled             // İptal Edildi - İptal edildi
    }

    /// <summary>
    /// Öncelik seviyeleri - SLA ve notification için kullanılır
    /// </summary>
    public enum Priority
    {
        Low,      // Düşük - Normal takip
        Normal,   // Normal - Standard SLA
        High,     // Yüksek - Öncelikli takip
        Urgent    // Acil - Anında müdahale
    }

    /// <summary>
    /// Cihaz durumları - Cihazın fiziksel konumu
    /// </summary>
    public enum DeviceStatus
    {
        InCustomer,        // Müşteride - Müşteri lokasyonunda
        InRepair,          // Tamirde - Serviste tamir ediliyor
        AtManufacturer,    // Üreticide - Garantiye gönderildi
        Completed,         // Tamamlandı - Tamir bitti
        Delivered          // Teslim Edildi - Müşteriye teslim edildi
    }

    /// <summary>
    /// Sevkiyat türleri - Kargo yönü
    /// </summary>
    public enum ShipmentType
    {
        ShippingToCustomer,      // Müşteriye Gönderiliyor
        ShippingToManufacturer,  // Üreticiye Gönderiliyor - Garanti
        ShippingToTechnician,    // Teknike Gönderiliyor - Saha tamiri
        ReceivedFromManufacturer // Üreticiden Alındı - Garanti dönüşü
    }

    /// <summary>
    /// Sevkiyat durumları
    /// </summary>
    public enum ShipmentStatus
    {
        Preparing,  // Hazırlanıyor - Paketleme aşaması
        Shipped,    // Kargoya Verildi - Tracking number var
        Delivered,  // Teslim Edildi - Karşı taraf aldı
        Cancelled   // İptal Edildi
    }

    /// <summary>
    /// Stok hareket türleri
    /// </summary>
    public enum StockMovementType
    {
        In,   // Giriş - Satın alma, iade
        Out   // Çıkış - Kullanım, fire
    }

    /// <summary>
    /// Bildirim türleri - UI renk ve icon için kullanılır
    /// </summary>
    public enum NotificationType
    {
        Info,     // Bilgi - Mavi
        Success,  // Başarı - Yeşil
        Warning,  // Uyarı - Turuncu
        Error     // Hata - Kırmızı
    }

    /// <summary>
    /// Kullanıcı rolleri - RBAC için
    /// NOT: Frontend roles.js ile SENKRON olmalı
    /// </summary>
    public enum UserRole
    {
        Admin,      // Admin - Tüm yetkiler
        Proje,      // Proje - Yönetim yetkileri
        Teknik,     // Teknik - Teknik yetkiler
        Saha,       // Saha - Saha yetkileri
        Kullanici   // Kullanıcı - Standart yetkiler
    }

    /// <summary>
    /// Servis form durumları
    /// </summary>
    public enum ServiceFormStatus
    {
        DRAFT,      // Taslak - Henüz gönderilmedi
        SUBMITTED   // Gönderildi - PDF oluşturuldu
    }
}


