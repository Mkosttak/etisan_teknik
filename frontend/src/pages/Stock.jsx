import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Table, Button, Modal, Form, Input, InputNumber, App, Space, Divider, Alert, Popconfirm, Typography, Tooltip, Tag, Pagination, Select } from 'antd'
import { PlusOutlined, EditOutlined, WarningOutlined, PlusCircleOutlined, HistoryOutlined, SearchOutlined, DeleteOutlined, FileExcelOutlined, InboxOutlined } from '@ant-design/icons'
import ExcelJS from 'exceljs'
import { saveAs } from 'file-saver'
import api from '../services/api'
import InstitutionSelect from '../components/InstitutionSelect'
import LoadingSpinner from '../components/LoadingSpinner'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const { Text } = Typography

const Stock = () => {
  const navigate = useNavigate()
  const { message } = App.useApp()
  const { isDarkMode } = useTheme()
  const [stockItems, setStockItems] = useState([])
  const [filteredStockItems, setFilteredStockItems] = useState([])
  const [searchText, setSearchText] = useState('')
  const [loading, setLoading] = useState(false)

  const [stockMovementModalVisible, setStockMovementModalVisible] = useState(false)
  const [adjustModalVisible, setAdjustModalVisible] = useState(false)
  const [selectedStockItem, setSelectedStockItem] = useState(null)
  const [stockMovements, setStockMovements] = useState([])
  const [movementsLoading, setMovementsLoading] = useState(false)
  const [adjustLoading, setAdjustLoading] = useState(false)
  const [adjustForm] = Form.useForm()

  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 25
  })

  useEffect(() => {
    fetchStockItems()
  }, [])

  const fetchStockItems = async () => {
    try {
      setLoading(true)
      const response = await api.get('/stock')
      const sorted = [...response.data].sort((a, b) => {
        const aIsNegative = a.currentStock < 0
        const bIsNegative = b.currentStock < 0
        const aIsLow = a.currentStock <= a.minimumStock && a.currentStock >= 0
        const bIsLow = b.currentStock <= b.minimumStock && b.currentStock >= 0
        if (aIsNegative && !bIsNegative) return -1
        if (!aIsNegative && bIsNegative) return 1
        if (aIsNegative && bIsNegative) return a.currentStock - b.currentStock
        if (aIsLow && !bIsLow) return -1
        if (!aIsLow && bIsLow) return 1
        return (a.itemName || '').localeCompare(b.itemName || '', 'tr')
      })
      setStockItems(sorted)
      setFilteredStockItems(sorted)
    } catch (error) {
      message.error(getErrorMessage(error))
    } finally {
      setLoading(false)
    }
  }

  const handleStockSearch = (value) => {
    setSearchText(value)
    let itemsToFilter = stockItems
    if (value) {
      itemsToFilter = stockItems.filter(item =>
        item.itemName?.toLowerCase().includes(value.toLowerCase())
      )
    }
    const sorted = [...itemsToFilter].sort((a, b) => {
      const aIsNegative = a.currentStock < 0
      const bIsNegative = b.currentStock < 0
      const aIsLow = a.currentStock <= a.minimumStock && a.currentStock >= 0
      const bIsLow = b.currentStock <= b.minimumStock && b.currentStock >= 0
      if (aIsNegative && !bIsNegative) return -1
      if (!aIsNegative && bIsNegative) return 1
      if (aIsNegative && bIsNegative) return a.currentStock - b.currentStock
      if (aIsLow && !bIsLow) return -1
      if (!aIsLow && bIsLow) return 1
      return (a.itemName || '').localeCompare(b.itemName || '', 'tr')
    })
    setFilteredStockItems(sorted)
    setPagination(prev => ({ ...prev, current: 1 }))
  }

  const handleExportExcel = async () => {
    const workbook = new ExcelJS.Workbook()
    const worksheet = workbook.addWorksheet('Stok Listesi')
    worksheet.columns = [
      { header: 'Ürün Adı', key: 'itemName', width: 30 },
      { header: 'Seri No', key: 'serialNumber', width: 20 },
      { header: 'Mevcut Stok', key: 'currentStock', width: 15 },
      { header: 'Açıklama', key: 'description', width: 40 }
    ]
    stockItems.forEach(item => {
      worksheet.addRow({
        itemName: item.itemName,
        serialNumber: item.serialNumber || '-',
        currentStock: item.currentStock,
        description: item.description || '-'
      })
    })
    const buffer = await workbook.xlsx.writeBuffer()
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const fileName = `Stok_Listesi_${new Date().toLocaleDateString('tr-TR').replace(/\./g, '_')}.xlsx`
    saveAs(blob, fileName)
  }

  const openAdjustModal = (item) => {
    setSelectedStockItem(item)
    adjustForm.resetFields()
    setAdjustModalVisible(true)
  }

  const handleStockAdjust = async (values) => {
    try {
      setAdjustLoading(true)
      const { quantity, institutionName, institutionOtherName, notes } = values
      const finalQuantity = quantity || 0
      if (finalQuantity === 0) {
        message.warning('Lütfen geçerli bir miktar giriniz')
        return
      }
      const absQuantity = Math.abs(finalQuantity)
      const movementType = finalQuantity > 0 ? 'In' : 'Out'
      const finalInstitutionName = institutionName === 'DİĞER' ? institutionOtherName : institutionName
      await api.post('/stock/movements', {
        stockItemId: selectedStockItem.id,
        movementType: movementType,
        quantity: absQuantity,
        institutionName: finalInstitutionName,
        notes: notes
      })
      message.success('Stok hareketi başarıyla kaydedildi')
      setAdjustModalVisible(false)
      fetchStockItems()
    } catch (error) {
      message.error(getErrorMessage(error))
    } finally {
      setAdjustLoading(false)
    }
  }

  const handleOpenStockMovement = async (item) => {
    setSelectedStockItem(item)
    setStockMovementModalVisible(true)
    try {
      setMovementsLoading(true)
      const response = await api.get(`/stock/${item.id}/movements`)
      setStockMovements(response.data)
    } catch (error) {
      message.error(getErrorMessage(error))
    } finally {
      setMovementsLoading(false)
    }
  }

  const handleDelete = async (id) => {
    try {
      await api.delete(`/stock/${id}`)
      message.success('Ürün silindi')
      fetchStockItems()
    } catch (error) {
      message.error(getErrorMessage(error))
    }
  }

  const columns = [
    {
      title: 'Ürün Adı',
      dataIndex: 'itemName',
      key: 'itemName',
      render: (name, record) => (
        <div>
          <div style={{ fontWeight: 500 }}>{name}</div>
          {record.description && (
            <div style={{ fontSize: 12, color: '#8c8c8c', marginTop: 2 }}>
              {record.description.length > 40 ? `${record.description.slice(0, 40)}...` : record.description}
            </div>
          )}
        </div>
      ),
    },
    {
      title: 'Seri No',
      dataIndex: 'serialNumber',
      key: 'serialNumber',
      render: (serialNumber) => serialNumber || '-',
    },
    {
      title: 'Mevcut Stok',
      dataIndex: 'currentStock',
      key: 'currentStock',
      render: (stock, record) => {
        const isNegative = stock < 0
        const isLow = stock <= record.minimumStock && stock >= 0
        return (
          <Tag
            color={isNegative ? 'error' : isLow ? 'error' : stock < record.minimumStock * 2 ? 'warning' : 'success'}
            style={{ fontSize: 15, fontWeight: 600, padding: '4px 12px', borderRadius: 6 }}
          >
            {(isNegative || isLow) && <WarningOutlined style={{ marginRight: 4 }} />}
            {stock}
          </Tag>
        )
      },
    },
    {
      title: 'İşlemler',
      key: 'actions',
      render: (_, record) => (
        <Space>
          <Tooltip title="Stok Düzenle (Artır/Azalt)">
            <Button
              type="link"
              icon={<PlusCircleOutlined style={{ color: '#1890ff' }} />}
              onClick={() => openAdjustModal(record)}
              style={{ color: '#1890ff' }}
            />
          </Tooltip>
          <Tooltip title="Detaylı stok hareketi">
            <Button
              type="link"
              icon={<HistoryOutlined />}
              onClick={() => handleOpenStockMovement(record)}
              style={{ color: '#1890ff' }}
            />
          </Tooltip>
          <Tooltip title="Ürün bilgilerini düzenle">
            <Button
              type="link"
              icon={<EditOutlined />}
              onClick={() => navigate(`/stock/${record.id}/edit`)}
              style={{ color: '#52c41a' }}
            />
          </Tooltip>
          <Popconfirm
            title="Ürünü silmek istediğinizden emin misiniz?"
            description={`"${record.itemName}" silinecek.`}
            onConfirm={() => handleDelete(record.id)}
            okText="Evet"
            cancelText="Hayır"
          >
            <Tooltip title="Ürünü sil">
              <Button type="link" danger icon={<DeleteOutlined />} />
            </Tooltip>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 20, marginBottom: 24, padding: '8px 4px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{ width: 42, height: 42, borderRadius: 12, background: isDarkMode ? '#177ddc' : '#1890ff', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#fff', fontSize: 20, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.3)' }}>
            <InboxOutlined />
          </div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>Stok Yönetimi</h1>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flex: 1, justifyContent: 'flex-end', minWidth: 300 }}>
          <Input
            placeholder="Ürün adına göre ara..."
            prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
            value={searchText}
            onChange={(e) => handleStockSearch(e.target.value)}
            allowClear
            style={{ maxWidth: 400, height: 42, borderRadius: 12, border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0', boxShadow: '0 4px 12px rgba(0,0,0,0.03)', background: isDarkMode ? '#1f1f1f' : '#fff' }}
          />
          <Space wrap>
            <Button icon={<FileExcelOutlined />} onClick={handleExportExcel} style={{ height: 42, borderRadius: 12, padding: '0 24px', fontWeight: 600, backgroundColor: '#107c41', color: 'white', borderColor: '#107c41', boxShadow: '0 4px 12px rgba(16, 124, 65, 0.2)' }}>
              Excel'e Aktar
            </Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => navigate('/stock/new')} style={{ height: 42, borderRadius: 12, padding: '0 24px', fontWeight: 600, boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)' }}>
              Yeni Stok
            </Button>
          </Space>
        </div>
      </div>

      {stockItems.some(item => item.currentStock <= item.minimumStock) && (
        <Alert
          message="Düşük Stok Uyarısı"
          description={
            <ul style={{ marginTop: 8, marginBottom: 0 }}>
              {stockItems.filter(item => item.currentStock <= item.minimumStock).map(item => (
                <li key={item.id}><strong>{item.itemName}</strong> {item.currentStock} adet kaldı</li>
              ))}
            </ul>
          }
          type="warning" showIcon icon={<WarningOutlined />} style={{ marginBottom: 16 }} closable
        />
      )}

      <Table
        columns={columns}
        dataSource={filteredStockItems.slice((pagination.current - 1) * pagination.pageSize, pagination.current * pagination.pageSize)}
        rowKey="id"
        loading={loading}
        pagination={false}
        scroll={{ x: true }}
        size="middle"
      />

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, alignItems: 'center' }}>
        <span style={{ marginRight: 16 }}>Toplam {filteredStockItems.length} stok malzemesi</span>
        <Pagination
          current={pagination.current}
          total={filteredStockItems.length}
          pageSize={pagination.pageSize}
          onChange={(page) => setPagination(prev => ({ ...prev, current: page }))}
          showSizeChanger={false}
          size="small"
        />
        <Select
          value={pagination.pageSize.toString()}
          style={{ width: 70, marginLeft: 8 }}
          onChange={(value) => setPagination(prev => ({ ...prev, pageSize: parseInt(value), current: 1 }))}
          options={[{ value: '25', label: '25' }, { value: '50', label: '50' }, { value: '100', label: '100' }]}
          showSearch={false}
        />
      </div>

      {/* Stok Düzenleme Modalı */}
      <Modal
        title={<Space><PlusCircleOutlined style={{ color: '#1890ff' }} /><span>Stok Düzenle - {selectedStockItem?.itemName}</span></Space>}
        open={adjustModalVisible}
        onCancel={() => setAdjustModalVisible(false)}
        footer={null}
        width={500}
      >
        <Form form={adjustForm} layout="vertical" onFinish={handleStockAdjust} initialValues={{ quantity: 1 }}>
          <Form.Item name="institutionName" label="Firma / Kurum">
            <InstitutionSelect placeholder="Kurum seçin (opsiyonel)" allowClear size="large" />
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(prev, curr) => prev.institutionName !== curr.institutionName}>
            {({ getFieldValue }) => getFieldValue('institutionName') === 'DİĞER' && (
              <Form.Item name="institutionOtherName" label="Firma Adı (Diğer)">
                <Input placeholder="Firma adını yazın" size="large" />
              </Form.Item>
            )}
          </Form.Item>
          <Form.Item name="quantity" label="Miktar (+/-)"><InputNumber style={{ width: '100%', fontSize: 16, height: 40 }} placeholder="Miktar" size="large" /></Form.Item>
          <Form.Item name="notes" label="Açıklama / Not"><Input.TextArea rows={3} placeholder="İşlem detaylarını yazınız (opsiyonel)" /></Form.Item>
          <Divider style={{ margin: '16px 0' }} />
          <Form.Item style={{ marginBottom: 0, textAlign: 'right' }}>
            <Space><Button onClick={() => setAdjustModalVisible(false)}>İptal</Button><Button type="primary" htmlType="submit" loading={adjustLoading} size="large" style={{ minWidth: 120 }}>Kaydet</Button></Space>
          </Form.Item>
        </Form>
      </Modal>

      {/* Stok Hareketi Modalı */}
      <Modal title={`Stok Hareket Geçmişi - ${selectedStockItem?.itemName}`} open={stockMovementModalVisible} onCancel={() => setStockMovementModalVisible(false)} footer={null} width={900}>
        {movementsLoading ? <div style={{ height: 300, position: 'relative' }}><LoadingSpinner /></div> : stockMovements.length === 0 ? <div style={{ textAlign: 'center', padding: '40px 0', color: '#8c8c8c' }}><HistoryOutlined style={{ fontSize: 48, marginBottom: 16 }} /><div>Henüz stok hareketi bulunmamaktadır</div></div> : (
          <Table dataSource={stockMovements} rowKey="id" pagination={{ pageSize: 10 }} size="small" scroll={{ x: 1000 }} columns={[
            { title: 'Tarih', dataIndex: 'createdAt', key: 'createdAt', width: 150, render: (date) => <div style={{ color: '#595959' }}>{new Date(date).toLocaleString('tr-TR')}</div> },
            { title: 'Tip', dataIndex: 'movementType', key: 'movementType', width: 80, align: 'center', render: (type) => <Tag color={type === 'In' ? 'green' : 'volcano'} style={{ borderRadius: 4, margin: 0 }}>{type === 'In' ? 'Giriş' : 'Çıkış'}</Tag> },
            { title: 'Miktar', dataIndex: 'quantity', key: 'quantity', width: 90, align: 'right', render: (qty, record) => <div style={{ fontWeight: 700, fontSize: 15, color: record.movementType === 'In' ? '#52c41a' : '#f5222d' }}>{record.movementType === 'In' ? '+' : '-'}{qty}</div> },
            { title: 'Firma / Kurum', dataIndex: 'institutionName', key: 'institutionName', width: 220, ellipsis: true, render: (name) => <span style={{ fontWeight: 500, color: '#262626' }}>{name || <span style={{ color: '#bfbfbf' }}>-</span>}</span> },
            { title: 'Detaylar / Notlar', dataIndex: 'notes', key: 'notes', width: 300, render: (notes, record) => <div style={{ maxWidth: 300, wordBreak: 'break-word', whiteSpace: 'normal', lineHeight: '1.4' }}>{record.reference && <Tag color="blue" style={{ marginBottom: 4, borderRadius: 6, fontWeight: 500, border: 'none', background: '#e6f7ff' }}>{record.reference}</Tag>}<div style={{ fontSize: 13, color: '#595959' }}>{notes || <span style={{ color: '#bfbfbf' }}>-</span>}</div></div> }
          ]} />
        )}
      </Modal>
    </div>
  )
}

export default Stock
