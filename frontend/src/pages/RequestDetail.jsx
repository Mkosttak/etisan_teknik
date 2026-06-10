import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Card,
  Descriptions,
  Tag,
  Button,
  Space,
  Typography,
  App,
  Spin,
  Row,
  Col
} from 'antd'
import {
  ArrowLeftOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons'
import api from '../services/api'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
import { useTheme } from '../context/ThemeContext'
import { Grid } from 'antd'
dayjs.locale('tr')

const { Title } = Typography

const RequestDetail = () => {
  const { message } = App.useApp()
  const { id } = useParams()
  const navigate = useNavigate()
  const { isDarkMode } = useTheme()
  const screens = Grid.useBreakpoint()
  const isMobile = !screens.md
  const [request, setRequest] = useState(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    fetchRequestDetail()
  }, [id])

  const fetchRequestDetail = async () => {
    try {
      setLoading(true)
      const response = await api.get(`/requests/${id}`)
      setRequest(response.data)
    } catch (error) {
      message.error('Talep detayları yüklenirken hata oluştu')
      navigate('/requests')
    } finally {
      setLoading(false)
    }
  }

  const statusColors = {
    Pending: 'orange',
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

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    )
  }

  if (!request) {
    return null
  }

  return (
    <div style={{ padding: isMobile ? '12px' : '16px' }}>
      <div style={{ marginBottom: isMobile ? 12 : 16 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/requests')}
          size={isMobile ? 'small' : 'middle'}
        >
          Geri
        </Button>
      </div>

      <Card>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <div style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: isMobile ? 'flex-start' : 'center',
            flexDirection: isMobile ? 'column' : 'row',
            gap: isMobile ? 12 : 0
          }}>
            <Title level={isMobile ? 4 : 3} style={{ margin: 0 }}>Talep Detayı #{request.id}</Title>
            <Space wrap>
              {request.isUrgent && (
                <Tag color="red" style={{ fontSize: isMobile ? 12 : 14, padding: isMobile ? '2px 8px' : '4px 12px', fontWeight: 'bold' }}>
                  🚨 ACİL
                </Tag>
              )}
              <Tag color={statusColors[request.status]} style={{ fontSize: isMobile ? 12 : 14, padding: isMobile ? '2px 8px' : '4px 12px' }}>
                {statusLabels[request.status]}
              </Tag>
            </Space>
          </div>

          <Card title="Firma Bilgileri" size="small">
            <Descriptions
              column={1}
              bordered
              size="small"
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
              <Descriptions.Item label="Firma Adı">
                {request.institutionName || '-'}
              </Descriptions.Item>
              {request.institutionName === 'DİĞER' && request.institutionOtherName && (
                <Descriptions.Item label="Diğer Firma Adı">
                  {request.institutionOtherName}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>

          <Card title="Talep Bilgileri" size="small">
            <Descriptions
              column={1}
              bordered
              size="small"
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
              <Descriptions.Item label="Açıklama">
                {request.description || '-'}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          {request.stockItems && request.stockItems.length > 0 && (
            <Card title="Gönderilen Stok Ürünleri" size="small">
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {request.stockItems.map((item, index) => {
                  // isCancelled kontrolü
                  const isCancelled = item.isCancelled === true || item.IsCancelled === true
                  const isShipped = item.isShipped === true || item.IsShipped === true

                  return (
                    <div
                      key={index}
                      style={{
                        padding: isMobile ? '12px 16px' : '16px 20px',
                        border: isDarkMode
                          ? `2px solid ${isCancelled ? 'rgba(255, 77, 79, 0.4)' : (isShipped ? 'rgba(82, 196, 26, 0.4)' : 'rgba(255, 255, 255, 0.1)')}`
                          : `2px solid ${isCancelled ? '#ff4d4f' : (isShipped ? '#b7eb8f' : '#d9d9d9')}`,
                        borderRadius: isMobile ? '8px' : '12px',
                        background: isDarkMode
                          ? isCancelled
                            ? 'linear-gradient(135deg, rgba(255, 77, 79, 0.1) 0%, rgba(255, 77, 79, 0.05) 100%)'
                            : (isShipped
                              ? 'linear-gradient(135deg, rgba(82, 196, 26, 0.1) 0%, rgba(82, 196, 26, 0.05) 100%)'
                              : 'linear-gradient(135deg, rgba(255, 255, 255, 0.05) 0%, rgba(255, 255, 255, 0.02) 100%)')
                          : isCancelled
                            ? 'linear-gradient(135deg, #fff1f0 0%, #ffe7e6 100%)'
                            : (isShipped
                              ? 'linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%)'
                              : 'linear-gradient(135deg, #fafafa 0%, #f0f0f0 100%)'),
                        opacity: isCancelled ? 0.7 : 1,
                        boxShadow: isDarkMode
                          ? '0 2px 8px rgba(0, 0, 0, 0.2)'
                          : '0 2px 6px rgba(0, 0, 0, 0.08)',
                        transition: 'all 0.2s ease'
                      }}
                    >
                      <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: isMobile ? 'flex-start' : 'center',
                        flexDirection: isMobile ? 'column' : 'row',
                        gap: isMobile ? 8 : 0
                      }}>
                        <div style={{ flex: 1, width: isMobile ? '100%' : 'auto' }}>
                          <strong style={{
                            textDecoration: isCancelled ? 'line-through' : 'none',
                            color: isDarkMode
                              ? (isCancelled ? 'rgba(255, 255, 255, 0.5)' : 'var(--text-primary)')
                              : (isCancelled ? '#8c8c8c' : '#000'),
                            fontSize: isMobile ? '13px' : '15px',
                            fontWeight: 600
                          }}>
                            {item.itemName}
                          </strong>
                          <div style={{ marginTop: isMobile ? 6 : 8 }}>
                            {isCancelled && (
                              <Tag color="red" style={{ marginRight: 8, fontSize: isMobile ? '10px' : '12px', padding: '2px 8px' }}>İptal</Tag>
                            )}
                            {isShipped && (
                              <Tag color="green" icon={<CheckCircleOutlined />} style={{ fontSize: isMobile ? '10px' : '12px', padding: '2px 8px' }}>
                                Gönderildi
                              </Tag>
                            )}
                          </div>
                        </div>
                        <Tag
                          color={isCancelled ? 'red' : (isShipped ? 'green' : 'blue')}
                          style={{
                            fontSize: isMobile ? 13 : 15,
                            fontWeight: 600,
                            padding: isMobile ? '4px 10px' : '6px 14px',
                            borderRadius: '8px',
                            boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
                            marginTop: isMobile ? 4 : 0
                          }}
                        >
                          <span style={{ textDecoration: isCancelled ? 'line-through' : 'none' }}>
                            {item.quantity} Adet
                          </span>
                        </Tag>
                      </div>
                    </div>
                  )
                })}
              </div>
            </Card>
          )}

          {request.extraItems && (
            <Card title="Ekstra Ürünler (Stoğumuzda Yok)" size="small">
              <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {request.extraItems.split(',').map((itemRaw, index) => {
                  const item = itemRaw.trim()
                  if (!item) return null

                  // Check shipment status
                  const shippedList = (request.shippedExtraItems || '').split(',').map(s => s.trim())
                  const isShipped = shippedList.includes(item)

                  // Format: "2x HDMI Kablo" -> quantity ve name'i ayır
                  const match = item.match(/^(\d+)x\s+(.+)$/)
                  let quantity = "1"
                  let name = item

                  if (match) {
                    quantity = match[1]
                    name = match[2]
                  }

                  return (
                    <div
                      key={index}
                      style={{
                        padding: isMobile ? '12px 16px' : '16px 20px',
                        border: isDarkMode
                          ? `2px solid ${isShipped ? 'rgba(82, 196, 26, 0.4)' : 'rgba(255, 193, 7, 0.4)'}`
                          : `2px solid ${isShipped ? '#b7eb8f' : '#ffd591'}`,
                        borderRadius: isMobile ? '8px' : '12px',
                        background: isDarkMode
                          ? isShipped
                            ? 'linear-gradient(135deg, rgba(82, 196, 26, 0.1) 0%, rgba(82, 196, 26, 0.05) 100%)'
                            : 'linear-gradient(135deg, rgba(255, 193, 7, 0.1) 0%, rgba(255, 193, 7, 0.05) 100%)'
                          : isShipped
                            ? 'linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%)'
                            : 'linear-gradient(135deg, #fff7e6 0%, #ffe58f 100%)',
                        boxShadow: isDarkMode
                          ? '0 2px 8px rgba(0, 0, 0, 0.2)'
                          : '0 2px 6px rgba(0, 0, 0, 0.08)',
                        transition: 'all 0.2s ease'
                      }}
                    >
                      <div style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: isMobile ? 'flex-start' : 'center',
                        flexDirection: isMobile ? 'column' : 'row',
                        gap: isMobile ? 8 : 0
                      }}>
                        <div style={{ display: 'flex', alignItems: 'center', flex: 1, width: isMobile ? '100%' : 'auto' }}>
                          <strong style={{
                            color: isDarkMode ? 'var(--text-primary)' : '#000',
                            fontSize: isMobile ? '13px' : '15px',
                            fontWeight: 600
                          }}>
                            {name}
                          </strong>
                          {isShipped && (
                            <Tag color="green" icon={<CheckCircleOutlined />} style={{ marginLeft: 8, fontSize: isMobile ? '10px' : '12px', padding: '2px 8px' }}>
                              Gönderildi
                            </Tag>
                          )}
                        </div>
                        <Tag
                          color={isShipped ? 'green' : 'orange'}
                          style={{
                            fontSize: isMobile ? 13 : 15,
                            fontWeight: 600,
                            padding: isMobile ? '4px 10px' : '6px 14px',
                            borderRadius: '8px',
                            boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)',
                            marginTop: isMobile ? 4 : 0
                          }}
                        >
                          {quantity} Adet
                        </Tag>
                      </div>
                    </div>
                  )
                })}
              </div>
            </Card>
          )}

          <Card title="Gönderilecek Adres Bilgileri" size="small">
            <Descriptions
              column={1}
              bordered
              size="small"
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
              <Descriptions.Item label="Adres">
                {request.shippingAddressLine || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="Şehir">
                {request.shippingCity || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="İlçe">
                {request.shippingDistrict || '-'}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Card title="İletişim Bilgileri" size="small">
            <Descriptions
              column={1}
              bordered
              size="small"
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
              <Descriptions.Item label="İletişim Kişisi">
                {request.contactPerson || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="Telefon">
                {request.contactPhone || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="E-posta">
                {request.contactEmail || '-'}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Card title="Tarih Bilgileri" size="small">
            <Descriptions
              column={1}
              bordered
              size="small"
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
              <Descriptions.Item label="Oluşturulma Tarihi">
                {dayjs(request.createdAt).format(isMobile ? 'DD.MM.YYYY HH:mm' : 'DD MMMM YYYY HH:mm')}
              </Descriptions.Item>
              {request.updatedAt && (
                <Descriptions.Item label="Güncellenme Tarihi">
                  {dayjs(request.updatedAt).format(isMobile ? 'DD.MM.YYYY HH:mm' : 'DD MMMM YYYY HH:mm')}
                </Descriptions.Item>
              )}
              {request.completedAt && (
                <Descriptions.Item label="Tamamlanma Tarihi">
                  {dayjs(request.completedAt).format(isMobile ? 'DD.MM.YYYY HH:mm' : 'DD MMMM YYYY HH:mm')}
                </Descriptions.Item>
              )}
            </Descriptions>
          </Card>
        </Space>
      </Card>
    </div>
  )
}

export default RequestDetail


