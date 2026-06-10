import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Card, Button, Tag, Space, Input, Select, Badge, Drawer, Modal, Upload, DatePicker,
  Form, Spin, Empty, App, Dropdown, Tooltip, Row, Col, Tabs, Switch, Checkbox, Steps, Avatar, Typography, Image
} from 'antd'
import {
  PlusOutlined, SearchOutlined, FilterOutlined,
  ReloadOutlined, AppstoreOutlined, UnorderedListOutlined,
  ClockCircleOutlined, CheckCircleOutlined,
  PauseCircleOutlined, CloseCircleOutlined, RightOutlined,
  MoreOutlined, EditOutlined, DeleteOutlined, EyeOutlined,
  FireOutlined, WarningOutlined, InboxOutlined, UserOutlined, CommentOutlined, SendOutlined, EnvironmentOutlined, HistoryOutlined,
  FileImageOutlined, FilePdfOutlined, FileExcelOutlined, FileWordOutlined, FileOutlined, DownloadOutlined
} from '@ant-design/icons'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import { turkishCitiesAndDistricts } from '../data/turkishCities'
import { useAuth } from '../context/AuthContext'
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

const toIstanbulTime = (value) => {
  if (!value) return dayjs()
  return dayjs.utc(value).tz('Europe/Istanbul')
}

// İş türleri
const KINDS = [
  { key: 'PlannedWork', label: 'Planlı İş', icon: <ClockCircleOutlined /> }
]

// İş aşamaları
const STAGES = [
  { key: 'Backlog', label: 'Yapılacaklar', icon: <ClockCircleOutlined />, color: 'blue' },
  { key: 'Done', label: 'Tamamlandı', icon: <CheckCircleOutlined />, color: 'green' },
  { key: 'Cancelled', label: 'İptal Edildi', icon: <CloseCircleOutlined />, color: 'red' }
]

// Kanban Column Component
const KanbanColumn = ({ stage, items, onCardClick, onStageChange, loading, isDarkMode }) => {
  const stageInfo = STAGES.find(s => s.key === stage)

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        minHeight: 'calc(100vh - 280px)', // Viewport yüksekliği - header - padding
        backgroundColor: isDarkMode ? '#1a1a1a' : '#f0f2f5',
        borderRadius: 16,
        padding: 12,
        boxShadow: isDarkMode ? 'inset 0 0 10px rgba(0,0,0,0.2)' : 'inset 0 0 10px rgba(0,0,0,0.02)'
      }}
    >
      <div style={{
        padding: '8px 12px 16px 12px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between'
      }}>
        <Space size={12}>
          <div style={{
            fontSize: 18,
            color: stageInfo?.color === 'blue' ? '#1890ff' : stageInfo?.color === 'green' ? '#52c41a' : '#f5222d',
            display: 'flex',
            alignItems: 'center'
          }}>
            {stageInfo?.icon}
          </div>
          <span style={{
            fontSize: 16,
            fontWeight: 700,
            color: isDarkMode ? '#fff' : '#434343'
          }}>
            {stageInfo?.label}
          </span>
        </Space>
        <Badge
          count={items.length}
          style={{
            backgroundColor: isDarkMode ? '#333' : '#fff',
            color: isDarkMode ? '#999' : '#8c8c8c',
            boxShadow: '0 2px 4px rgba(0,0,0,0.05)',
            border: 'none',
            fontSize: 12,
            fontWeight: 600
          }}
        />
      </div>

      <div style={{
        flex: 1,
        overflowY: 'auto',
        paddingRight: 4,
        paddingLeft: 4
      }}>
        {loading ? (
          <div style={{ textAlign: 'center', padding: 40 }}>
            <Spin size="large" />
          </div>
        ) : items.length === 0 ? (
          <div style={{ padding: 40 }}>
            <Empty description="Bu durumda iş yok" image={Empty.PRESENTED_IMAGE_SIMPLE} />
          </div>
        ) : (
          items.map(item => (
            <WorkItemCard
              key={item.id}
              item={item}
              onClick={onCardClick}
              onStageChange={onStageChange}
              isDarkMode={isDarkMode}
              stageColor={stageInfo?.color}
            />
          ))
        )}
      </div>
    </div>
  )
}

// Work Item Card Component
const WorkItemCard = ({ item, onClick, isDarkMode, stageColor }) => {
  const { Text } = Typography

  // Teslim tarihi durumunu ve stilini hesapla
  const getDeadlineInfo = () => {
    if (!item.dueAt) return { color: '#bfbfbf', icon: <ClockCircleOutlined />, label: 'Teslim: Belirtilmedi', isUrgent: false }

    const now = dayjs()
    const due = dayjs(item.dueAt)
    const isOverdue = due.isBefore(now, 'day')
    const isUpcoming = due.isBefore(now.add(2, 'day'), 'day') && !isOverdue

    // Sadece 'Backlog' (Yapılacaklar) aşamasındaysa uyarıları göster
    const isBacklog = item.stage === 'Backlog'

    if (isBacklog && isOverdue) return { color: '#ff4d4f', icon: <WarningOutlined />, label: 'Gecikmiş', isUrgent: true }
    if (isBacklog && isUpcoming) return { color: '#faad14', icon: <ClockCircleOutlined />, label: 'Yaklaşıyor', isUrgent: true }

    return { color: isDarkMode ? '#8c8c8c' : '#8c8c8c', icon: <ClockCircleOutlined />, label: 'Teslim Tarihi', isUrgent: false }
  }

  const deadline = getDeadlineInfo()
  const stripeColor = stageColor === 'blue' ? '#1890ff' : stageColor === 'green' ? '#52c41a' : '#f5222d'

  return (
    <div
      onClick={() => onClick(item)}
      className="work-item-card"
      style={{
        marginBottom: 12,
        cursor: 'pointer',
        transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
        backgroundColor: isDarkMode ? '#262626' : '#fff',
        borderRadius: 12,
        padding: '12px 16px',
        position: 'relative',
        overflow: 'hidden',
        border: 'none',
        boxShadow: isDarkMode ? '0 4px 12px rgba(0,0,0,0.3)' : '0 4px 12px rgba(0,0,0,0.05)',
        ...(deadline.isUrgent && !isDarkMode && { border: `1px solid ${deadline.color}40` }),
        ...(deadline.isUrgent && isDarkMode && { border: `1px solid ${deadline.color}30` })
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = 'translateY(-4px)'
        e.currentTarget.style.boxShadow = isDarkMode ? '0 8px 24px rgba(0,0,0,0.5)' : '0 8px 24px rgba(0,0,0,0.1)'
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = 'translateY(0)'
        e.currentTarget.style.boxShadow = isDarkMode ? '0 4px 12px rgba(0,0,0,0.3)' : '0 4px 12px rgba(0,0,0,0.05)'
      }}
    >
      {/* Sol Renkli Şerit */}
      <div style={{
        position: 'absolute',
        left: 0,
        top: 0,
        bottom: 0,
        width: 4,
        backgroundColor: deadline.isUrgent ? deadline.color : stripeColor,
        borderRadius: '4px 0 0 4px'
      }} />

      <div style={{ marginBottom: 10 }}>
        <Space style={{ width: '100%', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Text style={{
            fontSize: 15,
            fontWeight: 600,
            color: isDarkMode ? '#e8e8e8' : '#262626',
            lineHeight: 1.4
          }}>
            {item.title || 'Başlıksız'}
          </Text>
          <div style={{ display: 'flex', gap: 4 }}>
            {deadline.isUrgent && (
              <Tooltip title={deadline.label}>
                <div style={{
                  padding: '2px 6px',
                  borderRadius: 4,
                  background: `${deadline.color}15`,
                  color: deadline.color,
                  fontSize: 10,
                  fontWeight: 700,
                  display: 'flex',
                  alignItems: 'center',
                }}>
                  {deadline.icon}
                </div>
              </Tooltip>
            )}
            {item.priority === 'Critical' && (
              <div style={{
                padding: '2px 8px',
                borderRadius: 6,
                background: '#fff1f0',
                border: '1px solid #ffa39e',
                color: '#cf1322',
                fontSize: 10,
                fontWeight: 700,
                display: 'flex',
                alignItems: 'center',
                gap: 4
              }}>
                <FireOutlined style={{ fontSize: 12 }} /> ACİL
              </div>
            )}
          </div>
        </Space>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
        {item.institutionName && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <EnvironmentOutlined style={{ fontSize: 12, color: '#8c8c8c' }} />
            <Text type="secondary" style={{ fontSize: 13, color: isDarkMode ? '#8c8c8c' : '#595959' }}>
              {item.institutionName}
            </Text>
          </div>
        )}

        {item.description && (
          <Text
            type="secondary"
            style={{
              fontSize: 12,
              color: isDarkMode ? '#595959' : '#8c8c8c',
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
              overflow: 'hidden'
            }}
          >
            {item.description}
          </Text>
        )}
      </div>

      <div style={{
        marginTop: 12,
        paddingTop: 10,
        borderTop: isDarkMode ? '1px solid #333' : '1px solid #f5f5f5',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center'
      }}>
        <Tooltip title={deadline.label}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
            <ClockCircleOutlined style={{ fontSize: 12, color: deadline.color }} />
            <span style={{
              fontSize: 11,
              color: deadline.color,
              fontWeight: deadline.isUrgent ? 700 : 500
            }}>
              {item.dueAt ? dayjs(item.dueAt).format('DD MMM YYYY') : 'Teslim Belirtilmedi'}
            </span>
          </div>
        </Tooltip>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <Tooltip title="Detay">
            <EyeOutlined style={{ color: '#8c8c8c', fontSize: 14 }} />
          </Tooltip>
        </div>
      </div>
    </div>
  )
}


// Kurum listesi
const WorkFlow = () => {
  const { message, modal } = App.useApp()
  const navigate = useNavigate()
  const { user } = useAuth()
  const { isDarkMode } = useTheme()


  const [kanbanData, setKanbanData] = useState({
    Backlog: [],
    Done: [],
    Cancelled: [],
  })
  const [loading, setloading] = useState(false)
  const [filters, setFilters] = useState({
    kind: null,
    priority: null,
    search: '',
  })
  const [selectedItem, setSelectedItem] = useState(null)
  const [detailDrawerOpen, setDetailDrawerOpen] = useState(false)
  const [workItemHistory, setWorkItemHistory] = useState([])
  const [CommentmodalVisible, setCommentmodalVisible] = useState(false)
  const [commentForm] = Form.useForm()
  const [isMobile, setIsMobile] = useState(window.innerWidth < 768)
  const [activeTab, setActiveTab] = useState('Backlog')
  const [workItemAttachments, setWorkItemAttachments] = useState([])
  const [loadingAttachments, setloadingAttachments] = useState(false)
  const [imagePreviewVisible, setImagePreviewVisible] = useState(false)
  const [currentImageIndex, setCurrentImageIndex] = useState(0)

  useEffect(() => {
    const handleResize = () => setIsMobile(window.innerWidth < 768)
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  const fetchKanbanData = useCallback(async () => {
    try {
      setloading(true)
      const params = new URLSearchParams()
      if (filters.kind) params.append('kind', filters.kind)
      if (filters.priority) params.append('priority', filters.priority)

      if (filters.search) params.append('search', filters.search)

      const response = await api.get(`/workitems/kanban?${params.toString()}`)
      setKanbanData({
        Backlog: response.data.backlog || [],
        Done: response.data.done || [],
        Cancelled: response.data.cancelled || [],
      })
    } catch (error) {
      console.error('Error fetching kanban data:', error)
      message.error('Veriler yüklenirken hata oluştu')
    } finally {
      setloading(false)
    }
  }, [filters, message])

  useEffect(() => {
    fetchKanbanData()
  }, [fetchKanbanData])

  const handleCardClick = async (item) => {
    try {
      const response = await api.get(`/workitems/${item.id}`)
      setSelectedItem(response.data)
      setDetailDrawerOpen(true)
      fetchWorkItemHistory(item.id)
      fetchAttachments(item.id)
    } catch (error) {
      message.error('Detaylar yüklenirken hata oluştu')
    }
  }

  const fetchWorkItemHistory = async (id) => {
    try {
      const response = await api.get(`/workitems/${id}/history`)
      const sortedHistory = [...response.data].sort((a, b) =>
        new Date(a.createdAt) - new Date(b.createdAt)
      )
      setWorkItemHistory(sortedHistory)
    } catch (error) {
      console.error('Geçmiş yüklenemedi:', error)
    }
  }

  // File attachment helper functions
  const getFileType = (fileName) => {
    if (!fileName) return 'other'
    const ext = fileName.split('.').pop().toLowerCase()
    if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'].includes(ext)) return 'image'
    if (ext === 'pdf') return 'pdf'
    if (['xlsx', 'xls', 'csv'].includes(ext)) return 'excel'
    if (['docx', 'doc'].includes(ext)) return 'word'
    return 'other'
  }

  const getFileIcon = (fileName) => {
    const type = getFileType(fileName)
    const iconProps = { style: { fontSize: 24 } }
    switch (type) {
      case 'image':
        return <FileImageOutlined {...iconProps} style={{ ...iconProps.style, color: '#52c41a' }} />
      case 'pdf':
        return <FilePdfOutlined {...iconProps} style={{ ...iconProps.style, color: '#f5222d' }} />
      case 'excel':
        return <FileExcelOutlined {...iconProps} style={{ ...iconProps.style, color: '#107c10' }} />
      case 'word':
        return <FileWordOutlined {...iconProps} style={{ ...iconProps.style, color: '#2b579a' }} />
      default:
        return <FileOutlined {...iconProps} style={{ ...iconProps.style, color: '#8c8c8c' }} />
    }
  }

  const formatFileSize = (bytes) => {
    if (!bytes) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  const fetchAttachments = async (workItemId) => {
    try {
      setloadingAttachments(true)
      const response = await api.get(`/workitems/${workItemId}/attachments`)
      setWorkItemAttachments(response.data || [])
    } catch (error) {
      console.error('Error fetching attachments:', error)
      message.error('Dosyalar yüklenirken hata oluştu')
    } finally {
      setloadingAttachments(false)
    }
  }

  const handleFileClick = async (attachment) => {
    try {
      const type = getFileType(attachment.fileName)

      // Axios ile dosyayı blob olarak indir (Authorization header ile)
      const response = await api.get(
        `/workitems/${selectedItem.id}/attachments/${attachment.id}/download`,
        { responseType: 'blob' }
      )

      // Blob URL oluştur
      const blob = new Blob([response.data], { type: response.headers['content-type'] })
      const url = window.URL.createObjectURL(blob)

      if (type === 'pdf' || type === 'image') {
        // PDF ve resimleri yeni sekmede aç
        window.open(url, '_blank')
        setTimeout(() => window.URL.revokeObjectURL(url), 100)
      } else {
        // Diğer dosyaları indir
        const link = document.createElement('a')
        link.href = url
        link.download = attachment.fileName
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        window.URL.revokeObjectURL(url)
      }
    } catch (error) {
      console.error('Dosya açılamadı:', error)
      const errorMsg = error.response?.data?.message || 'Dosya açılamadı'
      message.error(errorMsg)
    }
  }

  const handleDeleteAttachment = async (attachmentId) => {
    try {
      await api.delete(`/workitems/${selectedItem.id}/attachments/${attachmentId}`)
      message.success('Dosya silindi')
      fetchAttachments(selectedItem.id)
    } catch (error) {
      console.error('Error deleting attachment:', error)
      message.error('Dosya silinirken hata oluştu')
    }
  }

  const handleStageChange = async (id, newStage) => {

    try {
      await api.patch(`/workitems/${id}/stage`, { stage: newStage })
      message.success('Durum güncellendi')
      fetchKanbanData()
    } catch (error) {
      message.error('Durum güncellenirken hata oluştu')
    }
  }


  const handleDelete = (id) => {
    modal.confirm({
      title: 'İşi Sil',
      content: 'Bu işi silmek istediğinizden emin misiniz?',
      okText: 'Evet, Sil',
      okType: 'danger',
      cancelText: 'İptal',
      onOk: async () => {
        try {
          await api.delete(`/workitems/${id}`)
          message.success('İş silindi')
          setDetailDrawerOpen(false)
          fetchKanbanData()
        } catch (error) {
          message.error('İş silinirken hata oluştu')
        }
      }
    })
  }

  // Mobil sekmeli görünüm
  const renderMobileView = () => {
    const tabItems = STAGES.map(stage => ({
      key: stage.key,
      label: (
        <Space>
          {stage.icon}
          {stage.label}
          <Badge count={kanbanData[stage.key]?.length || 0} size="small" />
        </Space>
      ),
      children: (
        <div style={{ padding: '12px 0' }}>
          {kanbanData[stage.key]?.length === 0 ? (
            <Empty description="Bu durumda iş yok" />
          ) : (
            kanbanData[stage.key]?.map(item => (
              <WorkItemCard
                key={item.id}
                item={item}
                onClick={handleCardClick}
                onStageChange={handleStageChange}
                isDarkMode={isDarkMode}
              />
            ))
          )}
        </div>
      )
    }))

    return (
      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        items={tabItems}
        tabPosition="top"
        style={{ marginTop: 16 }}
      />
    )
  }


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
            <AppstoreOutlined />
          </div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
            İş Akışı
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
            placeholder="İş veya kurum ara..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={filters.search}
            onChange={e => setFilters(f => ({ ...f, search: e.target.value }))}
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
            onClick={() => navigate('/workflow/new')}
            style={{
              height: 42,
              borderRadius: 12,
              padding: '0 24px',
              fontWeight: 600,
              boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)'
            }}
          >
            Yeni İş
          </Button>
        </div>
      </div>

      {/* Kanban Board */}
      {isMobile ? renderMobileView() : (
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, minmax(320px, 1fr))',
          gap: 16,
          paddingBottom: 16,
          marginTop: 16,
          alignItems: 'stretch',
        }}>
          {STAGES.map(stage => (
            <KanbanColumn
              key={stage.key}
              stage={stage.key}
              items={kanbanData[stage.key] || []}
              onCardClick={handleCardClick}
              onStageChange={handleStageChange}
              loading={loading}
              isDarkMode={isDarkMode}
            />
          ))}
        </div>
      )}


      {/* Detail Drawer */}
      <Drawer
        title={
          <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            <span style={{ fontSize: 13, color: isDarkMode ? 'rgba(255, 255, 255, 0.45)' : '#8c8c8c', fontWeight: 400 }}>
              {KINDS.find(k => k.key === selectedItem?.kind)?.label || 'İş Detayı'}
            </span>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <span style={{ fontSize: 18, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                {selectedItem?.title}
              </span>
              {selectedItem?.priority === 'Critical' && (
                <Tag color="#f50" icon={<FireOutlined />} style={{ borderRadius: 4, margin: 0, fontWeight: 600 }}>
                  ACİL
                </Tag>
              )}
            </div>
          </div>
        }
        open={detailDrawerOpen}
        onClose={() => setDetailDrawerOpen(false)}
        width={750}
        extra={
          <Space>
            {selectedItem?.relatedServiceFormId && (
              <Button
                type="primary"
                icon={<HistoryOutlined />}
                onClick={() => navigate(`/service-forms/${selectedItem.relatedServiceFormId}/view`)}
                style={{ borderRadius: 6, color: '#ffffff', fontWeight: 600 }}
              >
                Servis Formu
              </Button>
            )}
            <Button
              icon={<EditOutlined />}
              onClick={() => navigate(`/workflow/${selectedItem.id}/edit`)}
              style={{ borderRadius: 6 }}
            >
              Düzenle
            </Button>
            <Dropdown
              menu={{
                items: STAGES.filter(s => s.key !== selectedItem?.stage).map(stage => ({
                  key: stage.key,
                  label: `Durumu Değiştir: ${stage.label}`,
                  icon: stage.icon,
                  onClick: () => handleStageChange(selectedItem.id, stage.key)
                }))
              }}
              trigger={['click']}
            >
              <Button icon={<MoreOutlined />} style={{ borderRadius: 6 }} />
            </Dropdown>
          </Space>
        }
        styles={{
          header: {
            background: isDarkMode ? '#141414' : '#ffffff',
            borderBottom: isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #f0f0f0',
            padding: '20px 24px'
          },
          body: {
            background: isDarkMode ? '#0a0e14' : '#fafafa',
            padding: '24px'
          }
        }}
        className="workflow-detail-drawer"
      >
        {selectedItem && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
            {/* �st Bilgi Paneli */}
            <div style={{
              background: isDarkMode ? '#131720' : '#fff',
              borderRadius: 12,
              padding: '20px',
              border: isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #eef0f2',
              boxShadow: '0 2px 8px rgba(0,0,0,0.04)'
            }}>
              <Row gutter={[24, 24]}>
                <Col span={24}>
                  <div style={{ fontSize: 12, color: '#8c8c8c', marginBottom: 4, textTransform: 'uppercase', letterSpacing: 1 }}>KURUM / FİRMA</div>
                  <div style={{ fontSize: 20, fontWeight: 700, color: '#1890ff' }}>
                    <AppstoreOutlined style={{ marginRight: 8 }} />
                    {selectedItem.institutionName || 'Belirtilmemiş'}
                  </div>
                </Col>

                <Col xs={12} md={8}>
                  <div style={{ fontSize: 12, color: '#8c8c8c', marginBottom: 8 }}>Mevcut Durum</div>
                  <Tag
                    color={STAGES.find(s => s.key === selectedItem.stage)?.color}
                    style={{ fontSize: 13, padding: '4px 12px', borderRadius: 6, margin: 0, border: 'none' }}
                  >
                    {STAGES.find(s => s.key === selectedItem.stage)?.label}
                  </Tag>
                </Col>

                <Col xs={12} md={8}>
                  <div style={{ fontSize: 12, color: '#8c8c8c', marginBottom: 8 }}>Oluşturulma</div>
                  <Space size={4} style={{ fontSize: 14, color: isDarkMode ? '#fff' : '#262626', fontWeight: 500 }}>
                    <ClockCircleOutlined style={{ color: '#8c8c8c' }} />
                    {toIstanbulTime(selectedItem.createdAt).format('DD.MM.YYYY HH:mm')}
                  </Space>
                </Col>

                {selectedItem.dueAt && (
                  <Col xs={24} md={8}>
                    <div style={{ fontSize: 12, color: '#8c8c8c', marginBottom: 8 }}>Hedef Tarih</div>
                    <Space size={4} style={{ fontSize: 14, color: '#1890ff', fontWeight: 600 }}>
                      <ClockCircleOutlined />
                      {toIstanbulTime(selectedItem.dueAt).format('DD.MM.YYYY')}
                      <Tag color="blue" style={{ marginLeft: 8, fontSize: 11 }}>{toIstanbulTime(selectedItem.dueAt).fromNow()}</Tag>
                    </Space>
                  </Col>
                )}
              </Row>
            </div>

            {/* Aklama Kart */}
            {selectedItem.description && (
              <Card
                title={<Space><UnorderedListOutlined style={{ color: '#1890ff' }} /><span>Açıklama Detayları</span></Space>}
                styles={{ body: { padding: '16px' } }}
                style={{ borderRadius: 12, border: 'none', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}
              >
                <Typography.Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap', fontSize: 14, lineHeight: '1.6', color: isDarkMode ? '#cbd2d9' : '#262626' }}>
                  {selectedItem.description}
                </Typography.Paragraph>
              </Card>
            )}

            {/* letiim Bilgileri */}
            {(selectedItem.contactPerson || selectedItem.contactPhone || selectedItem.shippingAddressLine) && (
              <Card
                title={<Space><UserOutlined style={{ color: '#52c41a' }} /><span>İletişim & Adres</span></Space>}
                styles={{ body: { padding: '16px' } }}
                style={{ borderRadius: 12, border: 'none', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}
              >
                <Row gutter={[16, 16]}>
                  {selectedItem.contactPerson && (
                    <Col span={12}>
                      <div style={{ fontSize: 12, color: '#8c8c8c' }}>İlgili Kişi</div>
                      <div style={{ fontWeight: 500 }}>{selectedItem.contactPerson}</div>
                    </Col>
                  )}
                  {selectedItem.contactPhone && (
                    <Col span={12}>
                      <div style={{ fontSize: 12, color: '#8c8c8c' }}>Telefon</div>
                      <div style={{ fontWeight: 500 }}>{selectedItem.contactPhone}</div>
                    </Col>
                  )}
                  {selectedItem.shippingCity && (
                    <Col span={24}>
                      <div style={{ fontSize: 12, color: '#8c8c8c' }}>Lokasyon</div>
                      <div style={{ fontWeight: 500 }}>
                        <EnvironmentOutlined style={{ marginRight: 6 }} />
                        {selectedItem.shippingCity} {selectedItem.shippingDistrict && `/ ${selectedItem.shippingDistrict}`}
                      </div>
                    </Col>
                  )}
                  {selectedItem.shippingAddressLine && (
                    <Col span={24}>
                      <div style={{ fontSize: 12, color: '#8c8c8c' }}>Açık Adres</div>
                      <div style={{ fontSize: 13, color: isDarkMode ? '#8b94a1' : '#595959' }}>{selectedItem.shippingAddressLine}</div>
                    </Col>
                  )}
                </Row>
              </Card>
            )}

            {/* İşlem Gemii */}
            <Card
              title={<Space><HistoryOutlined style={{ color: '#1890ff' }} /><span>İşlem Geçmişi & Yorumlar</span></Space>}
              styles={{ body: { padding: '20px' } }}
              style={{ borderRadius: 12, border: 'none', boxShadow: '0 2px 8px rgba(0,0,0,0.04)' }}
              extra={
                <Button
                  type="primary"
                  size="small"
                  onClick={() => setCommentmodalVisible(true)}
                  icon={<CommentOutlined />}
                  style={{ borderRadius: 6 }}
                >
                  Yorum & Dosya
                </Button>
              }
            >
              <Steps
                direction="vertical"
                size="small"
                className="workflow-steps"
                items={(() => {
                  // Eskiden yeniye (Kronolojik) sırala (Ascending)
                  const allHistory = [...workItemHistory].sort((a, b) =>
                    dayjs(a.createdAt).valueOf() - dayjs(b.createdAt).valueOf()
                  )

                  const timelineItems = []

                  // Tm gemii grupla
                  // Tüm geçmişi grupla
                  let currentGroup = null
                  const groupedHistory = []

                  allHistory.forEach((item) => {
                    if (item.action === 'StageChanged' || item.action === 'Created') {
                      // Varsa önceki yorum grubunu ekle
                      if (currentGroup) {
                        groupedHistory.push(currentGroup)
                        currentGroup = null
                      }

                      // Durum değişikliğini ekle
                      groupedHistory.push({ type: 'single', item })
                    } else if (item.action === 'CommentAdded' || item.action === 'AttachmentAdded') {
                      // Yorum veya dosya
                      if (!currentGroup) {
                        currentGroup = {
                          type: 'group',
                          userName: item.userName,
                          createdAt: item.createdAt,
                          items: [item]
                        }
                      } else {
                        // Aynı kullanıcı ve yakın zaman (1dk içinde)?
                        const timeDiff = dayjs(currentGroup.createdAt).diff(dayjs(item.createdAt), 'millisecond')
                        if (currentGroup.userName === item.userName && Math.abs(timeDiff) < 60000) {
                          currentGroup.items.push(item)
                        } else {
                          groupedHistory.push(currentGroup)
                          currentGroup = {
                            type: 'group',
                            userName: item.userName,
                            createdAt: item.createdAt,
                            items: [item]
                          }
                        }
                      }
                    }
                  })

                  // Son grubu ekle
                  if (currentGroup) {
                    groupedHistory.push(currentGroup)
                  }

                  // Grupları timeline'a ekle
                  groupedHistory.forEach(group => {
                    if (group.type === 'single') {
                      const item = group.item
                      const stage = STAGES.find(s => s.key === item.newValue || (item.action === 'Created' && s.key === 'Backlog'))
                      if (stage) {
                        timelineItems.push({
                          status: 'finish',
                          title: stage.key === 'Backlog' ? null : (
                            <Space>
                              {stage.icon}
                              <span style={{ fontWeight: 600 }}>{stage.label}</span>
                            </Space>
                          ),
                          description: (
                            <div style={{ fontSize: 12, color: '#8c8c8c' }}>
                              {toIstanbulTime(item.createdAt).format('DD.MM.YYYY HH:mm')} - {item.userName}
                            </div>
                          )
                        })
                      }
                    } else {
                      // Grup (Yorumlar/Dosyalar)
                      timelineItems.push({
                        status: 'finish',
                        description: (
                          <div style={{
                            marginTop: 12,
                            padding: '8px 12px',
                            background: isDarkMode ? '#141414' : '#f9f9f9',
                            borderRadius: 6,
                            borderLeft: '3px solid #1890ff'
                          }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                              <Space size={6}>
                                <Avatar size={20} icon={<UserOutlined />} style={{ background: '#1890ff' }} />
                                <span style={{ fontWeight: 600, fontSize: 12, color: isDarkMode ? '#fff' : '#262626' }}>{group.userName}</span>
                              </Space>
                              <span style={{ fontSize: 11, color: isDarkMode ? 'rgba(255, 255, 255, 0.65)' : '#8c8c8c' }}>
                                {toIstanbulTime(group.createdAt).format('DD.MM.YYYY HH:mm')}
                              </span>
                            </div>

                            {group.items.map((subItem, sIdx) => (
                              <div key={sIdx} style={{ marginBottom: sIdx < group.items.length - 1 ? 8 : 0 }}>
                                {subItem.notes && subItem.action !== 'AttachmentAdded' && (
                                  <div style={{ fontSize: 13, color: isDarkMode ? '#fff' : '#262626', marginLeft: 26 }}>
                                    {subItem.notes}
                                  </div>
                                )}

                                {subItem.action === 'AttachmentAdded' && (() => {
                                  const fileName = subItem.newValue
                                  const fileType = getFileType(fileName)
                                  const handleAttachmentClick = async (e) => {
                                    e.preventDefault()
                                    try {
                                      // subItem.notes formatı: /api/workitems/{id}/attachments/{attachmentId}/download
                                      // api instance'ının baseURL'i zaten /api ile bittiği için başındaki /api'yi atıyoruz
                                      const downloadPath = subItem.notes?.replace(/^\/api/, '')
                                      if (!downloadPath) return

                                      // Axios ile dosyayı blob olarak indir
                                      const response = await api.get(downloadPath, { responseType: 'blob' })

                                      const blob = new Blob([response.data], { type: response.headers['content-type'] })
                                      const url = window.URL.createObjectURL(blob)

                                      if (fileType === 'pdf' || fileType === 'image') {
                                        window.open(url, '_blank')
                                        setTimeout(() => window.URL.revokeObjectURL(url), 100)
                                      } else {
                                        const link = document.createElement('a')
                                        link.href = url
                                        link.download = fileName
                                        document.body.appendChild(link)
                                        link.click()
                                        document.body.removeChild(link)
                                        window.URL.revokeObjectURL(url)
                                      }
                                    } catch (error) {
                                      console.error('Dosya açılamadı:', error)
                                      const errorMsg = error.response?.data?.message || 'Dosya açılamadı'
                                      message.error(errorMsg)
                                    }
                                  }

                                  return (
                                    <div style={{ fontSize: 13, color: isDarkMode ? '#fff' : '#262626', marginLeft: 26, display: 'flex', alignItems: 'center', gap: 8 }}>
                                      {getFileIcon(fileName)}
                                      <a
                                        href="#"
                                        onClick={handleAttachmentClick}
                                        style={{ fontWeight: 500, color: '#1890ff', textDecoration: 'none', cursor: 'pointer' }}
                                        onMouseEnter={(e) => e.target.style.textDecoration = 'underline'}
                                        onMouseLeave={(e) => e.target.style.textDecoration = 'none'}
                                      >
                                        {fileName}
                                      </a>
                                      <span style={{ fontSize: 11, color: '#8c8c8c' }}>
                                        {fileType === 'pdf' ? '(PDF)' : fileType === 'image' ? '(Görüntü)' : fileType === 'excel' ? '(Excel)' : fileType === 'word' ? '(Word)' : ''}
                                      </span>
                                    </div>
                                  )
                                })()}
                              </div>
                            ))}
                          </div>
                        )
                      })
                    }
                  })

                  if (timelineItems.length > 0) {
                    timelineItems[timelineItems.length - 1].status = 'process'
                  }

                  return timelineItems
                })()}
              />
            </Card>
          </div>
        )
        }
      </Drawer >


      {/* Yorum ve Dosya Ekleme Modal */}
      < Modal
        title="Yorum veya Dosya Ekle"
        open={CommentmodalVisible}
        onCancel={() => {
          setCommentmodalVisible(false)
          commentForm.resetFields()
        }}
        footer={null}
        width={500}
      >
        <Form
          form={commentForm}
          layout="vertical"
          onFinish={async (values) => {
            try {
              // 1. Dosya varsa yükle
              if (values.files && values.files.length > 0) {
                for (const file of values.files) {
                  const formData = new FormData()
                  formData.append('file', file.originFileObj)
                  await api.post(`/workitems/${selectedItem.id}/attachments`, formData, {
                    headers: { 'Content-Type': 'multipart/form-data' }
                  })
                }
              }

              // 2. Yorum varsa ekle
              if (values.comment) {
                await api.post(`/workitems/${selectedItem.id}/comments`, {
                  content: values.comment
                })
              }

              if (!values.comment && (!values.files || values.files.length === 0)) {
                message.warning('Yorum veya dosya eklemediniz')
                return
              }

              message.success('Başarıyla eklendi')
              setCommentmodalVisible(false)
              commentForm.resetFields()
              fetchWorkItemHistory(selectedItem.id)
              fetchAttachments(selectedItem.id)
            } catch (error) {
              console.error(error)
              message.error('Hata oluştu')
            }
          }}
        >
          <Form.Item
            name="files"
            label="Dosya Ekle (Opsiyonel)"
            valuePropName="fileList"
            getValueFromEvent={(e) => {
              if (Array.isArray(e)) return e
              return e?.fileList
            }}
          >
            <Upload.Dragger
              name="files"
              multiple
              beforeUpload={() => false}
            >
              <p className="ant-upload-drag-icon"><InboxOutlined /></p>
              <p className="ant-upload-text">Dosyaları buraya sürükleyin</p>
            </Upload.Dragger>
          </Form.Item>

          <Form.Item
            name="comment"
            label="Yorum"
          >
            <Input.TextArea
              rows={4}
              placeholder="Mesajınızı yazın..."
              maxLength={1000}
              showCount
            />
          </Form.Item>

          <Form.Item>
            <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
              <Button onClick={() => {
                setCommentmodalVisible(false)
                commentForm.resetFields()
              }}>
                İptal
              </Button>
              <Button type="primary" htmlType="submit" icon={<SendOutlined />}>
                Gönder
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal >

      <style>{`
        .work-item-card:hover {
          box-shadow: 0 4px 12px rgba(0,0,0,0.1);
          transform: translateY(-2px);
        }
        .workflow-steps .ant-steps-item-title {
          width: 100%;
        }
        
        /* Drawer Styling - Remove all orange backgrounds */
        .workflow-detail-drawer .ant-drawer-header {
          background: ${isDarkMode ? '#141414' : '#ffffff'} !important;
          border-bottom: ${isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #f0f0f0'} !important;
        }
        
        .workflow-detail-drawer .ant-drawer-body {
          background: ${isDarkMode ? '#0a0e14' : '#fafafa'} !important;
        }
        
        .workflow-detail-drawer .ant-drawer-content {
          background: ${isDarkMode ? '#141414' : '#ffffff'} !important;
        }
        
        /* Card Styling */
        .workflow-detail-drawer .ant-card {
          background: ${isDarkMode ? '#1a1a1a' : '#ffffff'} !important;
          border: ${isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #f0f0f0'} !important;
        }
        
        .workflow-detail-drawer .ant-card-head {
          background: ${isDarkMode ? '#1a1a1a' : '#ffffff'} !important;
          border-bottom: ${isDarkMode ? '1px solid rgba(255, 255, 255, 0.08)' : '1px solid #f0f0f0'} !important;
        }
        
        .workflow-detail-drawer .ant-card-body {
          background: ${isDarkMode ? '#1a1a1a' : '#ffffff'} !important;
        }
      `}</style>
    </div >
  )
}

export default WorkFlow



