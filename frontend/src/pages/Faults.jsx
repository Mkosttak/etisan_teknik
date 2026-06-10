import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Table, Button, Input, Space, Tag, Card, Pagination, Tooltip, Checkbox, App, Select } from 'antd'
import { PlusOutlined, EditOutlined, HistoryOutlined, EyeOutlined, SearchOutlined, FilterOutlined, CheckCircleOutlined, ToolOutlined } from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'

const Faults = () => {
  const navigate = useNavigate()
  const { message } = App.useApp()
  const { isDarkMode } = useTheme()
  const [faults, setFaults] = useState([])
  const [filteredFaults, setFilteredFaults] = useState([])
  const [searchText, setSearchText] = useState('')
  const [loading, setLoading] = useState(false)
  const [statusFilter, setStatusFilter] = useState([])

  const trLower = (value) => (value || '').toLocaleLowerCase('tr-TR')

  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 25
  })

  useEffect(() => {
    fetchFaults()
  }, [])

  const fetchFaults = async () => {
    try {
      setLoading(true)
      const response = await api.get('/faults')
      setFaults(response.data)
      setFilteredFaults(response.data)
    } catch (error) {
      message.error('Arızalar yüklenirken hata oluştu')
    } finally {
      setLoading(false)
    }
  }

  const applyFilters = (searchValue = searchText, statusFilterValue = statusFilter) => {
    let filtered = [...faults]

    if (searchValue) {
      const searchLower = trLower(searchValue)
      filtered = filtered.filter(fault =>
        trLower(fault.deviceSerialNumber?.toString()).includes(searchLower) ||
        trLower(fault.institutionName).includes(searchLower) ||
        trLower(fault.description).includes(searchLower)
      )
    }

    if (statusFilterValue && statusFilterValue.length > 0) {
      filtered = filtered.filter(fault => statusFilterValue.includes(fault.status))
    }

    filtered.sort((a, b) => {
      const aIsSentToCustomer = a.status === 'SentToCustomer'
      const bIsSentToCustomer = b.status === 'SentToCustomer'
      if (aIsSentToCustomer && !bIsSentToCustomer) return 1
      if (!aIsSentToCustomer && bIsSentToCustomer) return -1
      const dateA = new Date(a.createdAt || 0)
      const dateB = new Date(b.createdAt || 0)
      return dateB - dateA
    })

    setFilteredFaults(filtered)
    setPagination(prev => ({ ...prev, current: 1 }))
  }

  const handleSearch = (value) => {
    setSearchText(value)
    applyFilters(value, statusFilter)
  }

  const handleStatusFilterChange = (checkedValues) => {
    setStatusFilter(checkedValues)
    applyFilters(searchText, checkedValues)
  }

  const normalizeSerialNumber = (value) => {
    if (!value) return value
    const onlyNums = value.replace(/\D/g, '').slice(0, 9)
    if (onlyNums.length <= 3) return onlyNums
    if (onlyNums.length <= 6) return `${onlyNums.slice(0, 3)}-${onlyNums.slice(3)}`
    return `${onlyNums.slice(0, 3)}-${onlyNums.slice(3, 6)}-${onlyNums.slice(6, 9)}`
  }

  const statusColors = {
    Created: 'blue',
    ShippingToTechnician: 'cyan',
    ReceivedByTechnician: 'cyan',
    ShippingToManufacturer: 'purple',
    SentToService: 'purple',
    ReceivedFromManufacturer: 'geekblue',
    ShippingToCustomer: 'gold',
    SentToCustomer: 'gold',
    Delivered: 'green',
    Pending: 'cyan',
    InProgress: 'blue',
    WaitingForParts: 'purple',
    Completed: 'green',
    Cancelled: 'red'
  }

  const getStatusLabel = (status) => {
    const labels = {
      Created: 'Arıza Kaydı Oluştur',
      ShippingToTechnician: 'Teknik Ekibe Kargolanacak',
      ReceivedByTechnician: 'Teknik Ekibe Ulaştı',
      ShippingToManufacturer: 'Verifone Gönderilecek',
      SentToService: 'Verifone Gönderildi',
      ReceivedFromManufacturer: 'Teknik Ekibe Ulaştı (Tamir Ediliyor)',
      ShippingToCustomer: 'Müşteriye Kargolanacak',
      SentToCustomer: 'Müşteriye Gönderildi',
      Delivered: 'Teslim Edildi'
    }
    return labels[status] || status
  }

  const getAllStatuses = () => {
    const statuses = new Set()
    faults.forEach(fault => { if (fault.status) statuses.add(fault.status) })
    return Array.from(statuses).sort()
  }

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
      align: 'center',
      render: (id) => `#${String(id).padStart(5, '0')}`
    },
    {
      title: 'Cihaz',
      dataIndex: 'deviceSerialNumber',
      key: 'deviceSerialNumber',
      render: (text) => normalizeSerialNumber(text),
    },
    {
      title: 'Üniversite',
      dataIndex: 'institutionName',
      key: 'institutionName',
      render: (institutionName, record) => {
        const isRequestCompleted = record.backupDeviceRequestStatus === 'Completed'
        const isRequestActive = record.backupDeviceRequestStatus &&
          record.backupDeviceRequestStatus !== 'Completed' &&
          record.backupDeviceRequestStatus !== 'Cancelled'

        return (
          <Space size={8}>
            <span>{institutionName}</span>
            {record.sendBackupDevice && (
              <Tooltip title={isRequestCompleted ? "Yedek POS cihazı talebi tamamlandı" : isRequestActive ? "Yedek POS cihazı talebi aktif" : "Öncelikli yedek POS cihazı talebi oluşturuldu"}>
                <Tag color={isRequestCompleted ? 'green' : 'default'} icon={isRequestCompleted ? <CheckCircleOutlined /> : null} style={{ margin: 0, fontSize: 11, padding: '0 6px', lineHeight: '20px' }}>
                  YEDEK POS
                </Tag>
              </Tooltip>
            )}
          </Space>
        )
      }
    },
    {
      title: 'Durum',
      dataIndex: 'status',
      key: 'status',
      filteredValue: statusFilter.length > 0 ? statusFilter : null,
      filterDropdown: ({ setSelectedKeys, selectedKeys, confirm, clearFilters }) => {
        const allStatuses = getAllStatuses()
        const currentKeys = selectedKeys || []
        const indeterminate = currentKeys.length > 0 && currentKeys.length < allStatuses.length
        const checkAll = currentKeys.length === allStatuses.length && allStatuses.length > 0

        return (
          <div style={{ padding: 8, minWidth: 250 }}>
            <div style={{ marginBottom: 8, borderBottom: '1px solid #f0f0f0', paddingBottom: 8 }}>
              <Checkbox indeterminate={indeterminate} checked={checkAll} onChange={(e) => setSelectedKeys(e.target.checked ? allStatuses : [])} style={{ fontWeight: 600 }}>Tümünü Seç</Checkbox>
            </div>
            <div style={{ maxHeight: 300, overflowY: 'auto', marginBottom: 8 }}>
              <Checkbox.Group value={currentKeys} onChange={(checkedValues) => setSelectedKeys(checkedValues)} style={{ width: '100%', display: 'flex', flexDirection: 'column', gap: 4 }}>
                {allStatuses.map(status => (
                  <Checkbox key={status} value={status}>
                    <Tag color={statusColors[status] || 'default'} style={{ marginLeft: 4 }}>{getStatusLabel(status)}</Tag>
                  </Checkbox>
                ))}
              </Checkbox.Group>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', borderTop: '1px solid #f0f0f0', paddingTop: 8 }}>
              <Button type="link" size="small" onClick={() => { setSelectedKeys([]); handleStatusFilterChange([]); clearFilters({ closeDropdown: true }); }} disabled={currentKeys.length === 0}>Temizle</Button>
              <Button type="primary" size="small" onClick={() => { handleStatusFilterChange(selectedKeys || []); confirm({ closeDropdown: true }); }} icon={<FilterOutlined />}>Uygula</Button>
            </div>
          </div>
        )
      },
      filterIcon: (filtered) => <FilterOutlined style={{ color: filtered ? '#1890ff' : undefined }} />,
      render: (status) => <Tag color={statusColors[status] || 'default'}>{getStatusLabel(status)}</Tag>,
    },
    {
      title: 'İşlemler',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Tooltip title="Detay">
            <Button type="link" icon={<EyeOutlined />} onClick={() => navigate(`/faults/${record.id}`)} style={{ color: '#1890ff' }} />
          </Tooltip>
          <Tooltip title="Düzenle">
            <Button type="link" icon={<EditOutlined />} onClick={() => navigate(`/faults/${record.id}/edit`)} style={{ color: '#52c41a' }} />
          </Tooltip>
          {record.relatedServiceFormId && (
            <Tooltip title="Servis Formuna Git">
              <Button type="link" icon={<HistoryOutlined />} onClick={() => navigate(`/service-forms/${record.relatedServiceFormId}/view`)} style={{ color: '#1890ff' }} />
            </Tooltip>
          )}
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 20, marginBottom: 24, padding: '8px 4px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 42, height: 42, borderRadius: 12, background: isDarkMode ? '#177ddc' : '#1890ff', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontSize: 20, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.3)' }}>
            <ToolOutlined />
          </div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>POS Arıza Kayıtları</h1>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flex: 1, justifyContent: 'flex-end', minWidth: 300 }}>
          <Input
            placeholder="Seri no veya kurum ile ara..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={searchText}
            onChange={(e) => handleSearch(e.target.value)}
            allowClear
            style={{ maxWidth: 400, height: 42, borderRadius: 12, border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0', boxShadow: '0 4px 12px rgba(0,0,0,0.03)', background: isDarkMode ? '#1f1f1f' : '#fff' }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/faults/new')} style={{ height: 42, borderRadius: 12, padding: '0 24px', fontWeight: 600, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)' }}>
            Yeni Arıza
          </Button>
        </div>
      </div>

      <Table
        columns={columns}
        dataSource={filteredFaults.slice((pagination.current - 1) * pagination.pageSize, pagination.current * pagination.pageSize)}
        rowKey="id"
        loading={loading}
        pagination={false}
        scroll={{ x: true }}
        size="middle"
      />

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, alignItems: 'center' }}>
        <span style={{ marginRight: 16 }}>Toplam {filteredFaults.length} arıza</span>
        <Pagination
          current={pagination.current}
          total={filteredFaults.length}
          pageSize={pagination.pageSize}
          onChange={(page) => setPagination(prev => ({ ...prev, current: page }))}
          showSizeChanger={false}
          size="small"
        />
        <Select
          value={pagination.pageSize.toString()}
          style={{ width: 70, marginLeft: 8 }}
          onChange={(value) => setPagination(prev => ({ ...prev, pageSize: parseInt(value), current: 1 }))}
          options={[{ value: '25', label: '25' }, { value: '50', label: '50' }, { value: '100', label: '100' }]}
          showSearch={false}
        />
      </div>
    </div>
  )
}

export default Faults
