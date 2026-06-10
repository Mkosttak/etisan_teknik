import { useState, useEffect, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Form, Input, Select, App, Space, Tag, Card, Button, Checkbox, InputNumber, Alert, Spin, Divider, Row, Col, Typography } from 'antd'
import {
    PlusOutlined,
    SearchOutlined,
    ShoppingOutlined,
    WarningOutlined,
    DeleteOutlined,
    EnvironmentOutlined,
    UserOutlined,
    ArrowLeftOutlined,
    SaveOutlined,
    StarFilled
} from '@ant-design/icons'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import { turkishCitiesAndDistricts } from '../data/turkishCities'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { Title, Text } = Typography

const RequestForm = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const [form] = Form.useForm()

    const [loading, setLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [stockItems, setStockItems] = useState([])
    const [selectedStockItems, setSelectedStockItems] = useState({})
    const [stockSearchText, setStockSearchText] = useState('')
    const [extraItemsList, setExtraItemsList] = useState([])
    const [extraItemQuantity, setExtraItemQuantity] = useState(1)

    // Edit specific states
    const [editingRequest, setEditingRequest] = useState(null)
    const [originalStockItems, setOriginalStockItems] = useState([])
    const [originalExtraItems, setOriginalExtraItems] = useState([])
    const [cancelledStockItemIds, setCancelledStockItemIds] = useState([])
    const [cancelledExtraItemIndexes, setCancelledExtraItemIndexes] = useState([])

    // Address auto-fill states
    const [institutionAddresses, setInstitutionAddresses] = useState([])
    const [addressLoading, setAddressLoading] = useState(false)
    const [selectedAddressId, setSelectedAddressId] = useState(null)
    const [useDifferentAddress, setUseDifferentAddress] = useState(false)
    const [saveNewAddress, setSaveNewAddress] = useState(false)
    const [newAddressLabel, setNewAddressLabel] = useState('')
    const [selectedCity, setSelectedCity] = useState(null)
    const [availableDistricts, setAvailableDistricts] = useState([])

    const trLower = (value) => (value || '').toLocaleLowerCase('tr-TR')

    const normalizePhoneNumber = (value) => {
        if (!value || value.trim() === '') return ''
        const onlyNums = value.replace(/[^\d]/g, '')
        if (onlyNums.length <= 4) return onlyNums
        if (onlyNums.length <= 7) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4)}`
        if (onlyNums.length <= 9) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7)}`
        return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7, 9)} ${onlyNums.slice(9, 11)}`
    }

    useEffect(() => {
        fetchStockItems()
        if (id) {
            fetchRequestDetail(id)
        }
    }, [id])

    const fetchStockItems = async () => {
        try {
            const response = await api.get('/stock')
            setStockItems(response.data.filter(item => item.isActive))
        } catch (error) {
            message.error('Stok kalemleri yüklenemedi')
        }
    }

    const fetchRequestDetail = async (requestId) => {
        try {
            setLoading(true)
            const response = await api.get(`/requests/${requestId}`)
            const request = response.data
            setEditingRequest(request)

            // Setup form values
            let instName = request.institutionName
            let instOther = request.institutionOtherName
            if (instOther && instOther.trim()) {
                instName = 'DİĞER'
            }

            form.setFieldsValue({
                description: request.description,
                institutionName: instName,
                institutionOtherName: instOther,
                contactPerson: request.contactPerson,
                contactPhone: request.contactPhone,
                contactEmail: request.contactEmail,
                shippingAddressLine: request.shippingAddressLine,
                shippingCity: request.shippingCity,
                shippingDistrict: request.shippingDistrict,
                isUrgent: request.isUrgent,
            })

            // City/District setup
            if (request.shippingCity && turkishCitiesAndDistricts[request.shippingCity]) {
                setSelectedCity(request.shippingCity)
                setAvailableDistricts(turkishCitiesAndDistricts[request.shippingCity])
            }

            // Stock items setup
            if (request.stockItems && request.stockItems.length > 0) {
                setOriginalStockItems(request.stockItems)
                const stockItemsMap = {}
                request.stockItems.forEach(item => {
                    if (!item.isCancelled) {
                        stockItemsMap[item.stockItemId] = {
                            quantity: item.quantity,
                            requestStockItemId: item.id
                        }
                    }
                })
                setSelectedStockItems(stockItemsMap)
            }

            // Extra items setup
            if (request.extraItems) {
                const extraItems = request.extraItems.split(', ').map(item => {
                    const match = item.match(/^(\d+)x\s+(.+)$/)
                    if (match) {
                        return { name: match[2].trim(), quantity: parseInt(match[1]) }
                    }
                    return { name: item.trim(), quantity: 1 }
                })
                setOriginalExtraItems(extraItems)
                setExtraItemsList(extraItems)
            }

            // Fetch addresses for the institution
            if (instName && instName !== 'DİĞER') {
                handleInstitutionChange(instName, true)
            }

        } catch (error) {
            message.error('Talep detayları yüklenirken hata oluştu')
            navigate('/requests')
        } finally {
            setLoading(false)
        }
    }

    const handleInstitutionChange = useCallback(async (institutionName, isInitialLoad = false) => {
        if (!isInitialLoad) {
            setInstitutionAddresses([])
            setSelectedAddressId(null)
            setUseDifferentAddress(false)
            setSaveNewAddress(false)

            form.setFieldsValue({
                shippingCity: null,
                shippingDistrict: null,
                shippingAddressLine: null,
                contactPerson: null,
                contactPhone: '',
                contactEmail: null
            })
            setSelectedCity(null)
            setAvailableDistricts([])
        }

        if (!institutionName || institutionName === 'DİĞER') return

        try {
            setAddressLoading(true)
            const response = await api.get(`/institutions/by-name/${encodeURIComponent(institutionName)}/addresses`)
            const addresses = response.data || []
            setInstitutionAddresses(addresses)

            if (!isInitialLoad) {
                const defaultAddr = addresses.find(a => a.isDefault)
                if (defaultAddr) {
                    applyAddressToForm(defaultAddr)
                    setSelectedAddressId(defaultAddr.id)
                    setUseDifferentAddress(false)
                } else {
                    // Favori adres yoksa veya hiç adres yoksa
                    setUseDifferentAddress(true)
                    setSelectedAddressId(null)
                    // Alanları temizle (isteğe bağlı, zaten yukarda temizleniyor ama garantiye alalım)
                    form.setFieldsValue({
                        shippingCity: null,
                        shippingDistrict: null,
                        shippingAddressLine: null,
                        contactPerson: null,
                        contactPhone: '',
                        contactEmail: null
                    })
                }
            } else {
                // If editing, find if current address matches any registered address
                const currentVals = form.getFieldsValue()
                const matchingAddr = addresses.find(a =>
                    a.city === currentVals.shippingCity &&
                    a.district === currentVals.shippingDistrict &&
                    a.addressLine === currentVals.shippingAddressLine
                )
                if (matchingAddr) {
                    setSelectedAddressId(matchingAddr.id)
                } else {
                    setUseDifferentAddress(true)
                }
            }
        } catch (error) {
            console.error('Failed to fetch addresses:', error)
        } finally {
            setAddressLoading(false)
        }
    }, [form])

    const applyAddressToForm = useCallback((address) => {
        form.setFieldsValue({
            shippingCity: address.city || null,
            shippingDistrict: address.district || null,
            shippingAddressLine: address.addressLine || null,
            contactPerson: address.contactPerson || null,
            contactPhone: address.phone || '',
            contactEmail: address.email || null
        })
        if (address.city && turkishCitiesAndDistricts[address.city]) {
            setSelectedCity(address.city)
            setAvailableDistricts(turkishCitiesAndDistricts[address.city])
        }
    }, [form])

    const handleAddressSelect = (addressId) => {
        if (addressId === 'different') {
            setUseDifferentAddress(true)
            setSelectedAddressId(null)
            form.setFieldsValue({
                shippingCity: null,
                shippingDistrict: null,
                shippingAddressLine: null,
                contactPerson: null,
                contactPhone: '',
                contactEmail: null
            })
            setSelectedCity(null)
            setAvailableDistricts([])
        } else {
            setUseDifferentAddress(false)
            setSelectedAddressId(addressId)
            const address = institutionAddresses.find(a => a.id === addressId)
            if (address) applyAddressToForm(address)
        }
    }

    const handleCityChange = (city) => {
        setSelectedCity(city)
        if (city && turkishCitiesAndDistricts[city]) {
            setAvailableDistricts(turkishCitiesAndDistricts[city])
            form.setFieldsValue({ shippingDistrict: null })
        } else {
            setAvailableDistricts([])
        }
    }

    const handleStockItemToggle = (stockItemId, checked, requestStockItemId = null) => {
        if (checked) {
            let finalRequestStockItemId = requestStockItemId
            if (editingRequest && !finalRequestStockItemId) {
                const originalItem = originalStockItems.find(item => item.stockItemId === stockItemId && !item.isCancelled)
                if (originalItem) finalRequestStockItemId = originalItem.id
            }

            setSelectedStockItems(prev => ({
                ...prev,
                [stockItemId]: {
                    quantity: prev[stockItemId]?.quantity || 1,
                    requestStockItemId: finalRequestStockItemId || prev[stockItemId]?.requestStockItemId
                }
            }))
            if (editingRequest && finalRequestStockItemId) {
                setCancelledStockItemIds(prev => prev.filter(id => id !== finalRequestStockItemId))
            }
        } else {
            const newSelected = { ...selectedStockItems }
            const currentItem = newSelected[stockItemId]
            delete newSelected[stockItemId]
            setSelectedStockItems(newSelected)

            if (editingRequest && currentItem?.requestStockItemId) {
                setCancelledStockItemIds(prev => prev.includes(currentItem.requestStockItemId) ? prev : [...prev, currentItem.requestStockItemId])
            }
        }
    }

    const handleStockItemQuantityChange = (stockItemId, quantity) => {
        if (quantity && quantity > 0) {
            setSelectedStockItems(prev => ({
                ...prev,
                [stockItemId]: {
                    ...prev[stockItemId],
                    quantity
                }
            }))
        }
    }

    const handleAddExtraItem = () => {
        if (stockSearchText.trim()) {
            const itemName = stockSearchText.trim()
            const exists = extraItemsList.some(item => trLower(item.name) === trLower(itemName))

            if (!exists) {
                setExtraItemsList(prev => [...prev, { name: itemName, quantity: extraItemQuantity || 1 }])
                setStockSearchText('')
                setExtraItemQuantity(1)
                message.success('Ürün eklendi')
            } else {
                message.warning('Bu ürün zaten eklenmiş')
            }
        }
    }

    const handleRemoveExtraItem = (index) => {
        if (editingRequest && index < originalExtraItems.length) {
            setCancelledExtraItemIndexes(prev => prev.includes(index) ? prev : [...prev, index])
        }
        setExtraItemsList(prev => prev.filter((_, i) => i !== index))
    }

    const handleUpdateExtraItemQuantity = (index, quantity) => {
        if (quantity && quantity > 0) {
            setExtraItemsList(prev => prev.map((item, i) => i === index ? { ...item, quantity } : item))
        }
    }

    const handleSubmit = async (values) => {
        try {
            setSaving(true)
            const stockItemsArray = Object.entries(selectedStockItems).map(([stockItemId, itemData]) => ({
                stockItemId: parseInt(stockItemId),
                quantity: itemData.quantity
            }))

            const extraItemsText = extraItemsList.length > 0
                ? extraItemsList.map(item => `${item.quantity}x ${item.name}`).join(', ')
                : null

            const requestData = {
                ...values,
                stockItems: stockItemsArray,
                extraItems: extraItemsText
            }

            if (editingRequest) {
                requestData.cancelledStockItemIds = cancelledStockItemIds
                requestData.cancelledExtraItemIndexes = cancelledExtraItemIndexes
                await api.put(`/requests/${editingRequest.id}`, requestData)
                message.success('Talep başarıyla güncellendi')
            } else {
                await api.post('/requests', requestData)
                message.success('Talep başarıyla oluşturuldu')
            }

            // Save new address if requested
            if (saveNewAddress && values.institutionName && values.institutionName !== 'DİĞER') {
                try {
                    await api.post(`/institutions/by-name/${encodeURIComponent(values.institutionName)}/addresses`, {
                        label: newAddressLabel || 'Yeni Adres',
                        city: values.shippingCity,
                        district: values.shippingDistrict,
                        addressLine: values.shippingAddressLine,
                        contactPerson: values.contactPerson,
                        phone: values.contactPhone,
                        email: values.contactEmail,
                        isDefault: institutionAddresses.length === 0
                    })
                } catch (e) {
                    console.error('Address save failed', e)
                }
            }

            navigate('/requests')
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setSaving(false)
        }
    }

    if (loading) return <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}><Spin size="large" /></div>

    return (
        <div style={{ maxWidth: '100%', margin: '0 auto', padding: '24px 16px' }}>
            <div style={{ marginBottom: 24, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Space size={16}>
                    <Button
                        icon={<ArrowLeftOutlined />}
                        onClick={() => navigate('/requests')}
                        style={{ borderRadius: 8 }}
                    >
                        Geri
                    </Button>
                    <Title level={2} style={{ margin: 0 }}>
                        {id ? 'Talebi Düzenle' : 'Yeni Talep Oluştur'}
                    </Title>
                </Space>
                <Button
                    type="primary"
                    icon={<SaveOutlined />}
                    size="large"
                    onClick={() => form.submit()}
                    loading={saving}
                    style={{ borderRadius: 8, height: 45, padding: '0 32px' }}
                >
                    {id ? 'Güncelle' : 'Kaydet'}
                </Button>
            </div>

            <Form form={form} layout="vertical" onFinish={handleSubmit}>
                <Row gutter={24}>
                    <Col xs={24} lg={14}>
                        <Card title={<Space><EnvironmentOutlined /><span>Kurum ve Talep Bilgileri</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item name="institutionName" label="Kurum">
                                <InstitutionSelect placeholder="Kurum seçin" allowClear onChange={handleInstitutionChange} />
                            </Form.Item>

                            <Form.Item noStyle shouldUpdate={(prev, curr) => prev.institutionName !== curr.institutionName}>
                                {({ getFieldValue }) => getFieldValue('institutionName') === 'DİĞER' && (
                                    <Form.Item name="institutionOtherName" label="Kurum Adı (Diğer)">
                                        <Input placeholder="Kurum adını yazın" />
                                    </Form.Item>
                                )}
                            </Form.Item>

                            <Form.Item name="description" label="Açıklama">
                                <Input.TextArea rows={4} placeholder="Talebin detaylarını giriniz..." />
                            </Form.Item>

                            <Form.Item name="isUrgent" valuePropName="checked">
                                <Checkbox style={{
                                    padding: '12px 16px',
                                    background: isDarkMode ? 'rgba(255, 77, 79, 0.1)' : '#fff1f0',
                                    border: '1px solid #ffa39e',
                                    borderRadius: 8,
                                    width: '100%',
                                    color: '#f5222d',
                                    fontWeight: 600
                                }}>
                                    Bu talep acildir
                                </Checkbox>
                            </Form.Item>
                        </Card>

                        <Card title={<Space><ShoppingOutlined /><span>Ürün Seçimi</span></Space>} style={{ borderRadius: 12 }}>
                            <Alert
                                message="Stok Bilgisi"
                                description="Seçilen ürünler stoktan düşülecektir. Listede olmayan ürünler için manuel giriş yapabilirsiniz."
                                type="info"
                                showIcon
                                style={{ marginBottom: 20 }}
                            />

                            <Input
                                placeholder="Ürün adı veya seri no ile ara..."
                                prefix={<SearchOutlined />}
                                value={stockSearchText}
                                onChange={(e) => setStockSearchText(e.target.value)}
                                allowClear
                                style={{ marginBottom: 16, height: 40, borderRadius: 8 }}
                            />

                            {stockSearchText.trim() && (() => {
                                const searchLower = trLower(stockSearchText).trim()
                                const matchedItems = stockItems.filter(item =>
                                    trLower(item.itemName).includes(searchLower) ||
                                    trLower(item.serialNumber).includes(searchLower)
                                )

                                if (matchedItems.length > 0) {
                                    return (
                                        <div style={{ maxHeight: '250px', overflowY: 'auto' }}>
                                            {matchedItems.map(item => {
                                                const isSelected = selectedStockItems[item.id] !== undefined
                                                const isLowStock = item.currentStock <= item.minimumStock
                                                const itemData = selectedStockItems[item.id]
                                                const selectedQuantity = itemData?.quantity || 1

                                                return (
                                                    <div
                                                        key={item.id}
                                                        style={{
                                                            padding: '12px',
                                                            marginBottom: 8,
                                                            borderRadius: 8,
                                                            border: `1px solid ${isSelected ? '#1890ff' : (isDarkMode ? '#333' : '#f0f0f0')}`,
                                                            background: isSelected ? (isDarkMode ? 'rgba(24, 144, 255, 0.1)' : '#e6f7ff') : 'transparent'
                                                        }}
                                                    >
                                                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                                            <Checkbox
                                                                checked={isSelected}
                                                                onChange={(e) => handleStockItemToggle(item.id, e.target.checked)}
                                                            >
                                                                <Space>
                                                                    <span style={{ fontWeight: 500 }}>{item.itemName}</span>
                                                                    {item.serialNumber && <Tag color="blue">{item.serialNumber}</Tag>}
                                                                    <Tag color={isLowStock ? 'orange' : 'green'}>Stok: {item.currentStock}</Tag>
                                                                </Space>
                                                            </Checkbox>
                                                            {isSelected && (
                                                                <InputNumber
                                                                    min={1}
                                                                    value={selectedQuantity}
                                                                    onChange={(val) => handleStockItemQuantityChange(item.id, val)}
                                                                    style={{ width: 70 }}
                                                                />
                                                            )}
                                                        </div>
                                                    </div>
                                                )
                                            })}
                                        </div>
                                    )
                                } else {
                                    return (
                                        <div style={{ padding: '16px', border: '1px dashed #d9d9d9', borderRadius: 8, textAlign: 'center' }}>
                                            <div style={{ marginBottom: 12 }}>"{stockSearchText}" stokta bulunamadı.</div>
                                            <Space>
                                                <InputNumber min={1} value={extraItemQuantity} onChange={setExtraItemQuantity} style={{ width: 70 }} />
                                                <Button type="primary" size="small" icon={<PlusOutlined />} onClick={handleAddExtraItem}>Elde Olmayan Ürün Olarak Ekle</Button>
                                            </Space>
                                        </div>
                                    )
                                }
                            })()}

                            {/* Selection Summary */}
                            {(Object.keys(selectedStockItems).length > 0 || extraItemsList.length > 0) && (
                                <div style={{ marginTop: 20, padding: 12, background: isDarkMode ? '#1a1a1a' : '#fafafa', borderRadius: 8 }}>
                                    <Text strong>Seçilen Ürünler:</Text>
                                    <div style={{ marginTop: 8, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                                        {Object.entries(selectedStockItems).map(([id, data]) => {
                                            const item = stockItems.find(s => s.id === parseInt(id))
                                            return item && (
                                                <Tag key={id} closable onClose={() => handleStockItemToggle(id, false)} color="blue" style={{ padding: '4px 8px', borderRadius: 6 }}>
                                                    {item.itemName} x{data.quantity}
                                                </Tag>
                                            )
                                        })}
                                        {extraItemsList.map((item, i) => (
                                            <Tag key={i} closable onClose={() => handleRemoveExtraItem(i)} color="orange" style={{ padding: '4px 8px', borderRadius: 6 }}>
                                                {item.name} x{item.quantity}
                                            </Tag>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </Card>
                    </Col>

                    <Col xs={24} lg={10}>
                        <Card title={<Space><EnvironmentOutlined /><span>Sevkiyat Adresi</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            {addressLoading ? <div style={{ textAlign: 'center', padding: 20 }}><Spin /></div> : (
                                <>
                                    {institutionAddresses.length > 0 && (
                                        <div style={{ marginBottom: 20, padding: 12, background: isDarkMode ? 'rgba(24, 144, 255, 0.1)' : '#e6f7ff', borderRadius: 8 }}>
                                            <Text strong style={{ display: 'block', marginBottom: 8 }}>Sistemde Kayıtlı Adresler</Text>
                                            <Select
                                                style={{ width: '100%' }}
                                                placeholder="Adres seçin"
                                                value={useDifferentAddress ? 'different' : selectedAddressId}
                                                onChange={handleAddressSelect}
                                            >
                                                {institutionAddresses.map(addr => (
                                                    <Select.Option key={addr.id} value={addr.id}>
                                                        {addr.isDefault && <StarFilled style={{ color: '#faad14', marginRight: 4 }} />}
                                                        {addr.label} ({addr.city})
                                                    </Select.Option>
                                                ))}
                                                <Select.Option value="different">+ Farklı Adres Gir</Select.Option>
                                            </Select>
                                        </div>
                                    )}

                                    <Form.Item name="shippingAddressLine" label="Adres Satırı">
                                        <Input.TextArea rows={3} placeholder="Tam adres..." disabled={selectedAddressId && !useDifferentAddress} />
                                    </Form.Item>

                                    <Row gutter={12}>
                                        <Col span={12}>
                                            <Form.Item name="shippingCity" label="Şehir">
                                                <Select showSearch placeholder="Şehir" onChange={handleCityChange} disabled={selectedAddressId && !useDifferentAddress}>
                                                    {Object.keys(turkishCitiesAndDistricts).map(c => <Select.Option key={c} value={c}>{c}</Select.Option>)}
                                                </Select>
                                            </Form.Item>
                                        </Col>
                                        <Col span={12}>
                                            <Form.Item name="shippingDistrict" label="İlçe">
                                                <Select showSearch placeholder="İlçe" disabled={!selectedCity || (selectedAddressId && !useDifferentAddress)}>
                                                    {availableDistricts.map(d => <Select.Option key={d} value={d}>{d}</Select.Option>)}
                                                </Select>
                                            </Form.Item>
                                        </Col>
                                    </Row>
                                </>
                            )}
                        </Card>

                        <Card title={<Space><UserOutlined /><span>İletişim</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item name="contactPerson" label="İletişim Kişisi">
                                <Input placeholder="Ad Soyad" disabled={selectedAddressId && !useDifferentAddress} />
                            </Form.Item>
                            <Form.Item name="contactPhone" label="Telefon" normalize={normalizePhoneNumber}>
                                <Input placeholder="05XX XXX XX XX" maxLength={14} disabled={selectedAddressId && !useDifferentAddress} />
                            </Form.Item>
                            <Form.Item name="contactEmail" label="E-posta" rules={[{ type: 'email', message: 'Geçerli bir e-posta girin' }]}>
                                <Input placeholder="örnek@eposta.com" disabled={selectedAddressId && !useDifferentAddress} />
                            </Form.Item>

                            {/* Save New Address Option */}
                            <Form.Item noStyle shouldUpdate={(prev, curr) => prev.institutionName !== curr.institutionName}>
                                {({ getFieldValue }) => {
                                    const inst = getFieldValue('institutionName')
                                    return (useDifferentAddress || institutionAddresses.length === 0) && inst && inst !== 'DİĞER' && (
                                        <div style={{ marginTop: 16, padding: 12, border: '1px solid #d9f7be', background: '#f6ffed', borderRadius: 8 }}>
                                            <Checkbox checked={saveNewAddress} onChange={e => setSaveNewAddress(e.target.checked)}>
                                                Bu adresi kurum için kaydet
                                            </Checkbox>
                                            {saveNewAddress && (
                                                <Input
                                                    placeholder="Adres Etiketi (Örn: Merkez)"
                                                    value={newAddressLabel}
                                                    onChange={e => setNewAddressLabel(e.target.value)}
                                                    style={{ marginTop: 8 }}
                                                />
                                            )}
                                        </div>
                                    )
                                }}
                            </Form.Item>
                        </Card>
                    </Col>
                </Row>
            </Form>
        </div>
    )
}

export default RequestForm
