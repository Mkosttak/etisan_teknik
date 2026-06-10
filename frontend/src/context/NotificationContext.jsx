/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: NotificationContext.jsx
 * KONUM: frontend/src/context/
 * TİP: Frontend - React Context Provider
 * ════════════════════════════════════════════════════════════════

════════
 * 
 * AMAÇ:
 * Uygulama genelinde bildirim (notification) yönetimi.
 * 
 * SORUMLULUKLAR:
 * - Backend'den bildirimleri çekme
 * - Bildirim listesi state yönetimi
 * - Okunmamış bildirim sayısı takibi
 * - Bildirimi okundu işaretleme
 * - Tüm bildirimleri okundu işaretleme
 * - Otomatik periyodik bildirim kontrolü (30 saniye)
 * 
 * KULLANIM:
 * const { notifications, unreadCount, markAsRead } = useNotifications()
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { createContext, useContext, useState, useEffect } from 'react'
import api from '../services/api'
import { useAuth } from './AuthContext'

/**
 * NOTIFICATION CONTEXT
 * 
 * Context içeriği:
 * - notifications: Bildirim listesi (array)
 * - unreadCount: Oku

nmamış bildirim sayısı
 * - markAsRead: Tek bildirimi okundu işaretle
 * - markAllAsRead: Tüm bildirimleri okundu işaretle
 * - refresh: Bildirimleri manuel olarak yenile
 */
const NotificationContext = createContext()

/**
 * useNotifications HOOK
 * 
 * NotificationContext'e erişim sağlar.
 * 
 * @returns {Object} Notification context değerleri
 * @throws {Error} Provider dışında kullanıldığında
 */
export const useNotifications = () => {
    const context = useContext(NotificationContext)
    if (!context) {
        throw new Error('useNotifications must be used within NotificationProvider')
    }
    return context
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * NOTIFICATION PROVIDER COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Bildirim state'i manage eder ve backend'den bildirim çeker.
 * 
 * İŞLEYİŞ:
 * 1. Kullanıcı login olduğunda (user değiştiğinde) bildirim çekme başlar
 * 2. İlk fetch sonrası her 30 saniyede bir otomatik fetch yapılır
 * 3. Kullanıcı logout olduğunda interval temizlenir
 * 
 * STATE:
 * - notifications: Bildirim array'i
 * - unreadCount: isRead=false olan bildirim sayısı
 * 
 * @param {ReactNode} children - Sarmalanacak component'ler
 */
export const NotificationProvider = ({ children }) => {
    const { user } = useAuth() // Kullanıcı bilgisi
    const [notifications, setNotifications] = useState([]) // Bildirim listesi
    const [unreadCount, setUnreadCount] = useState(0) // Okunmamış sayı

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * BİLDİRİMLERİ ÇEK
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Backend'den kullanıcının bildirimlerini çek.
     * 
     * API ENDPOINT: GET /notifications
     * 
     * İŞLEYİŞ:
     * 1. Kullanıcı yoksa direkt return (guest users için çalışmaz)
     * 2. Backend'e GET /notifications isteği at
     * 3. Response'u notifications state'ine kaydet
     * 4. isRead=false olan bildirimleri say, unreadCount'a kaydet
     * 5. Hata durumunda console.error (silent fail)
     * 
     * NOT:
     * Bu fonksiyon hem manuel (refresh) hem otomatik (interval) çağrılır.
     */
    const fetchNotifications = async () => {
        if (!user) return

        try {
            const response = await api.get('/notifications')
            setNotifications(response.data)
            setUnreadCount(response.data.filter(n => !n.isRead).length)
        } catch (error) {
            console.error('Bildirimler yüklenemedi:', error)
        }
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * BİLDİRİMİ OKUNDU İŞARETLE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Tek bir bildirimi okundu olarak işaretle.
     * 
     * API ENDPOINT: PUT /notifications/{id}/read
     * 
     * İŞLEYİŞ:
     * 1. Backend'e PUT isteği at
     * 2. Başarılıysa local state'i güncelle (optimistic update)
     * 3. Bildirim array'inde ilgili bildirimin isRead'ini true yap
     * 4. unreadCount'u 1 azalt (minimum 0)
     * 
     * @param {number} notificationId - Okunacak bildirimin ID'si
     */
    const markAsRead = async (notificationId) => {
        try {
            await api.put(`/notifications/${notificationId}/read`)
            setNotifications(prev =>
                prev.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
            )
            setUnreadCount(prev => Math.max(0, prev - 1))
        } catch (error) {
            console.error('Bildirim okunamadı:', error)
        }
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * TÜM BİLDİRİMLERİ OKUNDU İŞARETLE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Kullanıcının tüm bildirimlerini okundu olarak işaretle.
     * 
     * API ENDPOINT: PUT /notifications/read-all
     * 
     * İŞLEYİŞ:
     * 1. Backend'e PUT isteği at
     * 2. Başarılıysa local state'i güncelle
     * 3. Tüm bildirimlerin isRead'ini true yap
     * 4. unreadCount'u 0 yap
     */
    const markAllAsRead = async () => {
        try {
            await api.put('/notifications/read-all')
            setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
            setUnreadCount(0)
        } catch (error) {
            console.error('Bildirimler okunamadı:', error)
        }
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * OTOMATİK BİLDİRİM KONTROLÜ
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Kullanıcı giriş yaptığında bildirim çekmeyi başlat.
     * 
     * İŞLEYİŞ:
     * 1. user değiştiğinde (login/logout) effect çalışır
     * 2. Kullanıcı varsa:
     *    - İlk fetch'i hemen yap
     *    - setInterval ile her 30 saniyede bir fetch yap
     * 3. Kullanıcı yoksa veya component unmount olduğunda interval'i temizle
     * 
     * INTERVAL SÜRESI: 30000ms = 30 saniye
     * 
     * NOT:
     * Real-time bildirim için WebSocket kullanılabilir (şu an polling yöntemi).
     */
    useEffect(() => {
        if (user) {
            fetchNotifications()
            // Her 30 saniyede bir bildirim kontrolü
            const interval = setInterval(fetchNotifications, 30000)
            return () => clearInterval(interval)
        }
    }, [user])

    /**
     * CONTEXT VALUE
     * 
     * useNotifications hook'u ile erişilebilecek değerler.
     */
    const value = {
        notifications,      // Bildirim listesi
        unreadCount,        // Okunmamış bildirim sayısı
        markAsRead,         // Tek bildirimi okundu işaretle
        markAllAsRead,      // Tüm bildirimleri okundu işaretle
        refresh: fetchNotifications, // Manuel yenileme fonksiyonu
    }

    return (
        <NotificationContext.Provider value={value}>
            {children}
        </NotificationContext.Provider>
    )
}
