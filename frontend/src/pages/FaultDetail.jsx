import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Modal,
  Form,
  Input,
  Select,
  Space,
  Avatar,
  Typography,
  Steps,
  App,
  Row,
  Col,
  Grid
} from 'antd'

import {
  ArrowLeftOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  UserOutlined,
  CommentOutlined,
  SendOutlined
} from '@ant-design/icons'
import api from '../services/api'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import { useTheme } from '../context/ThemeContext'
import { useAuth } from '../context/AuthContext'
import LoadingSpinner from '../components/LoadingSpinner'
dayjs.locale('tr')

const { useBreakpoint } = Grid

const { Title, Text, Paragraph } = Typography
const { TextArea } = Input

// Workflow aşamaları
const WORKFLOW_STAGES = [
  { key: 'Created', label: 'Arıza Kaydı Oluştur', color: 'blue' },
  { key: 'ReceivedByTechnician', label: 'Teknik Ekibe Ulaştı (Tamir Ediliyor)', color: 'cyan' },
  { key: 'ShippingToManufacturer', label: 'Verifone Gönderilecek', color: 'purple' },
  { key: 'SentToService', label: 'Verifone Gönderildi', color: 'purple' },
  { key: 'ReceivedFromManufacturer', label: 'Teknik Ekibe Ulaştı (Tamir Ediliyor)', color: 'geekblue' },
  { key: 'ShippingToCustomer', label: 'Cihaz Kargolanacak', color: 'gold' },
  { key: 'SentToCustomer', label: 'Müşteriye Gönderildi', color: 'gold' }
]

const FaultDetail = () => {
  const { id } = useParams()
  const navigate = useNavigate()
  const { isDarkMode } = useTheme()
  const { user } = useAuth()
  const screens = useBreakpoint()
  const isMobile = !screens.md
  const [fault, setFault] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(false)
  const [statusModalVisible, setStatusModalVisible] = useState(false)
  const [commentModalVisible, setCommentModalVisible] = useState(false)
  const [statusForm] = Form.useForm()
  const [commentForm] = Form.useForm()
  const { message } = App.useApp()


  useEffect(() => {
    fetchFaultDetail()
    fetchHistory()
  }, [id])

  const fetchFaultDetail = async () => {
    try {
      setLoading(true)
      const response = await api.get(`/faults/${id}`)
      setFault(response.data)
    } catch (error) {
      console.error('Fetch fault detail error:', error)
      message.error('Arıza detayları yüklenirken hata oluştu')
      navigate('/faults')
    } finally {

      setLoading(false)
    }
  }

  const fetchHistory = async () => {
    try {
      const response = await api.get(`/faults/${id}/history`)
      // Backend'den gelen veriyi ters çevir (OrderByDescending yerine ascending)
      const sortedHistory = [...response.data].sort((a, b) =>
        new Date(a.createdAt) - new Date(b.createdAt)
      )
      setHistory(sortedHistory)
    } catch (error) {
      console.error('Geçmiş yüklenemedi:', error)
    }
  }

  const getCurrentStageIndex = () => {
    if (!fault) return 0
    // Timeline'da son durum değişikliğinin index'ini bul
    const statusChanges = history.filter(h => h.action === 'StatusChanged' || h.action === 'Created')
    // Son adımın index'i (0-based, Steps component'i için)
    return statusChanges.length > 0 ? statusChanges.length - 1 : 0
  }

  const getStatusColor = (status) => {
    const stage = WORKFLOW_STAGES.find(s => s.key === status)
    return stage?.color || 'default'
  }

  const handleStatusUpdate = async (values) => {
    try {
      const response = await api.put(`/faults/${id}/status`, {
        status: values.status,
        notes: values.notes || '',
        errorCode: values.errorCode || null,
        createDraftServiceForm: false // Default to false to avoid 400 if missing
      })
      message.success('Durum güncellendi')
      setStatusModalVisible(false)
      statusForm.resetFields()
      fetchFaultDetail()
      fetchHistory()
    } catch (error) {
      console.error('Status update error:', error)
      const errorMessage = error.response?.data?.message || error.message || 'Durum güncellenirken hata oluştu'
      message.error(errorMessage)
    }
  }


  const handleAddComment = async (values) => {
    try {
      const response = await api.post(`/faults/${id}/comments`, {
        notes: values.notes
      })
      message.success('Yorum eklendi')
      setCommentModalVisible(false)
      commentForm.resetFields()
      fetchHistory()
    } catch (error) {
      console.error('Comment add error:', error)
      const errorMessage = error.response?.data?.message || error.message || 'Yorum eklenirken hata oluştu'
      message.error(errorMessage)
    }
  }

  const getNextAvailableStatuses = () => {
    if (!fault) return []
    // Tüm durumlar gösterilecek (ShippingToTechnician ve ReceivedFromManufacturer hariç)
    // ReceivedFromManufacturer kaldırıldı çünkü ReceivedByTechnician ile aynı işlevi görüyor
    return WORKFLOW_STAGES.filter(stage =>
      stage.key !== 'ShippingToTechnician' &&
      stage.key !== 'ReceivedFromManufacturer'
    )
  }



  if (loading || !fault) {
    return <LoadingSpinner />
  }

  const currentStageIndex = getCurrentStageIndex()

  return (
    <div style={{ padding: isMobile ? '12px' : '16px', maxWidth: '1400px', margin: '0 auto' }}>
      <div style={{ marginBottom: isMobile ? 16 : 24, display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/faults')}
          size={isMobile ? 'small' : 'middle'}
        >
          Geri
        </Button>
        <Title level={isMobile ? 4 : 3} style={{ margin: 0 }}>
          Arıza #{fault.id}
        </Title>
      </div>

      <Row gutter={[16, 16]}>
        {/* Sol Kolon - Bilgiler */}
        <Col xs={24} lg={12}>
          <Card title="Arıza Bilgileri" style={{ marginBottom: 16 }}>
            <Descriptions
              column={1}
              bordered
              size={isMobile ? 'small' : 'middle'}
              styles={{
                label: {
                  width: isMobile ? '120px' : '200px',
                  fontWeight: 500,
                  fontSize: isMobile ? '12px' : '14px'
                },
                content: {
                  fontSize: isMobile ? '12px' : '14px'
                }
              }}
            >
              <Descriptions.Item label="Cihaz Seri No">
                {fault.deviceSerialNumber}
              </Descriptions.Item>
              <Descriptions.Item label="Açıklama">
                <Paragraph style={{ margin: 0 }}>{fault.description}</Paragraph>
              </Descriptions.Item>
              <Descriptions.Item label="Durum">
                <Tag color={getStatusColor(fault.status)} style={{ fontSize: '14px', padding: '4px 12px' }}>
                  {WORKFLOW_STAGES.find(s => s.key === fault.status)?.label || fault.status}
                </Tag>
              </Descriptions.Item>
              {fault.assignedToUserName && (
                <Descriptions.Item label="Atanan Teknik">
                  <Space>
                    <Avatar icon={<UserOutlined />} size="small" />
                    {fault.assignedToUserName}
                  </Space>
                </Descriptions.Item>
              )}
              <Descriptions.Item label="Oluşturulma Tarihi">
                {dayjs(fault.createdAt).format('DD.MM.YYYY HH:mm')}
              </Descriptions.Item>
              {fault.completedAt && (
                <Descriptions.Item label="Tamamlanma Tarihi">
                  {dayjs(fault.completedAt).format('DD.MM.YYYY HH:mm')}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>

          <Card title="Gönderim Bilgileri">
            <Descriptions
              column={1}
              bordered
              size={isMobile ? 'small' : 'middle'}
              styles={{
                label: {
                  width: isMobile ? '120px' : '200px',
                  fontWeight: 500,
                  fontSize: isMobile ? '12px' : '14px'
                },
                content: {
                  fontSize: isMobile ? '12px' : '14px'
                }
              }}
            >
              {fault.shippingAddressLine && (
                <Descriptions.Item label="Adres">
                  {fault.shippingAddressLine}
                </Descriptions.Item>
              )}
              {fault.shippingCity && (
                <Descriptions.Item label="Şehir">
                  {fault.shippingCity}
                </Descriptions.Item>
              )}
              {fault.shippingDistrict && (
                <Descriptions.Item label="İlçe">
                  {fault.shippingDistrict}
                </Descriptions.Item>
              )}
              {fault.contactPerson && (
                <Descriptions.Item label="İletişim Kişisi">
                  {fault.contactPerson}
                </Descriptions.Item>
              )}
              {fault.contactPhone && (
                <Descriptions.Item label="Telefon">
                  {fault.contactPhone}
                </Descriptions.Item>
              )}
              {fault.contactEmail && (
                <Descriptions.Item label="E-posta">
                  {fault.contactEmail}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>
        </Col>

        {/* Sağ Kolon - Workflow Timeline */}
        <Col xs={24} lg={12}>
          <Card
            title="İşlem Akışı"
            extra={
              <Space wrap size="small" style={{ marginTop: isMobile ? 8 : 0 }}>
                <Button
                  type="primary"
                  icon={<CheckCircleOutlined />}
                  onClick={() => setStatusModalVisible(true)}
                  size={isMobile ? 'small' : 'middle'}
                >
                  {isMobile ? 'Durum' : 'Durum Güncelle'}
                </Button>
                <Button
                  icon={<CommentOutlined />}
                  onClick={() => setCommentModalVisible(true)}
                  size={isMobile ? 'small' : 'middle'}
                >
                  {isMobile ? 'Yorum' : 'Yorum Ekle'}
                </Button>
              </Space>
            }
          >
            {/* Yedek Cihaz Bilgisi - İşlem Akışı içinde */}
            {fault.sendBackupDevice && (() => {
              const isRequestCompleted = fault.backupDeviceRequestStatus === 'Completed'
              const isRequestActive = fault.backupDeviceRequestStatus &&
                fault.backupDeviceRequestStatus !== 'Completed' &&
                fault.backupDeviceRequestStatus !== 'Cancelled'

              // Renk ve stil ayarları
              const bgColor = isRequestCompleted
                ? (isDarkMode
                  ? 'linear-gradient(135deg, rgba(82, 196, 26, 0.15) 0%, rgba(82, 196, 26, 0.08) 100%)'
                  : 'linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%)')
                : (isDarkMode
                  ? 'linear-gradient(135deg, rgba(140, 140, 140, 0.15) 0%, rgba(140, 140, 140, 0.08) 100%)'
                  : 'linear-gradient(135deg, #f5f5f5 0%, #e8e8e8 100%)')

              const borderColor = isRequestCompleted
                ? (isDarkMode ? 'rgba(82, 196, 26, 0.3)' : '#b7eb8f')
                : (isDarkMode ? 'rgba(140, 140, 140, 0.3)' : '#d9d9d9')

              const iconColor = isRequestCompleted ? '#52c41a' : (isDarkMode ? 'rgba(255, 255, 255, 0.45)' : '#8c8c8c')

              return (
                <div style={{
                  marginBottom: isMobile ? 12 : 16,
                  padding: isMobile ? '12px 16px' : '16px 20px',
                  background: bgColor,
                  border: `1px solid ${borderColor}`,
                  borderRadius: isMobile ? '8px' : '12px',
                  boxShadow: isDarkMode
                    ? '0 4px 12px rgba(0, 0, 0, 0.15)'
                    : '0 2px 8px rgba(0, 0, 0, 0.08)'
                }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      {isRequestCompleted ? (
                        <CheckCircleOutlined style={{ color: iconColor, marginRight: 8, fontSize: isMobile ? 16 : 18 }} />
                      ) : (
                        <ClockCircleOutlined style={{ color: iconColor, marginRight: 8, fontSize: isMobile ? 16 : 18 }} />
                      )}
                      <strong style={{ fontSize: isMobile ? '13px' : '15px' }}>
                        {isRequestCompleted
                          ? 'Yedek POS cihazı talebi tamamlandı'
                          : isRequestActive
                            ? 'Yedek POS cihazı talebi aktif'
                            : 'Öncelikli yedek POS cihazı talebi oluşturuldu'}
                      </strong>
                    </div>
                    {fault.backupDeviceRequestId && (
                      <Button
                        type="link"
                        size="small"
                        style={{ paddingLeft: 0, fontWeight: 500, fontSize: isMobile ? '12px' : '14px' }}
                        onClick={() => navigate(`/requests/${fault.backupDeviceRequestId}`)}
                      >
                        {isMobile ? `Talep #${fault.backupDeviceRequestId} →` : `Talep #${fault.backupDeviceRequestId} detayını görüntüle →`}
                      </Button>
                    )}
                  </Space>
                </div>
              )
            })()}

            <Steps
              direction={isMobile ? 'horizontal' : 'vertical'}
              current={currentStageIndex}
              status={(fault.status === 'Delivered' || fault.status === 'SentToCustomer') ? 'finish' : 'process'}
              size={isMobile ? 'small' : 'default'}
              items={(() => {
                // Tüm history kayıtlarını kronolojik sıraya göre düzenle
                const allHistory = [...history].sort((a, b) =>
                  new Date(a.createdAt) - new Date(b.createdAt)
                )

                // Her aşama için birden fazla gerçekleşme olabilir
                // Her gerçekleşmeyi ayrı bir adım olarak göster
                const timelineItems = []

                allHistory.forEach((h, idx) => {
                  // Durum değişikliklerini ve Created action'ını adım olarak göster
                  if (h.action === 'StatusChanged' || h.action === 'Created') {
                    const stage = WORKFLOW_STAGES.find(s => s.key === h.newValue || (h.action === 'Created' && s.key === 'Created'))
                    if (stage) {
                      // Bu aşamaya ait yorumları bul (bu durum değişikliğinden sonra ve bir sonraki durum değişikliğinden önce)
                      const nextStatusChange = allHistory.slice(idx + 1).find(h2 => h2.action === 'StatusChanged')
                      const relatedComments = allHistory.filter(h2 =>
                        h2.action === 'Comment' &&
                        h2.newValue === stage.key &&
                        new Date(h2.createdAt) >= new Date(h.createdAt) &&
                        (!nextStatusChange || new Date(h2.createdAt) < new Date(nextStatusChange.createdAt))
                      )

                      const allRelatedHistory = [h, ...relatedComments].sort((a, b) =>
                        new Date(a.createdAt) - new Date(b.createdAt)
                      )

                      timelineItems.push({
                        title: stage.label,
                        status: 'finish', // Her adım tamamlanmış olarak gösterilir, en son adım process olacak
                        icon: <CheckCircleOutlined />,
                        description: (
                          <div style={{ marginTop: 8 }}>
                            {allRelatedHistory.map((histItem, histIdx) => {
                              // Tüm durum değişiklikleri gösterilecek (not olsun ya da olmasın)

                              return (
                                <div key={histIdx} style={{ marginBottom: isMobile ? 8 : 12, fontSize: isMobile ? '11px' : '12px' }}>
                                  <Space style={{ marginBottom: 4 }} wrap>
                                    <Avatar size={isMobile ? 'small' : 'default'} icon={<UserOutlined />} />
                                    <Text strong style={{ fontSize: isMobile ? '11px' : '12px' }}>{histItem.userName}</Text>
                                    <Text type="secondary" style={{ fontSize: isMobile ? '10px' : '12px' }}>
                                      {dayjs(histItem.createdAt).format(isMobile ? 'DD.MM HH:mm' : 'DD.MM.YYYY HH:mm')}
                                    </Text>
                                    {histItem.action === 'StatusChanged' && (
                                      <Tag color="blue" size="small" style={{ fontSize: isMobile ? '10px' : '12px' }}>Durum Değişikliği</Tag>
                                    )}
                                    {histItem.action === 'Created' && (
                                      <Tag color="blue" size="small" style={{ fontSize: isMobile ? '10px' : '12px' }}>Oluşturuldu</Tag>
                                    )}
                                    {histItem.action === 'Comment' && (
                                      <Tag color="green" size="small" style={{ fontSize: isMobile ? '10px' : '12px' }}>Yorum</Tag>
                                    )}
                                  </Space>
                                  {(histItem.notes || histItem.errorCode) && (
                                    <div style={{
                                      marginTop: isMobile ? 6 : 8,
                                      marginLeft: isMobile ? 0 : 32,
                                      padding: isMobile ? '10px 12px' : '14px 18px',
                                      backgroundColor: isDarkMode
                                        ? (histItem.action === 'Comment'
                                          ? 'rgba(82, 196, 26, 0.12)'
                                          : 'rgba(24, 144, 255, 0.12)')
                                        : (histItem.action === 'Comment'
                                          ? 'rgba(82, 196, 26, 0.08)'
                                          : 'rgba(24, 144, 255, 0.08)'),
                                      borderRadius: '10px',
                                      borderLeft: histItem.action === 'Comment' ? '4px solid #52c41a' : '4px solid #1890ff',
                                      boxShadow: isDarkMode
                                        ? '0 2px 8px rgba(0, 0, 0, 0.2)'
                                        : '0 2px 6px rgba(0, 0, 0, 0.08)',
                                      transition: 'all 0.2s ease'
                                    }}>
                                      {histItem.notes && (
                                        <div style={{
                                          color: 'inherit',
                                          lineHeight: 1.7,
                                          fontSize: isMobile ? '12px' : '13px'
                                        }}>
                                          {histItem.notes}
                                        </div>
                                      )}
                                      {histItem.errorCode && (
                                        <div style={{
                                          marginTop: histItem.notes ? (isMobile ? 6 : 10) : 0,
                                          paddingTop: histItem.notes ? (isMobile ? 6 : 10) : 0,
                                          borderTop: histItem.notes ? (isDarkMode ? '1px solid rgba(255, 255, 255, 0.1)' : '1px solid rgba(0, 0, 0, 0.1)') : 'none'
                                        }}>
                                          <Tag color="purple" style={{ marginTop: 0, fontSize: isMobile ? '11px' : '12px', padding: '2px 8px' }}>
                                            Hata Kodu: {histItem.errorCode}
                                          </Tag>
                                        </div>
                                      )}
                                    </div>
                                  )}
                                </div>
                              )
                            })}
                          </div>
                        )
                      })
                    }
                  }
                })

                // Son adımı process olarak işaretle (eğer Delivered değilse)
                if (timelineItems.length > 0 && fault.status !== 'Delivered' && fault.status !== 'SentToCustomer') {
                  timelineItems[timelineItems.length - 1].status = 'process'
                }

                return timelineItems
              })()}
            />
          </Card>
        </Col>
      </Row>

      {/* Durum Güncelleme Modal */}
      <Modal
        title="Durum Güncelle"
        open={statusModalVisible}
        onCancel={() => {
          setStatusModalVisible(false)
          statusForm.resetFields()
        }}
        footer={null}
        width={isMobile ? '90%' : 600}
      >
        <Form form={statusForm} layout="vertical" onFinish={handleStatusUpdate}>
          <Form.Item
            name="status"
            label="Yeni Durum"
            rules={[{ required: true, message: 'Durum seçin' }]}
          >
            <Select
              placeholder="Durum seçin"
              size="large"
              onChange={(value) => {
                // Verifone Gönderilecek seçilmediyse hata kodunu temizle
                if (value !== 'ShippingToManufacturer') {
                  statusForm.setFieldsValue({ errorCode: undefined })
                }
              }}
            >
              {getNextAvailableStatuses().map(stage => (
                <Select.Option key={stage.key} value={stage.key}>
                  {stage.label}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>

          <Form.Item
            noStyle
            shouldUpdate={(prevValues, currentValues) => prevValues.status !== currentValues.status}
          >
            {({ getFieldValue }) => {
              const selectedStatus = getFieldValue('status')
              if (selectedStatus === 'ShippingToManufacturer') {
                const errorCodes = [
                  { code: 'A1', description: 'Plastik kapak problemleri' },
                  { code: 'B1', description: 'Barkod okuyucu problemleri' },
                  { code: 'C1', description: 'Lens (Ekran Camı) kırık/kirli/tozlu' },
                  { code: 'C2', description: 'Kozmetik amaçlı parça değişimi talep ediliyor' },
                  { code: 'E1', description: 'Ekranda görüntü yok' },
                  { code: 'E2', description: 'Ekran görüntü var fakat arka ışık yanmıyor' },
                  { code: 'E3', description: 'Ekran kırık' },
                  { code: 'E4', description: 'Dokunmatik problemleri' },
                  { code: 'F1', description: 'Tamper' },
                  { code: 'F2', description: 'Terminal kilitleniyor (donuyor)' },
                  { code: 'F3', description: 'Terminal sürekli resetleniyor' },
                  { code: 'G1', description: 'Harici batarya problemleri' },
                  { code: 'G2', description: 'SIM konnektör problemleri' },
                  { code: 'G3', description: 'Printer kapağı problemleri' },
                  { code: 'G4', description: 'Enerji yok' },
                  { code: 'G5', description: 'Spiral kablo problemleri' },
                  { code: 'K1', description: 'Manyetik okuyucu problemleri' },
                  { code: 'K2', description: 'Smart kart okuyucu problemleri' },
                  { code: 'L1', description: 'Sıvı teması' },
                  { code: 'M1', description: 'Dial-up bağlantı problemleri' },
                  { code: 'M2', description: 'Ethernet bağlantı problemleri' },
                  { code: 'M3', description: 'GSM/GPRS bağlantı problemleri' },
                  { code: 'M4', description: 'Yükleme problemleri' },
                  { code: 'M7', description: 'Temassız okuyucu problemleri' },
                  { code: 'M8', description: 'USB problemleri' },
                  { code: 'P1', description: 'Printer problemleri' },
                  { code: 'P2', description: 'Printer silindir problemleri' },
                  { code: 'S1', description: 'Terminal içinde yabancı madde' },
                  { code: 'S2', description: 'Yazılım problemleri' },
                  { code: 'S3', description: 'Key yükleme problemleri' },
                  { code: 'T1', description: 'Ana tuş takımı problemleri' },
                  { code: 'T2', description: 'Fonksiyon tuş takımı problemleri' },
                  { code: 'X', description: 'Deklare edilen sorun yok' }
                ]

                return (
                  <Form.Item
                    name="errorCode"
                    label="Hata Kodu"
                    rules={[{ required: true, message: 'Hata kodu seçin' }]}
                  >
                    <Select
                      placeholder="Hata kodu seçin veya arayın"
                      size="large"
                      showSearch
                      optionFilterProp="children"
                      filterOption={(input, option) => {
                        const searchText = input.toLowerCase()
                        const optionText = String(option?.children || '').toLowerCase()
                        const code = String(option?.value || '').toLowerCase()
                        return (
                          optionText.includes(searchText) ||
                          code.includes(searchText)
                        )
                      }}
                    >
                      {errorCodes.map(error => (
                        <Select.Option key={error.code} value={error.code}>
                          {error.code} - {error.description}
                        </Select.Option>
                      ))}
                    </Select>
                  </Form.Item>
                )
              }
              return null
            }}
          </Form.Item>

          <Form.Item
            name="notes"
            label="Notlar"
          >
            <TextArea
              rows={4}
              placeholder="Durum değişikliği hakkında notlarınızı yazın..."
              maxLength={500}
              showCount
            />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SendOutlined />}
              >
                Güncelle
              </Button>
              <Button onClick={() => {
                setStatusModalVisible(false)
                statusForm.resetFields()
              }}>
                İptal
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Yorum Ekleme Modal */}
      <Modal
        title="Yorum Ekle"
        open={commentModalVisible}
        onCancel={() => {
          setCommentModalVisible(false)
          commentForm.resetFields()
        }}
        footer={null}
        width={isMobile ? '90%' : 600}
      >
        <Form form={commentForm} layout="vertical" onFinish={handleAddComment}>
          <Form.Item
            name="notes"
            label="Yorum"
            rules={[{ required: true, message: 'Yorum yazın' }]}
          >
            <TextArea
              rows={6}
              placeholder="Yorumunuzu yazın..."
              maxLength={1000}
              showCount
            />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" icon={<SendOutlined />}>
                Gönder
              </Button>
              <Button onClick={() => {
                setCommentModalVisible(false)
                commentForm.resetFields()
              }}>
                İptal
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}

export default FaultDetail

