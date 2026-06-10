import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Form, Input, Select, App, Space, Card, Button, Spin, Row, Col, Typography } from 'antd'
import {
    UserOutlined,
    MailOutlined,
    PhoneOutlined,
    LockOutlined,
    ArrowLeftOutlined,
    SaveOutlined,
    CheckCircleOutlined
} from '@ant-design/icons'
import api from '../services/api'
import { ROLES } from '../constants/roles'
import PasswordStrengthChecklist from '../components/PasswordStrengthChecklist'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { Title, Text } = Typography

const UserForm = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const [form] = Form.useForm()

    const [loading, setLoading] = useState(false)
    const [saving, setSaving] = useState(false)
    const [editingUser, setEditingUser] = useState(null)

    const normalizePhoneNumber = (value) => {
        if (!value) return value
        const onlyNums = value.replace(/[^\d]/g, '')
        if (onlyNums.length <= 4) return onlyNums
        if (onlyNums.length <= 7) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4)}`
        if (onlyNums.length <= 9) return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7)}`
        return `${onlyNums.slice(0, 4)} ${onlyNums.slice(4, 7)} ${onlyNums.slice(7, 9)} ${onlyNums.slice(9, 11)}`
    }

    useEffect(() => {
        if (id) {
            fetchUserDetail(id)
        }
    }, [id])

    const fetchUserDetail = async (userId) => {
        try {
            setLoading(true)
            const response = await api.get('/users')
            const user = response.data.find(u => u.id === parseInt(userId))
            if (!user) throw new Error('Kullanıcı bulunamadı')

            setEditingUser(user)
            form.setFieldsValue({
                firstName: user.firstName,
                lastName: user.lastName,
                email: user.email,
                role: user.role,
                phoneNumber: user.phoneNumber,
                isActive: user.isActive
            })
        } catch (error) {
            message.error('Kullanıcı detayları yüklenirken hata oluştu')
            navigate('/users')
        } finally {
            setLoading(false)
        }
    }

    const handleSubmit = async (values) => {
        try {
            setSaving(true)
            if (editingUser) {
                await api.put(`/users/${editingUser.id}`, values)
                message.success('Kullanıcı güncellendi')
            } else {
                await api.post('/users', values)
                message.success('Kullanıcı oluşturuldu')
            }
            navigate('/users')
        } catch (error) {
            console.error('Create/Update Error:', error);
            message.error(getErrorMessage(error));
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
                        onClick={() => navigate('/users')}
                        style={{ borderRadius: 8 }}
                    >
                        Geri
                    </Button>
                    <Title level={2} style={{ margin: 0 }}>
                        {id ? 'Kullanıcıyı Düzenle' : 'Yeni Kullanıcı Oluştur'}
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
                        <Card title={<Space><UserOutlined /><span>Temel Bilgiler</span></Space>} style={{ marginBottom: 24, borderRadius: 12 }}>
                            <Row gutter={16}>
                                <Col span={12}>
                                    <Form.Item name="firstName" label="Ad" rules={[{ required: true, message: 'Ad gerekli' }]}>
                                        <Input placeholder="Ad" prefix={<UserOutlined style={{ color: '#bfbfbf' }} />} />
                                    </Form.Item>
                                </Col>
                                <Col span={12}>
                                    <Form.Item name="lastName" label="Soyad" rules={[{ required: true, message: 'Soyad gerekli' }]}>
                                        <Input placeholder="Soyad" />
                                    </Form.Item>
                                </Col>
                            </Row>

                            <Form.Item
                                name="email"
                                label="E-posta"
                                rules={[{ required: true, message: 'Email gerekli' }, { type: 'email', message: 'Geçerli bir email adresi girin' }]}
                            >
                                <Input placeholder="email@example.com" prefix={<MailOutlined style={{ color: '#bfbfbf' }} />} autoComplete="new-user-email" />
                            </Form.Item>

                            <Form.Item name="phoneNumber" label="Telefon" normalize={normalizePhoneNumber}>
                                <Input placeholder="05XX XXX XX XX" maxLength={14} prefix={<PhoneOutlined style={{ color: '#bfbfbf' }} />} />
                            </Form.Item>
                        </Card>
                    </Col>

                    <Col xs={24} lg={10}>
                        <Card title={<Space><LockOutlined /><span>Güvenlik ve Yetkilendirme</span></Space>} style={{ borderRadius: 12 }}>
                            <Form.Item
                                name="role"
                                label="Kullanıcı Rolü"
                                rules={[{ required: true, message: 'Rol seçimi gerekli' }]}
                                initialValue={ROLES.KULLANICI}
                            >
                                <Select size="large">
                                    {[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI].map(role => (
                                        <Select.Option key={role} value={role}>{role}</Select.Option>
                                    ))}
                                </Select>
                            </Form.Item>

                            <Form.Item
                                name="password"
                                label={id ? 'Yeni Şifre (Boş bırakılabilir)' : 'Şifre'}
                                rules={id ? [] : [{ required: true, message: 'Şifre gerekli' }]}
                            >
                                <Input.Password placeholder="Şifre" prefix={<LockOutlined style={{ color: '#bfbfbf' }} />} autoComplete="new-password" />
                            </Form.Item>

                            <Form.Item noStyle shouldUpdate={(prevValues, currentValues) => prevValues.password !== currentValues.password}>
                                {({ getFieldValue }) => (
                                    <PasswordStrengthChecklist password={getFieldValue('password')} />
                                )}
                            </Form.Item>

                            {id && (
                                <Form.Item name="isActive" label="Hesap Durumu" initialValue={true} style={{ marginTop: 24 }}>
                                    <Select size="large">
                                        <Select.Option value={true}>Aktif</Select.Option>
                                        <Select.Option value={false}>Pasif</Select.Option>
                                    </Select>
                                </Form.Item>
                            )}
                        </Card>
                    </Col>
                </Row>
            </Form>
        </div>
    )
}

export default UserForm
