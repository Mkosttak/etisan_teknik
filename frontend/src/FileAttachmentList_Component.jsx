// FileAttachmentList Component - INSERT THIS BEFORE const WorkFlow = () => { at line 255
const FileAttachmentList = ({ attachments, loading, selectedItem, isDarkMode, isMobile, workItemAttachments, handleFileClick, handleDeleteAttachment, modal, imagePreviewVisible, setImagePreviewVisible, currentImageIndex, setCurrentImageIndex, getFileType, getFileIcon, formatFileSize, toIstanbulTime }) => {
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

            {imageAttachments.length > 0 && (
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
            )}
        </div>
    )
}

