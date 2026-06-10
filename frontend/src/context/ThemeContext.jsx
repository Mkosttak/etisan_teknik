/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: ThemeContext.jsx
 * KONUM: frontend/src/context/
 * TİP: Frontend - React Context Provider
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulama genelinde tema yönetimi (dark/light mode).
 * 
 * SORUMLULUKLAR:
 * - Dark mode / Light mode state yönetimi
 * - Tema değiştirme (toggle)
 * - Ant Design tema konfigürasyonu
 * - localStorage'da tema tercihi saklama
 * - Body CSS class yönetimi
 * 
 * KULLANIM:
 * const { isDarkMode, toggleTheme } = useTheme()
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { createContext, useContext, useState, useEffect } from 'react'
import { ConfigProvider, theme } from 'antd'

/**
 * THEME CONTEXT
 * 
 * Context içeriği:
 * - isDarkMode: Boolean, dark mode aktif mi?
 * - toggleTheme: Function, temayı değiştir
 */
const ThemeContext = createContext()

/**
 * useTheme HOOK
 * 
 * ThemeContext'e erişim sağlar.
 * 
 * @returns {Object} Theme context değerleri
 * @throws {Error} Provider dışında kullanıldığında
 */
export const useTheme = () => {
    const context = useContext(ThemeContext)
    if (!context) {
        throw new Error('useTheme must be used within ThemeProvider')
    }
    return context
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * THEME PROVIDER COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Tema state'i manage eder ve Ant Design ConfigProvider ile uygular.
 * 
 * İŞLEYİŞ:
 * 1. Component mount olduğunda localStorage'dan tema tercihi okunur
 * 2. isDarkMode state'i set edilir
 * 3. Her tema değişikliğinde localStorage güncellenir
 * 4. Body'ye dark-mode class'ı eklenir/çıkarılır
 * 5. Ant Design ConfigProvider ile tema uygulanır
 * 
 * @param {ReactNode} children - Sarmalanacak component'ler
 */
export const ThemeProvider = ({ children }) => {
    /**
     * DARK MODE STATE
     * 
     * İlk değer localStorage'dan okunur:
     * - 'darkMode' key'i varsa JSON.parse ile boolean'a çevrilir
     * - Yoksa default: false (light mode)
     */
    const [isDarkMode, setIsDarkMode] = useState(() => {
        const saved = localStorage.getItem('darkMode')
        return saved ? JSON.parse(saved) : false
    })

    /**
     * TEMA DEĞİŞİKLİĞİ SIDE EFFECT
     * 
     * isDarkMode her değiştiğinde:
     * 1. localStorage'a kaydet
     * 2. document.body'ye dark-mode class ekle/çıkar (CSS için)
     */
    useEffect(() => {
        localStorage.setItem('darkMode', JSON.stringify(isDarkMode))

        // Body class'ını güncelle
        if (isDarkMode) {
            document.body.classList.add('dark-mode')
        } else {
            document.body.classList.remove('dark-mode')
        }
    }, [isDarkMode])

    /**
     * TEMA DEĞİŞTİRME FONKSİYONU
     * 
     * Dark ↔ Light toggle yapar.
     */
    const toggleTheme = () => {
        setIsDarkMode(prev => !prev)
    }

    /**
     * ANT DESIGN TEMA KONFİGÜRASYONU
     * 
     * algorithm: Dark mode ise darkAlgorithm, değilse defaultAlgorithm
     * token: Ant Design design token'ları (renkler, border-radius, font)
     * 
     * Renkler:
     * - colorPrimary: Ana renk (mavi)
     * - colorSuccess: Başarı rengi (yeşil)
     * - colorWarning: Uyarı rengi (turuncu)
     * - colorError: Hata rengi (kırmızı)
     */
    const themeConfig = {
        algorithm: isDarkMode ? theme.darkAlgorithm : theme.defaultAlgorithm,
        token: {
            colorPrimary: '#1890ff',
            colorInfo: '#1890ff',
            colorSuccess: '#52c41a',
            colorWarning: '#faad14',
            colorError: '#ff4d4f',
            borderRadius: 8,
            fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
        },
    }

    /**
     * CONTEXT VALUE
     * 
     * useTheme hook'u ile erişilebilecek değerler.
     */
    const value = {
        isDarkMode,
        toggleTheme,
    }

    /**
     * PROVIDER RENDER
     * 
     * ThemeContext.Provider → Child component'lere isDarkMode ve toggleTheme sağlar
     * ConfigProvider → Ant Design component'lerine tema uygular
     */
    return (
        <ThemeContext.Provider value={value}>
            <ConfigProvider theme={themeConfig}>
                {children}
            </ConfigProvider>
        </ThemeContext.Provider>
    )
}
