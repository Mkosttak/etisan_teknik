import { useState, useEffect, useRef, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
    Steps, Button, Form, Input, Select, DatePicker, TimePicker, Upload, Card, Space,
    Divider, Row, Col, App, Modal, Descriptions, Typography, Spin, Checkbox, Radio,
    Tag, InputNumber, Alert
} from 'antd'
import {
    SaveOutlined, SendOutlined, ArrowLeftOutlined, ArrowRightOutlined,
    PlusOutlined, CameraOutlined, DeleteOutlined, CheckCircleOutlined,
    EditOutlined, SearchOutlined, WarningOutlined
} from '@ant-design/icons'
import { useAuth } from '../context/AuthContext'
import { useTheme } from '../context/ThemeContext'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import SignaturePad from '../components/SignaturePad'

dayjs.locale('tr')

const { TextArea } = Input
const { Title, Text } = Typography

const trLower = (value) => (value || '').toLocaleLowerCase('tr-TR')

// Telefon No Formatlama Fonksiyonu (05XX XXX XX XX)
const normalizePhoneNumber = (value) => {
    if (!value) return value
    const onlyNums = value.replace(/[^\d]/g, '')
    if (onlyNums.length <= 4) return onlyNums
    if (onlyNums.length <= 7) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4)}`
    if (onlyNums.length <= 9) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7)}`
    return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7, 9)} ${onlyNums.slice(9, 11)}`
}

// Tarih ve Saati birleştir
const combineDateAndTime = (date, time) => {
    if (!date) return null

    // Tarihi yerel saat diliminde al
    const d = dayjs(date)

    if (!time) {
        // Saat seçilmediyse, sadece tarihi döndür
        return d.format('YYYY-MM-DD')
    }

    // Saati al ve tarih ile birleştir
    const t = dayjs(time)
    const combined = d.hour(t.hour()).minute(t.minute()).second(0).millisecond(0)

    // Yerel saat diliminde ISO string olarak döndür (UTC dönüşümü yok)
    return combined.format('YYYY-MM-DDTHH:mm:ss')
}

// Kurum listesi
const ServiceFormWizard = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const { user } = useAuth()
    const { isDarkMode } = useTheme()
    const [form] = Form.useForm()
    const { message, modal, notification } = App.useApp()

    const [currentStep, setCurrentStep] = useState(0)
    const [loading, setLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [formId, setFormId] = useState(id ? parseInt(id) : null)
    const [formData, setFormData] = useState({})
    const [photos, setPhotos] = useState([])
    const [etisanSignature, setEtisanSignature] = useState(null)
    const [authoritySignature, setAuthoritySignature] = useState(null)

    // Stok yönetimi
    const [stockItems, setStockItems] = useState([])
    const [selectedStockItems, setSelectedStockItems] = useState({}) // { stockItemId: quantity }
    const [stockSearchText, setStockSearchText] = useState('')
    const [extraItemsList, setExtraItemsList] = useState([]) // [{ name: string, quantity: number }]
    const [extraItemQuantity, setExtraItemQuantity] = useState(1)

    // Değiştirilecek Parçalar
    const [selectedReplaceStockItems, setSelectedReplaceStockItems] = useState({}) // { stockItemId: quantity }
    const [replaceExtraItemsList, setReplaceExtraItemsList] = useState([]) // [{ name: string, quantity: number }]
    const [replaceSearchText, setReplaceSearchText] = useState('')
    const [replaceExtraQuantity, setReplaceExtraQuantity] = useState(1)

    const autoSaveTimerRef = useRef(null)
    const lastSavedRef = useRef(null)

    // Mevcut formu yükle
    useEffect(() => {
        if (id) {
            loadForm(parseInt(id))
        }
        fetchStockItems()
    }, [id])

    // Stok kalemleri yüklendikten sonra materialsUsed'i parse et
    useEffect(() => {
        if (formData.materialsUsed && stockItems.length > 0) {
            parseMaterialsUsed(formData.materialsUsed, stockItems)
        }
        if (formData.partsToReplace && stockItems.length > 0) {
            parsePartsToReplace(formData.partsToReplace, stockItems)
        }
    }, [stockItems.length, formData.materialsUsed, formData.partsToReplace])

    const fetchStockItems = async () => {
        try {
            const response = await api.get('/stock')
            const activeItems = response.data.filter(item => item.isActive)
            setStockItems(activeItems)
        } catch (error) {
            console.error('Stok kalemleri yüklenemedi')
        }
    }

    const parseMaterialsUsed = (materialsUsed, stockItemsList) => {
        if (!materialsUsed) {
            setSelectedStockItems({})
            setExtraItemsList([])
            return
        }

        // Format: "2x Ürün Adı, 1x Diğer Ürün"
        const materials = materialsUsed.split(', ').map(item => {
            const match = item.match(/^(\d+)x\s+(.+)$/)
            if (match) {
                let name = match[2].trim()
                // Name içinde gereksiz "adet" ve sayıları temizle
                // Örneğin "1 adet 12 adet Pil" -> "Pil"
                name = name.replace(/^\d+\s+adet\s+/gi, '').replace(/\s+\d+\s+adet\s+/gi, ' ').trim()
                return {
                    name: name,
                    quantity: parseInt(match[1])
                }
            }
            // Eğer format eşleşmezse, direkt name olarak kullan ama temizle
            let name = item.trim()
            name = name.replace(/^\d+\s+adet\s+/gi, '').replace(/\s+\d+\s+adet\s+/gi, ' ').trim()
            return {
                name: name,
                quantity: 1
            }
        })

        // Stok ürünlerini ve ekstra ürünleri ayır
        const stockItemsMap = {}
        const extraItems = []

        materials.forEach(material => {
            // Stok listesinde bu ürün var mı kontrol et
            const stockItem = stockItemsList.find(s => s.itemName === material.name)
            if (stockItem) {
                stockItemsMap[stockItem.id] = material.quantity
            } else {
                extraItems.push(material)
            }
        })

        setSelectedStockItems(stockItemsMap)
        setExtraItemsList(extraItems)
    }

    const parsePartsToReplace = (partsToReplace, stockItemsList) => {
        if (!partsToReplace) {
            setSelectedReplaceStockItems({})
            setReplaceExtraItemsList([])
            return
        }

        const materials = partsToReplace.split(', ').map(item => {
            const match = item.match(/^(\d+)x\s+(.+)$/)
            if (match) {
                return { name: match[2].trim(), quantity: parseInt(match[1]) }
            }
            return { name: item.trim(), quantity: 1 }
        })

        const stockItemsMap = {}
        const extraItems = []

        materials.forEach(material => {
            const stockItem = stockItemsList.find(s => s.itemName === material.name)
            if (stockItem) {
                stockItemsMap[stockItem.id] = material.quantity
            } else {
                extraItems.push(material)
            }
        })

        setSelectedReplaceStockItems(stockItemsMap)
        setReplaceExtraItemsList(extraItems)
    }

    // İmzalar adımına geçildiğinde kurum yetkilisi bilgilerini otomatik doldur
    useEffect(() => {
        if (currentStep === 4) {
            // Biraz bekle, form alanları render edilsin
            setTimeout(() => {
                const values = form.getFieldsValue(true)

                // Kurum Yetkilisi - kurum iletisim bilgilerinden doldur
                const contactFullName = [values.contactFirstName, values.contactLastName].filter(Boolean).join(' ')
                if (!values.authorityFullName && contactFullName) {
                    form.setFieldsValue({
                        authorityFullName: contactFullName,
                        authorityEmail: values.email,
                        authorityPhone: values.phone
                    })
                }
            }, 100)
        }
    }, [currentStep])

    // Autosave (15 saniyede bir)
    useEffect(() => {
        if (formId) {
            autoSaveTimerRef.current = setInterval(() => {
                handleAutoSave()
            }, 15000)
        }
        return () => {
            if (autoSaveTimerRef.current) {
                clearInterval(autoSaveTimerRef.current)
            }
        }
    }, [formId])

    const loadForm = async (formIdToLoad) => {
        try {
            setLoading(true)
            const response = await api.get(`/serviceforms/${formIdToLoad}`)
            const data = response.data

            // Sadece gönderilmiş formları view sayfasına yönlendir
            if (data.status === 'SUBMITTED') {
                navigate(`/service-forms/${formIdToLoad}/view`)
                return
            }

            // Taslak formları düzenleme modunda aç

            // Form alanlarını doldur
            form.setFieldsValue({
                institutionName: data.institutionName,
                institutionOtherName: data.institutionOtherName,
                address: data.address,
                contactFirstName: data.contactFirstName,
                contactLastName: data.contactLastName,
                phone: data.phone,
                email: data.email,
                serviceStartDate: data.serviceStartDate ? dayjs(data.serviceStartDate) : null,
                serviceStartTime: data.serviceStartDate && dayjs(data.serviceStartDate).format('HH:mm') !== '00:00'
                    ? dayjs(data.serviceStartDate)
                    : null,
                serviceEndDate: data.serviceEndDate ? dayjs(data.serviceEndDate) : null,
                serviceEndTime: data.serviceEndDate && dayjs(data.serviceEndDate).format('HH:mm') !== '00:00'
                    ? dayjs(data.serviceEndDate)
                    : null,
                serviceScope: data.serviceScope,
                serviceScopeOther: data.serviceScopeOther,
                serviceType: data.serviceType,
                serviceTypeOther: data.serviceTypeOther,
                reportedIssue: data.reportedIssue,
                workPerformed: data.workPerformed,
                materialsUsed: data.materialsUsed,
                issueStatus: data.issueStatus,
                issueStatusOther: data.issueStatusOther,
                etisanFullName: `${data.etisanFirstName || user?.firstName || ''} ${data.etisanLastName || user?.lastName || ''}`.trim(),
                authorityFullName: `${data.authorityFirstName || data.contactFirstName || ''} ${data.authorityLastName || data.contactLastName || ''}`.trim(),
                authorityEmail: data.authorityEmail || data.email || '',
                authorityPhone: data.authorityPhone || data.phone || ''
            })

            // Fotoğraflar
            if (data.photos) {
                try {
                    setPhotos(JSON.parse(data.photos))
                } catch (e) {
                    setPhotos([])
                }
            }

            // İmzalar
            setEtisanSignature(data.etisanSignature)
            setAuthoritySignature(data.authoritySignature)

            setFormData(data)
        } catch (error) {
            console.error('Form load error:', error)
            // Sadece 404 hatalarında formlar sayfasına yönlendir
            // Diğer hatalarda kullanıcı formda kalabilir
            if (error.response?.status === 404) {
                message.error('Form bulunamadı')
                navigate('/service-forms')
            } else {
                message.warning('Form yüklenemedi, lütfen sayfayı yenileyin')
            }
        } finally {
            setLoading(false)
        }
    }

    const splitFullName = (fullName) => {
        const normalized = (fullName || '').trim().replace(/\s+/g, ' ')
        if (!normalized) return { firstName: '', lastName: '' }
        const parts = normalized.split(' ')
        if (parts.length === 1) return { firstName: parts[0], lastName: '' }
        return { firstName: parts[0], lastName: parts.slice(1).join(' ') }
    }

    const getFormPayload = (includeSignatures = true) => {
        const values = form.getFieldsValue()

        const etisanName = splitFullName(values.etisanFullName)
        const authorityName = splitFullName(values.authorityFullName)

        // Kullanılan malzemeleri formatla
        const materialsArray = []

        // Stok ürünlerini ekle
        Object.entries(selectedStockItems).forEach(([stockItemId, quantity]) => {
            const item = stockItems.find(s => s.id === parseInt(stockItemId))
            if (item) {
                // quantity'nin number olduğundan emin ol
                const qty = typeof quantity === 'number' ? quantity : parseInt(quantity) || 1
                // itemName'in temiz olduğundan emin ol
                const cleanItemName = item.itemName.trim()
                materialsArray.push(`${qty}x ${cleanItemName}`)
            }
        })

        // Ekstra ürünleri ekle
        extraItemsList.forEach(item => {
            // quantity'nin number olduğundan emin ol
            const qty = typeof item.quantity === 'number' ? item.quantity : parseInt(item.quantity) || 1
            // name'in temiz olduğundan emin ol
            const cleanName = item.name.trim()
            materialsArray.push(`${qty}x ${cleanName}`)
        })

        const materialsUsed = materialsArray.length > 0 ? materialsArray.join(', ') : null

        // Değiştirilecek parçaları formatla
        const replaceArray = []
        Object.entries(selectedReplaceStockItems).forEach(([stockItemId, quantity]) => {
            const item = stockItems.find(s => s.id === parseInt(stockItemId))
            if (item) replaceArray.push(`${quantity}x ${item.itemName.trim()}`)
        })
        replaceExtraItemsList.forEach(item => {
            replaceArray.push(`${item.quantity}x ${item.name.trim()}`)
        })
        const partsToReplace = replaceArray.length > 0 ? replaceArray.join(', ') : null

        // Fotoğrafları serialize et - error handling ile
        let photosJson = null
        try {
            if (photos && photos.length > 0) {
                photosJson = JSON.stringify(photos)
                // Çok büyük olup olmadığını kontrol et (10MB limit)
                const sizeInMB = photosJson.length / (1024 * 1024)
                if (sizeInMB > 10) {
                    console.warn(`Fotoğraflar çok büyük: ${sizeInMB.toFixed(2)} MB`)
                    message.warning(`Fotoğraflar çok büyük (${sizeInMB.toFixed(2)} MB). Kaydetme başarısız olabilir.`)
                }
            }
        } catch (error) {
            console.error('Fotoğraf serialize hatası:', error)
            message.error('Fotoğraflar kaydedilemedi, lütfen fotoğraf sayısını azaltın')
        }

        return {
            institutionName: values.institutionName,
            institutionIsOther: values.institutionName === 'DİĞER',
            institutionOtherName: values.institutionOtherName,
            address: values.address,
            contactFirstName: values.contactFirstName,
            contactLastName: values.contactLastName,
            phone: values.phone,
            email: values.email,
            serviceStartDate: combineDateAndTime(values.serviceStartDate, values.serviceStartTime),
            serviceEndDate: combineDateAndTime(values.serviceEndDate, values.serviceEndTime),
            serviceScope: values.serviceScope,
            serviceScopeOther: values.serviceScopeOther,
            serviceType: values.serviceType,
            serviceTypeOther: values.serviceTypeOther,
            reportedIssue: values.reportedIssue,
            workPerformed: values.workPerformed,
            materialsUsed: materialsUsed,
            partsToReplace: partsToReplace,
            issueStatus: values.issueStatus,
            issueStatusOther: values.issueStatusOther,
            photos: photosJson,
            etisanFirstName: etisanName.firstName,
            etisanLastName: etisanName.lastName,
            etisanSignature: includeSignatures ? etisanSignature : null,
            authorityFirstName: authorityName.firstName,
            authorityLastName: authorityName.lastName,
            authorityEmail: values.authorityEmail,
            authorityPhone: values.authorityPhone,
            authoritySignature: includeSignatures ? authoritySignature : null
        }
    }

    const handleSaveDraft = async () => {
        try {
            setSaving(true)
            const payload = getFormPayload(false)

            if (formId) {
                await api.put(`/serviceforms/${formId}`, payload)
                message.success('Taslak kaydedildi')
            } else {
                const response = await api.post('/serviceforms', payload)
                const newFormId = response.data.id
                setFormId(newFormId)
                // URL'yi güncelle - böylece sayfa yenilenmeden form ID'si URL'de olacak
                navigate(`/service-forms/${newFormId}`, { replace: true })
                message.success('Taslak oluşturuldu')
            }
            lastSavedRef.current = new Date()
        } catch (error) {
            message.error('Kaydetme başarısız')
        } finally {
            setSaving(false)
        }
    }

    const handleAutoSave = async () => {
        if (!formId) return

        try {
            const payload = getFormPayload(false)
            await api.put(`/serviceforms/${formId}`, payload)
            lastSavedRef.current = new Date()
        } catch (error) {
            console.error('Autosave failed:', error)
        }
    }

    const handleSubmit = async () => {
        try {
            // Önce form validasyonunu kontrol et
            await form.validateFields()

            setSaving(true)
            const payload = getFormPayload(true)

            // Önce kaydet
            if (formId) {
                await api.put(`/serviceforms/${formId}`, payload)
            } else {
                const response = await api.post('/serviceforms', payload)
                setFormId(response.data.id)
            }

            // Sonra submit et
            await api.put(`/serviceforms/${formId || payload.id}/submit`)

            message.success('Form başarıyla gönderildi!')
            navigate('/service-forms')
        } catch (error) {
            if (error.errorFields) {
                message.error('Lütfen tüm zorunlu alanları doldurun')
            } else {
                message.error('Gönderme başarısız')
            }
        } finally {
            setSaving(false)
        }
    }

    const resizeImage = (file, maxSize = 1280, quality = 0.8) =>
        new Promise((resolve, reject) => {
            const reader = new FileReader()
            reader.onload = () => {
                const img = new Image()
                img.onload = () => {
                    let width = img.width
                    let height = img.height

                    if (width > height && width > maxSize) {
                        height = Math.round((height * maxSize) / width)
                        width = maxSize
                    } else if (height > maxSize) {
                        width = Math.round((width * maxSize) / height)
                        height = maxSize
                    }

                    const canvas = document.createElement('canvas')
                    canvas.width = width
                    canvas.height = height
                    const ctx = canvas.getContext('2d')
                    if (!ctx) {
                        reject(new Error('canvas'))
                        return
                    }
                    ctx.drawImage(img, 0, 0, width, height)
                    resolve(canvas.toDataURL('image/jpeg', quality))
                }
                img.onerror = () => reject(new Error('image-load'))
                img.src = reader.result
            }
            reader.onerror = () => reject(new Error('file-read'))
            reader.readAsDataURL(file)
        })

    const handlePhotoUpload = async (info) => {
        const files = info.fileList || []

        const processed = await Promise.all(files.map(async (file, index) => {
            const raw = file.originFileObj || file
            const uid = file.uid || `${Date.now()}-${index}`
            try {
                const base64 = await resizeImage(raw)
                return { uid, url: base64 }
            } catch (error) {
                return new Promise((resolve) => {
                    const reader = new FileReader()
                    reader.onload = () => resolve({ uid, url: reader.result })
                    reader.onerror = () => resolve(null)
                    reader.readAsDataURL(raw)
                })
            }
        }))

        setPhotos(prev => {
            const existing = new Set(prev.map(p => p.uid))
            const fresh = processed.filter(p => p && !existing.has(p.uid))
            return [...prev, ...fresh]
        })
    }

    const handlePhotoRemove = (uid) => {
        setPhotos(prev => prev.filter(p => p.uid !== uid))
    }

    // Stok ürün seçimi fonksiyonları
    const handleStockItemToggle = (stockItemId, checked) => {
        if (checked) {
            setSelectedStockItems(prev => ({ ...prev, [stockItemId]: 1 }))
        } else {
            const newSelected = { ...selectedStockItems }
            delete newSelected[stockItemId]
            setSelectedStockItems(newSelected)
        }
    }

    const handleStockItemQuantityChange = (stockItemId, quantity) => {
        if (quantity && quantity > 0) {
            setSelectedStockItems(prev => ({ ...prev, [stockItemId]: quantity }))
        }
    }

    const handleAddExtraItem = () => {
        if (stockSearchText.trim()) {
            const itemName = stockSearchText.trim()
            const exists = extraItemsList.some(item => trLower(item.name) === trLower(itemName))

            if (!exists) {
                const newItem = {
                    name: itemName,
                    quantity: extraItemQuantity || 1
                }
                setExtraItemsList(prev => [...prev, newItem])
                setStockSearchText('')
                setExtraItemQuantity(1)
                message.success('Ekstra ürün eklendi')
            } else {
                message.warning('Bu ürün zaten eklenmiş')
            }
        }
    }

    const handleRemoveExtraItem = (index) => {
        setExtraItemsList(prev => prev.filter((_, i) => i !== index))
    }

    const handleUpdateExtraItemQuantity = (index, quantity) => {
        if (quantity && quantity > 0) {
            setExtraItemsList(prev => prev.map((item, i) =>
                i === index ? { ...item, quantity } : item
            ))
        }
    }

    // Parça Değişim fonksiyonları
    const handleReplaceItemToggle = (stockItemId, checked) => {
        if (checked) {
            setSelectedReplaceStockItems(prev => ({ ...prev, [stockItemId]: 1 }))
        } else {
            const newSelected = { ...selectedReplaceStockItems }
            delete newSelected[stockItemId]
            setSelectedReplaceStockItems(newSelected)
        }
    }

    const handleReplaceQuantityChange = (stockItemId, quantity) => {
        if (quantity && quantity > 0) {
            setSelectedReplaceStockItems(prev => ({ ...prev, [stockItemId]: quantity }))
        }
    }

    const handleAddReplaceExtra = () => {
        if (replaceSearchText.trim()) {
            const itemName = replaceSearchText.trim()
            if (!replaceExtraItemsList.some(item => trLower(item.name) === trLower(itemName))) {
                setReplaceExtraItemsList(prev => [...prev, { name: itemName, quantity: replaceExtraQuantity || 1 }])
                setReplaceSearchText('')
                setReplaceExtraQuantity(1)
            }
        }
    }

    const handleRemoveReplaceExtra = (index) => {
        setReplaceExtraItemsList(prev => prev.filter((_, i) => i !== index))
    }

    const handleUpdateReplaceExtraQuantity = (index, quantity) => {
        if (quantity && quantity > 0) {
            setReplaceExtraItemsList(prev => prev.map((item, i) =>
                i === index ? { ...item, quantity } : item
            ))
        }
    }

    const renderReplaceSection = () => (
        <Form.Item label="Değiştirilecek Parçalar">
            <Alert
                message="Parça Değişim Talebi"
                description="Bu alana eklenen parçalar stoktan düşülmez, sadece 'Yapılacak İşler' ekranına talep olarak iletilir."
                type="warning"
                showIcon
                style={{ marginBottom: 16 }}
            />

            <Input
                placeholder="İhtiyaç duyulan parçayı arayın veya yazın..."
                prefix={<SearchOutlined />}
                value={replaceSearchText}
                onChange={(e) => setReplaceSearchText(e.target.value)}
                allowClear
                style={{ marginBottom: 16 }}
            />

            {replaceSearchText.trim() && (() => {
                const searchLower = trLower(replaceSearchText).trim()
                const matchedItems = stockItems.filter(item =>
                    trLower(item.itemName).includes(searchLower) ||
                    trLower(item.serialNumber).includes(searchLower)
                )

                if (matchedItems.length > 0) {
                    return (
                        <div style={{ maxHeight: '200px', overflowY: 'auto', marginBottom: 16 }}>
                            {matchedItems.map(item => {
                                const isSelected = selectedReplaceStockItems[item.id] !== undefined
                                return (
                                    <div
                                        key={item.id}
                                        style={{
                                            padding: '8px 12px',
                                            marginBottom: '4px',
                                            border: `1px solid ${isSelected ? '#1890ff' : '#d9d9d9'}`,
                                            borderRadius: '4px',
                                            display: 'flex',
                                            justifyContent: 'space-between',
                                            alignItems: 'center',
                                            background: isSelected ? (isDarkMode ? 'rgba(24, 144, 255, 0.1)' : '#e6f7ff') : 'transparent'
                                        }}
                                    >
                                        <Checkbox
                                            checked={isSelected}
                                            onChange={(e) => handleReplaceItemToggle(item.id, e.target.checked)}
                                        >
                                            {item.itemName}
                                        </Checkbox>
                                        {isSelected && (
                                            <InputNumber
                                                min={1}
                                                value={selectedReplaceStockItems[item.id]}
                                                onChange={(val) => handleReplaceQuantityChange(item.id, val)}
                                                size="small"
                                                style={{ width: 60 }}
                                            />
                                        )}
                                    </div>
                                )
                            })}
                        </div>
                    )
                } else {
                    return (
                        <div style={{ padding: '16px', border: `1px dashed ${isDarkMode ? 'rgba(255,255,255,0.2)' : '#d9d9d9'}`, borderRadius: '4px', textAlign: 'center', marginBottom: 16 }}>
                            <div style={{ marginBottom: 8 }}>"{replaceSearchText}" stokta bulunamadı.</div>
                            <Space>
                                <InputNumber min={1} value={replaceExtraQuantity} onChange={setReplaceExtraQuantity} style={{ width: 70 }} />
                                <Button type="primary" size="small" icon={<PlusOutlined />} onClick={handleAddReplaceExtra}>
                                    Yeni Parça Olarak Ekle
                                </Button>
                            </Space>
                        </div>
                    )
                }
            })()}

            {(Object.keys(selectedReplaceStockItems).length > 0 || replaceExtraItemsList.length > 0) && (
                <div style={{ marginTop: 12, padding: 8, background: isDarkMode ? 'rgba(255,165,0,0.1)' : '#fffbe6', borderRadius: 4, border: `1px solid ${isDarkMode ? 'rgba(255,165,0,0.2)' : '#ffe58f'}` }}>
                    <div style={{ marginBottom: 4, fontWeight: 500, fontSize: 12 }}>Talebe Eklenen Parçalar:</div>
                    {Object.entries(selectedReplaceStockItems).map(([id, qty]) => {
                        const item = stockItems.find(s => s.id === parseInt(id))
                        return item && (
                            <Tag key={id} closable onClose={() => handleReplaceItemToggle(id, false)} color="orange" style={{ margin: 4 }}>
                                {item.itemName} x{qty}
                            </Tag>
                        )
                    })}
                    {replaceExtraItemsList.map((item, i) => (
                        <Tag key={i} closable onClose={() => handleRemoveReplaceExtra(i)} color="warning" style={{ margin: 4 }}>
                            {item.name} x{item.quantity}
                        </Tag>
                    ))}
                </div>
            )}
        </Form.Item>
    )

    const nextStep = () => {
        handleSaveDraft()
        setCurrentStep(prev => Math.min(prev + 1, 4))
        // Sayfayı en üste kaydır
        window.scrollTo({ top: 0, behavior: 'smooth' })
    }

    const prevStep = () => {
        setCurrentStep(prev => Math.max(prev - 1, 0))
    }

    const goToStep = (step) => {
        setCurrentStep(step)
    }

    // Step 1: Genel Bilgiler
    const renderStep1 = () => (
        <Card title="Genel Bilgiler" size="small">
            <Row gutter={[16, 16]}>
                {/* Form Tarihi - Üstte tek başına */}
                <Col xs={24}>
                    <Form.Item
                        name="formDate"
                        label="Form Düzenlenme Tarihi"
                        initialValue={dayjs()}
                    >
                        <DatePicker
                            format="DD.MM.YYYY"
                            style={{ width: '100%' }}
                            placeholder="Tarih seçin"
                        />
                    </Form.Item>
                </Col>


                {/* Firma Bilgileri */}
                <Col xs={24}>
                    <div style={{
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid #e8e8e8'
                    }}>
                        <Row gutter={[16, 16]}>
                            <Col xs={24} md={12}>
                                <Form.Item
                                    name="institutionName"
                                    label="Firma Adı"
                                    rules={[{ required: true, message: 'Lütfen firma adı seçin' }]}
                                    required={false}
                                >
                                    <InstitutionSelect placeholder="Kurum seçin" allowClear />
                                </Form.Item>
                            </Col>

                            <Col xs={24} md={12}>
                                <Form.Item noStyle shouldUpdate={(prev, curr) => prev.institutionName !== curr.institutionName}>
                                    {({ getFieldValue }) =>
                                        getFieldValue('institutionName') === 'DİĞER' && (
                                            <Form.Item
                                                name="institutionOtherName"
                                                label="Kurum Adı (Diğer)"
                                                rules={[{ required: true, message: 'Lütfen kurum adını yazın' }]}
                                                required={false}
                                            >
                                                <Input placeholder="Kurum adını yazın" />
                                            </Form.Item>
                                        )
                                    }
                                </Form.Item>
                            </Col>

                            <Col xs={24}>
                                <Form.Item
                                    name="address"
                                    label="Adres"
                                    rules={[{ required: true, message: 'Lütfen adres girin' }]}
                                    required={false}
                                >
                                    <TextArea
                                        rows={3}
                                        placeholder="Tam adres bilgisi"
                                        showCount
                                        maxLength={500}
                                    />
                                </Form.Item>
                            </Col>
                        </Row>
                    </div>
                </Col>

                {/* İletişim Bilgileri */}
                <Col xs={24}>
                    <div style={{
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid #e8e8e8'
                    }}>
                        <Row gutter={[16, 16]}>
                            <Col xs={24} md={12}>
                                <Form.Item
                                    name="contactFirstName"
                                    label="İletişim Kişisi"
                                    rules={[{ required: true, message: 'Lütfen ad soyad girin' }]}
                                    required={false}
                                >
                                    <Input placeholder="Ad Soyad" />
                                </Form.Item>
                            </Col>

                            <Col xs={24} md={12}>
                                <Form.Item
                                    name="phone"
                                    label="Telefon"
                                    rules={[
                                        { required: false }
                                    ]}
                                    normalize={normalizePhoneNumber}
                                >
                                    <Input placeholder="05XX XXX XX XX" maxLength={14} />
                                </Form.Item>
                            </Col>

                            <Col xs={24} md={12}>
                                <Form.Item
                                    name="email"
                                    label="E-posta"
                                    rules={[
                                        { required: true, message: 'Lütfen e-posta adresi girin' },
                                        { type: 'email', message: 'Geçerli bir e-posta adresi girin' }
                                    ]}
                                    required={false}
                                >
                                    <Input placeholder="email@example.com" type="email" />
                                </Form.Item>
                            </Col>
                        </Row>
                    </div>
                </Col>

                {/* Servis Tarihleri */}
                <Col xs={24}>
                    <div style={{
                        padding: '16px',
                        borderRadius: '8px',
                        border: '1px solid #e8e8e8'
                    }}>
                        <Row gutter={[16, 16]}>


                            <Col xs={24} md={12}>
                                <Row gutter={8}>
                                    <Col flex="auto">
                                        <Form.Item
                                            name="serviceStartDate"
                                            label="Servis Başlangıç Tarihi"
                                            rules={[{ required: true, message: 'Tarih seçin' }]}
                                        >
                                            <DatePicker
                                                style={{ width: '100%' }}
                                                format="DD.MM.YYYY"
                                                placeholder="Tarih seçin"
                                            />
                                        </Form.Item>
                                    </Col>
                                    <Col flex="120px">
                                        <Form.Item
                                            name="serviceStartTime"
                                            label="Saati"
                                        >
                                            <TimePicker
                                                style={{ width: '100%' }}
                                                format="HH:mm"
                                                placeholder="Saat"
                                                allowClear
                                                showNow={false}
                                                needConfirm={false}
                                            />
                                        </Form.Item>
                                    </Col>
                                </Row>
                            </Col>

                            <Col xs={24} md={12}>
                                <Row gutter={8}>
                                    <Col flex="auto">
                                        <Form.Item
                                            name="serviceEndDate"
                                            label="Servis Bitiş Tarihi"
                                        >
                                            <DatePicker
                                                style={{ width: '100%' }}
                                                format="DD.MM.YYYY"
                                                placeholder="Tarih seçin"
                                            />
                                        </Form.Item>
                                    </Col>
                                    <Col flex="120px">
                                        <Form.Item
                                            name="serviceEndTime"
                                            label="Saati"
                                        >
                                            <TimePicker
                                                style={{ width: '100%' }}
                                                format="HH:mm"
                                                placeholder="Saat"
                                                allowClear
                                                showNow={false}
                                                needConfirm={false}
                                            />
                                        </Form.Item>
                                    </Col>
                                </Row>
                            </Col>
                        </Row>
                    </div>
                </Col>

                {/* SERVİS KAPSAMI BÖLÜMÜ */}
                <Col xs={24}>
                    <Divider orientation="left" style={{ fontWeight: '600', fontSize: '15px', marginTop: 8, marginBottom: 16 }}>
                        SERVİS KAPSAMI
                    </Divider>
                </Col>

                <Col xs={24}>
                    <Form.Item name="serviceScope" label="Servis Kapsamı" rules={[{ required: true }]}>
                        <Radio.Group>
                            <Space direction="horizontal" wrap>
                                <Radio
                                    value="SozlesmeIci"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceScope') === 'SozlesmeIci') {
                                            form.setFieldValue('serviceScope', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Sözleşme İçi
                                </Radio>
                                <Radio
                                    value="SozlesmeDisi"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceScope') === 'SozlesmeDisi') {
                                            form.setFieldValue('serviceScope', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Sözleşme Dışı
                                </Radio>
                                <Radio
                                    value="Diger"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceScope') === 'Diger') {
                                            form.setFieldValue('serviceScope', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Diğer
                                </Radio>
                            </Space>
                        </Radio.Group>
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item noStyle shouldUpdate={(prev, curr) => prev.serviceScope !== curr.serviceScope}>
                        {({ getFieldValue }) =>
                            getFieldValue('serviceScope') === 'Diger' && (
                                <Form.Item name="serviceScopeOther" label="Diğer (Açıklama)" rules={[{ required: true }]}>
                                    <Input placeholder="Lütfen açıklayın" />
                                </Form.Item>
                            )
                        }
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item name="serviceType" label="Hizmet Türü" rules={[{ required: true }]}>
                        <Radio.Group>
                            <Space direction="horizontal" wrap>
                                <Radio
                                    value="ArizaGiderme"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceType') === 'ArizaGiderme') {
                                            form.setFieldValue('serviceType', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Arıza Giderme
                                </Radio>
                                <Radio
                                    value="Bakim"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceType') === 'Bakim') {
                                            form.setFieldValue('serviceType', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Bakım
                                </Radio>
                                <Radio
                                    value="Kesif"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceType') === 'Kesif') {
                                            form.setFieldValue('serviceType', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Keşif
                                </Radio>
                                <Radio
                                    value="Diger"
                                    onClick={(e) => {
                                        if (form.getFieldValue('serviceType') === 'Diger') {
                                            form.setFieldValue('serviceType', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Diğer
                                </Radio>
                            </Space>
                        </Radio.Group>
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item noStyle shouldUpdate={(prev, curr) => prev.serviceType !== curr.serviceType}>
                        {({ getFieldValue }) =>
                            getFieldValue('serviceType') === 'Diger' && (
                                <Form.Item name="serviceTypeOther" label="Diğer (Açıklama)" rules={[{ required: true }]}>
                                    <Input placeholder="Lütfen açıklayın" />
                                </Form.Item>
                            )
                        }
                    </Form.Item>
                </Col>


            </Row>
        </Card>
    )

    // Step 2: Servis Detayları
    const renderStep2 = () => (
        <Card title="Servis Detayları" size="small">
            <Row gutter={[16, 0]}>
                <Col xs={24}>
                    <Form.Item name="reportedIssue" label="Bildirilen Sorun">
                        <TextArea rows={4} placeholder="Müşteri tarafından bildirilen sorun..." />
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item name="workPerformed" label="Yapılan İşlem">
                        <TextArea rows={4} placeholder="Gerçekleştirilen işlemler..." />
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item label="Kullanılan Malzemeler">
                        <Alert
                            message="Stok Yönetimi"
                            description="Stoktan ürün seçebilir veya stoğumuzda olmayan ürünleri ekleyebilirsiniz."
                            type="info"
                            showIcon
                            style={{ marginBottom: 16 }}
                        />

                        {/* Arama Input */}
                        <Input
                            placeholder="Ürün adı veya seri numarası ile ara..."
                            prefix={<SearchOutlined />}
                            value={stockSearchText}
                            onChange={(e) => setStockSearchText(e.target.value)}
                            allowClear
                            style={{ marginBottom: 16 }}
                        />

                        {/* Arama Sonuçları */}
                        {stockSearchText.trim() && (() => {
                            const searchLower = trLower(stockSearchText).trim()
                            const matchedItems = stockItems.filter(item =>
                                trLower(item.itemName).includes(searchLower) ||
                                trLower(item.serialNumber).includes(searchLower) ||
                                trLower(item.description).includes(searchLower)
                            )

                            if (matchedItems.length > 0) {
                                return (
                                    <div style={{ maxHeight: '300px', overflowY: 'auto', marginBottom: 16 }}>
                                        {matchedItems.map(item => {
                                            const isSelected = selectedStockItems[item.id] !== undefined
                                            const isLowStock = item.currentStock <= item.minimumStock
                                            const selectedQuantity = selectedStockItems[item.id] || 1

                                            return (
                                                <div
                                                    key={item.id}
                                                    style={{
                                                        padding: '12px',
                                                        marginBottom: '8px',
                                                        border: `1px solid ${isSelected
                                                            ? (isDarkMode ? 'rgba(24, 144, 255, 0.5)' : '#1890ff')
                                                            : (isDarkMode ? 'rgba(255, 255, 255, 0.2)' : '#d9d9d9')}`,
                                                        borderRadius: '4px',
                                                        background: isSelected
                                                            ? (isDarkMode ? 'rgba(24, 144, 255, 0.15)' : '#e6f7ff')
                                                            : (isDarkMode ? 'rgba(255, 255, 255, 0.05)' : '#fff'),
                                                        transition: 'all 0.3s'
                                                    }}
                                                >
                                                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                                                        <div style={{ flex: 1 }}>
                                                            <Checkbox
                                                                checked={isSelected}
                                                                onChange={(e) => handleStockItemToggle(item.id, e.target.checked)}
                                                            >
                                                                <Space>
                                                                    <span style={{ fontWeight: 500 }}>{item.itemName}</span>
                                                                    {item.serialNumber && (
                                                                        <Tag color="blue">SN: {item.serialNumber}</Tag>
                                                                    )}
                                                                    <Tag color={isLowStock ? 'orange' : 'green'}>
                                                                        {isLowStock && <WarningOutlined style={{ marginRight: 4 }} />}
                                                                        Mevcut Stok: {item.currentStock}
                                                                    </Tag>
                                                                </Space>
                                                            </Checkbox>
                                                            {item.description && (
                                                                <div style={{
                                                                    marginLeft: 24,
                                                                    fontSize: 12,
                                                                    color: isDarkMode ? 'rgba(255, 255, 255, 0.65)' : '#8c8c8c',
                                                                    marginTop: 4
                                                                }}>
                                                                    {item.description}
                                                                </div>
                                                            )}
                                                        </div>
                                                        {isSelected && (
                                                            <div style={{ marginLeft: 16 }}>
                                                                <InputNumber
                                                                    min={1}
                                                                    value={selectedQuantity}
                                                                    onChange={(value) => handleStockItemQuantityChange(item.id, value)}
                                                                    style={{ width: 80 }}
                                                                />
                                                                <span style={{
                                                                    marginLeft: 8,
                                                                    fontSize: 12,
                                                                    color: isDarkMode ? 'rgba(255, 255, 255, 0.65)' : '#8c8c8c'
                                                                }}>adet</span>
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            )
                                        })}
                                    </div>
                                )
                            } else {
                                return (
                                    <div style={{
                                        padding: '20px',
                                        border: `1px dashed ${isDarkMode ? 'rgba(255, 255, 255, 0.2)' : '#d9d9d9'}`,
                                        borderRadius: '4px',
                                        background: isDarkMode ? 'rgba(255, 255, 255, 0.03)' : '#fafafa',
                                        marginBottom: 16
                                    }}>
                                        <div style={{
                                            marginBottom: 16,
                                            textAlign: 'center',
                                            color: isDarkMode ? 'rgba(255, 255, 255, 0.65)' : '#8c8c8c'
                                        }}>
                                            <SearchOutlined style={{ fontSize: 24, marginBottom: 8 }} />
                                            <div>"{stockSearchText}" için eşleşme bulunamadı</div>
                                        </div>
                                        <div style={{
                                            display: 'flex',
                                            alignItems: 'center',
                                            justifyContent: 'center',
                                            gap: 12,
                                            flexWrap: 'wrap'
                                        }}>
                                            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                                                <span style={{ fontWeight: 500 }}>Adet:</span>
                                                <InputNumber
                                                    min={1}
                                                    value={extraItemQuantity}
                                                    onChange={(value) => setExtraItemQuantity(value || 1)}
                                                    style={{ width: 100 }}
                                                />
                                            </div>
                                            <Button
                                                type="primary"
                                                icon={<PlusOutlined />}
                                                onClick={handleAddExtraItem}
                                            >
                                                Stoğumuzda Olmayan Ürün Olarak Ekle
                                            </Button>
                                        </div>
                                    </div>
                                )
                            }
                        })()}

                        {/* Seçilen Ürünler Özeti */}
                        {(Object.keys(selectedStockItems).length > 0 || extraItemsList.length > 0) && (
                            <div style={{
                                marginTop: 16,
                                padding: 12,
                                background: isDarkMode ? 'rgba(24, 144, 255, 0.1)' : '#f0f5ff',
                                borderRadius: 4,
                                border: isDarkMode ? '1px solid rgba(24, 144, 255, 0.2)' : 'none'
                            }}>
                                <strong style={{ color: isDarkMode ? 'rgba(255, 255, 255, 0.85)' : undefined }}>
                                    Seçilen Ürünler:
                                </strong>
                                <div style={{ marginTop: 8 }}>
                                    {/* Stok ürünleri */}
                                    {Object.entries(selectedStockItems).map(([stockItemId, quantity]) => {
                                        const item = stockItems.find(s => s.id === parseInt(stockItemId))
                                        return item ? (
                                            <div
                                                key={stockItemId}
                                                style={{
                                                    display: 'inline-flex',
                                                    alignItems: 'center',
                                                    margin: '4px',
                                                    padding: '4px 8px',
                                                    background: isDarkMode ? 'rgba(24, 144, 255, 0.15)' : '#e6f7ff',
                                                    border: isDarkMode ? '1px solid rgba(24, 144, 255, 0.3)' : '1px solid #91d5ff',
                                                    borderRadius: '4px'
                                                }}
                                            >
                                                <Tag color="blue" style={{ margin: 0 }}>
                                                    {item.itemName} × {quantity}
                                                </Tag>
                                                <InputNumber
                                                    min={1}
                                                    value={quantity}
                                                    onChange={(value) => handleStockItemQuantityChange(parseInt(stockItemId), value)}
                                                    size="small"
                                                    style={{ width: 70, marginLeft: 8 }}
                                                />
                                                <Button
                                                    type="text"
                                                    danger
                                                    size="small"
                                                    icon={<DeleteOutlined />}
                                                    onClick={() => handleStockItemToggle(parseInt(stockItemId), false)}
                                                    style={{ marginLeft: 4 }}
                                                />
                                            </div>
                                        ) : null
                                    })}

                                    {/* Ekstra ürünler */}
                                    {extraItemsList.map((item, index) => (
                                        <div
                                            key={index}
                                            style={{
                                                display: 'inline-flex',
                                                alignItems: 'center',
                                                margin: '4px',
                                                padding: '4px 8px',
                                                background: isDarkMode ? 'rgba(255, 193, 7, 0.15)' : '#fff7e6',
                                                border: isDarkMode ? '1px solid rgba(255, 193, 7, 0.3)' : '1px solid #ffd591',
                                                borderRadius: '4px'
                                            }}
                                        >
                                            <Tag color="orange" style={{ margin: 0 }}>
                                                {item.name} × {item.quantity} (Stokta Yok)
                                            </Tag>
                                            <InputNumber
                                                min={1}
                                                value={item.quantity}
                                                onChange={(value) => handleUpdateExtraItemQuantity(index, value)}
                                                size="small"
                                                style={{ width: 70, marginLeft: 8 }}
                                            />
                                            <Button
                                                type="text"
                                                danger
                                                size="small"
                                                icon={<DeleteOutlined />}
                                                onClick={() => handleRemoveExtraItem(index)}
                                                style={{ marginLeft: 4 }}
                                            />
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item name="issueStatus" label="Durum">
                        <Radio.Group>
                            <Space direction="horizontal" wrap>
                                <Radio
                                    value="SorunGiderilmedi"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'SorunGiderilmedi') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Sorun Giderilmedi
                                </Radio>
                                <Radio
                                    value="SorunGiderildi"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'SorunGiderildi') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Sorun Giderildi
                                </Radio>
                                <Radio
                                    value="SorunaRastlanmadi"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'SorunaRastlanmadi') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Soruna Rastlanmadı
                                </Radio>
                                <Radio
                                    value="ParcaDegistirildi"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'ParcaDegistirildi') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Parça Değiştirildi
                                </Radio>
                                <Radio
                                    value="ParcaDegistirilecek"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'ParcaDegistirilecek') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Parça Değiştirilecek
                                </Radio>
                                <Radio
                                    value="Diger"
                                    onClick={(e) => {
                                        if (form.getFieldValue('issueStatus') === 'Diger') {
                                            form.setFieldValue('issueStatus', undefined);
                                            e.preventDefault();
                                        }
                                    }}
                                >
                                    Diğer
                                </Radio>
                            </Space>
                        </Radio.Group>
                    </Form.Item>
                </Col>

                <Col xs={24}>
                    <Form.Item noStyle shouldUpdate={(prev, curr) => prev.issueStatus !== curr.issueStatus}>
                        {({ getFieldValue }) => {
                            const status = getFieldValue('issueStatus')
                            if (status === 'Diger') {
                                return (
                                    <Form.Item name="issueStatusOther" label="Durum Açıklaması">
                                        <Input placeholder="Açıklama yazın" />
                                    </Form.Item>
                                )
                            }
                            if (status === 'ParcaDegistirilecek') {
                                return renderReplaceSection()
                            }
                            return null
                        }}
                    </Form.Item>
                </Col>
            </Row>
        </Card>
    )

    // Step 3: Fotoğraflar
    const renderStep3 = () => (
        <Card title="Fotoğraflar" size="small">
            <Upload
                listType="picture-card"
                fileList={[]}
                multiple
                beforeUpload={() => false}
                onChange={handlePhotoUpload}
                accept="image/*"
            >
                <div>
                    <CameraOutlined style={{ fontSize: 24 }} />
                    <div style={{ marginTop: 8 }}>Fotoğraf Ekle</div>
                </div>
            </Upload>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginTop: 16 }}>
                {photos.map(photo => (
                    <div key={photo.uid} style={{ position: 'relative' }}>
                        <img
                            src={photo.url}
                            alt="Photo"
                            style={{
                                width: 120,
                                height: 120,
                                objectFit: 'cover',
                                borderRadius: 8,
                                border: '1px solid #d9d9d9'
                            }}
                        />
                        <Button
                            type="primary"
                            danger
                            size="small"
                            icon={<DeleteOutlined />}
                            style={{ position: 'absolute', top: 4, right: 4 }}
                            onClick={() => handlePhotoRemove(photo.uid)}
                        />
                    </div>
                ))}
            </div>

            {photos.length === 0 && (
                <Text type="secondary">Henüz fotoğraf eklenmedi</Text>
            )}
        </Card>
    )

    // Step 4: Gözden Geçir
    const renderStep4 = () => {
        // Form değerlerini doğrudan al (true = tüm alanları dahil et)
        const values = form.getFieldsValue(true)

        // Kullanılan malzemeleri formatla
        const materialsArray = []

        // Stok ürünlerini ekle
        Object.entries(selectedStockItems).forEach(([stockItemId, quantity]) => {
            const item = stockItems.find(s => s.id === parseInt(stockItemId))
            if (item) {
                // quantity'nin number olduğundan emin ol
                const qty = typeof quantity === 'number' ? quantity : parseInt(quantity) || 1
                // itemName'in temiz olduğundan emin ol (gereksiz "adet" vs. içermemeli)
                const cleanItemName = item.itemName.trim()
                materialsArray.push(`${qty}x ${cleanItemName}`)
            }
        })

        // Ekstra ürünleri ekle
        extraItemsList.forEach(item => {
            // quantity'nin number olduğundan emin ol
            const qty = typeof item.quantity === 'number' ? item.quantity : parseInt(item.quantity) || 1
            // name'in temiz olduğundan emin ol
            const cleanName = item.name.trim()
            materialsArray.push(`${qty}x ${cleanName}`)
        })

        const materialsUsedText = materialsArray.length > 0 ? materialsArray.join(', ') : '-'

        return (
            <Card title="Gözden Geçir" size="small">
                <Space direction="vertical" style={{ width: '100%' }} size="large">
                    <Card
                        type="inner"
                        title="Genel Bilgiler"
                        extra={<Button size="small" icon={<EditOutlined />} onClick={() => goToStep(0)}>Düzenle</Button>}
                    >
                        <Row gutter={[16, 16]}>
                            {/* Sol Sütun */}
                            <Col xs={24} md={12}>
                                <Descriptions column={1} bordered size="small">
                                    <Descriptions.Item label="Firma Adı">
                                        {values?.institutionName === 'DİĞER' ? values?.institutionOtherName : values?.institutionName || '-'}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Adres">{values?.address || '-'}</Descriptions.Item>
                                    <Descriptions.Item label="Servis Başlangıç">
                                        {values?.serviceStartDate ? dayjs(values.serviceStartDate).format('DD.MM.YYYY HH:mm') : '-'}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Servis Bitiş">
                                        {values?.serviceEndDate ? dayjs(values.serviceEndDate).format('DD.MM.YYYY HH:mm') : '-'}
                                    </Descriptions.Item>
                                </Descriptions>
                            </Col>

                            {/* Sağ Sütun */}
                            <Col xs={24} md={12}>
                                <Descriptions column={1} bordered size="small">
                                    <Descriptions.Item label="İletişim Kişisi">
                                        {values?.contactFirstName || ''} {values?.contactLastName || ''}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="E-posta">{values?.email || '-'}</Descriptions.Item>
                                    <Descriptions.Item label="Telefon">{values?.phone || '-'}</Descriptions.Item>
                                </Descriptions>
                            </Col>

                            {/* Servis Kapsamı - Sol Sütun */}
                            <Col xs={24} md={12}>
                                <Descriptions column={1} bordered size="small">
                                    <Descriptions.Item label="Servis Kapsamı">
                                        {values?.serviceScope === 'SozlesmeIci' ? 'Sözleşme İçi' :
                                            values?.serviceScope === 'SozlesmeDisi' ? 'Sözleşme Dışı' :
                                                values?.serviceScope === 'Diger' ? values?.serviceScopeOther : '-'}
                                    </Descriptions.Item>
                                </Descriptions>
                            </Col>

                            {/* Hizmet Türü - Sağ Sütun */}
                            <Col xs={24} md={12}>
                                <Descriptions column={1} bordered size="small">
                                    <Descriptions.Item label="Hizmet Türü">
                                        {values?.serviceType === 'ArizaGiderme' ? 'Arıza Giderme' :
                                            values?.serviceType === 'Bakim' ? 'Bakım' :
                                                values?.serviceType === 'Kesif' ? 'Keşif' :
                                                    values?.serviceType === 'Diger' ? values?.serviceTypeOther : '-'}
                                    </Descriptions.Item>
                                </Descriptions>
                            </Col>
                        </Row>
                    </Card>

                    {/* Servis Detayları */}
                    <Card
                        type="inner"
                        title="Servis Detayları"
                        extra={<Button size="small" icon={<EditOutlined />} onClick={() => goToStep(1)}>Düzenle</Button>}
                    >
                        <Descriptions
                            column={1}
                            bordered
                            size="small"
                        >
                            <Descriptions.Item label="Bildirilen Sorun">{values?.reportedIssue || '-'}</Descriptions.Item>
                            <Descriptions.Item label="Yapılan İşlem">{values?.workPerformed || '-'}</Descriptions.Item>
                            <Descriptions.Item label="Kullanılan Malzemeler">{materialsUsedText}</Descriptions.Item>
                            <Descriptions.Item label="Durum">
                                {values?.issueStatus === 'SorunGiderilmedi' ? 'Sorun Giderilmedi' :
                                    values?.issueStatus === 'SorunGiderildi' ? 'Sorun Giderildi' :
                                        values?.issueStatus === 'SorunaRastlanmadi' ? 'Soruna Rastlanmadı' :
                                            values?.issueStatus === 'ParcaDegistirildi' ? 'Parça Değiştirildi' :
                                                values?.issueStatus === 'ParcaDegistirilecek' ? 'Parça Değiştirilecek' :
                                                    values?.issueStatus === 'Diger' ? values?.issueStatusOther : '-'}
                            </Descriptions.Item>
                            {values?.issueStatus === 'ParcaDegistirilecek' && (
                                <Descriptions.Item label="Değiştirilecek Parçalar">
                                    {(Object.keys(selectedReplaceStockItems).length > 0 || replaceExtraItemsList.length > 0) ? (
                                        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
                                            {Object.entries(selectedReplaceStockItems).map(([id, qty]) => {
                                                const item = stockItems.find(s => s.id === parseInt(id))
                                                return item && <Tag color="orange" key={id}>{item.itemName} x{qty}</Tag>
                                            })}
                                            {replaceExtraItemsList.map((item, i) => (
                                                <Tag color="warning" key={i}>{item.name} x{item.quantity}</Tag>
                                            ))}
                                        </div>
                                    ) : '-'}
                                </Descriptions.Item>
                            )}
                        </Descriptions>
                    </Card>

                    {/* Fotoğraflar */}
                    <Card
                        type="inner"
                        title={`Fotoğraflar (${photos.length})`}
                        extra={<Button size="small" icon={<EditOutlined />} onClick={() => goToStep(2)}>Düzenle</Button>}
                    >
                        {photos.length > 0 ? (
                            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                                {photos.map(photo => (
                                    <img
                                        key={photo.uid}
                                        src={photo.url}
                                        alt="Photo"
                                        style={{ width: 80, height: 80, objectFit: 'cover', borderRadius: 4 }}
                                    />
                                ))}
                            </div>
                        ) : (
                            <Text type="secondary">Fotoğraf eklenmedi</Text>
                        )}
                    </Card>
                </Space>
            </Card>
        )
    }

    // Step 5: İmzalar
    const renderStep5 = () => {
        return (
            <Card title="Onay ve İmzalar" size="small">
                <Row gutter={[24, 24]}>
                    {/* Etisan Servis Yetkilisi */}
                    <Col xs={24}>
                        <Card type="inner" title="Etisan Servis Yetkilisi">
                            <Row gutter={[16, 0]}>
                                <Col xs={24}>
                                    <Form.Item name="etisanFullName" label="Ad Soyad">
                                        <Input placeholder="Ad Soyad giriniz" />
                                    </Form.Item>
                                </Col>
                                <Col xs={24}>
                                    <Form.Item label="İmza">
                                        <SignaturePad
                                            value={etisanSignature}
                                            onChange={setEtisanSignature}
                                        />
                                    </Form.Item>
                                </Col>
                            </Row>
                        </Card>
                    </Col>

                    {/* Kurum Yetkilisi */}
                    <Col xs={24}>
                        <Card type="inner" title="Kurum Yetkilisi">
                            <Row gutter={[16, 0]}>
                                <Col xs={24}>
                                    <Form.Item name="authorityFullName" label="Ad Soyad">
                                        <Input />
                                    </Form.Item>
                                </Col>
                                <Col xs={24} md={12}>
                                    <Form.Item name="authorityEmail" label="E-posta">
                                        <Input type="email" />
                                    </Form.Item>
                                </Col>
                                <Col xs={24} md={12}>
                                    <Form.Item name="authorityPhone" label="Telefon" normalize={normalizePhoneNumber}>
                                        <Input placeholder="05XX XXX XX XX" maxLength={14} />
                                    </Form.Item>
                                </Col>
                                <Col xs={24}>
                                    <Form.Item label="İmza">
                                        <SignaturePad
                                            value={authoritySignature}
                                            onChange={setAuthoritySignature}
                                        />
                                    </Form.Item>
                                </Col>
                                <Col xs={24}>
                                    <Form.Item
                                        name="signatureConsent"
                                        valuePropName="checked"
                                        rules={[
                                            {
                                                validator: (_, value) =>
                                                    value
                                                        ? Promise.resolve()
                                                        : Promise.reject(new Error('Elektronik imza onayını kabul etmelisiniz'))
                                            }
                                        ]}
                                    >
                                        <Checkbox>
                                            Elektronik imzamın elle atılmış bir imza kadar geçerli olacağını ve yürürlükteki yasaların imza verdiği ölçüde orijinal olarak kabul edileceğini anlıyor ve kabul ediyorum.
                                        </Checkbox>
                                    </Form.Item>
                                </Col>
                            </Row>
                        </Card>
                    </Col>
                </Row>
            </Card>
        )
    }

    const steps = [
        { title: 'Genel Bilgiler', content: renderStep1 },
        { title: 'Servis Detayları', content: renderStep2 },
        { title: 'Fotoğraflar', content: renderStep3 },
        { title: 'Gözden Geçir', content: renderStep4 },
        { title: 'İmzalar', content: renderStep5 }
    ]

    if (loading) {
        return (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
                <Spin size="large" />
            </div>
        )
    }

    return (
        <div style={{ maxWidth: 1200, margin: '0 auto' }}>
            <div style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
                <Space>
                    <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/service-forms')}>
                        Geri
                    </Button>
                    <Title level={4} style={{ margin: 0 }}>
                        {formId ? `Form #${formId.toString().padStart(4, '0')} - Düzenle` : 'Yeni Form'}
                    </Title>
                </Space>

                <Button
                    icon={<SaveOutlined />}
                    onClick={handleSaveDraft}
                    loading={saving}
                >
                    Taslak Kaydet
                </Button>
            </div>

            <Steps
                current={currentStep}
                items={steps.map(s => ({ title: s.title }))}
                style={{ marginBottom: 24 }}
                size="small"
                responsive={false}
            />

            <Form form={form} layout="vertical" preserve={true}>
                {steps[currentStep].content()}
            </Form>

            <div style={{ marginTop: 24, display: 'flex', justifyContent: 'space-between' }}>
                <Button
                    icon={<ArrowLeftOutlined />}
                    onClick={prevStep}
                    disabled={currentStep === 0}
                >
                    Geri
                </Button>

                <Space>
                    {currentStep < 4 && (
                        <Button
                            type="primary"
                            icon={<ArrowRightOutlined />}
                            onClick={nextStep}
                        >
                            İleri
                        </Button>
                    )}

                    {currentStep === 4 && (
                        <Button
                            type="primary"
                            icon={<SendOutlined />}
                            onClick={handleSubmit}
                            loading={saving}
                            size="large"
                        >
                            Formu Gönder
                        </Button>
                    )}
                </Space>
            </div>
        </div>
    )
}

export default ServiceFormWizard


















