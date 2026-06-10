import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Form, Input, App, Space, Card, Button, Spin, Row, Col, Typography, Checkbox, DatePicker, Upload } from 'antd'
import {
    AppstoreOutlined,
    ArrowLeftOutlined,
    SaveOutlined,
    InboxOutlined,
    EnvironmentOutlined,
    UnorderedListOutlined,
    ClockCircleOutlined
} from '@ant-design/icons'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'
import dayjs from 'dayjs'

const { Title, Text } = Typography

const WorkItemForm = () => {
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
            fetchWorkItemDetail(id)
        }
    }, [id])

    const fetchWorkItemDetail = async (itemId) => {
        try {
            setLoading(true)
            const response = await api.get(`/workitems/${itemId}`)
            const item = response.data
            setEditingItem(item)
            form.setFieldsValue({
                institutionName: item.institutionName,
                institutionOtherName: item.institutionOtherName,
                title: item.title,
                description: item.description,
                dueAt: item.dueAt ? dayjs(item.dueAt) : null,
                isUrgent: item.priority === 'Critical'
            })
        } catch (error) {
            console.error('Error fetching work item:', error)
            message.error('İş detayları yüklenirken hata oluştu')
            navigate('/workflow')
        } finally {
            setLoading(false)
        }
    }

    const handleSubmit = async (values) => {
        try {
            setSaving(true)
            const { files, ...rest } = values
            const payload = {
                ...rest,
                kind: 'PlannedWork',
                priority: values.isUrgent ? 'Critical' : 'Normal',
                dueAt: values.dueAt ? values.dueAt.toISOString() : null,
            }

            let workItemId = id
            if (id) {
                await api.put(`/workitems/${id}`, payload)
                message.success('İş güncellendi')
            } else {
                const res = await api.post('/workitems', payload)
                workItemId = res.data.id
                message.success('İş oluşturuldu')
            }

            // Upload files if any
            if (files && files.length > 0) {
                for (const file of files) {
                    const formData = new FormData()
                    formData.append('file', file.originFileObj)
                    await api.post(`/workitems/${workItemId}/attachments`, formData, {
                        headers: { 'Content-Type': 'multipart/form-data' }
                    })
                }
            }

            navigate('/workflow')
        } catch (error) {
            console.error('Submit Error:', error)
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
                        onClick={() => navigate('/workflow')}
                        style={{ borderRadius: 8 }}
                    >
                        Geri
                    </Button>
                    <Title level={2} style={{ margin: 0 }}>
                        {id ? 'İşi Düzenle' : 'Yeni İş Oluştur'}
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

            <Form form={form} layout="vertical" onFinish={handleSubmit} autoComplete="off">
                <Row gutter={24}>
                    <Col xs={24} lg={14}>
                        <Card title={<Space><EnvironmentOutlined /><span>Firma / Kurum Bilgileri</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item
                                name="institutionName"
                                label="Firma Adı"
                            >
                                <InstitutionSelect placeholder="Kurum seçin" allowClear />
                            </Form.Item>

                            <Form.Item noStyle shouldUpdate={(prev, curr) => prev.institutionName !== curr.institutionName}>
                                {({ getFieldValue }) =>
                                    getFieldValue('institutionName') === 'DİĞER' && (
                                        <Form.Item
                                            name="institutionOtherName"
                                            label="Kurum Adı (Diğer)"
                                        >
                                            <Input placeholder="Kurum Adını yazın" />
                                        </Form.Item>
                                    )
                                }
                            </Form.Item>
                        </Card>

                        <Card title={<Space><UnorderedListOutlined /><span>İş Detayları</span></Space>} style={{ borderRadius: 12 }}>
                            <Form.Item name="title" label="Başlık">
                                <Input placeholder="İş başlığı" size="large" />
                            </Form.Item>

                            <Form.Item name="description" label="Açıklama">
                                <Input.TextArea rows={6} placeholder="Detaylı açıklama..." />
                            </Form.Item>
                        </Card>
                    </Col>

                    <Col xs={24} lg={10}>
                        <Card title={<Space><ClockCircleOutlined /><span>Zamanlama ve Dosyalar</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Form.Item
                                name="dueAt"
                                label="Hedef Tamamlanma Tarihi"
                            >
                                <DatePicker
                                    style={{ width: '100%' }}
                                    placeholder="Tarih seçin"
                                    format="DD/MM/YYYY"
                                    size="large"
                                />
                            </Form.Item>

                            <Form.Item name="isUrgent" valuePropName="checked">
                                <Checkbox style={{
                                    padding: '12px 16px',
                                    background: isDarkMode ? 'rgba(255, 77, 79, 0.1)' : '#fff1f0',
                                    border: '1px solid #ffa39e',
                                    borderRadius: 8,
                                    width: '100%',
                                    color: '#f5222d',
                                    fontWeight: 600
                                }}>
                                    Bu iş acildir
                                </Checkbox>
                            </Form.Item>
                        </Card>

                        <Card title={<Space><InboxOutlined /><span>Dosya Ekleri</span></Space>} style={{ borderRadius: 12 }}>
                            <Form.Item
                                name="files"
                                label="Dosya Yükle"
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
                                    accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.gif"
                                >
                                    <p className="ant-upload-drag-icon">
                                        <InboxOutlined />
                                    </p>
                                    <p className="ant-upload-text">Dosyaları sürükleyin veya tıklayın</p>
                                    <p className="ant-upload-hint">
                                        Birden fazla dosya yükleyebilirsiniz.
                                    </p>
                                </Upload.Dragger>
                            </Form.Item>
                        </Card>
                    </Col>
                </Row>
            </Form>
        </div>
    )
}

export default WorkItemForm
