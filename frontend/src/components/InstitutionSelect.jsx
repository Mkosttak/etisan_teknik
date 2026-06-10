/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: InstitutionSelect.jsx
 * KONUM: frontend/src/components/
 * TİP: Frontend - React Component (Form Field)
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Kurum seçimi için özelleştirilmiş Ant Design Select component'i.
 * Backend'den kurum listesini çeker, arama yapılmasını sağlar.
 * 
 * KULLANIM ALANLARI:
 * - Arıza formu (kurum seçimi)
 * - Talep formu (kurum seçimi)
 * - Servis formu (kurum seçimi)
 * 
 * ÖZELLİKLER:
 * - Server-side search (debounce ile)
 * - "DİĞER" opsiyonu (includeOther=true ise)
 * - Mevcut value'yu options'a otomatik ekler
 * - Loading state
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { useEffect, useMemo, useState } from 'react'
import { Select, Spin } from 'antd'
import api from '../services/api'

// Sabitler
const OTHER_INSTITUTION = 'DİĞER' // "Diğer" seçeneği
const DEFAULT_LIMIT = 200 // API'den max kaç kurum çekilecek
const SEARCH_DEBOUNCE_MS = 300 // Arama debounce süresi (ms)

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * INSTITUTION SELECT COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * PROPS:
 * @param {string} value - Seçili kurum adı
 * @param {function} onChange - Değer değiştiğinde çağrılacak callback
 * @param {string} placeholder - Placeholder metni (default: "Kurum seçin")
 * @param {boolean} allowClear - Temizleme butonu göster mi? (default: true)
 * @param {boolean} includeOther - "DİĞER" seçeneğini ekle mi? (default: true)
 * @param {boolean} disabled - Disable durumu (default: false)
 * @param {...rest} - Diğer Ant Design Select props
 * 
 * İŞLEYİŞ:
 * 1. Component mount olduğunda backend'den kurumları çeker
 * 2. Kullanıcı arama yaptığında (onSearch) 300ms debounce ile tekrar çeker
 * 3. Mevcut value options'ta yoksa otomatik ekler
 * 4. includeOther=true ise "DİĞER" seçeneğini ekler
 * 
 * API ENDPOINT: GET /institutions/names
 * Query params: search, includeOther, limit
 */
const InstitutionSelect = ({
  value,
  onChange,
  placeholder = 'Kurum seçin',
  allowClear = true,
  includeOther = true,
  disabled = false,
  ...rest
}) => {
  // State
  const [options, setOptions] = useState([]) // Backend'den gelen kurum listesi
  const [fetching, setFetching] = useState(false) // Loading durumu
  const [search, setSearch] = useState('') // Arama terimi

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * BACKEND'DEN KURUM LİSTESİNİ ÇEK
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * AMAÇ:
   * search veya includeOther değiştiğinde backend'den kurumları çek.
   * 
   * İŞLEYİŞ:
   * 1. 300ms debounce (kullanıcı yazmayı bitirene kadar bekle)
   * 2. API'ye GET /institutions/names isteği at
   * 3. Response'u options state'ine kaydet
   * 4. Hata durumunda boş array set et
   * 
   * DEBOUNCE:
   * Kullanıcı her tuşa bastığında API isteği atmak yerine,
   * 300ms duraksadığında istek atılır (performans optimizasyonu).
   * 
   * CLEANUP:
   * Component unmount olduğunda veya search değiştiğinde:
   * - cancelled flag'i true yapılır (race condition önleme)
   * - setTimeout temizlenir
   */
  useEffect(() => {
    let cancelled = false // Race condition önleme flag'i

    const handle = setTimeout(async () => {
      try {
        setFetching(true)
        const response = await api.get('/institutions/names', {
          params: {
            search: search || undefined, // Boş string yerine undefined
            includeOther,
            limit: DEFAULT_LIMIT
          }
        })
        if (!cancelled) {
          setOptions(Array.isArray(response.data) ? response.data : [])
        }
      } catch (error) {
        if (!cancelled) {
          setOptions([])
        }
      } finally {
        if (!cancelled) {
          setFetching(false)
        }
      }
    }, SEARCH_DEBOUNCE_MS)

    return () => {
      cancelled = true
      clearTimeout(handle)
    }
  }, [search, includeOther])

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * OPTIONS LİSTESİNİ HAZIRLA
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * AMAÇ:
   * Backend'den gelen options'a ek olarak:
   * 1. Mevcut value'yu ekle (options'ta yoksa)
   * 2. "DİĞER" seçeneğini ekle (includeOther=true ise)
   * 
   * İŞLEYİŞ:
   * 1. options array'ini Set'e çevir (unique değerler)
   * 2. Mevcut value varsa ve options'ta yoksa ekle
   * 3. includeOther=true ise ve "DİĞER" yoksa ekle
   * 4. Set'i array'e çevir ve { value, label } formatına map et
   * 
   * MEMO:
   * options, value veya includeOther değişmedikçe yeniden hesaplama.
   */
  const mergedOptions = useMemo(() => {
    const unique = new Set(options)

    // Mevcut value'yu ekle (yoksa)
    if (value && !unique.has(value)) {
      unique.add(value)
    }

    // "DİĞER" seçeneğini ekle
    if (includeOther && !unique.has(OTHER_INSTITUTION)) {
      unique.add(OTHER_INSTITUTION)
    }

    // Ant Design Select formatına çevir
    return Array.from(unique).map((name) => ({
      value: name,
      label: name
    }))
  }, [options, value, includeOther])

  /**
   * SELECT COMPONENT RENDER
   * 
   * PROPS:
   * - value/onChange: Controlled component
   * - showSearch: Arama özelliği aktif
   * - filterOption=false: Server-side filtering (client-side değil)
   * - onSearch: Kullanıcı arama yaptığında search state'i güncellenir
   * - notFoundContent: Loading sırasında Spin, data yoksa null
   * - options: mergedOptions (backend + value + "DİĞER")
   */
  return (
    <Select
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      allowClear={allowClear}
      showSearch
      filterOption={false} // Server-side filtering
      virtual={false} // Fix for jumpy scroll behavior
      onSearch={setSearch}
      notFoundContent={fetching ? <Spin size="small" /> : null}
      options={mergedOptions}
      disabled={disabled}
      {...rest}
    />
  )
}

export default InstitutionSelect
