/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: LoadingSpinner.jsx
 * KONUM: frontend/src/components/
 * TİP: Frontend - React Component (Utility)
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulamada loading (yükleniyor) durumlarında gösterilen spinner component'i.
 * 
 * KULLANIM ALANLARI:
 * - PrivateRoute: Authentication kontrolü yapılırken
 * - RoleProtectedRoute: Yetki kontrolü yapılırken
 * - Diğer loading state'leri
 * 
 * TASARIM:
 * - Etisan logo ortalanmış
 * - Dönen halka animasyonu (CSS)
 * - "Yükleniyor..." yazısı
 * 
 * NOT:
 * CSS dosyası: LoadingSpinner.css (aynı klasör)
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import React from 'react'
import './LoadingSpinner.css'

/**
 * LOADING SPINNER COMPONENT
 * 
 * Ekranın ortasında logo ve animasyonlu halka gösterir.
 * 
 * @returns {JSX.Element} Loading spinner UI
 */
const LoadingSpinner = () => {
    return (
        <div className="loading-container">
            <SpinnerContent size="large" />
            <div style={{ marginTop: 20, color: '#666', fontSize: 16, fontWeight: 500 }}>
                Yükleniyor...
            </div>
        </div>
    )
}

export const SpinnerContent = ({ size = 'large', style }) => {
    const sizeMap = {
        small: { wrapper: 24, logo: 12 },
        default: { wrapper: 40, logo: 20 },
        large: { wrapper: 120, logo: 60 }
    }

    const dimensions = sizeMap[size] || sizeMap.large

    return (
        <div className="spinner-wrapper" style={{ width: dimensions.wrapper, height: dimensions.wrapper, ...style }}>
            <img
                src="/etisan-proje-logo-kucuk.png"
                alt="Loading"
                className="logo-image"
                style={{ width: dimensions.logo }}
            />
            <div className="spinner-ring"></div>
        </div>
    )
}

export default LoadingSpinner

