import { useState, useEffect } from 'react'
import { Table, Button, Modal, Form, Input, Select, Space, Popconfirm, Tag, Tooltip, App, Card, Row, Col, Switch, Tabs, List, Empty, Grid, Divider, Checkbox } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, BankOutlined, SearchOutlined, ReloadOutlined, EnvironmentOutlined, StarOutlined, StarFilled, PhoneOutlined, MailOutlined, UserOutlined, CloseOutlined, DatabaseOutlined } from '@ant-design/icons'
import api from '../services/api'
import { turkishCitiesAndDistricts } from '../data/turkishCities'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { useBreakpoint } = Grid

// Telefon No Formatlama Fonksiyonu (05XX XXX XX XX)
const normalizePhoneNumber = (value) => {
    if (!value) return value
    const onlyNums = value.replace(/[^\d]/g, '')
    if (onlyNums.length <= 4) return onlyNums
    if (onlyNums.length <= 7) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4)}`
    if (onlyNums.length <= 9) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7)}`
    return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7, 9)} ${onlyNums.slice(9, 11)}`
}

const InstitutionManagement = () => {
    const { message } = App.useApp()
    const { isDarkMode } = useTheme()
    const screens = useBreakpoint()
    const isMobile = !screens.md

    const [institutions, setInstitutions] = useState([])
    const [loading, setLoading] = useState(false)
    const [modalVisible, setModalVisible] = useState(false)
    const [editingInstitution, setEditingInstitution] = useState(null)
    const [search, setSearch] = useState('')
    const [form] = Form.useForm()

    // Address states
    const [selectedInstitution, setSelectedInstitution] = useState(null)
    const [addresses, setAddresses] = useState([])
    const [addressLoading, setAddressLoading] = useState(false)
    const [addressModalVisible, setAddressModalVisible] = useState(false)
    const [editingAddress, setEditingAddress] = useState(null)
    const [addressForm] = Form.useForm()
    const [selectedCity, setSelectedCity] = useState(null)
    const [availableDistricts, setAvailableDistricts] = useState([])
    const [activeTab, setActiveTab] = useState('institutions')

    // ⭐ PERFORMANCE: Debounced search with AbortController cleanup
    // İlk yükleme anında, sonraki aramalar debounced
    useEffect(() => {
        const abortController = new AbortController()

        const loadData = async () => {
            try {
                setLoading(true)
                const response = await api.get('/institutions/manage', {
                    params: { search: search || undefined },
                    signal: abortController.signal
                })
                // Filter out DİĞER from the list
                const filteredData = response.data.filter(inst => inst.name !== 'DİĞER')
                setInstitutions(filteredData)
            } catch (error) {
                if (error.name !== 'CanceledError' && error.name !== 'AbortError') {
                    message.error(getErrorMessage(error))
                }
            } finally {
                setLoading(false)
            }
        }

        // İlk yükleme (search boş) -> anında yükle
        // Sonraki aramalar -> 300ms debounce
        if (search === '') {
            loadData()
        } else {
            const timer = setTimeout(loadData, 300)
            return () => {
                clearTimeout(timer)
                abortController.abort()
            }
        }

        return () => abortController.abort()
    }, [search])

    const fetchInstitutions = async () => {
        try {
            setLoading(true)
            const response = await api.get('/institutions/manage', {
                params: { search: search || undefined }
            })
            // Filter out DİĞER from the list
            const filteredData = response.data.filter(inst => inst.name !== 'DİĞER')
            setInstitutions(filteredData)
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setLoading(false)
        }
    }

    const fetchAddresses = async (institutionId) => {
        try {
            setAddressLoading(true)
            const response = await api.get(`/institutions/${institutionId}/addresses`)
            setAddresses(response.data)
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setAddressLoading(false)
        }
    }

    const handleAdd = () => {
        setEditingInstitution(null)
        form.resetFields()
        form.setFieldsValue({ isActive: true })
        setModalVisible(true)
    }

    const handleEdit = (institution) => {
        setEditingInstitution(institution)
        form.setFieldsValue({
            name: institution.name,
            isActive: institution.isActive
        })
        setModalVisible(true)
    }

    const handleDelete = async (id) => {
        try {
            await api.delete(`/institutions/manage/${id}`)
            message.success('Kurum silindi')
            fetchInstitutions()
            if (selectedInstitution?.id === id) {
                setSelectedInstitution(null)
                setAddresses([])
            }
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const handleSubmit = async (values) => {
        try {
            if (editingInstitution) {
                await api.put(`/institutions/manage/${editingInstitution.id}`, values)
                message.success('Kurum güncellendi')
            } else {
                await api.post('/institutions/manage', values)
                message.success('Kurum oluşturuldu')
            }
            setModalVisible(false)
            form.resetFields()
            fetchInstitutions()
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    // Address handlers
    const handleSelectInstitution = (institution) => {
        setSelectedInstitution(institution)
        fetchAddresses(institution.id)
        setActiveTab('addresses')
    }

    const handleAddAddress = () => {
        setEditingAddress(null)
        addressForm.resetFields()
        addressForm.setFieldsValue({ isDefault: false })
        setSelectedCity(null)
        setAvailableDistricts([])
        setAddressModalVisible(true)
    }

    const handleEditAddress = (address) => {
        setEditingAddress(address)
        if (address.city && turkishCitiesAndDistricts[address.city]) {
            setSelectedCity(address.city)
            setAvailableDistricts(turkishCitiesAndDistricts[address.city])
        }
        addressForm.setFieldsValue({
            label: address.label,
            city: address.city,
            district: address.district,
            addressLine: address.addressLine,
            contactPerson: address.contactPerson,
            phone: address.phone,
            email: address.email,
            isDefault: address.isDefault
        })
        setAddressModalVisible(true)
    }

    const handleDeleteAddress = async (id) => {
        try {
            await api.delete(`/institutions/addresses/${id}`)
            message.success('Adres silindi')
            fetchAddresses(selectedInstitution.id)
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const handleAddressSubmit = async (values) => {
        try {
            if (editingAddress) {
                await api.put(`/institutions/addresses/${editingAddress.id}`, values)
                message.success('Adres güncellendi')
            } else {
                await api.post(`/institutions/${selectedInstitution.id}/addresses`, values)
                message.success('Adres oluşturuldu')
            }
            setAddressModalVisible(false)
            addressForm.resetFields()
            fetchAddresses(selectedInstitution.id)
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const handleCityChange = (city) => {
        setSelectedCity(city)
        if (city && turkishCitiesAndDistricts[city]) {
            setAvailableDistricts(turkishCitiesAndDistricts[city])
            addressForm.setFieldsValue({ district: null })
        } else {
            setAvailableDistricts([])
        }
    }

    const columns = [
        {
            title: 'Kurum Adı',
            dataIndex: 'name',
            key: 'name',
            render: (name, record) => (
                <Button
                    type="link"
                    style={{ padding: 0, fontWeight: 600, color: '#1890ff' }}
                    onClick={() => handleSelectInstitution(record)}
                >
                    <Space>
                        <BankOutlined />
                        {name}
                    </Space>
                </Button>
            )
        },
        {
            title: 'Durum',
            dataIndex: 'isActive',
            key: 'isActive',
            width: 100,
            render: (isActive) => (
                <Tag color={isActive ? 'success' : 'default'}>
                    {isActive ? 'Aktif' : 'Pasif'}
                </Tag>
            )
        },
        {
            title: 'İşlemler',
            key: 'actions',
            width: 150,
            render: (_, record) => (
                <Space>
                    <Tooltip title="Adresler">
                        <Button
                            type="link"
                            icon={<EnvironmentOutlined />}
                            onClick={() => handleSelectInstitution(record)}
                            style={{ color: '#1890ff' }}
                        />
                    </Tooltip>
                    <Tooltip title="Düzenle">
                        <Button
                            type="link"
                            icon={<EditOutlined />}
                            onClick={() => handleEdit(record)}
                            style={{ color: '#52c41a' }}
                        />
                    </Tooltip>
                    {record.name !== 'DİĞER' && (
                        <Popconfirm
                            title="Kurumu silmek istediğinizden emin misiniz?"
                            onConfirm={() => handleDelete(record.id)}
                            okText="Evet"
                            cancelText="Hayır"
                        >
                            <Tooltip title="Sil">
                                <Button type="link" danger icon={<DeleteOutlined />} />
                            </Tooltip>
                        </Popconfirm>
                    )}
                </Space>
            )
        }
    ]

    const renderInstitutionsTab = () => (
        <>
            {/* Refined Header & Search Area */}
            <div style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                flexWrap: 'wrap',
                gap: 20,
                marginBottom: 24,
                padding: '8px 4px'
            }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                    <div style={{
                        width: 42,
                        height: 42,
                        borderRadius: 12,
                        background: isDarkMode ? '#177ddc' : '#1890ff',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: '#fff',
                        fontSize: 20,
                        boxShadow: '0 4px 12px rgba(24, 144, 255, 0.3)'
                    }}>
                        <DatabaseOutlined />
                    </div>
                    <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                        Kurum Yönetimi
                    </h1>
                </div>

                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 16,
                    flex: 1,
                    justifyContent: 'flex-end',
                    minWidth: 300
                }}>
                    <Input
                        placeholder="Kurum ara..."
                        allowClear
                        prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        style={{
                            maxWidth: 400,
                            height: 42,
                            borderRadius: 12,
                            border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
                            boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                            background: isDarkMode ? '#1f1f1f' : '#fff'
                        }}
                    />
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={handleAdd}
                        style={{
                            height: 42,
                            borderRadius: 12,
                            padding: '0 24px',
                            fontWeight: 600,
                            boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)'
                        }}
                    >
                        Yeni Kurum
                    </Button>
                </div>
            </div>

            <Card style={{ marginBottom: 16 }}>
                <Row gutter={[16, 16]}>
                    <Col xs={8} sm={8}>
                        <div style={{ textAlign: 'center' }}>
                            <div style={{ fontSize: isMobile ? 18 : 24, fontWeight: 600, color: '#1890ff' }}>
                                {institutions.length}
                            </div>
                            <div style={{ color: '#888', fontSize: isMobile ? 11 : 14 }}>Toplam</div>
                        </div>
                    </Col>
                    <Col xs={8} sm={8}>
                        <div style={{ textAlign: 'center' }}>
                            <div style={{ fontSize: isMobile ? 18 : 24, fontWeight: 600, color: '#52c41a' }}>
                                {institutions.filter(i => i.isActive).length}
                            </div>
                            <div style={{ color: '#888', fontSize: isMobile ? 11 : 14 }}>Aktif</div>
                        </div>
                    </Col>
                    <Col xs={8} sm={8}>
                        <div style={{ textAlign: 'center' }}>
                            <div style={{ fontSize: isMobile ? 18 : 24, fontWeight: 600, color: '#faad14' }}>
                                {institutions.filter(i => !i.isActive).length}
                            </div>
                            <div style={{ color: '#888', fontSize: isMobile ? 11 : 14 }}>Pasif</div>
                        </div>
                    </Col>
                </Row>
            </Card>

            <Table
                columns={columns}
                dataSource={institutions}
                rowKey="id"
                loading={loading}
                pagination={{ pageSize: 25, pageSizeOptions: ['25', '50', '100'], showSizeChanger: true }}
                scroll={{ x: true }}
                size={isMobile ? 'small' : 'middle'}
            />
        </>
    )

    const renderAddressCard = (address) => (
        <Card
            key={address.id}
            size="small"
            style={{
                marginBottom: 12,
                border: address.isDefault
                    ? '2px solid #1890ff'
                    : (isDarkMode ? '1px solid #303030' : '1px solid #f0f0f0'),
                borderRadius: 8
            }}
            actions={[
                <Tooltip title="Düzenle" key="edit">
                    <Button type="link" icon={<EditOutlined />} onClick={() => handleEditAddress(address)} style={{ color: '#52c41a' }} />
                </Tooltip>,
                <Popconfirm
                    key="delete"
                    title="Adresi silmek istediğinizden emin misiniz?"
                    onConfirm={() => handleDeleteAddress(address.id)}
                    okText="Evet"
                    cancelText="Hayır"
                >
                    <Button type="link" danger icon={<DeleteOutlined />} />
                </Popconfirm>
            ]}
        >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                    <div style={{ fontWeight: 600, marginBottom: 8, display: 'flex', alignItems: 'center', gap: 8 }}>
                        {address.isDefault && <StarFilled style={{ color: '#faad14' }} />}
                        <span>{address.label || 'Adres'}</span>
                        {address.isDefault && <Tag color="gold" style={{ marginLeft: 4 }}>Varsayılan</Tag>}
                    </div>

                    {(address.city || address.district) && (
                        <div style={{ marginBottom: 4, color: isDarkMode ? '#a0a0a0' : '#666' }}>
                            <EnvironmentOutlined style={{ marginRight: 8 }} />
                            {[address.city, address.district].filter(Boolean).join(', ')}
                        </div>
                    )}

                    {address.addressLine && (
                        <div style={{ marginBottom: 4, color: isDarkMode ? '#a0a0a0' : '#666', fontSize: 13 }}>
                            {address.addressLine}
                        </div>
                    )}

                    <Divider style={{ margin: '8px 0' }} />

                    <Space wrap size={[16, 4]}>
                        {address.contactPerson && (
                            <span style={{ fontSize: 13 }}>
                                <UserOutlined style={{ marginRight: 4 }} />
                                {address.contactPerson}
                            </span>
                        )}
                        {address.phone && (
                            <span style={{ fontSize: 13 }}>
                                <PhoneOutlined style={{ marginRight: 4 }} />
                                {address.phone}
                            </span>
                        )}
                        {address.email && (
                            <span style={{ fontSize: 13 }}>
                                <MailOutlined style={{ marginRight: 4 }} />
                                {address.email}
                            </span>
                        )}
                    </Space>
                </div>
            </div>
        </Card>
    )

    const renderAddressesTab = () => (
        <>
            {selectedInstitution ? (
                <>
                    <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
                        <div>
                            <h3 style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8, color: '#1890ff' }}>
                                <BankOutlined />
                                {selectedInstitution.name}
                            </h3>
                            <span style={{ color: '#888', fontSize: 13 }}>
                                {addresses.length} adres kayıtlı
                            </span>
                        </div>
                        <Space>
                            <Button type="primary" icon={<PlusOutlined />} onClick={handleAddAddress}>
                                Yeni Adres
                            </Button>
                            <Button
                                type="text"
                                icon={<CloseOutlined style={{ fontSize: 18 }} />}
                                onClick={() => { setSelectedInstitution(null); setAddresses([]) }}
                            />
                        </Space>
                    </div>

                    {addressLoading ? (
                        <Card loading style={{ marginTop: 16 }} />
                    ) : addresses.length > 0 ? (
                        <div>
                            {addresses.map(renderAddressCard)}
                        </div>
                    ) : (
                        <Empty
                            description="Henüz adres eklenmemiş"
                            style={{ padding: 40 }}
                        >
                            <Button type="primary" icon={<PlusOutlined />} onClick={handleAddAddress}>
                                İlk Adresi Ekle
                            </Button>
                        </Empty>
                    )}
                </>
            ) : (
                <Empty
                    description="Adreslerini görmek için bir kurum seçin"
                    style={{ padding: 60 }}
                >
                    <Button onClick={() => setActiveTab('institutions')}>
                        Kurum Listesine Git
                    </Button>
                </Empty>
            )}
        </>
    )

    return (
        <div>

            {/* Main Content - Institution list and addresses side by side */}
            {!selectedInstitution ? (
                // Show only institutions tab content
                renderInstitutionsTab()
            ) : (
                // Show both institutions and addresses side by side on desktop
                isMobile ? (
                    // Mobile: Stack vertically
                    <>
                        <div style={{ marginBottom: 12 }}>
                            <Button onClick={() => { setSelectedInstitution(null); setAddresses([]) }}>
                                ← Tüm Kurumlar
                            </Button>
                        </div>
                        {renderAddressesTab()}
                    </>
                ) : (
                    // Desktop: Side by side
                    <Row gutter={24}>
                        <Col span={10}>
                            {renderInstitutionsTab()}
                        </Col>
                        <Col span={14}>
                            {renderAddressesTab()}
                        </Col>
                    </Row>
                )
            )}

            {/* Institution Modal */}
            <Modal
                title={editingInstitution ? 'Kurum Düzenle' : 'Yeni Kurum'}
                open={modalVisible}
                onCancel={() => { setModalVisible(false); form.resetFields() }}
                footer={null}
                width={isMobile ? '95%' : 500}
            >
                <Form form={form} layout="vertical" onFinish={handleSubmit}>
                    <Form.Item
                        name="name"
                        label="Kurum Adı"
                        rules={[{ required: true, message: 'Kurum adı gerekli' }]}
                    >
                        <Input placeholder="Kurum adı" />
                    </Form.Item>
                    <Form.Item name="isActive" label="Durum" valuePropName="checked">
                        <Switch checkedChildren="Aktif" unCheckedChildren="Pasif" />
                    </Form.Item>
                    <Form.Item style={{ marginBottom: 0 }}>
                        <Space>
                            <Button type="primary" htmlType="submit">
                                {editingInstitution ? 'Güncelle' : 'Oluştur'}
                            </Button>
                            <Button onClick={() => { setModalVisible(false); form.resetFields() }}>
                                İptal
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>

            {/* Address Modal */}
            <Modal
                title={editingAddress ? 'Adres Düzenle' : 'Yeni Adres'}
                open={addressModalVisible}
                onCancel={() => { setAddressModalVisible(false); addressForm.resetFields() }}
                footer={null}
                width={isMobile ? '95%' : 720}
            >
                <Form form={addressForm} layout="vertical" onFinish={handleAddressSubmit}>
                    <Form.Item name="label" label="Adres Etiketi">
                        <Input placeholder="Örn: Merkez Kampüs, Hastane, Rektörlük" />
                    </Form.Item>

                    <Form.Item name="addressLine" label="Açık Adres">
                        <Input.TextArea rows={2} placeholder="Detaylı adres bilgisi" />
                    </Form.Item>

                    <Row gutter={16}>
                        <Col xs={24} sm={12}>
                            <Form.Item name="city" label="Şehir">
                                <Select
                                    showSearch
                                    placeholder="Şehir seçin"
                                    optionFilterProp="children"
                                    filterOption={(input, option) =>
                                        option.children?.toLocaleLowerCase('tr-TR').includes(input.toLocaleLowerCase('tr-TR'))
                                    }
                                    onChange={handleCityChange}
                                    allowClear
                                >
                                    {Object.keys(turkishCitiesAndDistricts).map(city => (
                                        <Select.Option key={city} value={city}>{city}</Select.Option>
                                    ))}
                                </Select>
                            </Form.Item>
                        </Col>
                        <Col xs={24} sm={12}>
                            <Form.Item name="district" label="İlçe">
                                <Select
                                    showSearch
                                    placeholder={selectedCity ? "İlçe seçin" : "Önce şehir seçin"}
                                    optionFilterProp="children"
                                    filterOption={(input, option) =>
                                        option.children?.toLocaleLowerCase('tr-TR').includes(input.toLocaleLowerCase('tr-TR'))
                                    }
                                    disabled={!selectedCity}
                                    allowClear
                                >
                                    {availableDistricts.map(district => (
                                        <Select.Option key={district} value={district}>{district}</Select.Option>
                                    ))}
                                </Select>
                            </Form.Item>
                        </Col>
                    </Row>

                    <Divider orientation="left" plain style={{ fontSize: 13 }}>İletişim Bilgileri</Divider>

                    <Row gutter={16}>
                        <Col xs={24} sm={8}>
                            <Form.Item name="contactPerson" label="İletişim Kişisi">
                                <Input placeholder="Ad Soyad" prefix={<UserOutlined />} />
                            </Form.Item>
                        </Col>
                        <Col xs={24} sm={8}>
                            <Form.Item name="phone" label="Telefon" normalize={normalizePhoneNumber}>
                                <Input placeholder="05XX XXX XX XX" prefix={<PhoneOutlined />} maxLength={14} />
                            </Form.Item>
                        </Col>
                        <Col xs={24} sm={8}>
                            <Form.Item name="email" label="E-posta">
                                <Input placeholder="E-posta" prefix={<MailOutlined />} />
                            </Form.Item>
                        </Col>
                    </Row>

                    <Form.Item name="isDefault" valuePropName="checked" style={{ marginBottom: 16 }}>
                        <Checkbox>
                            <Space>
                                <StarOutlined />
                                <span>Bu adresi varsayılan olarak belirle</span>
                            </Space>
                        </Checkbox>
                    </Form.Item>

                    <Form.Item style={{ marginBottom: 0 }}>
                        <Space>
                            <Button type="primary" htmlType="submit">
                                {editingAddress ? 'Güncelle' : 'Oluştur'}
                            </Button>
                            <Button onClick={() => { setAddressModalVisible(false); addressForm.resetFields() }}>
                                İptal
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>
        </div>
    )
}

export default InstitutionManagement
