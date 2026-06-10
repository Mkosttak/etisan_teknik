import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Table, Button, Space, Tag, Tabs, Empty, App, Popconfirm, Input, Select, Pagination, Tooltip } from 'antd'
import { PlusOutlined, EyeOutlined, EditOutlined, DeleteOutlined, FilePdfOutlined, SearchOutlined, HistoryOutlined, DownloadOutlined } from '@ant-design/icons'
import api from '../services/api'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'
dayjs.locale('tr')

const ServiceForms = () => {
    const navigate = useNavigate()
    const [forms, setForms] = useState([])
    const { isDarkMode } = useTheme()
    const [loading, setLoading] = useState(false)
    const [activeTab, setActiveTab] = useState('all')
    const trLower = (value) => (value || '').toLocaleLowerCase('tr-TR')

    // Filter state
    const [searchText, setSearchText] = useState('')
    const [pagination, setPagination] = useState({
        current: 1,
        pageSize: 25
    })

    useEffect(() => {
        fetchForms()
    }, [activeTab])

    const fetchForms = async () => {
        try {
            setLoading(true)
            const params = activeTab !== 'all' ? { status: activeTab } : {}
            const response = await api.get('/serviceforms', { params })
            setForms(response.data)
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setLoading(false)
        }
    }

    const handleDelete = async (id) => {
        try {
            await api.delete(`/serviceforms/${id}`)
            message.success('Taslak silindi')
            fetchForms()
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const handleDownloadPdf = async (record) => {
        try {
            // Backend ile aynı isimlendirme mantığını kullan
            const institution = record.institutionOtherName || record.institutionName || 'ServisFormu'
            const date = dayjs(record.formDate).format('YYYYMMDD')
            const cleanName = institution.replace(/[İıĞğÜüŞşÖöÇç]/g, (m) => {
                const map = { 'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g', 'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's', 'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c' }
                return map[m]
            }).replace(/[\s.,]/g, '_')

            const formNo = `Form${record.id.toString().padStart(4, '0')}`
            const fileName = `${formNo}_${cleanName}_${date}.pdf`

            // Axios ile PDF'i blob olarak indir (Authorization header ile)
            const response = await api.get(
                `/serviceforms/${record.id}/pdf/${fileName}`,
                { responseType: 'blob' }
            )

            // Blob URL oluştur
            const blob = new Blob([response.data], { type: 'application/pdf' })
            const url = window.URL.createObjectURL(blob)

            // Yeni sekmede aç
            window.open(url, '_blank')

            // Memory temizliği (optional - browser otomatik yapar ama cleanup yapalım)
            setTimeout(() => window.URL.revokeObjectURL(url), 100)
        } catch (error) {
            message.error('PDF açılamadı')
        }
    }

    const handleDownloadPdfFile = async (record) => {
        try {
            // Backend ile aynı isimlendirme mantığını kullan
            const institution = record.institutionOtherName || record.institutionName || 'ServisFormu'
            const date = dayjs(record.formDate).format('YYYYMMDD')
            const cleanName = institution.replace(/[İıĞğÜüŞşÖöÇç]/g, (m) => {
                const map = { 'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g', 'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's', 'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c' }
                return map[m]
            }).replace(/[\s.,]/g, '_')

            const formNo = `Form${record.id.toString().padStart(4, '0')}`
            const fileName = `${formNo}_${cleanName}_${date}.pdf`

            // Axios ile PDF'i blob olarak indir
            const response = await api.get(
                `/serviceforms/${record.id}/pdf/${fileName}`,
                { responseType: 'blob' }
            )

            // Blob URL oluştur ve download et
            const blob = new Blob([response.data], { type: 'application/pdf' })
            const url = window.URL.createObjectURL(blob)

            // Download link oluştur ve tıkla
            const link = document.createElement('a')
            link.href = url
            link.download = fileName // Dosya ismini set et
            document.body.appendChild(link)
            link.click()

            // Cleanup
            document.body.removeChild(link)
            window.URL.revokeObjectURL(url)
        } catch (error) {
            message.error('PDF indirilemedi')
        }
    }

    const getStatusTag = (status) => {
        if (status === 'SUBMITTED') {
            return <Tag color="success">Gönderildi</Tag>
        }
        return <Tag color="warning">Taslak</Tag>
    }

    const getIssueStatusLabel = (status) => {
        const labels = {
            'SorunGiderilmedi': 'Sorun Giderilmedi',
            'SorunGiderildi': 'Sorun Giderildi',
            'SorunaRastlanmadi': 'Soruna Rastlanmadı',
            'ParcaDegistirildi': 'Parça Değiştirildi',
            'ParcaDegistirilecek': 'Parça Değiştirilecek',
            'Diger': 'Diğer'
        }
        return labels[status] || status || '-'
    }

    const columns = [
        {
            title: 'Form No',
            dataIndex: 'id',
            key: 'id',
            render: (id) => `#${id.toString().padStart(4, '0')}`
        },
        {
            title: 'Tarih',
            dataIndex: 'formDate',
            key: 'formDate',
            render: (date) => dayjs(date).format('DD.MM.YYYY HH:mm')
        },
        {
            title: 'Kurum',
            key: 'institution',
            ellipsis: true,
            render: (_, record) => record.institutionOtherName || record.institutionName || '-'
        },
        {
            title: 'Durum',
            dataIndex: 'issueStatus',
            key: 'issueStatus',
            render: (status) => getIssueStatusLabel(status)
        },
        {
            title: 'Kayıt',
            dataIndex: 'status',
            key: 'status',
            render: (status) => getStatusTag(status)
        },
        {
            title: 'Oluşturan',
            dataIndex: 'createdByUserName',
            key: 'createdByUserName',
            render: (name) => name || '-'
        },
        {
            title: 'İşlemler',
            key: 'actions',
            render: (_, record) => (
                <Space>
                    {record.status === 'SUBMITTED' ? (
                        <>
                            <Tooltip title="Detay">
                                <Button
                                    type="link"
                                    icon={<EyeOutlined />}
                                    onClick={() => navigate(`/service-forms/${record.id}/view`)}
                                    style={{ color: '#1890ff' }}
                                />
                            </Tooltip>
                            <Tooltip title="PDF Görüntüle">
                                <Button
                                    type="link"
                                    icon={<FilePdfOutlined />}
                                    onClick={() => handleDownloadPdf(record)}
                                    style={{ color: '#1890ff' }}
                                />
                            </Tooltip>
                            <Tooltip title="PDF İndir">
                                <Button
                                    type="link"
                                    icon={<DownloadOutlined />}
                                    onClick={() => handleDownloadPdfFile(record)}
                                    style={{ color: '#52c41a' }}
                                />
                            </Tooltip>
                        </>
                    ) : (
                        <>
                            <Tooltip title="Düzenle">
                                <Button
                                    type="link"
                                    icon={<EditOutlined />}
                                    onClick={() => navigate(`/service-forms/${record.id}`)}
                                    style={{ color: '#52c41a' }}
                                />
                            </Tooltip>
                            <Popconfirm
                                title="Taslağı silmek istediğinize emin misiniz?"
                                onConfirm={() => handleDelete(record.id)}
                                okText="Evet"
                                cancelText="Hayır"
                            >
                                <Tooltip title="Sil">
                                    <Button
                                        type="link"
                                        danger
                                        icon={<DeleteOutlined />}
                                    />
                                </Tooltip>
                            </Popconfirm>
                        </>
                    )}
                </Space>
            )
        }
    ]

    const filteredForms = forms
        .filter(form => {
            if (searchText) {
                const institution = form.institutionIsOther
                    ? form.institutionOtherName
                    : form.institutionName
                const address = form.address || ''

                const searchLower = trLower(searchText)
                const institutionMatch = trLower(institution).includes(searchLower)
                const addressMatch = trLower(address).includes(searchLower)

                if (!institutionMatch && !addressMatch) {
                    return false
                }
            }
            return true
        })
        .sort((a, b) => {
            // Taslaklar her zaman en üstte
            if (a.status === 'DRAFT' && b.status !== 'DRAFT') return -1
            if (a.status !== 'DRAFT' && b.status === 'DRAFT') return 1
            // Aynı durumdaysa tarihe göre sırala (yeniden eskiye)
            return new Date(b.formDate) - new Date(a.formDate)
        })

    const tabItems = [
        { key: 'all', label: 'Tüm Formlar' },
        { key: 'DRAFT', label: 'Taslaklar' },
        { key: 'SUBMITTED', label: 'Gönderilenler' }
    ]

    return (
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
                        <HistoryOutlined />
                    </div>
                    <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                        Teknik Servis Formları
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
                        placeholder="Kurum veya adres ara..."
                        prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                        value={searchText}
                        onChange={(e) => {
                            setSearchText(e.target.value)
                            setPagination(prev => ({ ...prev, current: 1 }))
                        }}
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
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => navigate('/service-forms/new')}
                        style={{
                            height: 42,
                            borderRadius: 12,
                            padding: '0 24px',
                            fontWeight: 600,
                            boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)'
                        }}
                    >
                        Yeni Form
                    </Button>
                </div>
            </div>

            <Tabs
                activeKey={activeTab}
                onChange={setActiveTab}
                items={tabItems}
                style={{ marginBottom: 16 }}
            />

            <Table
                columns={columns}
                dataSource={filteredForms.slice((pagination.current - 1) * pagination.pageSize, pagination.current * pagination.pageSize)}
                rowKey="id"
                loading={loading}
                pagination={false}
                scroll={{ x: true }}
                tableLayout="auto"
                locale={{
                    emptyText: <Empty description="Henuz form bulunmuyor" />
                }}
            />
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, alignItems: 'center' }}>
                <span style={{ marginRight: 16 }}>Toplam {filteredForms.length} form</span>
                <Pagination
                    current={pagination.current}
                    total={filteredForms.length}
                    pageSize={pagination.pageSize}
                    onChange={(page) => setPagination(prev => ({ ...prev, current: page }))}
                    showSizeChanger={false}
                    size="small"
                />
                <Select
                    value={pagination.pageSize.toString()}
                    style={{ width: 70, marginLeft: 8 }}
                    onChange={(value) => setPagination(prev => ({ ...prev, pageSize: parseInt(value), current: 1 }))}
                    options={[
                        { value: '25', label: '25' },
                        { value: '50', label: '50' },
                        { value: '100', label: '100' },
                    ]}
                    showSearch={false}
                />
            </div>
        </div>
    )
}

export default ServiceForms


















