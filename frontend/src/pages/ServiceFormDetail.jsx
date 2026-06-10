import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Card, Descriptions, Tag, Button, Space, Spin, Divider, Typography, Row, Col, Image, App, message } from 'antd'
import { ArrowLeftOutlined, FormOutlined, FilePdfOutlined, DownloadOutlined } from '@ant-design/icons'
import api from '../services/api'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'

dayjs.locale('tr')

const { Title, Text, Paragraph } = Typography

const ServiceFormDetail = () => {
    const navigate = useNavigate()
    const { id } = useParams()
    const [form, setForm] = useState(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        loadForm()
    }, [id])

    const loadForm = async () => {
        try {
            setLoading(true)
            const response = await api.get(`/serviceforms/${id}`)
            setForm(response.data)
        } catch (error) {
            navigate('/service-forms')
        } finally {
            setLoading(false)
        }
    }

    const getIssueStatusLabel = (status) => {
        const labels = {
            'SorunGiderilmedi': 'Sorun Giderilmedi',
            'SorunGiderildi': 'Sorun Giderildi',
            'SorunaRastlanmadi': 'Soruna Rastlanmadı',
            'ParcaDegistirildi': 'Parça Değiştirildi',
            'ParcaDegistirilecek': 'Parça Değiştirilecek',
            'Diger': 'Diğer'
        }
        return labels[status] || status || '-'
    }

    const getServiceScopeLabel = (scope) => {
        const labels = {
            'SozlesmeIci': 'Sözleşme İçi',
            'SozlesmeDisi': 'Sözleşme Dışı',
            'Diger': 'Diğer'
        }
        return labels[scope] || scope || '-'
    }

    const getServiceTypeLabel = (type) => {
        const labels = {
            'ArizaGiderme': 'Arıza Giderme',
            'Bakim': 'Bakım',
            'Kesif': 'Keşif',
            'Diger': 'Diğer'
        }
        return labels[type] || type || '-'
    }

    // Tarih ve saati formatlar. Saat 00:00 ise sadece tarihi gösterir.
    const formatDateTimeForDisplay = (dateTime) => {
        if (!dateTime) return '-'

        const date = dayjs(dateTime)
        const dateStr = date.format('DD.MM.YYYY')
        const timeStr = date.format('HH:mm')

        // Eğer saat 00:00 ise, sadece tarihi göster
        return timeStr === '00:00' ? dateStr : `${dateStr} ${timeStr}`
    }

    const handleDownloadPdf = async () => {
        try {
            // Backend ile aynı isimlendirme mantığını kullan
            const institution = (form.institutionIsOther || form.institutionName === 'DİĞER')
                ? form.institutionOtherName
                : form.institutionName || 'ServisFormu'
            const date = dayjs(form.formDate).format('YYYYMMDD')
            const cleanName = institution.replace(/[İıĞğÜüŞşÖöÇç]/g, (m) => {
                const map = { 'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g', 'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's', 'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c' }
                return map[m]
            }).replace(/[\s.,]/g, '_')

            const formNo = `Form${id.toString().padStart(4, '0')}`
            const fileName = `${formNo}_${cleanName}_${date}.pdf`

            // Axios ile PDF'i blob olarak indir (Authorization header ile)
            const response = await api.get(
                `/serviceforms/${id}/pdf/${fileName}`,
                { responseType: 'blob' }
            )

            // Blob URL oluştur
            const blob = new Blob([response.data], { type: 'application/pdf' })
            const url = window.URL.createObjectURL(blob)

            // Yeni sekmede aç
            window.open(url, '_blank')

            // Memory temizliği
            setTimeout(() => window.URL.revokeObjectURL(url), 100)
        } catch (error) {
            message.error('PDF açılamadı')
        }
    }

    const handleDownloadPdfFile = async () => {
        try {
            // Backend ile aynı isimlendirme mantığını kullan
            const institution = (form.institutionIsOther || form.institutionName === 'DİĞER')
                ? form.institutionOtherName
                : form.institutionName || 'ServisFormu'
            const date = dayjs(form.formDate).format('YYYYMMDD')
            const cleanName = institution.replace(/[İıĞğÜüŞşÖöÇç]/g, (m) => {
                const map = { 'İ': 'I', 'ı': 'i', 'Ğ': 'G', 'ğ': 'g', 'Ü': 'U', 'ü': 'u', 'Ş': 'S', 'ş': 's', 'Ö': 'O', 'ö': 'o', 'Ç': 'C', 'ç': 'c' }
                return map[m]
            }).replace(/[\s.,]/g, '_')

            const formNo = `Form${id.toString().padStart(4, '0')}`
            const fileName = `${formNo}_${cleanName}_${date}.pdf`

            // Axios ile PDF'i blob olarak indir
            const response = await api.get(
                `/serviceforms/${id}/pdf/${fileName}`,
                { responseType: 'blob' }
            )

            // Blob URL oluştur ve download et
            const blob = new Blob([response.data], { type: 'application/pdf' })
            const url = window.URL.createObjectURL(blob)

            // Download link oluştur ve tıkla
            const link = document.createElement('a')
            link.href = url
            link.download = fileName
            document.body.appendChild(link)
            link.click()

            // Cleanup
            document.body.removeChild(link)
            window.URL.revokeObjectURL(url)
        } catch (error) {
            message.error('PDF indirilemedi')
        }
    }

    if (loading) {
        return (
            <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
                <Spin size="large" />
            </div>
        )
    }

    if (!form) return null

    const photos = form.photos ? JSON.parse(form.photos) : []

    return (
        <div style={{ maxWidth: 1200, margin: '0 auto' }}>
            <div style={{ marginBottom: 24, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
                <Space>
                    <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/service-forms')}>
                        Geri
                    </Button>
                    <Title level={4} style={{ margin: 0 }}>
                        <FormOutlined /> Form #{form.id.toString().padStart(4, '0')}
                    </Title>
                    <Tag color="success">Gönderildi</Tag>
                </Space>

                {form.formDate && (
                    <Space>
                        <Text type="secondary">
                            Form Tarihi: {dayjs(form.formDate).format('DD.MM.YYYY HH:mm')}
                        </Text>
                        <Button
                            type="primary"
                            icon={<FilePdfOutlined />}
                            onClick={handleDownloadPdf}
                        >
                            PDF Görüntüle
                        </Button>
                        <Button
                            type="default"
                            icon={<DownloadOutlined />}
                            onClick={handleDownloadPdfFile}
                        >
                            PDF İndir
                        </Button>
                    </Space>
                )}
            </div>

            <Space direction="vertical" style={{ width: '100%' }} size="large">
                {/* Genel Bilgiler */}
                <Card title="Genel Bilgiler">
                    <Row gutter={[16, 16]}>
                        {/* Sol Sütun */}
                        <Col xs={24} md={12}>
                            <Descriptions column={1} bordered size="small">
                                <Descriptions.Item label="Firma Adı">
                                    {(form.institutionIsOther || form.institutionName === 'DİĞER') ? form.institutionOtherName : form.institutionName || '-'}
                                </Descriptions.Item>
                                <Descriptions.Item label="Adres">
                                    {form.address || '-'}
                                </Descriptions.Item>
                                <Descriptions.Item label="Servis Başlangıç">
                                    {formatDateTimeForDisplay(form.serviceStartDate)}
                                </Descriptions.Item>
                                <Descriptions.Item label="Servis Bitiş">
                                    {formatDateTimeForDisplay(form.serviceEndDate)}
                                </Descriptions.Item>
                            </Descriptions>
                        </Col>

                        {/* Sağ Sütun */}
                        <Col xs={24} md={12}>
                            <Descriptions column={1} bordered size="small">
                                <Descriptions.Item label="İletişim Kişisi">
                                    {form.contactFirstName} {form.contactLastName}
                                </Descriptions.Item>
                                <Descriptions.Item label="E-posta">
                                    {form.email || '-'}
                                </Descriptions.Item>
                                <Descriptions.Item label="Telefon">
                                    {form.phone || '-'}
                                </Descriptions.Item>
                            </Descriptions>
                        </Col>

                        {/* Servis Kapsamı - Sol Sütun */}
                        <Col xs={24} md={12}>
                            <Descriptions column={1} bordered size="small">
                                <Descriptions.Item label="Servis Kapsamı">
                                    {getServiceScopeLabel(form.serviceScope)}
                                    {form.serviceScopeOther && ` (${form.serviceScopeOther})`}
                                </Descriptions.Item>
                            </Descriptions>
                        </Col>

                        {/* Hizmet Türü - Sağ Sütun */}
                        <Col xs={24} md={12}>
                            <Descriptions column={1} bordered size="small">
                                <Descriptions.Item label="Hizmet Türü">
                                    {getServiceTypeLabel(form.serviceType)}
                                    {form.serviceTypeOther && ` (${form.serviceTypeOther})`}
                                </Descriptions.Item>
                            </Descriptions>
                        </Col>
                    </Row>
                </Card>

                {/* Servis Detayları */}
                <Card title="Servis Detayları">
                    <Descriptions column={1} bordered size="small">
                        <Descriptions.Item label="Bildirilen Sorun">
                            <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                                {form.reportedIssue || '-'}
                            </Paragraph>
                        </Descriptions.Item>
                        <Descriptions.Item label="Yapılan İşlem">
                            <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                                {form.workPerformed || '-'}
                            </Paragraph>
                        </Descriptions.Item>
                        <Descriptions.Item label="Kullanılan Malzemeler">
                            <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                                {form.materialsUsed || '-'}
                            </Paragraph>
                        </Descriptions.Item>
                        <Descriptions.Item label="Durum">
                            <Tag color={form.issueStatus === 'SorunGiderildi' ? 'success' : 'default'}>
                                {getIssueStatusLabel(form.issueStatus)}
                            </Tag>
                            {form.issueStatusOther && ` (${form.issueStatusOther})`}
                        </Descriptions.Item>
                        {(form.issueStatus === 'ParcaDegistirilecek' || form.partsToReplace) && (
                            <Descriptions.Item label="Değiştirilecek Parçalar">
                                <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                                    {form.partsToReplace || '-'}
                                </Paragraph>
                            </Descriptions.Item>
                        )}
                    </Descriptions>
                </Card>

                {/* Fotoğraflar */}
                {photos.length > 0 && (
                    <Card title={`Fotoğraflar (${photos.length})`}>
                        <Image.PreviewGroup>
                            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
                                {photos.map((photo, index) => (
                                    <Image
                                        key={photo.uid || index}
                                        src={photo.url}
                                        alt={`Fotoğraf ${index + 1}`}
                                        width={150}
                                        height={150}
                                        style={{ objectFit: 'cover', borderRadius: 8 }}
                                    />
                                ))}
                            </div>
                        </Image.PreviewGroup>
                    </Card>
                )}

                {/* İmzalar */}
                <Card title="Onay ve İmzalar">
                    <Row gutter={[24, 24]}>
                        <Col xs={24} lg={12}>
                            <Card type="inner" title="Etisan Servis Yetkilisi">
                                <Descriptions column={1} size="small">
                                    <Descriptions.Item label="Ad Soyad">
                                        {form.etisanFirstName} {form.etisanLastName}
                                    </Descriptions.Item>
                                </Descriptions>
                                {form.etisanSignature && (
                                    <div style={{ marginTop: 12 }}>
                                        <Text type="secondary">İmza:</Text>
                                        <div style={{
                                            border: '1px solid #d9d9d9',
                                            borderRadius: 8,
                                            padding: 8,
                                            marginTop: 8,
                                            backgroundColor: '#fff'
                                        }}>
                                            <img
                                                src={form.etisanSignature}
                                                alt="Etisan İmza"
                                                style={{ maxWidth: '100%', maxHeight: 100 }}
                                            />
                                        </div>
                                    </div>
                                )}
                            </Card>
                        </Col>

                        <Col xs={24} lg={12}>
                            <Card type="inner" title="Kurum Yetkilisi">
                                <Descriptions column={1} size="small">
                                    <Descriptions.Item label="Ad Soyad">
                                        {form.authorityFirstName} {form.authorityLastName}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="E-posta">
                                        {form.authorityEmail || '-'}
                                    </Descriptions.Item>
                                    <Descriptions.Item label="Telefon">
                                        {form.authorityPhone || '-'}
                                    </Descriptions.Item>
                                </Descriptions>
                                {form.authoritySignature && (
                                    <div style={{ marginTop: 12 }}>
                                        <Text type="secondary">İmza:</Text>
                                        <div style={{
                                            border: '1px solid #d9d9d9',
                                            borderRadius: 8,
                                            padding: 8,
                                            marginTop: 8,
                                            backgroundColor: '#fff'
                                        }}>
                                            <img
                                                src={form.authoritySignature}
                                                alt="Kurum İmza"
                                                style={{ maxWidth: '100%', maxHeight: 100 }}
                                            />
                                        </div>
                                    </div>
                                )}
                            </Card>
                        </Col>
                    </Row>
                </Card>

                {/* Metadata */}
                <Card size="small">
                    <Descriptions column={{ xs: 1, sm: 3 }} size="small">
                        <Descriptions.Item label="Oluşturan">
                            {form.createdByUserName || '-'}
                        </Descriptions.Item>
                        <Descriptions.Item label="Oluşturulma">
                            {dayjs(form.createdAt).format('DD.MM.YYYY HH:mm')}
                        </Descriptions.Item>
                        <Descriptions.Item label="Gönderim">
                            {form.submittedAt ? dayjs(form.submittedAt).format('DD.MM.YYYY HH:mm') : '-'}
                        </Descriptions.Item>
                    </Descriptions>
                </Card>
            </Space>
        </div>
    )
}

export default ServiceFormDetail
