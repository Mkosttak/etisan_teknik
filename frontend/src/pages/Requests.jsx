import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Table, Button, Input, App, Space, Tag, Tooltip, Pagination, Select } from 'antd'
import { PlusOutlined, EditOutlined, EyeOutlined, SearchOutlined, ShoppingOutlined, CheckCircleOutlined } from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'

const Requests = () => {
  const navigate = useNavigate()
  const { isDarkMode } = useTheme()
  const { message } = App.useApp()

  const trLower = (value) => (value || '').toLocaleLowerCase('tr-TR')
  const [requests, setRequests] = useState([])
  const [filteredRequests, setFilteredRequests] = useState([])
  const [searchText, setSearchText] = useState('')
  const [loading, setLoading] = useState(false)
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 25
  })

  useEffect(() => {
    fetchRequests()
  }, [])

  const sortRequests = (requestsList) => {
    return [...requestsList].sort((a, b) => {
      const getPriority = (req) => {
        if (req.status === 'Cancelled') return 4
        if (req.status === 'Completed') return 3
        if (req.isUrgent) return 1
        return 2
      }

      const priorityA = getPriority(a)
      const priorityB = getPriority(b)

      if (priorityA !== priorityB) return priorityA - priorityB
      const dateA = new Date(a.createdAt || 0)
      const dateB = new Date(b.createdAt || 0)
      return dateB - dateA
    })
  }

  const fetchRequests = async () => {
    try {
      setLoading(true)
      const response = await api.get('/requests')
      const sortedRequests = sortRequests(response.data)
      setRequests(sortedRequests)
      setFilteredRequests(sortedRequests)
    } catch (error) {
      message.error('Talepler yüklenirken hata oluştu')
    } finally {
      setLoading(false)
    }
  }

  const handleSearch = (value) => {
    setSearchText(value)
    if (!value) {
      setFilteredRequests(sortRequests(requests))
      return
    }
    const searchLower = trLower(value)
    const filtered = requests.filter(req =>
      trLower(req.deviceSerialNumber).includes(searchLower) ||
      trLower(req.institutionName).includes(searchLower) ||
      trLower(req.description).includes(searchLower) ||
      trLower(req.contactPerson).includes(searchLower)
    )
    setFilteredRequests(sortRequests(filtered))
    setPagination(prev => ({ ...prev, current: 1 }))
  }

  const statusColors = {
    Pending: 'cyan',
    InProgress: 'blue',
    Completed: 'green',
    Cancelled: 'red'
  }

  const statusLabels = {
    Pending: 'Beklemede',
    InProgress: 'İşlemde',
    Completed: 'Tamamlandı',
    Cancelled: 'İptal'
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
      title: 'Kurum',
      dataIndex: 'institutionName',
      key: 'institutionName',
      render: (text, record) => {
        const institutionName = text || record.institutionOtherName
        return (
          <Space style={{ display: 'inline-flex', alignItems: 'center', gap: 8, flexDirection: 'row-reverse' }}>
            <span>{institutionName || <span style={{ color: '#bfbfbf', fontStyle: 'italic' }}>-</span>}</span>
            {record.isUrgent && (
              <Tag color="red" style={{ fontWeight: 'bold', fontSize: '11px', margin: 0, borderRadius: 6, padding: '2px 8px', border: 'none', boxShadow: '0 4px 12px rgba(255, 77, 79, 0.3)' }}>
                🚨 ACİL
              </Tag>
            )}
          </Space>
        )
      },
    },
    {
      title: 'Açıklama',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
      render: (text) => {
        if (!text) return <span style={{ color: '#bfbfbf', fontStyle: 'italic' }}>-</span>
        return text.length > 50 ? <Tooltip title={text}><span>{text.substring(0, 50)}...</span></Tooltip> : text
      },
    },
    {
      title: 'Ürünler',
      key: 'products',
      render: (_, record) => {
        const products = []
        if (record.stockItems && record.stockItems.length > 0) {
          record.stockItems.forEach(item => {
            if (!item.isCancelled) {
              products.push({ text: `${item.quantity}x ${item.itemName}`, isShipped: item.isShipped })
            }
          })
        }
        if (record.extraItems) {
          const extraItems = record.extraItems.split(', ').filter(item => item.trim())
          const shippedExtras = (record.shippedExtraItems || '').split(',').map(s => s.trim())
          extraItems.forEach(item => {
            const isShipped = shippedExtras.includes(item.trim())
            products.push({ text: item, isShipped: isShipped })
          })
        }
        if (products.length === 0) return <span style={{ color: '#bfbfbf', fontStyle: 'italic' }}>-</span>
        return (
          <div style={{ maxWidth: 300, display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {products.map((product, index) => (
              <Tag key={index} color={product.isShipped ? 'green' : 'blue'} style={{ marginBottom: 0, marginRight: 0, borderRadius: 6, padding: '2px 8px', fontSize: '12px', border: 'none', display: 'flex', alignItems: 'center' }}>
                {product.isShipped && <CheckCircleOutlined style={{ marginRight: 4 }} />}
                {product.text}
              </Tag>
            ))}
          </div>
        )
      },
    },
    {
      title: 'Durum',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={statusColors[status] || 'default'} style={{ borderRadius: 6, padding: '2px 8px', fontSize: '12px', border: 'none', fontWeight: 500 }}>
          {statusLabels[status]}
        </Tag>
      ),
    },
    {
      title: 'İşlemler',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Tooltip title="Detay">
            <Button type="link" icon={<EyeOutlined />} onClick={() => navigate(`/requests/${record.id}`)} style={{ color: '#1890ff' }} />
          </Tooltip>
          <Tooltip title="Düzenle">
            <Button type="link" icon={<EditOutlined />} onClick={() => navigate(`/requests/${record.id}/edit`)} style={{ color: '#52c41a' }} />
          </Tooltip>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 20, marginBottom: 24, padding: '8px 4px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 42, height: 42, borderRadius: 12, background: isDarkMode ? '#177ddc' : '#1890ff', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontSize: 20, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.3)' }}>
            <ShoppingOutlined />
          </div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>Talepler</h1>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flex: 1, justifyContent: 'flex-end', minWidth: 300 }}>
          <Input
            placeholder="Seri no, kurum veya kişi ile ara..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={searchText}
            onChange={(e) => handleSearch(e.target.value)}
            allowClear
            style={{ maxWidth: 400, height: 42, borderRadius: 12, border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0', boxShadow: '0 4px 12px rgba(0,0,0,0.03)', background: isDarkMode ? '#1f1f1f' : '#fff' }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/requests/new')} style={{ height: 42, borderRadius: 12, padding: '0 24px', fontWeight: 600, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)' }}>
            Yeni Talep
          </Button>
        </div>
      </div>

      <Table
        columns={columns}
        dataSource={filteredRequests.slice((pagination.current - 1) * pagination.pageSize, pagination.current * pagination.pageSize)}
        rowKey="id"
        loading={loading}
        pagination={false}
        scroll={{ x: true }}
        tableLayout="auto"
        size="middle"
      />

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, alignItems: 'center' }}>
        <span style={{ marginRight: 16 }}>Toplam {filteredRequests.length} talep</span>
        <Pagination
          current={pagination.current}
          total={filteredRequests.length}
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

export default Requests
