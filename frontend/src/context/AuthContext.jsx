/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: AuthContext.jsx
 * KONUM: frontend/src/context/
 * TİP: Frontend - React Context Provider
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulama genelinde authentication state yönetimi.
 * Login, logout ve kullanıcı bilgilerini tüm component'lere sağlar.
 * 
 * SORUMLULUKLAR:
 * - Kullanıcı state yönetimi (user object)
 * - Login işlemi (email + password)
 * - Logout işlemi  
 * - Token yönetimi (localStorage via api.js)
 * - Session kontrolü (sayfa yenilendiğinde)
 * - Hoş geldin modalı kontrolü
 * 
 * KULLANIM:
 * const { user, login, logout, loading } = useAuth()
 * 
 * NOT:
 * Bu context App.jsx içinde AuthProvider ile sarmalanır.
 * useAuth hook'u sadece AuthProvider içinde kullanılabilir.
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { createContext, useContext, useState, useEffect } from 'react'
import { App } from 'antd'
import api, { setAuthTokens, clearAuthTokens, getAuthAccessToken, getAuthRefreshToken } from '../services/api'

/**
 * AUTHENTICATION CONTEXT
 * 
 * Context içeriği:
 * - user: Kullanıcı bilgileri (null ise giriş yapılmamış)
 * - loading: İlk yüklemede true (token kontrolü yapılırken)
 * - login: Login fonksiyonu
 * - logout: Logout fonksiyonu
 * - checkAuth: Session kontrolü fonksiyonu
 * - showWelcomeModal: Hoş geldin modalı gösterilmeli mi?
 * - setShowWelcomeModal: Modal durumunu değiştir
 */
const AuthContext = createContext()

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * useAuth HOOK
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * AuthContext'e erişim sağlar.
 * 
 * KULLANIM:
 * const { user, login, logout } = useAuth()
 * 
 * HATA:
 * AuthProvider dışında kullanılırsa hata fırlatır.
 * 
 * @returns {Object} Auth context değerleri
 * @throws {Error} Provider dışında kullanıldığında
 */
export const useAuth = () => {
    const context = useContext(AuthContext)
    if (!context) {
        throw new Error('useAuth must be used within AuthProvider')
    }
    return context
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * AUTH PROVIDER COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Authentication state'i manage eder ve tüm child component'lere sağlar.
 * 
 * STATE:
 * - user: Kullanıcı bilgileri (id, email, name, role) veya null
 * - loading: Session kontrolü yapılırken true
 * - showWelcomeModal: Hoş geldin modalı gösterim flag'i (her oturum açıldığında)
 * 
 * LIFECYCLE:
 * 1. Component mount olduğunda checkAuth() çağrılır
 * 2. localStorage'da token varsa, /auth/me endpoint'ine istek atılır
 * 3. Başarılıysa user set edilir, başarısızsa token'lar temizlenir
 * 4. loading false yapılır
 * 
 * @param {ReactNode} children - Sarmalanacak component'ler
 */
export const AuthProvider = ({ children }) => {
    const { message } = App.useApp() // Ant Design message API (toast notifications)
    const [user, setUser] = useState(null) // Kullanıcı bilgileri
    const [loading, setLoading] = useState(true) // İlk yükleme durumu
    const [showWelcomeModal, setShowWelcomeModal] = useState(false) // Hoş geldin modalı

    /**
     * COMPONENT MOUNT - İlk session kontrolü
     * 
     * useEffect boş dependency array ile sadece 1 kere çalışır (component mount).
     */
    useEffect(() => {
        checkAuth()
    }, [])

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * SESSION KONTROLÜ
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Sayfa yenilendiğinde veya ilk açılışta mevcut session'ı kontrol et.
     * 
     * İŞLEYİŞ:
     * 1. localStorage'dan access token al
     * 2. Token varsa, /auth/me endpoint'ine GET isteği at
     * 3. Başarılı response -> user state'i set et
     * 4. Hata (401, 403, vb.) -> token'ları temizle, user null
     * 5. Hoş geldin modalını göster (her oturum açıldığında)
     * 6. loading'i false yap
     * 
     * NOT:
     * - Token yoksa direkt loading false yapılır
     * - api.js içindeki interceptor'lar otomatik token refresh yapar
     */
    const checkAuth = async () => {
        const token = getAuthAccessToken()
        if (token) {
            try {
                const response = await api.get('/auth/me')
                setUser(response.data)

                // Hoş geldin modalını sadece ilk girişte göster
                // 'hasSeenWelcome' flag'i yoksa modal göster ve flag'i set et
                const hasSeenWelcome = localStorage.getItem('hasSeenWelcome')
                if (!hasSeenWelcome) {
                    setShowWelcomeModal(true)
                    localStorage.setItem('hasSeenWelcome', 'true')
                }
            } catch (error) {
                // Token geçersiz veya expired -> temizle
                clearAuthTokens()
                setUser(null)
                localStorage.removeItem('hasSeenWelcome') // Hata durumunda flag'i temizle
            }
        }
        setLoading(false)
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * LOGIN FONKSİYONU
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Kullanıcı email ve password ile giriş yapar.
     * 
     * İŞLEYİŞ:
     * 1. Önce /auth/login-v2 endpoint'i dene (refresh token desteği)
     * 2. Başarılıysa:
     *    - accessToken ve refreshToken'ları localStorage'a kaydet
     *    - user state'i set et
     *    - Success message göster
     *    - Hoş geldin modalını göster (her oturum açıldığında)
     *    - return true
     * 3. v2 endpoint 404 dönerse (eski backend):
     *    - /auth/login (legacy) endpoint'i dene
     *    - Sadece accessToken kaydedilir (refreshToken yok)
     *    - return true
     * 4. Hata durumunda:
     *    - Error message göster
     *    - return false
     * 
     * @param {string} email - Kullanıcı email
     * @param {string} password - Kullanıcı şifresi
     * @returns {Promise<boolean>} Başarılı ise true, hata ise false
     */
    const login = async (email, password) => {
        try {
            // Önce refresh token destekli login dene
            try {
                const response = await api.post('/auth/login-v2', { email, password })
                const { accessToken, refreshToken, user: userData } = response.data

                setAuthTokens(accessToken, refreshToken)
                setUser(userData)
                message.success('Giriş başarılı!')


                return true
            } catch (v2Error) {
                // v2 endpoint yoksa legacy'ye düş
                if (v2Error.response?.status === 404) {
                    const response = await api.post('/auth/login', { email, password })
                    const { token, user: userData } = response.data

                    setAuthTokens(token, null) // Legacy: refreshToken yok
                    setUser(userData)
                    message.success('Giriş başarılı!')


                    return true
                }
                throw v2Error
            }
        } catch (error) {
            // Hata mesajını göster (backend'den gelen veya default)
            message.error(error.response?.data?.message || 'Giriş başarısız')
            return false
        }
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * LOGOUT FONKSİYONU
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Kullanıcı çıkış yapar, session sonlandırılır.
     * 
     * İŞLEYİŞ:
     * 1. refreshToken varsa, backend'e /auth/logout POST isteği at
     *    - Backend refresh token'ı revoke eder (DB'den siler/pasif yapar)
     *    - Hata olsa bile devam et (finally bloğu her zaman çalışır)
     * 2. localStorage'daki tüm token'ları temizle
     * 3. user state'i null yap
     * 4. "Çıkış yapıldı" info message göster
     * 
     * NOT:
     * - finally bloğu sayesinde backend hatası olsa bile logout tamamlanır
     * - api.js interceptor logout sonrası /login'e yönlendirecektir
     */
    const logout = async () => {
        try {
            const refreshToken = getAuthRefreshToken()
            if (refreshToken) {
                // Sunucuda refresh token'ı iptal et
                await api.post('/auth/logout', { refreshToken }).catch(() => {
                    // Logout API hatası olsa bile devam et
                })
            }
        } finally {
            // Her durumda token'ları temizle ve çıkış yap
            clearAuthTokens()
            setUser(null)
            message.info('Çıkış yapıldı')
        }
    }

    /**
     * CONTEXT VALUE
     * 
     * useAuth hook'u ile erişilebilecek değerler:
     * - user: Kullanıcı bilgileri veya null
     * - loading: İlk yükleme durumu
     * - login: Login fonksiyonu
     * - logout: Logout fonksiyonu
     * - checkAuth: Session kontrolü (manuel refresh için)
     * - showWelcomeModal: Modal gösterilmeli mi?
     * - setShowWelcomeModal: Modal durumunu değiştir
     */
    const value = {
        user,
        loading,
        login,
        logout,
        checkAuth,
        showWelcomeModal,
        setShowWelcomeModal
    }

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
