// ============================================
// FILE ATTACHMENT DISPLAY COMPONENT FOR WORKFLOW
// Add this code to WorkFlow.jsx
// ============================================

// 1. ADD TO IMPORTS (after existing @ant-design/icons imports):
import {
    FileImageOutlined,
    FilePdfOutlined,
    FileExcelOutlined,
    FileWordOutlined,
    FileOutlined,
    DownloadOutlined,
    DeleteOutlined,
    EyeOutlined
} from '@ant-design/icons'
import { Image } from 'antd' // Make sure Image is imported

// 2. ADD STATE VARIABLES (in WorkFlow component after existing useState):
const [workItemAttachments, setWorkItemAttachments] = useState([])
const [loadingAttachments, setLoadingAttachments] = useState(false)
const [imagePreviewVisible, setImagePreviewVisible] = useState(false)
const [currentImageIndex, setCurrentImageIndex] = useState(0)

// 3. ADD HELPER FUNCTIONS (after existing helper functions):

// Get file type from filename
const getFileType = (fileName) => {
    if (!fileName) return 'other'
    const ext = fileName.split('.').pop().toLowerCase()
    if (['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'].includes(ext)) return 'image'
    if (ext === 'pdf') return 'pdf'
    if (['xlsx', 'xls', 'csv'].includes(ext)) return 'excel'
    if (['docx', 'doc'].includes(ext)) return 'word'
    return 'other'
}

// Get icon for file type
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

// Format file size
const formatFileSize = (bytes) => {
    if (!bytes) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
}

// Handle file click
const handleFileClick = (attachment) => {
    const type = getFileType(attachment.fileName)
    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5241'
    const downloadUrl = `${baseUrl}/api/workitems/${selectedItem.id}/attachments/${attachment.id}/download`

    if (type === 'pdf') {
        // Open PDF in new tab
        window.open(downloadUrl, '_blank')
    } else if (type === 'image') {
        // Show image in preview modal (handled by Ant Design Image)
        const imageIndex = workItemAttachments
            .filter(a => getFileType(a.fileName) === 'image')
            .findIndex(a => a.id === attachment.id)
        setCurrentImageIndex(imageIndex)
        setImagePreviewVisible(true)
    } else {
        // Download Excel, Word, and other files
        const link = document.createElement('a')
        link.href = downloadUrl
        link.download = attachment.fileName
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
    }
}

// Fetch attachments
const fetchAttachments = async (workItemId) => {
    try {
        setLoadingAttachments(true)
        const response = await api.get(`/workitems/${workItemId}/attachments`)
        setWorkItemAttachments(response.data || [])
    } catch (error) {
        console.error('Error fetching attachments:', error)
        message.error('Dosyalar yüklenirken hata oluştu')
    } finally {
        setLoadingAttachments(false)
    }
}

// Delete attachment
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

// 4. UPDATE handleCardClick function to fetch attachments:
const handleCardClick = async (item) => {
    setSelectedItem(item)
    setDetailDrawerOpen(true)
    fetchWorkItemHistory(item.id)
    fetchAttachments(item.id) // ADD THIS LINE
}

// 5. CREATE FileAttachmentList COMPONENT (add before main return):

const FileAttachmentList = ({ attachments, loading }) => {
    if (loading) {
        return (
            <div style={{ textAlign: 'center', padding: 20 }}>
                <Spin />
            </div>
        )
    }

    if (!attachments || attachments.length === 0) {
        return (
            <Empty
                description="Henüz dosya eklenmemiş"
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                style={{ padding: 20 }}
            />
        )
    }

    const imageAttachments = attachments.filter(a => getFileType(a.fileName) === 'image')
    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5241'

    return (
        <div style={{ padding: isMobile ? 8 : 0 }}>
            <Row gutter={[16, 16]}>
                {attachments.map((attachment) => {
                    const fileType = getFileType(attachment.fileName)
                    const isImage = fileType === 'image'

                    return (
                        <Col xs={24} sm={12} md={8} key={attachment.id}>
                            <Card
                                size="small"
                                hoverable
                                style={{
                                    borderRadius: 8,
                                    border: isDarkMode ? '1px solid rgba(255,255,255,0.1)' : '1px solid #f0f0f0',
                                    background: isDarkMode ? '#1a1a1a' : '#fafafa'
                                }}
                                styles={{
                                    body: { padding: 12 }
                                }}
                            >
                                <Space direction="vertical" style={{ width: '100%' }} size={8}>
                                    {/* File Icon and Name */}
                                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                                        {getFileIcon(attachment.fileName)}
                                        <div style={{ flex: 1, minWidth: 0 }}>
                                            <div
                                                style={{
                                                    fontSize: 14,
                                                    fontWeight: 500,
                                                    overflow: 'hidden',
                                                    textOverflow: 'ellipsis',
                                                    whiteSpace: 'nowrap',
                                                    color: isDarkMode ? '#fff' : '#262626'
                                                }}
                                                title={attachment.fileName}
                                            >
                                                {attachment.fileName}
                                            </div>
                                            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 4 }}>
                                                {formatFileSize(attachment.fileSize)}
                                                {attachment.uploadedAt && (
                                                    <span> • {toIstanbulTime(attachment.uploadedAt).format('DD.MM.YYYY')}</span>
                                                )}
                                            </div>
                                        </div>
                                    </div>

                                    {/* Image Thumbnail */}
                                    {isImage && (
                                        <div
                                            style={{
                                                width: '100%',
                                                height: 120,
                                                borderRadius: 4,
                                                overflow: 'hidden',
                                                cursor: 'pointer',
                                                background: isDarkMode ? '#000' : '#f5f5f5',
                                                display: 'flex',
                                                alignItems: 'center',
                                                justifyContent: 'center'
                                            }}
                                            onClick={() => handleFileClick(attachment)}
                                        >
                                            <img
                                                src={`${baseUrl}/api/workitems/${selectedItem.id}/attachments/${attachment.id}/download`}
                                                alt={attachment.fileName}
                                                style={{
                                                    maxWidth: '100%',
                                                    maxHeight: '100%',
                                                    objectFit: 'contain'
                                                }}
                                            />
                                        </div>
                                    )}

                                    {/* Action Buttons */}
                                    <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
                                        <Tooltip title={isImage ? 'Görüntüle' : fileType === 'pdf' ? 'Yeni Sekmede Aç' : 'İndir'}>
                                            <Button
                                                type="primary"
                                                size="small"
                                                icon={isImage ? <EyeOutlined /> : <DownloadOutlined />}
                                                onClick={() => handleFileClick(attachment)}
                                            >
                                                {isImage ? 'Görüntüle' : fileType === 'pdf' ? 'Aç' : 'İndir'}
                                            </Button>
                                        </Tooltip>
                                        <Tooltip title="Sil">
                                            <Button
                                                danger
                                                size="small"
                                                icon={<DeleteOutlined />}
                                                onClick={() => {
                                                    modal.confirm({
                                                        title: 'Dosyayı Sil',
                                                        content: `"${attachment.fileName}" dosyasını silmek istediğinizden emin misiniz?`,
                                                        okText: 'Evet, Sil',
                                                        cancelText: 'İptal',
                                                        okType: 'danger',
                                                        onOk: () => handleDeleteAttachment(attachment.id)
                                                    })
                                                }}
                                            />
                                        </Tooltip>
                                    </Space>
                                </Space>
                            </Card>
                        </Col>
                    )
                })}
            </Row>

            {/* Image Preview Modal */}
            <Image.PreviewGroup
                preview={{
                    visible: imagePreviewVisible,
                    onVisibleChange: (visible) => setImagePreviewVisible(visible),
                    current: currentImageIndex,
                    onChange: (current) => setCurrentImageIndex(current)
                }}
                style={{ display: 'none' }}
            >
                {imageAttachments.map((attachment) => (
                    <Image
                        key={attachment.id}
                        src={`${baseUrl}/api/workitems/${selectedItem.id}/attachments/${attachment.id}/download`}
                        alt={attachment.fileName}
                    />
                ))}
            </Image.PreviewGroup>
        </div>
    )
}

// 6. ADD ATTACHMENTS SECTION TO DETAIL DRAWER
// Find the section after İşlem Geçmişi (History) card and add:

{/* Dosyalar Card */ }
<Card
    title={
        <Space>
            <FileOutlined style={{ color: '#1890ff' }} />
            <span>Dosyalar</span>
            <Badge count={workItemAttachments.length} style={{ backgroundColor: '#52c41a' }} />
        </Space>
    }
    size="small"
    style={{
        marginTop: 16,
        borderRadius: 8,
        border: isDarkMode ? '1px solid rgba(255,255,255,0.1)' : '1px solid #f0f0f0'
    }}
>
    <FileAttachmentList
        attachments={workItemAttachments}
        loading={loadingAttachments}
    />
</Card>

// 7. UPDATE COMMENT FILE UPLOAD SUCCESS HANDLER
// After successful comment with files, call fetchAttachments again:
// In the onFinish handler for comment form, after fetchWorkItemHistory:
fetchAttachments(selectedItem.id) // ADD THIS LINE
