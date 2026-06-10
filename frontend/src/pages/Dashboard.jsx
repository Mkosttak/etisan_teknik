import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Card, Row, Col, Statistic, List, Tag, Spin, Button, Empty, Space, App, Grid,
  Typography, Progress, Badge
} from 'antd'
import {
  FileTextOutlined,
  ToolOutlined,
  AppstoreOutlined,
  InboxOutlined,
  PlusOutlined,
  WarningOutlined,
  ClockCircleOutlined,
  FireOutlined,
  CheckCircleOutlined,
  ExclamationCircleOutlined,
  RocketOutlined,
  CarOutlined
} from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import dayjs from 'dayjs'
import utc from 'dayjs/plugin/utc'
import timezone from 'dayjs/plugin/timezone'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/tr'

dayjs.extend(relativeTime)
dayjs.extend(utc)
dayjs.extend(timezone)
dayjs.tz.setDefault('Europe/Istanbul')
dayjs.locale('tr')

const { Title, Text } = Typography
const { useBreakpoint } = Grid

const toIstanbulTime = (value) => {
  if (!value) return dayjs()
  return dayjs.utc(value).tz('Europe/Istanbul')
}

const normalizeAddress = (str = '') => {
  if (!str) return ''
  return str
    .toLowerCase()
    .trim()
    .replace(/\s+/g, ' ')
    .replace(/[.,\/#!$%\^&\*;:{}=\-_`~()]/g, '')
}

const Dashboard = () => {
  const navigate = useNavigate()
  const { message } = App.useApp()
  const { isDarkMode } = useTheme()
  const screens = useBreakpoint()
  const isMobile = !screens.md

  const [loading, setLoading] = useState(true)
  const [dashboardData, setDashboardData] = useState({
    pendingRequests: 0,
    activeFaults: 0,
    todayWorkItems: 0,
    pendingShipments: 0,
    urgentItems: [],
    overdueItems: [],
    lowStockItems: []
  })

  useEffect(() => {
    fetchDashboardData()
  }, [])

  const fetchDashboardData = async () => {
    try {
      setLoading(true)

      // Paralel API çağrıları
      const [requestsRes, faultsRes, workItemsRes, stocksRes, verifoneRes] = await Promise.all([
        api.get('/requests'),
        api.get('/faults'),
        api.get('/workitems/kanban'),
        api.get('/stock'),
        api.get('/shipments/verifone')
      ])

      const requests = requestsRes.data
      const faults = faultsRes.data
      // WorkItems kanban formatında geliyor, tüm stage'leri birleştir
      const workItemsKanban = workItemsRes.data
      const workItems = [
        ...(workItemsKanban.backlog || []),
        ...(workItemsKanban.done || []),
        ...(workItemsKanban.cancelled || [])
      ]
      const stocks = stocksRes.data
      const verifoneShipments = verifoneRes.data || []

      // Bugünün başlangıcı (Istanbul saati)
      const todayStart = dayjs().tz('Europe/Istanbul').startOf('day')
      const now = dayjs().tz('Europe/Istanbul')

      // İstatistikleri hesapla
      const pendingRequests = requests.filter(r => r.status === 'Pending').length
      const activeFaults = faults.filter(f => f.status !== 'Completed' && f.status !== 'Delivered' && f.status !== 'Cancelled').length

      // Bugün oluşturulan veya güncellenmiş iş kartları
      const todayWorkItems = workItems.filter(wi => {
        const createdDate = toIstanbulTime(wi.createdAt)
        const updatedDate = toIstanbulTime(wi.updatedAt)
        return createdDate.isAfter(todayStart) || updatedDate.isAfter(todayStart)
      }).length

      // Kritik stoklar (currentStock <= minimumStock)
      const lowStockItems = stocks
        .filter(s => s.isActive && s.currentStock <= s.minimumStock)
        .map(s => ({
          id: s.id,
          name: s.itemName,
          current: s.currentStock,
          minimum: s.minimumStock,
          percentage: s.minimumStock > 0 ? (s.currentStock / s.minimumStock) * 100 : 0
        }))
        .slice(0, 5)

      // Acil öğeler (Acil talepler + Kritik iş kartları)
      const urgentRequests = requests
        .filter(r => r.isUrgent && r.status !== 'Completed' && r.status !== 'Cancelled')
        .map(r => ({
          id: r.id,
          type: 'request',
          title: r.institutionName || 'Talep',
          description: r.description,
          status: r.status,
          createdAt: r.createdAt
        }))

      const criticalWorkItems = workItems
        .filter(wi => wi.priority === 'Critical' && wi.stage !== 'Done' && wi.stage !== 'Cancelled')
        .map(wi => ({
          id: wi.id,
          type: 'workitem',
          title: wi.title,
          description: wi.description,
          status: wi.stage,
          createdAt: wi.createdAt
        }))

      const urgentItems = [...urgentRequests, ...criticalWorkItems]
        .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
        .slice(0, 5)

      // Zamanı geçen işler
      const overdueWorkItems = workItems
        .filter(wi => {
          if (!wi.dueAt || wi.stage === 'Done' || wi.stage === 'Cancelled') return false
          const dueDate = toIstanbulTime(wi.dueAt)
          return dueDate.isBefore(now)
        })
        .map(wi => ({
          id: wi.id,
          type: 'workitem',
          title: wi.title,
          institutionName: wi.institutionName,
          dueAt: wi.dueAt,
          overdueDays: now.diff(toIstanbulTime(wi.dueAt), 'days')
        }))

      // 7 günden eski bekleyen arızalar
      const sevenDaysAgo = now.subtract(7, 'days')
      const oldPendingFaults = faults
        .filter(f => {
          if (f.status !== 'Pending' && f.status !== 'Created') return false
          const createdDate = toIstanbulTime(f.createdAt)
          return createdDate.isBefore(sevenDaysAgo)
        })
        .map(f => ({
          id: f.id,
          type: 'fault',
          title: f.institutionName || 'Arıza',
          description: f.description,
          createdAt: f.createdAt,
          age: now.diff(toIstanbulTime(f.createdAt), 'days')
        }))

      const overdueItems = [...overdueWorkItems, ...oldPendingFaults]
        .sort((a, b) => (b.overdueDays || b.age || 0) - (a.overdueDays || a.age || 0))
        .slice(0, 5)

      // Gönderilecek kargolar (kargo sayfasındaki kartlarla uyumlu)
      const pendingRequestsForShipment = requests.filter(r => r.status === 'Pending' || r.status === 'InProgress')
      const pendingFaultsForShipment = faults.filter(f => f.status === 'ShippingToCustomer')

      const allPendingItems = [
        ...pendingRequestsForShipment.map(r => ({ ...r, _sourceType: 'request' })),
        ...pendingFaultsForShipment.map(f => ({ ...f, _sourceType: 'fault' }))
      ]

      const groupByAddress = (items) => {
        const groups = {}

        items.forEach(item => {
          const instName = item.institutionName || 'Kurum'
          const addressKey = `${normalizeAddress(item.shippingAddressLine || '')}-${normalizeAddress(item.shippingCity || '')}-${normalizeAddress(item.shippingDistrict || '')}`
          const contactKey = `${(item.contactPerson || '').toLowerCase()}-${(item.contactPhone || '').toLowerCase()}`
          const key = `${instName}-${addressKey}-${contactKey}-${item.target || ''}`

          if (!groups[key]) {
            groups[key] = {
              id: key,
              title: instName,
              shippingCity: item.shippingCity,
              shippingDistrict: item.shippingDistrict,
              shippingAddressLine: item.shippingAddressLine,
              contactPerson: item.contactPerson,
              contactPhone: item.contactPhone,
              items: []
            }
          }

          groups[key].items.push(item)
        })

        return Object.values(groups)
      }

      const groupByInstitution = (items) => {
        const groups = {}

        items.forEach(item => {
          const instName = item.institutionName || item.shippingCity || 'Kurum'
          const key = instName

          if (!groups[key]) {
            groups[key] = {
              id: key,
              title: instName,
              shippingCity: item.shippingCity,
              shippingDistrict: item.shippingDistrict,
              shippingAddressLine: item.shippingAddressLine,
              contactPerson: item.contactPerson,
              contactPhone: item.contactPhone,
              items: []
            }
          }

          groups[key].items.push(item)
        })

        return Object.values(groups)
      }

      const ankaraItems = allPendingItems.filter(item => normalizeAddress(item.shippingCity).includes('ankara'))
      const otherItems = allPendingItems.filter(item => !normalizeAddress(item.shippingCity).includes('ankara'))

      const groupedCargoPackages = groupByAddress(otherItems)
      const groupedAnkaraPackages = groupByInstitution(ankaraItems)

      const pendingShipments = groupedCargoPackages.length + groupedAnkaraPackages.length + verifoneShipments.length

      setDashboardData({
        pendingRequests,
        activeFaults,
        todayWorkItems,
        pendingShipments,
        urgentItems,
        overdueItems,
        lowStockItems
      })
    } catch (error) {
      console.error('Dashboard yüklenirken hata:', error)
      message.error('Veriler yüklenirken hata oluştu')
    } finally {
      setLoading(false)
    }
  }


  const getStatusColor = (status) => {
    const colors = {
      Pending: 'orange',
      InProgress: 'blue',
      Completed: 'green',
      Backlog: 'default',
      Done: 'green',
      Cancelled: 'red'
    }
    return colors[status] || 'default'
  }

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
        <Spin size="large">
          <div style={{ padding: 50 }} />
        </Spin>
      </div>
    )
  }

  return (
    <div style={{ padding: isMobile ? '16px 0' : 0 }}>
      {/* Başlık */}
      <div style={{ marginBottom: 24 }}>
        <Title level={2} style={{ margin: 0, display: 'flex', alignItems: 'center', gap: 8 }}>
          <RocketOutlined style={{ color: '#1890ff' }} />
          Dashboard
        </Title>
        <Text type="secondary">Sistem özeti ve hızlı erişim</Text>
      </div>

      {/* Üst İstatistikler */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={12} sm={12} md={6}>
          <Card
            hoverable
            onClick={() => navigate('/requests')}
            style={{
              borderRadius: 12,
              border: 'none',
              background: isDarkMode
                ? 'linear-gradient(135deg, #1890ff 0%, #096dd9 100%)'
                : 'linear-gradient(135deg, #e6f7ff 0%, #bae7ff 100%)',
              cursor: 'pointer'
            }}
          >
            <Statistic
              title={<span style={{ color: isDarkMode ? 'rgba(255,255,255,0.85)' : '#096dd9', fontWeight: 500 }}>Bekleyen Talepler</span>}
              value={dashboardData.pendingRequests}
              prefix={<FileTextOutlined style={{ color: isDarkMode ? '#fff' : '#1890ff' }} />}
              valueStyle={{ color: isDarkMode ? '#fff' : '#1890ff', fontWeight: 700 }}
            />
          </Card>
        </Col>

        <Col xs={12} sm={12} md={6}>
          <Card
            hoverable
            onClick={() => navigate('/faults')}
            style={{
              borderRadius: 12,
              border: 'none',
              background: isDarkMode
                ? 'linear-gradient(135deg, #fa8c16 0%, #d46b08 100%)'
                : 'linear-gradient(135deg, #fff7e6 0%, #ffd591 100%)',
              cursor: 'pointer'
            }}
          >
            <Statistic
              title={<span style={{ color: isDarkMode ? 'rgba(255,255,255,0.85)' : '#d46b08', fontWeight: 500 }}>Aktif Arızalar</span>}
              value={dashboardData.activeFaults}
              prefix={<ToolOutlined style={{ color: isDarkMode ? '#fff' : '#fa8c16' }} />}
              valueStyle={{ color: isDarkMode ? '#fff' : '#fa8c16', fontWeight: 700 }}
            />
          </Card>
        </Col>

        <Col xs={12} sm={12} md={6}>
          <Card
            hoverable
            onClick={() => navigate('/workflow')}
            style={{
              borderRadius: 12,
              border: 'none',
              background: isDarkMode
                ? 'linear-gradient(135deg, #52c41a 0%, #389e0d 100%)'
                : 'linear-gradient(135deg, #f6ffed 0%, #b7eb8f 100%)',
              cursor: 'pointer'
            }}
          >
            <Statistic
              title={<span style={{ color: isDarkMode ? 'rgba(255,255,255,0.85)' : '#389e0d', fontWeight: 500 }}>Bugünkü İşler</span>}
              value={dashboardData.todayWorkItems}
              prefix={<AppstoreOutlined style={{ color: isDarkMode ? '#fff' : '#52c41a' }} />}
              valueStyle={{ color: isDarkMode ? '#fff' : '#52c41a', fontWeight: 700 }}
            />
          </Card>
        </Col>

        <Col xs={12} sm={12} md={6}>
          <Card
            hoverable
            onClick={() => navigate('/shipments')}
            style={{
              borderRadius: 12,
              border: 'none',
              background: isDarkMode
                ? 'linear-gradient(135deg, #722ed1 0%, #531dab 100%)'
                : 'linear-gradient(135deg, #f9f0ff 0%, #d3adf7 100%)',
              cursor: 'pointer'
            }}
          >
            <Statistic
              title={<span style={{ color: isDarkMode ? 'rgba(255,255,255,0.85)' : '#531dab', fontWeight: 500 }}>Gönderilecek Kargolar</span>}
              value={dashboardData.pendingShipments}
              prefix={<CarOutlined style={{ color: isDarkMode ? '#fff' : '#722ed1' }} />}
              valueStyle={{ color: isDarkMode ? '#fff' : '#722ed1', fontWeight: 700 }}
            />
          </Card>
        </Col>
      </Row>

      {/* Orta Kısım - Uyarılar ve Stok */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} md={12}>
          <Card
            title={
              <Space>
                <FireOutlined style={{ color: '#fa8c16' }} />
                <span>Acil Durumlar ve Gecikmiş İşler</span>
                {(dashboardData.urgentItems.length + dashboardData.overdueItems.length) > 0 && (
                  <Badge count={dashboardData.urgentItems.length + dashboardData.overdueItems.length} style={{ backgroundColor: '#f5222d' }} />
                )}
              </Space>
            }
            style={{ borderRadius: 12, height: '100%', minHeight: 400 }}
            styles={{ body: { padding: '16px' } }}
          >
            {dashboardData.urgentItems.length === 0 && dashboardData.overdueItems.length === 0 ? (
              <Empty description="Acil veya gecikmiş iş yok" image={Empty.PRESENTED_IMAGE_SIMPLE} />
            ) : (
              <List
                size="small"
                dataSource={[...dashboardData.urgentItems, ...dashboardData.overdueItems]}
                renderItem={item => (
                  <List.Item
                    onClick={() => {
                      if (item.type === 'request') navigate(`/requests/${item.id}`)
                      else if (item.type === 'workitem') navigate('/workflow')
                      else if (item.type === 'fault') navigate(`/faults/${item.id}`)
                    }}
                    style={{ cursor: 'pointer', padding: '12px 0' }}
                  >
                    <List.Item.Meta
                      avatar={
                        item.type === 'request' ? (
                          <FileTextOutlined style={{ fontSize: 20, color: '#1890ff' }} />
                        ) : item.type === 'workitem' && item.overdueDays !== undefined ? (
                          <ClockCircleOutlined style={{ fontSize: 20, color: '#f5222d' }} />
                        ) : item.type === 'fault' ? (
                          <ToolOutlined style={{ fontSize: 20, color: '#fa8c16' }} />
                        ) : (
                          <ExclamationCircleOutlined style={{ fontSize: 20, color: '#faad14' }} />
                        )
                      }
                      title={
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                          <span style={{ fontWeight: 500 }}>{item.title}</span>
                          {item.overdueDays !== undefined ? (
                            <Tag color="red" style={{ margin: 0 }}>
                              <ClockCircleOutlined style={{ marginRight: 4 }} />
                              {item.overdueDays} gün gecikmiş
                            </Tag>
                          ) : item.age !== undefined ? (
                            <Tag color="orange" style={{ margin: 0 }}>
                              <WarningOutlined style={{ marginRight: 4 }} />
                              {item.age} gün bekliyor
                            </Tag>
                          ) : (
                            <Tag color="red" style={{ margin: 0 }}>
                              <FireOutlined /> ACİL
                            </Tag>
                          )}
                        </div>
                      }
                      description={
                        <div style={{ fontSize: 12 }}>
                          {item.description && <div style={{ marginBottom: 4 }}>{item.description.substring(0, 60)}...</div>}
                          {item.institutionName && <div style={{ color: isDarkMode ? 'rgba(255,255,255,0.65)' : '#8c8c8c' }}>
                            🏢 {item.institutionName}
                          </div>}
                          {item.createdAt && (
                            <div style={{ color: isDarkMode ? 'rgba(255,255,255,0.45)' : '#bfbfbf', marginTop: 4 }}>
                              {toIstanbulTime(item.createdAt).format('DD.MM.YYYY HH:mm')}
                            </div>
                          )}
                        </div>
                      }
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>

        <Col xs={24} md={12}>
          <Card
            title={
              <Space>
                <WarningOutlined style={{ color: '#f5222d' }} />
                <span>Kritik Stok Uyarıları</span>
                {dashboardData.lowStockItems.length > 0 && (
                  <Badge count={dashboardData.lowStockItems.length} style={{ backgroundColor: '#f5222d' }} />
                )}
              </Space>
            }
            style={{ borderRadius: 12, height: '100%', minHeight: 400 }}
            styles={{ body: { padding: '16px' } }}
          >
            {dashboardData.lowStockItems.length === 0 ? (
              <Empty description="Kritik stok yok" image={Empty.PRESENTED_IMAGE_SIMPLE} />
            ) : (
              <List
                size="small"
                dataSource={dashboardData.lowStockItems}
                renderItem={item => (
                  <List.Item
                    onClick={() => navigate('/stock')}
                    style={{ cursor: 'pointer', padding: '12px 0' }}
                  >
                    <List.Item.Meta
                      avatar={<InboxOutlined style={{ fontSize: 20, color: item.percentage <= 50 ? '#f5222d' : '#fa8c16' }} />}
                      title={<span style={{ fontWeight: 500 }}>{item.name}</span>}
                      description={
                        <div style={{ marginTop: 8 }}>
                          <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4, fontSize: 12 }}>
                            <span>Mevcut: {item.current}</span>
                            <span>Minimum: {item.minimum}</span>
                          </div>
                          <Progress
                            percent={Math.min(item.percentage, 100)}
                            strokeColor={item.percentage <= 50 ? '#f5222d' : '#faad14'}
                            size="small"
                            showInfo={false}
                          />
                        </div>
                      }
                    />
                  </List.Item>
                )}
              />
            )}
          </Card>
        </Col>
      </Row>
    </div>
  )
}

export default Dashboard
