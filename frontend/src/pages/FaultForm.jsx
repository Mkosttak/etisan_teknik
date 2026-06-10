import { useState, useEffect, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Form, Input, Select, App, Space, Tag, Card, Button, Checkbox, Spin, Row, Col, Typography, Tooltip } from 'antd'
import {
    PlusOutlined,
    EnvironmentOutlined,
    ArrowLeftOutlined,
    SaveOutlined,
    StarFilled,
    ToolOutlined,
    CheckCircleOutlined
} from '@ant-design/icons'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import { turkishCitiesAndDistricts } from '../data/turkishCities'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { Title, Text } = Typography

const FaultForm = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const [form] = Form.useForm()
    const institutionName = Form.useWatch('institutionName', form)

    const [loading, setLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [editingFault, setEditingFault] = useState(null)

    // Address auto-fill states
    const [institutionAddresses, setInstitutionAddresses] = useState([])
    const [addressLoading, setAddressLoading] = useState(false)
    const [selectedAddressId, setSelectedAddressId] = useState(null)
    const [useDifferentAddress, setUseDifferentAddress] = useState(false)
    const [saveNewAddress, setSaveNewAddress] = useState(false)
    const [newAddressLabel, setNewAddressLabel] = useState('')
    const [selectedCity, setSelectedCity] = useState(null)
    const [availableDistricts, setAvailableDistricts] = useState([])

    const normalizeSerialNumber = (value) => {
        if (!value) return value
        const onlyNums = value.replace(/\D/g, '').slice(0, 9)
        if (onlyNums.length <= 3) return onlyNums
        if (onlyNums.length <= 6) return `${onlyNums.slice(0, 3)}-${onlyNums.slice(3)}`
        return `${onlyNums.slice(0, 3)}-${onlyNums.slice(3, 6)}-${onlyNums.slice(6, 9)}`
    }

    const normalizePhoneNumber = (value) => {
        if (!value) return value
        const onlyNums = value.replace(/[^\d]/g, '')
        if (onlyNums.length <= 4) return onlyNums
        if (onlyNums.length <= 7) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4)}`
        if (onlyNums.length <= 9) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7)}`
        return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7, 9)} ${onlyNums.slice(9, 11)}`
    }

    useEffect(() => {
        if (id) {
            fetchFaultDetail(id)
        }
    }, [id])

    const fetchFaultDetail = async (faultId) => {
        try {
            setLoading(true)
            const response = await api.get(`/faults/${faultId}`)
            const fault = response.data
            setEditingFault(fault)

            form.setFieldsValue({
                institutionName: fault.institutionName,
                institutionOtherName: fault.institutionOtherName,
                deviceSerialNumber: fault.deviceSerialNumber,
                description: fault.description,
                shippingAddressLine: fault.shippingAddressLine,
                shippingCity: fault.shippingCity,
                shippingDistrict: fault.shippingDistrict,
                contactPerson: fault.contactPerson,
                contactPhone: fault.contactPhone,
                contactEmail: fault.contactEmail,
                sendBackupDevice: fault.sendBackupDevice || false,
                createDraftServiceForm: false,
            })

            if (fault.shippingCity && turkishCitiesAndDistricts[fault.shippingCity]) {
                setSelectedCity(fault.shippingCity)
                setAvailableDistricts(turkishCitiesAndDistricts[fault.shippingCity])
            }

            if (fault.institutionName && fault.institutionName !== 'DİĞER') {
                handleInstitutionChange(fault.institutionName, true)
            }
        } catch (error) {
            message.error('Arıza detayları yüklenirken hata oluştu')
            navigate('/faults')
        } finally {
            setLoading(false)
        }
    }

    const handleInstitutionChange = useCallback(async (name, isInitialLoad = false) => {
        if (!isInitialLoad) {
            setInstitutionAddresses([])
            setSelectedAddressId(null)
            setUseDifferentAddress(false)
            setSaveNewAddress(false)
            form.setFieldsValue({
                shippingCity: null, shippingDistrict: null, shippingAddressLine: null,
                contactPerson: null, contactPhone: null, contactEmail: null
            })
            setSelectedCity(null)
            setAvailableDistricts([])
        }

        if (!name || name === 'DİĞER') return

        try {
            setAddressLoading(true)
            const response = await api.get(`/institutions/by-name/${encodeURIComponent(name)}/addresses`)
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
                    // Alanları temizle
                    form.setFieldsValue({
                        shippingCity: null,
                        shippingDistrict: null,
                        shippingAddressLine: null,
                        contactPerson: null,
                        contactPhone: null,
                        contactEmail: null
                    })
                }
            } else {
                const vals = form.getFieldsValue()
                const matching = addresses.find(a => a.city === vals.shippingCity && a.district === vals.shippingDistrict && a.addressLine === vals.shippingAddressLine)
                if (matching) setSelectedAddressId(matching.id)
                else setUseDifferentAddress(true)
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
            contactPhone: address.phone || null,
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
                shippingCity: null, shippingDistrict: null, shippingAddressLine: null,
                contactPerson: null, contactPhone: null, contactEmail: null
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

    const handleSubmit = async (values) => {
        try {
            setSaving(true)
            if (editingFault) {
                await api.put(`/faults/${editingFault.id}`, values)
                message.success('Arıza güncellendi')
            } else {
                await api.post('/faults', values)
                message.success('Arıza kaydı oluşturuldu')
            }

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

            navigate('/faults')
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
                        onClick={() => navigate('/faults')}
                        style={{ borderRadius: 8 }}
                    >
                        Geri
                    </Button>
                    <Title level={2} style={{ margin: 0 }}>
                        {id ? 'Arızayı Düzenle' : 'Yeni Arıza Kaydı'}
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
                        <Card title={<Space><ToolOutlined /><span>Cihaz ve Arıza Bilgileri</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
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

                            <Form.Item
                                name="deviceSerialNumber"
                                label="Cihaz Seri No"
                                normalize={normalizeSerialNumber}
                                rules={[{ min: 11, message: '9 haneli olmalıdır (XXX-XXX-XXX)' }]}
                            >
                                <Input placeholder="XXX-XXX-XXX" maxLength={11} />
                            </Form.Item>

                            <Form.Item name="description" label="Arıza Açıklaması">
                                <Input.TextArea rows={4} placeholder="Arıza detaylarını giriniz..." />
                            </Form.Item>

                            <Space direction="vertical" style={{ width: '100%' }}>
                                <Card size="small" style={{ border: '1px solid #1890ff', background: isDarkMode ? 'rgba(24, 144, 255, 0.05)' : '#e6f7ff' }}>
                                    <Form.Item name="sendBackupDevice" valuePropName="checked" style={{ marginBottom: 0 }}>
                                        <Checkbox>
                                            <strong>Yedek cihaz gönderilecek mi?</strong>
                                            <div style={{ fontSize: 12, color: isDarkMode ? 'rgba(255,255,255,0.6)' : '#595959', marginTop: 4, fontWeight: 'normal' }}>
                                                İşaretlerseniz otomatik olarak 1 adet POS cihazı talebi oluşturulacaktır.
                                            </div>
                                        </Checkbox>
                                    </Form.Item>
                                </Card>

                                <Card size="small" style={{ border: '1px solid #52c41a', background: isDarkMode ? 'rgba(82, 196, 26, 0.05)' : '#f6ffed' }}>
                                    <Form.Item name="createDraftServiceForm" valuePropName="checked" style={{ marginBottom: 0 }}>
                                        <Checkbox>
                                            <strong>Taslak Form oluşturulsun mu?</strong>
                                            <div style={{ fontSize: 12, color: isDarkMode ? 'rgba(255,255,255,0.6)' : '#595959', marginTop: 4, fontWeight: 'normal' }}>
                                                İşaretlerseniz otomatik olarak taslak servis formu oluşturulacaktır.
                                            </div>
                                        </Checkbox>
                                    </Form.Item>
                                </Card>
                            </Space>
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

                        <Card title={<Space><EnvironmentOutlined /><span>İletişim Bilgileri</span></Space>} style={{ borderRadius: 12 }}>
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
                            {(useDifferentAddress || institutionAddresses.length === 0) && institutionName && institutionName !== 'DİĞER' && (
                                <div style={{ marginTop: 16, padding: 12, border: '1px solid #d9f7be', background: isDarkMode ? 'rgba(82, 196, 26, 0.1)' : '#f6ffed', borderRadius: 8 }}>
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
                            )}
                        </Card>
                    </Col>
                </Row>
            </Form>
        </div>
    )
}

export default FaultForm
