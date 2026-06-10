import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Form, Input, App, Space, Card, Button, Spin, Row, Col, Typography, InputNumber } from 'antd'
import {
    InboxOutlined,
    ArrowLeftOutlined,
    SaveOutlined,
    InfoCircleOutlined,
    WarningOutlined
} from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { Title, Text } = Typography

const StockForm = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const [form] = Form.useForm()

    const [loading, setLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [editingItem, setEditingItem] = useState(null)

    useEffect(() => {
        if (id) {
            fetchStockDetail(id)
        }
    }, [id])

    const fetchStockDetail = async (itemId) => {
        try {
            setLoading(true)
            const response = await api.get('/stock')
            const item = response.data.find(i => i.id === parseInt(itemId))
            if (!item) throw new Error('Ürün bulunamadı')

            setEditingItem(item)
            form.setFieldsValue({
                itemName: item.itemName,
                serialNumber: item.serialNumber,
                currentStock: item.currentStock,
                minimumStock: item.minimumStock,
                description: item.description
            })
        } catch (error) {
            message.error('Ürün detayları yüklenirken hata oluştu')
            navigate('/stock')
        } finally {
            setLoading(false)
        }
    }

    const handleSubmit = async (values) => {
        try {
            setSaving(true)
            const submitData = {
                itemName: values.itemName,
                serialNumber: values.serialNumber,
                description: values.description,
                minimumStock: values.minimumStock || 0
            }

            if (editingItem) {
                await api.put(`/stock/${editingItem.id}`, submitData)

                // If stock amount was changed manually in the form field (not via movements)
                const nextStock = typeof values.currentStock === 'number' ? values.currentStock : 0
                if (typeof editingItem.currentStock === 'number' && nextStock !== editingItem.currentStock) {
                    await api.post('/stock/movements', {
                        stockItemId: editingItem.id,
                        movementType: 'Adjustment',
                        quantity: nextStock,
                        reference: 'ManualEdit',
                        notes: 'Stok düzenleme ekranından güncellendi'
                    })
                }
                message.success('Ürün güncellendi')
            } else {
                submitData.currentStock = values.currentStock || 0
                await api.post('/stock', submitData)
                message.success('Ürün eklendi')
            }
            navigate('/stock')
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
                        onClick={() => navigate('/stock')}
                        style={{ borderRadius: 8 }}
                    >
                        Geri
                    </Button>
                    <Title level={2} style={{ margin: 0 }}>
                        {id ? 'Stok Ürününü Düzenle' : 'Yeni Stok Ürünü Ekle'}
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
                    <Col xs={24} md={16}>
                        <Card title={<Space><InboxOutlined /><span>Ürün Bilgileri</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item name="itemName" label="Ürün Adı" rules={[{ required: true, message: 'Ürün adı gerekli' }]}>
                                <Input placeholder="Ürün adını girin" size="large" />
                            </Form.Item>

                            <Form.Item name="serialNumber" label="Seri No (Opsiyonel)">
                                <Input placeholder="Seri numarası" />
                            </Form.Item>

                            <Form.Item name="description" label="Açıklama">
                                <Input.TextArea rows={4} placeholder="Ürün açıklaması..." />
                            </Form.Item>
                        </Card>
                    </Col>

                    <Col xs={24} md={8}>
                        <Card title={<Space><InfoCircleOutlined /><span>Stok Durumu</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item
                                name="currentStock"
                                label="Mevcut Miktar"
                                initialValue={0}
                                rules={[{ required: true, message: 'Miktar gerekli' }]}
                            >
                                <InputNumber style={{ width: '100%' }} size="large" />
                            </Form.Item>

                            <Form.Item
                                name="minimumStock"
                                label="Minimum Kritik Seviye"
                                initialValue={0}
                                rules={[{ required: true, message: 'Minimum miktar gerekli' }]}
                                help="Stok bu seviyenin altına düştüğünde uyarı verilir."
                            >
                                <InputNumber style={{ width: '100%' }} />
                            </Form.Item>
                        </Card>

                        <Card style={{ borderRadius: 12, background: isDarkMode ? 'rgba(255, 169, 64, 0.05)' : '#fffbe6', border: '1px solid #ffe58f' }}>
                            <Space align="start">
                                <WarningOutlined style={{ color: '#faad14', marginTop: 4 }} />
                                <Text type="secondary" style={{ fontSize: 13 }}>
                                    Stok miktarını buradan doğrudan güncelleyebilirsiniz. Bu işlem "Manuel Düzenleme" olarak kaydedilecektir.
                                </Text>
                            </Space>
                        </Card>
                    </Col>
                </Row>
            </Form>
        </div>
    )
}

export default StockForm
