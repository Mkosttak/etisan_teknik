import { useState } from 'react'
import { Form, Input, Button, Typography, Card, App, theme } from 'antd'
import { UserOutlined, LockOutlined, LoginOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useTheme } from '../context/ThemeContext'
import { motion, AnimatePresence } from 'framer-motion'

const { Title, Text } = Typography

const Login = () => {
    const [loading, setLoading] = useState(false)
    const { login } = useAuth()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const navigate = useNavigate()
    const { token } = theme.useToken()
    const [form] = Form.useForm()

    const [errorMsg, setErrorMsg] = useState(null)

    const onFinish = async (values) => {
        setLoading(true)
        setErrorMsg(null)
        try {
            const success = await login(values.email, values.password)
            if (success) {
                navigate('/')
            } else {
                setErrorMsg('E-posta adresi veya şifre hatalı. Lütfen bilgilerinizi kontrol edin.')
                form.setFieldsValue({ password: '' })
            }
        } catch (err) {
            setErrorMsg('Bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.')
            form.setFieldsValue({ password: '' })
        } finally {
            setLoading(false)
        }
    }

    return (
        <div style={{
            minHeight: '100vh',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: isDarkMode
                ? 'radial-gradient(circle at center, #1a1a1a 0%, #0a0a0a 100%)'
                : 'radial-gradient(circle at center, #f0f2f5 0%, #e6e9ed 100%)',
            padding: '24px',
            position: 'relative',
            overflow: 'hidden'
        }}>
            {/* Dekoratif Arka Plan (Sadece çok hafif bir dokunuş) */}
            <div style={{
                position: 'absolute',
                top: '-15%',
                right: '-5%',
                width: '400px',
                height: '400px',
                background: isDarkMode ? 'rgba(24, 144, 255, 0.03)' : 'rgba(24, 144, 255, 0.05)',
                borderRadius: '50%',
                filter: 'blur(100px)',
                zIndex: 0
            }} />

            <Card
                bordered={false}
                style={{
                    width: '100%',
                    maxWidth: 420,
                    borderRadius: 16,
                    boxShadow: isDarkMode
                        ? '0 20px 50px rgba(0,0,0,0.4)'
                        : '0 10px 30px rgba(0,0,0,0.06)',
                    background: isDarkMode ? '#141414' : '#ffffff',
                    zIndex: 1
                }}
            >
                <div style={{ textAlign: 'center', marginBottom: 40 }}>
                    <img
                        src="/etisan-logo.png"
                        alt="Etisan Logo"
                        style={{
                            height: 54,
                            marginBottom: 20,
                            display: 'inline-block'
                        }}
                    />
                    <div>
                        <Title level={3} style={{ margin: 0, fontWeight: 700 }}>
                            Teknik Servis Paneli
                        </Title>
                        <Text type="secondary" style={{ fontSize: 14 }}>
                            Devam etmek için hesabınıza giriş yapın
                        </Text>
                    </div>
                </div>

                <AnimatePresence mode="wait">
                    {errorMsg && (
                        <motion.div
                            initial={{ opacity: 0, y: -10 }}
                            animate={{ opacity: 1, y: 0 }}
                            exit={{ opacity: 0, y: -10 }}
                            style={{ marginBottom: 20 }}
                        >
                            <div style={{
                                padding: '12px 16px',
                                background: isDarkMode ? 'rgba(255, 77, 79, 0.1)' : '#fff2f0',
                                border: `1px solid ${isDarkMode ? 'rgba(255, 77, 79, 0.2)' : '#ffccc7'}`,
                                borderRadius: 8,
                                display: 'flex',
                                alignItems: 'center',
                                gap: '12px'
                            }}>
                                <div style={{
                                    width: 8,
                                    height: 8,
                                    borderRadius: '50%',
                                    background: '#ff4d4f',
                                    boxShadow: '0 0 10px rgba(255, 77, 79, 0.5)'
                                }} />
                                <Text style={{ color: isDarkMode ? '#ff7875' : '#ff4d4f', fontSize: 13, fontWeight: 500 }}>
                                    {errorMsg}
                                </Text>
                            </div>
                        </motion.div>
                    )}
                </AnimatePresence>

                <Form
                    form={form}
                    name="login_v2"
                    layout="vertical"
                    onFinish={onFinish}
                    requiredMark={false}
                    size="large"
                >
                    <Form.Item
                        label={<Text strong style={{ fontSize: 13, color: token.colorTextSecondary }}>E-posta Adresi</Text>}
                        name="email"
                        rules={[
                            { required: true, message: 'Lütfen e-posta adresinizi girin' },
                            { type: 'email', message: 'Geçerli bir e-posta adresi girin' }
                        ]}
                    >
                        <Input
                            prefix={<UserOutlined style={{ color: token.colorPrimary, opacity: 0.7 }} />}
                            placeholder="isminiz@sirket.com"
                            style={{
                                borderRadius: 10,
                                height: 48,
                                border: `1px solid ${isDarkMode ? 'rgba(255,255,255,0.1)' : '#d9d9d9'}`,
                                background: isDarkMode ? 'rgba(255,255,255,0.02)' : '#fff'
                            }}
                        />
                    </Form.Item>

                    <Form.Item
                        label={<Text strong style={{ fontSize: 13, color: token.colorTextSecondary }}>Şifre</Text>}
                        name="password"
                        rules={[{ required: true, message: 'Lütfen şifrenizi girin' }]}
                        style={{ marginBottom: 24 }}
                    >
                        <Input.Password
                            prefix={<LockOutlined style={{ color: token.colorPrimary, opacity: 0.7 }} />}
                            placeholder="••••••••"
                            style={{
                                borderRadius: 10,
                                height: 48,
                                border: `1px solid ${isDarkMode ? 'rgba(255,255,255,0.1)' : '#d9d9d9'}`,
                                background: isDarkMode ? 'rgba(255,255,255,0.02)' : '#fff'
                            }}
                        />
                    </Form.Item>

                    <Form.Item style={{ marginBottom: 0 }}>
                        <Button
                            type="primary"
                            htmlType="submit"
                            block
                            loading={loading}
                            icon={!loading && <LoginOutlined />}
                            style={{
                                height: 50,
                                borderRadius: 10,
                                fontSize: 16,
                                fontWeight: 700,
                                background: 'linear-gradient(90deg, #1890ff 0%, #096dd9 100%)',
                                border: 'none',
                                boxShadow: isDarkMode
                                    ? '0 8px 20px rgba(24, 144, 255, 0.15)'
                                    : '0 8px 20px rgba(24, 144, 255, 0.25)',
                                transition: 'all 0.3s ease'
                            }}
                        >
                            {loading ? 'Giriş Yapılıyor...' : 'Giriş Yap'}
                        </Button>
                    </Form.Item>
                </Form>

                <div style={{
                    marginTop: 40,
                    paddingTop: 24,
                    borderTop: `1px solid ${isDarkMode ? 'rgba(255,255,255,0.06)' : '#f0f0f0'}`,
                    textAlign: 'center'
                }}>
                    <Text type="secondary" style={{ fontSize: 12 }}>
                        Destek için <a href="#" onClick={e => e.preventDefault()}>proje</a> ile iletişime geçin.
                    </Text>

                </div>
            </Card>
        </div>
    )
}

export default Login
