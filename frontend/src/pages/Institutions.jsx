import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
    Card, Input, Table, Tag, Space, Button, Tabs, DatePicker, Select, Grid,
    Row, Col, Statistic, Empty, App, Spin
} from 'antd'
import {
    SearchOutlined, BankOutlined, ArrowLeftOutlined,
    FileTextOutlined, ToolOutlined, FormOutlined,
    AppstoreOutlined, ShoppingOutlined, EnvironmentOutlined
} from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import dayjs from 'dayjs'
import { getErrorMessage } from '../utils/errorHelper'

const { RangePicker } = DatePicker
const { useBreakpoint } = Grid

const Institutions = () => {
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { isDarkMode } = useTheme()
    const screens = useBreakpoint()
    const isMobile = !screens.md

    const [loading, setLoading] = useState(false)
    const [institutions, setInstitutions] = useState([])
    const [selectedInstitution, setSelectedInstitution] = useState(null)
    const [summary, setSummary] = useState(null)
    const [searchTerm, setSearchTerm] = useState('')
    const [activeTab, setActiveTab] = useState('requests')

    // Tab data states
    const [requests, setRequests] = useState({ items: [], total: 0, loading: false })
    const [faults, setFaults] = useState({ items: [], total: 0, loading: false })
    const [serviceForms, setServiceForms] = useState({ items: [], total: 0, loading: false })
    const [workItems, setWorkItems] = useState({ items: [], total: 0, loading: false })
    const [stockMovements, setStockMovements] = useState({ items: [], total: 0, loading: false })

    // Filter states
    const [requestFilters, setRequestFilters] = useState({ status: null, dateRange: null, page: 1 })
    const [faultFilters, setFaultFilters] = useState({ status: null, dateRange: null, page: 1 })
    const [serviceFormFilters, setServiceFormFilters] = useState({ status: null, dateRange: null, page: 1 })
    const [workItemFilters, setWorkItemFilters] = useState({ stage: null, priority: null, dateRange: null, page: 1 })
    const [stockFilters, setStockFilters] = useState({ dateRange: null, page: 1 })
    const mobileTableHintStyle = {
        fontSize: 12,
        color: isDarkMode ? '#bfbfbf' : '#8c8c8c',
        marginBottom: 8
    }

    // ⭐ PERFORMANCE: AbortController ile cleanup (StrictMode double render fix)
    useEffect(() => {
        if (!selectedInstitution) return

        const abortController = new AbortController()

        const loadData = async () => {
            try {
                // Summary yükle
                const summaryResponse = await api.get(
                    `/institutions/${encodeURIComponent(selectedInstitution)}/summary`,
                    { signal: abortController.signal }
                )
                setSummary(summaryResponse.data)

                // Tab data yükle
                await fetchTabData(activeTab, abortController.signal)
            } catch (error) {
                if (error.name !== 'CanceledError' && error.name !== 'AbortError') {
                    message.error(getErrorMessage(error))
                }
            }
        }

        loadData()

        return () => abortController.abort()
    }, [selectedInstitution, activeTab])

    // ⭐ PERFORMANCE: Debounced search with AbortController cleanup
    // İlk yükleme anında, sonraki aramalar debounced
    useEffect(() => {
        const abortController = new AbortController()

        const loadData = async () => {
            setLoading(true)
            try {
                const params = new URLSearchParams()
                if (searchTerm) params.append('search', searchTerm)
                params.append('pageSize', '100')

                const response = await api.get(`/institutions?${params}`, {
                    signal: abortController.signal
                })
                setInstitutions(response.data.items || [])
            } catch (error) {
                if (error.name !== 'CanceledError' && error.name !== 'AbortError') {
                    message.error(getErrorMessage(error))
                }
            } finally {
                setLoading(false)
            }
        }

        // İlk yükleme (searchTerm boş) -> anında yükle
        // Sonraki aramalar -> 500ms debounce
        if (searchTerm === '') {
            loadData()
        } else {
            const timer = setTimeout(loadData, 500)
            return () => {
                clearTimeout(timer)
                abortController.abort()
            }
        }

        return () => abortController.abort()
    }, [searchTerm])

    const fetchInstitutions = async () => {
        setLoading(true)
        try {
            const params = new URLSearchParams()
            if (searchTerm) params.append('search', searchTerm)
            params.append('pageSize', '100')

            const response = await api.get(`/institutions?${params}`)
            setInstitutions(response.data.items || [])
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setLoading(false)
        }
    }

    const fetchSummary = async () => {
        try {
            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/summary`)
            setSummary(response.data)
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const fetchTabData = async (tab) => {
        switch (tab) {
            case 'requests':
                await fetchRequests()
                break
            case 'faults':
                await fetchFaults()
                break
            case 'serviceForms':
                await fetchServiceForms()
                break
            case 'workItems':
                await fetchWorkItems()
                break
            case 'stock':
                await fetchStockMovements()
                break
        }
    }

    const fetchRequests = async () => {
        setRequests(prev => ({ ...prev, loading: true }))
        try {
            const params = new URLSearchParams()
            params.append('page', requestFilters.page)
            params.append('pageSize', '10')
            if (requestFilters.status) params.append('status', requestFilters.status)
            if (requestFilters.dateRange) {
                params.append('startDate', requestFilters.dateRange[0].toISOString())
                params.append('endDate', requestFilters.dateRange[1].toISOString())
            }

            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/requests?${params}`)
            setRequests({ items: response.data.items, total: response.data.totalCount, loading: false })
        } catch (error) {
            setRequests(prev => ({ ...prev, loading: false }))
            message.error(getErrorMessage(error))
        }
    }

    const fetchFaults = async () => {
        setFaults(prev => ({ ...prev, loading: true }))
        try {
            const params = new URLSearchParams()
            params.append('page', faultFilters.page)
            params.append('pageSize', '10')
            if (faultFilters.status) params.append('status', faultFilters.status)
            if (faultFilters.dateRange) {
                params.append('startDate', faultFilters.dateRange[0].toISOString())
                params.append('endDate', faultFilters.dateRange[1].toISOString())
            }

            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/faults?${params}`)
            setFaults({ items: response.data.items, total: response.data.totalCount, loading: false })
        } catch (error) {
            setFaults(prev => ({ ...prev, loading: false }))
            message.error(getErrorMessage(error))
        }
    }

    const fetchServiceForms = async () => {
        setServiceForms(prev => ({ ...prev, loading: true }))
        try {
            const params = new URLSearchParams()
            params.append('page', serviceFormFilters.page)
            params.append('pageSize', '10')
            if (serviceFormFilters.status) params.append('status', serviceFormFilters.status)
            if (serviceFormFilters.dateRange) {
                params.append('startDate', serviceFormFilters.dateRange[0].toISOString())
                params.append('endDate', serviceFormFilters.dateRange[1].toISOString())
            }

            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/service-forms?${params}`)
            setServiceForms({ items: response.data.items, total: response.data.totalCount, loading: false })
        } catch (error) {
            setServiceForms(prev => ({ ...prev, loading: false }))
            message.error(getErrorMessage(error))
        }
    }

    const fetchWorkItems = async () => {
        setWorkItems(prev => ({ ...prev, loading: true }))
        try {
            const params = new URLSearchParams()
            params.append('page', workItemFilters.page)
            params.append('pageSize', '10')
            if (workItemFilters.stage) params.append('stage', workItemFilters.stage)
            if (workItemFilters.priority) params.append('priority', workItemFilters.priority)
            if (workItemFilters.dateRange) {
                params.append('startDate', workItemFilters.dateRange[0].toISOString())
                params.append('endDate', workItemFilters.dateRange[1].toISOString())
            }

            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/work-items?${params}`)
            setWorkItems({ items: response.data.items, total: response.data.totalCount, loading: false })
        } catch (error) {
            setWorkItems(prev => ({ ...prev, loading: false }))
            message.error(getErrorMessage(error))
        }
    }

    const fetchStockMovements = async () => {
        setStockMovements(prev => ({ ...prev, loading: true }))
        try {
            const params = new URLSearchParams()
            params.append('page', stockFilters.page)
            params.append('pageSize', '20')
            if (stockFilters.dateRange) {
                params.append('startDate', stockFilters.dateRange[0].toISOString())
                params.append('endDate', stockFilters.dateRange[1].toISOString())
            }

            const response = await api.get(`/institutions/${encodeURIComponent(selectedInstitution)}/stock-movements?${params}`)
            setStockMovements({ items: response.data.items, total: response.data.totalCount, loading: false })
        } catch (error) {
            setStockMovements(prev => ({ ...prev, loading: false }))
            message.error(getErrorMessage(error))
        }
    }

    const requestColumns = [
        {
            title: 'ID',
            dataIndex: 'id',
            key: 'id',
            width: 80,
        },
        {
            title: 'Durum',
            dataIndex: 'status',
            key: 'status',
            render: (status) => {
                const statusMap = {
                    Pending: { label: 'Beklemede', color: 'cyan' },
                    InProgress: { label: 'Devam Ediyor', color: 'blue' },
                    Completed: { label: 'Tamamlandı', color: 'green' },
                    Cancelled: { label: 'İptal Edildi', color: 'red' }
                }
                const mapped = statusMap[status] || { label: status, color: 'default' }
                return <Tag color={mapped.color}>{mapped.label}</Tag>
            }
        },
        {
            title: 'Acil',
            dataIndex: 'isUrgent',
            key: 'isUrgent',
            render: (isUrgent) => isUrgent ? <Tag color="red">ACİL</Tag> : null
        },
        {
            title: 'Oluşturan',
            dataIndex: 'createdByUserName',
            key: 'createdByUserName',
        },
        {
            title: 'Tarih',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => dayjs(date).format('DD.MM.YYYY HH:mm')
        },
        {
            title: 'İşlem',
            key: 'action',
            render: (_, record) => (
                <Button size="small" onClick={() => navigate(`/requests/${record.id}`)}>
                    Detay
                </Button>
            )
        }
    ]

    const faultColumns = [
        {
            title: 'ID',
            dataIndex: 'id',
            key: 'id',
            width: 80,
        },
        {
            title: 'Firma Adı',
            dataIndex: 'institutionName',
            key: 'institutionName',
        },
        {
            title: 'Seri No',
            dataIndex: 'deviceSerialNumber',
            key: 'deviceSerialNumber',
        },
        {
            title: 'Durum',
            dataIndex: 'status',
            key: 'status',
            render: (status) => {
                const statusMap = {
                    Created: { label: 'Oluşturuldu', color: 'blue' },
                    Pending: { label: 'Beklemede', color: 'cyan' },
                    InProgress: { label: 'Devam Ediyor', color: 'blue' },
                    Completed: { label: 'Tamamlandı', color: 'green' },
                    Delivered: { label: 'Teslim Edildi', color: 'green' },
                    ShippingToTechnician: { label: 'Tekniğe Gönderiliyor', color: 'cyan' },
                    SentToCustomer: { label: 'Müşteriye Gönderildi', color: 'purple' }
                }
                const mapped = statusMap[status] || { label: status, color: 'default' }
                return <Tag color={mapped.color}>{mapped.label}</Tag>
            }
        },
        {
            title: 'Açıklama',
            dataIndex: 'description',
            key: 'description',
            ellipsis: true,
        },
        {
            title: 'Tarih',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => dayjs(date).format('DD.MM.YYYY HH:mm')
        },
        {
            title: 'İşlem',
            key: 'action',
            render: (_, record) => (
                <Button size="small" type="primary" onClick={() => navigate(`/faults/${record.id}`)}>
                    Git
                </Button>
            )
        }
    ]

    const serviceFormColumns = [
        {
            title: 'ID',
            dataIndex: 'id',
            key: 'id',
            width: 80,
        },
        {
            title: 'Durum',
            dataIndex: 'status',
            key: 'status',
            render: (status) => {
                const statusMap = {
                    DRAFT: { label: 'Taslak', color: 'default' },
                    SUBMITTED: { label: 'Gönderildi', color: 'green' }
                }
                const mapped = statusMap[status] || { label: status, color: 'default' }
                return <Tag color={mapped.color}>{mapped.label}</Tag>
            }
        },
        {
            title: 'Servis Tipi',
            dataIndex: 'serviceType',
            key: 'serviceType',
        },
        {
            title: 'Form Tarihi',
            dataIndex: 'formDate',
            key: 'formDate',
            render: (date) => dayjs(date).format('DD.MM.YYYY')
        },
        {
            title: 'Oluşturulma',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => dayjs(date).format('DD.MM.YYYY HH:mm')
        },
        {
            title: 'İşlem',
            key: 'action',
            render: (_, record) => (
                <Button size="small" onClick={() => navigate(`/service-forms/${record.id}/view`)}>
                    Detay
                </Button>
            )
        }
    ]

    const workItemColumns = [
        {
            title: 'ID',
            dataIndex: 'id',
            key: 'id',
            width: 80,
        },
        {
            title: 'Başlık',
            dataIndex: 'title',
            key: 'title',
        },
        {
            title: 'Durum',
            dataIndex: 'stage',
            key: 'stage',
            render: (stage) => {
                const stageMap = {
                    Backlog: { label: 'Yapılacaklar', color: 'default' },
                    Done: { label: 'Tamamlandı', color: 'green' },
                    Cancelled: { label: 'İptal Edildi', color: 'red' }
                }
                const mapped = stageMap[stage] || { label: stage, color: 'default' }
                return <Tag color={mapped.color}>{mapped.label}</Tag>
            }
        },
        {
            title: 'Öncelik',
            dataIndex: 'priority',
            key: 'priority',
            render: (priority) => {
                const priorityMap = {
                    Low: { label: 'Düşük', color: 'default' },
                    Normal: { label: 'Normal', color: 'blue' },
                    High: { label: 'Yüksek', color: 'orange' },
                    Critical: { label: 'Kritik', color: 'red' }
                }
                const mapped = priorityMap[priority] || { label: priority, color: 'default' }
                return <Tag color={mapped.color}>{mapped.label}</Tag>
            }
        },
        {
            title: 'Tarih',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => dayjs(date).format('DD.MM.YYYY HH:mm')
        },
        {
            title: 'İşlem',
            key: 'action',
            render: (_, record) => (
                <Button size="small" onClick={() => navigate('/workflow')}>
                    Görüntüle
                </Button>
            )
        }
    ]

    const stockColumns = [
        {
            title: 'Malzeme Adı',
            dataIndex: 'itemName',
            key: 'itemName',
        },
        {
            title: 'Toplam Miktar',
            dataIndex: 'totalQuantityUsed',
            key: 'totalQuantityUsed',
        },
        {
            title: 'Kullanım Sayısı',
            dataIndex: 'usageCount',
            key: 'usageCount',
        },
        {
            title: 'Son Kullanım',
            dataIndex: 'lastUsedDate',
            key: 'lastUsedDate',
            render: (date) => date ? dayjs(date).format('DD.MM.YYYY') : '-'
        }
    ]

    const renderListView = () => (
        <div>
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
                        <BankOutlined />
                    </div>
                    <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                        Üniversiteler
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
                        prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        allowClear
                        style={{
                            maxWidth: 400,
                            height: 42,
                            borderRadius: 12,
                            border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
                            boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                            background: isDarkMode ? '#1f1f1f' : '#fff'
                        }}
                    />
                </div>
            </div>

            {loading ? (
                <div style={{ textAlign: 'center', padding: 50 }}>
                    <Spin size="large" />
                </div>
            ) : institutions.length === 0 ? (
                <Empty description="Kurum bulunamadı" />
            ) : (
                <Row gutter={isMobile ? [0, 12] : [16, 16]}>
                    {institutions.map((inst) => (
                        <Col xs={24} sm={12} md={8} lg={6} key={inst.name}>
                            <Card
                                hoverable
                                onClick={() => setSelectedInstitution(inst.name)}
                                style={{
                                    borderRadius: 12,
                                    background: isDarkMode ? '#1f1f1f' : '#fff',
                                    border: isDarkMode ? '1px solid rgba(255, 255, 255, 0.1)' : '1px solid #f0f0f0',
                                    height: '100%',
                                    minHeight: 320
                                }}
                            >
                                <div style={{ textAlign: 'center', marginBottom: 16 }}>
                                    <BankOutlined style={{ fontSize: 48, color: '#1890ff' }} />
                                </div>
                                <div style={{
                                    fontSize: 16,
                                    fontWeight: 600,
                                    marginBottom: 16,
                                    textAlign: 'center',
                                    color: isDarkMode ? '#fff' : '#262626'
                                }}>
                                    {inst.name}
                                </div>
                                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 12 }}>
                                    {[
                                        { label: 'Talepler', value: inst.requestCount, color: '#1677ff', bg: isDarkMode ? 'rgba(22, 119, 255, 0.15)' : '#e6f4ff' },
                                        { label: 'Arızalar', value: inst.faultCount, color: '#fa8c16', bg: isDarkMode ? 'rgba(250, 140, 22, 0.15)' : '#fff7e6' },
                                        { label: 'Formlar', value: inst.serviceFormCount, color: '#52c41a', bg: isDarkMode ? 'rgba(82, 196, 26, 0.15)' : '#f6ffed' },
                                        { label: 'İş Kartları', value: inst.workItemCount, color: '#722ed1', bg: isDarkMode ? 'rgba(114, 46, 209, 0.15)' : '#f9f0ff' }
                                    ].map((stat) => (
                                        <div
                                            key={stat.label}
                                            style={{
                                                padding: '10px 12px',
                                                borderRadius: 10,
                                                background: stat.bg,
                                                border: isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #f0f0f0'
                                            }}
                                        >
                                            <div style={{ fontSize: 12, color: isDarkMode ? '#bfbfbf' : '#595959' }}>{stat.label}</div>
                                            <div style={{ fontSize: 20, fontWeight: 600, color: stat.color }}>{stat.value}</div>
                                        </div>
                                    ))}
                                </div>
                            </Card>
                        </Col>
                    ))}
                </Row>
            )}
        </div>
    )

    const renderDetailView = () => (
        <div>
            <Button
                icon={<ArrowLeftOutlined />}
                onClick={() => setSelectedInstitution(null)}
                style={{ marginBottom: 24 }}
            >
                Geri Dön
            </Button>

            <Card style={{ marginBottom: 24, borderRadius: 12 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24 }}>
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
                        <BankOutlined />
                    </div>
                    <h2 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                        {selectedInstitution}
                    </h2>
                </div>

                {summary && (
                    <Row gutter={16}>
                        <Col xs={12} sm={8} md={6} lg={4}>
                            <Statistic
                                title="Toplam Talep"
                                value={summary.totalRequests}
                                prefix={<FileTextOutlined />}
                                valueStyle={{ color: '#1890ff' }}
                            />
                            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 8 }}>
                                Bekleyen: {summary.pendingRequests} | Devam Eden: {summary.inProgressRequests}
                            </div>
                        </Col>
                        <Col xs={12} sm={8} md={6} lg={4}>
                            <Statistic
                                title="Toplam Arıza"
                                value={summary.totalFaults}
                                prefix={<ToolOutlined />}
                                valueStyle={{ color: '#fa8c16' }}
                            />
                            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 8 }}>
                                Bekleyen: {summary.pendingFaults} | Devam Eden: {summary.inProgressFaults}
                            </div>
                        </Col>
                        <Col xs={12} sm={8} md={6} lg={4}>
                            <Statistic
                                title="Servis Formu"
                                value={summary.totalServiceForms}
                                prefix={<FormOutlined />}
                                valueStyle={{ color: '#52c41a' }}
                            />
                            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 8 }}>
                                Taslak: {summary.draftServiceForms} | Gönderildi: {summary.submittedServiceForms}
                            </div>
                        </Col>
                        <Col xs={12} sm={8} md={6} lg={4}>
                            <Statistic
                                title="İşler"
                                value={summary.totalWorkItems}
                                prefix={<AppstoreOutlined />}
                                valueStyle={{ color: '#722ed1' }}
                            />
                            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 8 }}>
                                Yapılacaklar: {summary.backlogWorkItems} | Tamamlanan: {summary.doneWorkItems}
                            </div>
                        </Col>

                    </Row>
                )}
            </Card>

            <Tabs
                activeKey={activeTab}
                onChange={setActiveTab}
                items={[
                    {
                        key: 'requests',
                        label: (
                            <span>
                                <FileTextOutlined /> Talepler
                            </span>
                        ),
                        children: (
                            <div>
                                <Space style={{ marginBottom: 16 }}>
                                    <Select
                                        placeholder="Durum"
                                        style={{ width: 150 }}
                                        allowClear
                                        value={requestFilters.status}
                                        onChange={(value) => setRequestFilters(prev => ({ ...prev, status: value, page: 1 }))}
                                        options={[
                                            { value: 'Pending', label: 'Beklemede' },
                                            { value: 'InProgress', label: 'Devam Ediyor' },
                                            { value: 'Completed', label: 'Tamamlandı' },
                                            { value: 'Cancelled', label: 'İptal' }
                                        ]}
                                    />
                                    <RangePicker
                                        value={requestFilters.dateRange}
                                        onChange={(dates) => setRequestFilters(prev => ({ ...prev, dateRange: dates, page: 1 }))}
                                    />
                                    <Button type="primary" onClick={fetchRequests}>Filtrele</Button>
                                </Space>
                                {isMobile && (
                                    <div style={mobileTableHintStyle}>
                                        Tabloyu yana kaydırarak diğer sütunları görebilirsiniz →
                                    </div>
                                )}
                                <Table
                                    columns={requestColumns}
                                    dataSource={requests.items}
                                    loading={requests.loading}
                                    rowKey="id"
                                    scroll={{ x: 'max-content' }}
                                    pagination={{
                                        current: requestFilters.page,
                                        pageSize: 10,
                                        total: requests.total,
                                        onChange: (page) => setRequestFilters(prev => ({ ...prev, page }))
                                    }}
                                    onChange={(pagination) => {
                                        setRequestFilters(prev => ({ ...prev, page: pagination.current }))
                                    }}
                                />
                            </div>
                        )
                    },
                    {
                        key: 'faults',
                        label: (
                            <span>
                                <ToolOutlined /> Arızalar
                            </span>
                        ),
                        children: (
                            <div>
                                <Space style={{ marginBottom: 16 }}>
                                    <Select
                                        placeholder="Durum"
                                        style={{ width: 150 }}
                                        allowClear
                                        value={faultFilters.status}
                                        onChange={(value) => setFaultFilters(prev => ({ ...prev, status: value, page: 1 }))}
                                        options={[
                                            { value: 'Created', label: 'Oluşturuldu' },
                                            { value: 'Pending', label: 'Beklemede' },
                                            { value: 'InProgress', label: 'Devam Ediyor' },
                                            { value: 'Completed', label: 'Tamamlandı' },
                                            { value: 'Delivered', label: 'Teslim Edildi' }
                                        ]}
                                    />
                                    <RangePicker
                                        value={faultFilters.dateRange}
                                        onChange={(dates) => setFaultFilters(prev => ({ ...prev, dateRange: dates, page: 1 }))}
                                    />
                                    <Button type="primary" onClick={fetchFaults}>Filtrele</Button>
                                </Space>
                                {isMobile && (
                                    <div style={mobileTableHintStyle}>
                                        Tabloyu yana kaydırarak diğer sütunları görebilirsiniz →
                                    </div>
                                )}
                                <Table
                                    columns={faultColumns}
                                    dataSource={faults.items}
                                    loading={faults.loading}
                                    rowKey="id"
                                    scroll={{ x: 'max-content' }}
                                    pagination={{
                                        current: faultFilters.page,
                                        pageSize: 10,
                                        total: faults.total,
                                        onChange: (page) => setFaultFilters(prev => ({ ...prev, page }))
                                    }}
                                    onChange={(pagination) => {
                                        setFaultFilters(prev => ({ ...prev, page: pagination.current }))
                                    }}
                                />
                            </div>
                        )
                    },
                    {
                        key: 'serviceForms',
                        label: (
                            <span>
                                <FormOutlined /> Servis Formları
                            </span>
                        ),
                        children: (
                            <div>
                                <Space style={{ marginBottom: 16 }}>
                                    <Select
                                        placeholder="Durum"
                                        style={{ width: 150 }}
                                        allowClear
                                        value={serviceFormFilters.status}
                                        onChange={(value) => setServiceFormFilters(prev => ({ ...prev, status: value, page: 1 }))}
                                        options={[
                                            { value: 'DRAFT', label: 'Taslak' },
                                            { value: 'SUBMITTED', label: 'Gönderildi' }
                                        ]}
                                    />
                                    <RangePicker
                                        value={serviceFormFilters.dateRange}
                                        onChange={(dates) => setServiceFormFilters(prev => ({ ...prev, dateRange: dates, page: 1 }))}
                                    />
                                    <Button type="primary" onClick={fetchServiceForms}>Filtrele</Button>
                                </Space>
                                {isMobile && (
                                    <div style={mobileTableHintStyle}>
                                        Tabloyu yana kaydırarak diğer sütunları görebilirsiniz →
                                    </div>
                                )}
                                <Table
                                    columns={serviceFormColumns}
                                    dataSource={serviceForms.items}
                                    loading={serviceForms.loading}
                                    rowKey="id"
                                    scroll={{ x: 'max-content' }}
                                    pagination={{
                                        current: serviceFormFilters.page,
                                        pageSize: 10,
                                        total: serviceForms.total,
                                        onChange: (page) => setServiceFormFilters(prev => ({ ...prev, page }))
                                    }}
                                    onChange={(pagination) => {
                                        setServiceFormFilters(prev => ({ ...prev, page: pagination.current }))
                                    }}
                                />
                            </div>
                        )
                    },
                    {
                        key: 'workItems',
                        label: (
                            <span>
                                <AppstoreOutlined /> İş Akışı
                            </span>
                        ),
                        children: (
                            <div>
                                <Space style={{ marginBottom: 16 }}>
                                    <Select
                                        placeholder="Durum"
                                        style={{ width: 150 }}
                                        allowClear
                                        value={workItemFilters.stage}
                                        onChange={(value) => setWorkItemFilters(prev => ({ ...prev, stage: value, page: 1 }))}
                                        options={[
                                            { value: 'Backlog', label: 'Yapılacaklar' },
                                            { value: 'Done', label: 'Tamamlandı' },
                                            { value: 'Cancelled', label: 'İptal' }
                                        ]}
                                    />
                                    <Select
                                        placeholder="Öncelik"
                                        style={{ width: 150 }}
                                        allowClear
                                        value={workItemFilters.priority}
                                        onChange={(value) => setWorkItemFilters(prev => ({ ...prev, priority: value, page: 1 }))}
                                        options={[
                                            { value: 'Low', label: 'Düşük' },
                                            { value: 'Normal', label: 'Normal' },
                                            { value: 'High', label: 'Yüksek' },
                                            { value: 'Critical', label: 'Kritik' }
                                        ]}
                                    />
                                    <RangePicker
                                        value={workItemFilters.dateRange}
                                        onChange={(dates) => setWorkItemFilters(prev => ({ ...prev, dateRange: dates, page: 1 }))}
                                    />
                                    <Button type="primary" onClick={fetchWorkItems}>Filtrele</Button>
                                </Space>
                                {isMobile && (
                                    <div style={mobileTableHintStyle}>
                                        Tabloyu yana kaydırarak diğer sütunları görebilirsiniz →
                                    </div>
                                )}
                                <Table
                                    columns={workItemColumns}
                                    dataSource={workItems.items}
                                    loading={workItems.loading}
                                    rowKey="id"
                                    scroll={{ x: 'max-content' }}
                                    pagination={{
                                        current: workItemFilters.page,
                                        pageSize: 10,
                                        total: workItems.total,
                                        onChange: (page) => setWorkItemFilters(prev => ({ ...prev, page }))
                                    }}
                                    onChange={(pagination) => {
                                        setWorkItemFilters(prev => ({ ...prev, page: pagination.current }))
                                    }}
                                />
                            </div>
                        )
                    },
                    {
                        key: 'stock',
                        label: (
                            <span>
                                <ShoppingOutlined /> Kullanılan Malzemeler
                            </span>
                        ),
                        children: (
                            <div>
                                <Space style={{ marginBottom: 16 }}>
                                    <RangePicker
                                        value={stockFilters.dateRange}
                                        onChange={(dates) => setStockFilters(prev => ({ ...prev, dateRange: dates, page: 1 }))}
                                    />
                                    <Button type="primary" onClick={fetchStockMovements}>Filtrele</Button>
                                </Space>
                                {isMobile && (
                                    <div style={mobileTableHintStyle}>
                                        Tabloyu yana kaydırarak diğer sütunları görebilirsiniz →
                                    </div>
                                )}
                                <Table
                                    columns={stockColumns}
                                    dataSource={stockMovements.items}
                                    loading={stockMovements.loading}
                                    rowKey="stockItemId"
                                    scroll={{ x: 'max-content' }}
                                    pagination={{
                                        current: stockFilters.page,
                                        pageSize: 20,
                                        total: stockMovements.total,
                                        onChange: (page) => setStockFilters(prev => ({ ...prev, page }))
                                    }}
                                    onChange={(pagination) => {
                                        setStockFilters(prev => ({ ...prev, page: pagination.current }))
                                    }}

                                />
                            </div>
                        )
                    }
                ]}
            />
        </div>
    )

    return (
        <div style={{ paddingTop: 8 }}>
            {selectedInstitution ? renderDetailView() : renderListView()}
        </div>
    )
}

export default Institutions
