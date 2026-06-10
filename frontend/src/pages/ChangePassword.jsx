import { useState } from 'react'
import { Card, Form, Input, Button, Typography, App, Row, Col, Grid } from 'antd'
import { LockOutlined, SafetyCertificateOutlined } from '@ant-design/icons'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import PasswordStrengthChecklist from '../components/PasswordStrengthChecklist'

const { Title, Text } = Typography
const { useBreakpoint } = Grid

const ChangePassword = () => {
    const { message } = App.useApp()
    const { isDarkMode } = useTheme()
    const screens = useBreakpoint()
    const isMobile = !screens.md
    const [form] = Form.useForm()
    const [loading, setLoading] = useState(false)

    const handleSubmit = async (values) => {
        try {
            setLoading(true)
            await api.post('/auth/change-password', {
                currentPassword: values.currentPassword,
                newPassword: values.newPassword
            })
            message.success('Şifreniz başarıyla güncellendi')
            form.resetFields()
        } catch (error) {
            const apiMessage = error.response?.data?.message
            message.error(apiMessage || 'Şifre güncellenemedi')
        } finally {
            setLoading(false)
        }
    }

    const infoItems = [
        'En az 12 karakter kullanın',
        'Büyük/küçük harf ve rakam ekleyin',
        'Özel karakterlerle şifreyi güçlendirin'
    ]

    const panelBackground = isDarkMode
        ? 'linear-gradient(135deg, rgba(22, 119, 255, 0.18) 0%, rgba(17, 24, 39, 0.9) 65%)'
        : 'linear-gradient(135deg, #e8f3ff 0%, #f9fbff 70%)'

    const cardBorder = isDarkMode ? '1px solid rgba(255,255,255,0.08)' : '1px solid #eef2ff'

    return (
        <div style={{ maxWidth: 1020, margin: '0 auto' }}>
            <Card
                style={{
                    borderRadius: 20,
                    overflow: 'hidden',
                    border: cardBorder,
                    boxShadow: isDarkMode ? '0 16px 40px rgba(0,0,0,0.35)' : '0 16px 40px rgba(15, 23, 42, 0.08)'
                }}
            >
                <Row gutter={isMobile ? [0, 24] : [28, 0]} align="stretch">
                    <Col xs={24} md={10} lg={9}>
                        <div
                            style={{
                                height: '100%',
                                padding: isMobile ? 18 : 24,
                                borderRadius: 16,
                                background: panelBackground,
                                border: cardBorder
                            }}
                        >
                            <div
                                style={{
                                    width: 54,
                                    height: 54,
                                    borderRadius: 16,
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    background: isDarkMode ? 'rgba(22,119,255,0.2)' : '#e6f4ff',
                                    marginBottom: 16
                                }}
                            >
                                <SafetyCertificateOutlined style={{ fontSize: 24, color: '#1677ff' }} />
                            </div>
                            <Title level={4} style={{ marginBottom: 8 }}>
                                Güvenli Şifre
                            </Title>
                            <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
                                Şifrenizi düzenli aralıklarla yenilemek hesabınızı daha güvenli tutar.
                            </Text>
                            <div style={{ display: 'grid', gap: 10 }}>
                                {infoItems.map((item) => (
                                    <div
                                        key={item}
                                        style={{
                                            display: 'flex',
                                            gap: 10,
                                            alignItems: 'flex-start',
                                            padding: '10px 12px',
                                            borderRadius: 12,
                                            background: isDarkMode ? 'rgba(255,255,255,0.06)' : '#ffffff',
                                            border: cardBorder
                                        }}
                                    >
                                        <LockOutlined style={{ color: '#1677ff', marginTop: 2 }} />
                                        <Text style={{ fontSize: 13, color: isDarkMode ? '#d1d5db' : '#475569' }}>
                                            {item}
                                        </Text>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </Col>
                    <Col xs={24} md={14} lg={15}>
                        <div style={{ padding: isMobile ? 0 : '8px 12px' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 12 }}>
                                <div
                                    style={{
                                        width: 44,
                                        height: 44,
                                        borderRadius: 14,
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        background: isDarkMode ? 'rgba(22,119,255,0.18)' : '#e6f4ff'
                                    }}
                                >
                                    <LockOutlined style={{ fontSize: 20, color: '#1677ff' }} />
                                </div>
                                <div>
                                    <Title level={4} style={{ margin: 0 }}>
                                        Şifre Değiştir
                                    </Title>
                                    <Text type="secondary">
                                        En az 12 karakter kullanın ve güçlü bir şifre seçin.
                                    </Text>
                                </div>
                            </div>

                            <Form
                                form={form}
                                layout="vertical"
                                onFinish={handleSubmit}
                                autoComplete="off"
                            >
                                <Form.Item
                                    name="currentPassword"
                                    label="Mevcut Şifre"
                                    rules={[{ required: true, message: 'Mevcut şifre zorunludur' }]}
                                >
                                    <Input.Password
                                        placeholder="Mevcut şifre"
                                        autoComplete="new-password"
                                    />
                                </Form.Item>
                                <Form.Item
                                    name="newPassword"
                                    label="Yeni Şifre"
                                    rules={[
                                        { required: true, message: 'Yeni şifre zorunludur' },
                                        { required: true, message: 'Yeni şifre zorunludur' },
                                        { min: 12, message: 'Yeni şifre en az 12 karakter olmalı' }
                                    ]}
                                    hasFeedback
                                >
                                    <Input.Password
                                        placeholder="Yeni şifre"
                                        autoComplete="new-password"
                                    />
                                </Form.Item>

                                <Form.Item noStyle shouldUpdate={(prevValues, currentValues) => prevValues.newPassword !== currentValues.newPassword}>
                                    {({ getFieldValue }) => (
                                        <PasswordStrengthChecklist password={getFieldValue('newPassword')} />
                                    )}
                                </Form.Item>
                                <Form.Item
                                    name="confirmPassword"
                                    label="Yeni Şifre (Tekrar)"
                                    dependencies={['newPassword']}
                                    hasFeedback
                                    rules={[
                                        { required: true, message: 'Yeni şifreyi tekrar girin' },
                                        ({ getFieldValue }) => ({
                                            validator(_, value) {
                                                if (!value || getFieldValue('newPassword') === value) {
                                                    return Promise.resolve()
                                                }
                                                return Promise.reject(new Error('Şifreler eşleşmiyor'))
                                            }
                                        })
                                    ]}
                                >
                                    <Input.Password
                                        placeholder="Yeni şifre (tekrar)"
                                        autoComplete="new-password"
                                    />
                                </Form.Item>
                                <Button
                                    type="primary"
                                    htmlType="submit"
                                    loading={loading}
                                    block
                                    style={{ height: 44, borderRadius: 10, fontWeight: 600, boxShadow: '0 8px 18px rgba(22,119,255,0.25)' }}
                                >
                                    Şifreyi Güncelle
                                </Button>
                            </Form>
                        </div>
                    </Col>
                </Row>
            </Card>
        </div>
    )
}

export default ChangePassword
