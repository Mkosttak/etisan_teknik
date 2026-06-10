/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: main.jsx
 * KONUM: frontend/src/
 * TİP: Frontend - Application Entry Point
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * React uygulamasının başlangıç noktası (entry point).
 * DOM'a React uygulamasını mount eder.
 * 
 * SORUMLULUKLAR:
 * - React root oluşturma
 * - App component'ini render etme
 * - StrictMode wrapper (development için)
 * - Global CSS import
 * 
 * NOT:
 * Bu dosya Vite tarafından otomatik olarak çalıştırılır (index.html'den).
 * Herhangi bir değişiklik yapılmamalı, App.jsx'ten devam edilmeli.
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css' // Global CSS dosyası

/**
 * REACT UYGULAMASINI MOUNT ET
 * 
 * İşleyiş:
 * 1. DOM'dan #root element'i al (public/index.html)
 * 2. React root oluştur
 * 3. App component'ini StrictMode ile sarmalayarak render et
 * 
 * React.StrictMode:
 * - Development mode'da ek kontroller yapar
 * - Deprecated API kullanımlarını uyarır
 * - Production build'de devre dışı kalır
 */
ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <App />
    </React.StrictMode>,
)
