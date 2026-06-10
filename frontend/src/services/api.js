/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: api.js
 * KONUM: frontend/src/services/
 * TİP: Frontend - API Service Layer
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Backend API ile iletişim için merkezi Axios instance ve yapılandırması.
 * Tüm HTTP istekleri bu dosya üzerinden yapılır.
 * 
 * SORUMLULUKLAR:
 * - Axios instance oluşturma ve konfigürasyon
 * - JWT token yönetimi (access + refresh token)
 * - Otomatik token ekleme (request interceptor)
 * - Otomatik token yenileme (response interceptor)
 * - 401 hatalarında session yönetimi
 * - Token storage (localStorage)
 * 
 * KULLANIM:
 * import api from '@/services/api'
 * const response = await api.get('/endpoint')
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import axios from 'axios'

/**
 * ANA AXIOS INSTANCE
 * 
 * Backend API'sine yapılacak tüm istekler için kullanılan merkezi instance.
 * 
 * Konfigürasyon:
 * - baseURL: Environment variable'dan alınır (.env.production -> VITE_API_BASE_URL=/api)
 * - headers: JSON content type default
 * - interceptors: Request ve response interceptor'ları aşağıda tanımlı
 */
const api = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
    headers: {
        'Content-Type': 'application/json',
    },
})

/**
 * TOKEN YÖNETİM FONKSİYONLARI
 * 
 * localStorage üzerinden JWT token yönetimi.
 * 
 * NOT: İki tip token saklanır:
 * - accessToken: Kısa ömürlü (30 dk), her request'te kullanılır
 * - refreshToken: Uzun ömürlü (7 gün), accessToken yenilemek için kullanılır
 */

/**
 * Access token'ı localStorage'dan al
 * 
 * Geriye uyumluluk için 'token' key'ini de kontrol eder (legacy sistem).
 * 
 * @returns {string|null} Access token veya null
 */
const getAccessToken = () => localStorage.getItem('accessToken') || localStorage.getItem('token')

/**
 * Refresh token'ı localStorage'dan al
 * 
 * @returns {string|null} Refresh token veya null
 */
const getRefreshToken = () => localStorage.getItem('refreshToken')

/**
 * Token'ları localStorage'a kaydet
 * 
 * Her iki token'ı da kaydeder. Geriye uyumluluk için 'token' key'i de set edilir.
 * 
 * @param {string} accessToken - JWT access token
 * @param {string} refreshToken - JWT refresh token (opsiyonel)
 */
const setTokens = (accessToken, refreshToken) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('token', accessToken) // Geriye uyumluluk
    if (refreshToken) {
        localStorage.setItem('refreshToken', refreshToken)
    }
}

/**
 * Tüm token'ları localStorage'dan temizle
 * 
 * Logout işlemi sırasında veya token refresh başarısız olduğunda çağrılır.
 */
const clearTokens = () => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('token')
}

/**
 * TOKEN REFRESH DURUM YÖNETİMİ
 * 
 * Birden fazla request aynı anda 401 alırsa, sadece 1 kere refresh yapılması
 * için kullanılan global state değişkenleri.
 * 
 * İşleyiş:
 * 1. İlk 401 alan request -> isRefreshing = true, refresh işlemi başlar
 * 2. Diğer 401 alan request'ler -> failedQueue'ya eklenir, bekler
 * 3. Refresh başarılı olunca -> Queue'daki tüm request'ler yeni token ile retry edilir
 * 4. Refresh başarısız olunca -> Tüm queue reject edilir, logout yapılır
 */
let isRefreshing = false // Token yenileme işlemi devam ediyor mu?
let failedQueue = [] // 401 alan ve bekleyen request'lerin kuyruğu

/**
 * Bekleyen request kuyruğunu işle
 * 
 * Token refresh başarılı/başarısız olduktan sonra, bekleyen tüm request'leri
 * resolve veya reject eder.
 * 
 * @param {Error|null} error - Hata varsa (refresh failed), null ise başarılı
 * @param {string|null} token - Yeni access token (başarılıysa)
 */
const processQueue = (error, token = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error)
        } else {
            prom.resolve(token)
        }
    })
    failedQueue = []
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * REQUEST INTERCEPTOR
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * Her HTTP request öncesinde çalışır.
 * 
 * AMAÇ:
 * - Access token'ı Authorization header'ına eklemek
 * 
 * KULLANIM:
 * Otomatik çalışır, manuel kullanım gerekmez.
 */
api.interceptors.request.use(
    (config) => {
        const token = getAccessToken()
        if (token) {
            config.headers.Authorization = `Bearer ${token}` // JWT token eklenir
        }
        return config
    },
    (error) => {
        return Promise.reject(error)
    }
)

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * RESPONSE INTERCEPTOR
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * Her HTTP response sonrasında çalışır.
 * 
 * AMAÇ:
 * - 401 Unauthorized hatalarını yakalamak
 * - Token süresi dolmuşsa, refresh token ile yeni access token almak
 * - Başarısız request'i yeni token ile yeniden denemek
 * - Refresh başarısız olursa, kullanıcıyı logout edip login sayfasına yönlendirmek
 * 
 * İŞLEYİŞ:
 * 1. Response 401 mi? Hayır -> Normal akış devam
 * 2. Response 401 -> Refresh endpoint'i mi? Evet -> Direkt logout
 * 3. Başka bir refresh devam ediyor mu? Evet -> Kuyruğa ekle, bekle
 * 4. Refresh token var mı? Hayır -> Direkt logout
 * 5. Refresh token ile yeni access token al
 * 6. Başarılı -> Orijinal request'i yeni token ile retry et
 * 7. Başarısız -> Logout, login sayfasına yönlendir
 */
api.interceptors.response.use(
    (response) => response, // Başarılı response'ları olduğu gibi geçir
    async (error) => {
        const originalRequest = error.config

        // 401 hatası ve refresh token mevcutsa
        if (error.response?.status === 401 && !originalRequest._retry) {
            // Refresh token endpoint'ine gelen 401 ise direkt logout
            // (refresh token da geçersiz demektir, yenileme yapılamaz)
            if (originalRequest.url?.includes('/auth/refresh-token') ||
                originalRequest.url?.includes('/auth/login') ||
                originalRequest.url?.includes('/auth/login-v2')) {
                clearTokens()
                // Eğer login sayfasında değilsek login'e yönlendir
                if (window.location.pathname !== '/login') {
                    window.location.href = '/login'
                }
                return Promise.reject(error)
            }

            // Başka bir request zaten refresh yapıyorsa, kuyruğa ekle
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject })
                }).then(token => {
                    originalRequest.headers.Authorization = `Bearer ${token}`
                    return api(originalRequest) // Yeni token ile retry
                }).catch(err => {
                    return Promise.reject(err)
                })
            }

            // İlk 401 alan request, refresh işlemini başlat
            originalRequest._retry = true // Infinite loop önleme flag'i
            isRefreshing = true

            const refreshToken = getRefreshToken()

            if (refreshToken) {
                try {
                    // Refresh token ile yeni access token al
                    // NOT: api instance kullanılmaz (interceptor loop'u önlemek için)
                    const response = await axios.post(
                        `${api.defaults.baseURL}/auth/refresh-token`,
                        { refreshToken }
                    )

                    const { accessToken, refreshToken: newRefreshToken } = response.data
                    setTokens(accessToken, newRefreshToken) // Yeni token'ları kaydet

                    processQueue(null, accessToken) // Bekleyen request'leri resolve et

                    // Orijinal request'i yeni token ile retry et
                    originalRequest.headers.Authorization = `Bearer ${accessToken}`
                    return api(originalRequest)
                } catch (refreshError) {
                    // Refresh başarısız -> Tüm kuyruk reject, logout
                    processQueue(refreshError, null)
                    clearTokens()
                    window.location.href = '/login'
                    return Promise.reject(refreshError)
                } finally {
                    isRefreshing = false
                }
            } else {
                // Refresh token yoksa temizle
                clearTokens()
                // Eğer login sayfasında değilsek login'e yönlendir
                if (window.location.pathname !== '/login') {
                    window.location.href = '/login'
                }
            }
        }

        return Promise.reject(error)
    }
)

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * EXPORT EDİLEN FONKSİYONLAR
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * Bu fonksiyonlar AuthContext tarafından kullanılır:
 * - setAuthTokens: Login sonrası token'ları set etmek için
 * - clearAuthTokens: Logout sırasında token'ları temizlemek için
 * - getAuthAccessToken: Mevcut access token'ı kontrol etmek için
 * - getAuthRefreshToken: Mevcut refresh token'ı kontrol etmek için
 */
export const setAuthTokens = setTokens
export const clearAuthTokens = clearTokens
export const getAuthAccessToken = getAccessToken
export const getAuthRefreshToken = getRefreshToken

/**
 * DEFAULT EXPORT: ANA AXIOS INSTANCE
 * 
 * Projede kullanım:
 * import api from '@/services/api'
 * 
 * await api.get('/users')
 * await api.post('/auth/login', { email, password })
 * await api.put('/users/1', userData)
 * await api.delete('/users/1')
 */
export default api
