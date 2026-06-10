/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: SignaturePad.jsx
 * KONUM: frontend/src/components/
 * TİP: Frontend - React Component (Canvas-based)
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Kullanıcının imza atması için canvas-based signature pad component'i.
 * 
 * KULLANIM ALANLARI:
 * - Servis formu (müşteri imzası, teknik imzası)
 * 
 * ÖZELLİKLER:
 * - Mouse desteği (masaüstü)
 * - Touch desteği (mobil/tablet)
 * - Retina display desteği (2x canvas)
 * - Base64 PNG olarak kaydetme
 * - Temizleme (clear) butonu
 * - Disabled mode
 * 
 * TEKNİK DETAYLAR:
 * - Canvas boyutu: 2x (retina için)
 * - İmza rengi: HER ZAMAN SİYAH (#000)
 * - Arkaplan rengi: HER ZAMAN BEYAZ (#fff) - PDF için
 * - Output format: Base64 PNG
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { useRef, useEffect, useState } from 'react'
import { Button } from 'antd'
import { ClearOutlined } from '@ant-design/icons'
import { useTheme } from '../context/ThemeContext'

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * SIGNATURE PAD COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * PROPS:
 * @param {string|null} value - Base64 PNG imza verisi
 * @param {function} onChange - İmza değiştiğinde çağrılacak callback (dataUrl)
 * @param {boolean} disabled - Disabled durumu (default: false)
 * 
 * STATE:
 * - isDrawing: Çizim devam ediyor mu?
 * - hasSignature: İmza var mı?
 * 
 * REFS:
 * - canvasRef: Canvas DOM elementi
 * - containerRef: Canvas container DOM elementi
 * - ctxRef: Canvas 2D context (çizim için)
 * 
 * CANVAS BOYUTLARI:
 * - Physical: Ekran genişliği x 150px
 * - Canvas internal: (Ekran genişliği x 2) x 300px (Retina display için)
 * - Scale factor: 2x
 */
const SignaturePad = ({ value, onChange, disabled = false }) => {
    const { isDarkMode } = useTheme()
    const canvasRef = useRef(null)
    const containerRef = useRef(null)
    const [isDrawing, setIsDrawing] = useState(false)
    const [hasSignature, setHasSignature] = useState(false)
    const ctxRef = useRef(null) // Canvas 2D context

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * CANVAS BAŞLATMA VE KONFİGÜRASYON
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Component mount olduğunda veya isDarkMode/value değiştiğinde canvas'ı hazırla.
     * 
     * İŞLEYİŞ:
     * 1. Container boyutlarını al
     * 2. Canvas boyutunu 2x olarak ayarla (Retina display için)
     * 3. 2D context'i al ve stil ayarlarını yap
     * 4. Arkaplanı beyaz yap (PDF ve görüntüleme için)
     * 5. Mevcut imza varsa canvas'a yükle
     * 
     * RENKLER:
     * - İmza rengi: HER ZAMAN SİYAH (#000)
     * - Arkaplan: HER ZAMAN BEYAZ (#fff)
     * NOT: Dark mode'da bile bu renkler sabit kalır çünkü
     *      imza PDF'e gidecek ve beyaz kağıt üzerinde görünmeli.
     * 
     * CANVAS SCALE:
     * Canvas 2x boyutunda ama CSS ile 1x gösteriliyor (Retina için).
     * ctx.scale KULLANILMAZ - double scaling oluşturur.
     */
    useEffect(() => {
        const canvas = canvasRef.current
        if (!canvas) return

        const container = containerRef.current
        const rect = container.getBoundingClientRect()

        // Canvas boyutunu ayarla (2x for retina)
        canvas.width = rect.width * 2
        canvas.height = 300 // 150px * 2 for retina

        const ctx = canvas.getContext('2d')
        // ctx.scale KULLANMA - double scaling oluşturur

        // Stil ayarları
        // İmza rengi: HER ZAMAN SİYAH (Arkaplan beyaz olduğu için)
        ctx.strokeStyle = '#000'
        ctx.lineWidth = 4 // Canvas 2x olduğu için 2 * 2
        ctx.lineCap = 'round'
        ctx.lineJoin = 'round'

        // Arkaplan - HER ZAMAN BEYAZ (PDF ve görüntüleme için)
        ctx.fillStyle = '#fff'
        ctx.fillRect(0, 0, rect.width * 2, 300)

        ctxRef.current = ctx

        // Mevcut imza varsa yükle
        if (value) {
            const img = new Image()
            img.onload = () => {
                ctx.drawImage(img, 0, 0, rect.width * 2, 300)
                setHasSignature(true)
            }
            img.src = value
        }
    }, [isDarkMode, value])

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * TOUCH EVENT HANDLER (MOBİL/TABLET)
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Mobil cihazlarda parmak ile imza atma desteği.
     * 
     * ÖNEMLİ:
     * Native event listener kullanılır (passive: false ile).
     * Bunun sebebi: e.preventDefault() çağrılabilmesi için.
     * React synthetic event'lerde passive: false yapılamıyor.
     * 
     * İŞLEYİŞ:
     * 1. touchstart: Çizim başlar, beginPath + moveTo
     * 2. touchmove: Çizim devam eder, lineTo + stroke
     * 3. touchend/touchcancel: Çizim biter, canvas'ı base64 PNG olarak kaydet
     * 
     * KOORDİNAT HESAPLAMA:
     * - Touch koordinatları canvas koordinatlarına çevrilir
     * - 2x scale factor uygulanır (canvas 2x olduğu için)
     * 
     * CLEANUP:
     * Component unmount veya disabled değiştiğinde event listener'lar temizlenir.
     */
    useEffect(() => {
        const canvas = canvasRef.current
        if (!canvas || disabled) return

        /**
         * Touch/mouse koordinatlarını canvas koordinatlarına çevir
         * 
         * @param {TouchEvent|MouseEvent} e - Event object
         * @returns {Object} { x, y } Canvas koordinatları (2x scaled)
         */
        const getCoordinates = (e) => {
            const rect = canvas.getBoundingClientRect()

            if (e.touches && e.touches.length > 0) {
                // Touch event
                return {
                    x: (e.touches[0].clientX - rect.left) * 2,
                    y: (e.touches[0].clientY - rect.top) * 2
                }
            }

            // Mouse event
            return {
                x: (e.clientX - rect.left) * 2,
                y: (e.clientY - rect.top) * 2
            }
        }

        let drawing = false

        const handleTouchStart = (e) => {
            e.preventDefault() // Scroll ve zoom'u engelle
            const ctx = ctxRef.current
            if (!ctx) return

            const { x, y } = getCoordinates(e)
            ctx.beginPath()
            ctx.moveTo(x, y)
            drawing = true
            setIsDrawing(true)
            setHasSignature(true)
        }

        const handleTouchMove = (e) => {
            if (!drawing) return
            e.preventDefault() // Scroll'u engelle

            const ctx = ctxRef.current
            if (!ctx) return

            const { x, y } = getCoordinates(e)
            ctx.lineTo(x, y)
            ctx.stroke()
        }

        const handleTouchEnd = (e) => {
            if (!drawing) return
            e.preventDefault()

            drawing = false
            setIsDrawing(false)

            // Canvas'ı base64 olarak kaydet
            const dataUrl = canvas.toDataURL('image/png')
            onChange?.(dataUrl)
        }

        // Native event listener'ları ekle (passive: false)
        canvas.addEventListener('touchstart', handleTouchStart, { passive: false })
        canvas.addEventListener('touchmove', handleTouchMove, { passive: false })
        canvas.addEventListener('touchend', handleTouchEnd, { passive: false })
        canvas.addEventListener('touchcancel', handleTouchEnd, { passive: false })

        return () => {
            canvas.removeEventListener('touchstart', handleTouchStart)
            canvas.removeEventListener('touchmove', handleTouchMove)
            canvas.removeEventListener('touchend', handleTouchEnd)
            canvas.removeEventListener('touchcancel', handleTouchEnd)
        }
    }, [disabled, onChange])

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * VALUE DEĞİŞTİĞİNDE CANVAS'I GÜNCELLE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Parent component'ten value prop'u değiştiğinde canvas'ı güncelle.
     * 
     * İŞLEYİŞ:
     * 1. value varsa Image oluştur
     * 2. Image yüklendiğinde:
     *    - Arkaplanı beyaz yap
     *    - Image'i canvas'a çiz
     *    - hasSignature = true
     */
    useEffect(() => {
        if (value && canvasRef.current && ctxRef.current) {
            const canvas = canvasRef.current
            const ctx = ctxRef.current
            const container = containerRef.current
            const rect = container.getBoundingClientRect()

            const img = new Image()
            img.onload = () => {
                // Arkaplan - HER ZAMAN BEYAZ
                ctx.fillStyle = '#fff'
                ctx.fillRect(0, 0, rect.width * 2, 300)
                ctx.drawImage(img, 0, 0, rect.width * 2, 300)
                setHasSignature(true)
            }
            img.src = value
        }
    }, [value, isDarkMode])

    /**
     * Mouse koordinatlarını canvas koordinatlarına çevir
     * 
     * @param {MouseEvent} e - Mouse event
     * @returns {Object} { x, y } Canvas koordinatları (2x scaled)
     */
    const getMouseCoordinates = (e) => {
        const canvas = canvasRef.current
        const rect = canvas.getBoundingClientRect()

        // Canvas 2x olduğu için koordinatları 2 ile çarp
        return {
            x: (e.clientX - rect.left) * 2,
            y: (e.clientY - rect.top) * 2
        }
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * MOUSE EVENT HANDLERS (MASAÜSTÜ)
     * ═══════════════════════════════════════════════════════════════════════
     */

    /**
     * Çizim başlat (mouseDown)
     */
    const startDrawing = (e) => {
        if (disabled) return

        const ctx = ctxRef.current
        if (!ctx) return

        const { x, y } = getMouseCoordinates(e)
        ctx.beginPath()
        ctx.moveTo(x, y)
        setIsDrawing(true)
        setHasSignature(true)
    }

    /**
     * Çizim devam ettir (mouseMove)
     */
    const draw = (e) => {
        if (!isDrawing || disabled) return

        const ctx = ctxRef.current
        if (!ctx) return

        const { x, y } = getMouseCoordinates(e)
        ctx.lineTo(x, y)
        ctx.stroke()
    }

    /**
     * Çizim bitir (mouseUp / mouseLeave)
     */
    const stopDrawing = () => {
        if (!isDrawing) return

        setIsDrawing(false)

        // Canvas'ı base64 olarak kaydet
        const canvas = canvasRef.current
        const dataUrl = canvas.toDataURL('image/png')
        onChange?.(dataUrl)
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * İMZAYI TEMİZLE
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * AMAÇ:
     * Canvas'ı temizle, state'i sıfırla.
     * 
     * İŞLEYİŞ:
     * 1. Arkaplanı beyaz yap (fillRect)
     * 2. hasSignature = false
     * 3. onChange(null) çağır (parent'a bildir)
     */
    const clearSignature = () => {
        const ctx = ctxRef.current
        const container = containerRef.current
        if (!ctx || !container) return

        const rect = container.getBoundingClientRect()
        // Arkaplan - HER ZAMAN BEYAZ
        ctx.fillStyle = '#fff'
        ctx.fillRect(0, 0, rect.width * 2, 300)
        setHasSignature(false)
        onChange?.(null)
    }

    /**
     * ═══════════════════════════════════════════════════════════════════════
     * RENDER
     * ═══════════════════════════════════════════════════════════════════════
     * 
     * YAPISI:
     * - Container div (ref, border, background)
     * - Canvas (mouse/touch event'ler)
     * - Placeholder text (imza yoksa)
     * - Temizle butonu (disabled değilse)
     * 
     * STİLLER:
     * - Border: Dashed (dark mode'a göre renk)
     * - Background: HER ZAMAN BEYAZ (#fff)
     * - Cursor: crosshair (disabled değilse)
     */
    return (
        <div>
            <div
                ref={containerRef}
                style={{
                    border: `2px dashed ${isDarkMode ? 'rgba(255, 255, 255, 0.2)' : '#d9d9d9'}`,
                    borderRadius: 8,
                    overflow: 'hidden',
                    backgroundColor: '#fff', // Her zaman beyaz - PDF ve görüntüleme için
                    position: 'relative'
                }}
            >
                <canvas
                    ref={canvasRef}
                    style={{
                        width: '100%',
                        height: 150,
                        touchAction: 'none', // Touch gesture'ları engelle (scroll, zoom vb.)
                        cursor: disabled ? 'not-allowed' : 'crosshair'
                    }}
                    onMouseDown={startDrawing}
                    onMouseMove={draw}
                    onMouseUp={stopDrawing}
                    onMouseLeave={stopDrawing}
                />

                {/* Placeholder text */}
                {!hasSignature && !disabled && (
                    <div
                        style={{
                            position: 'absolute',
                            top: '50%',
                            left: '50%',
                            transform: 'translate(-50%, -50%)',
                            color: isDarkMode ? 'rgba(255, 255, 255, 0.4)' : '#bfbfbf',
                            pointerEvents: 'none',
                            fontSize: 14
                        }}
                    >
                        İmza için buraya çizin
                    </div>
                )}
            </div>

            {/* Temizle butonu */}
            {!disabled && (
                <div style={{ marginTop: 8 }}>
                    <Button
                        size="small"
                        icon={<ClearOutlined />}
                        onClick={clearSignature}
                        disabled={!hasSignature}
                    >
                        Temizle
                    </Button>
                </div>
            )}
        </div>
    )
}

export default SignaturePad
