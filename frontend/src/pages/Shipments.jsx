import { useState, useEffect } from 'react'
import { Table, App, Tag, Space, Button, Card, Modal, Form, Input, Collapse, Badge, Checkbox, Tooltip, Row, Col, Statistic, Pagination, Select } from 'antd'
import { useNavigate } from 'react-router-dom'
import {
  DownloadOutlined,
  PrinterOutlined,
  SendOutlined,
  InboxOutlined,
  EnvironmentOutlined,
  PhoneOutlined,
  UserOutlined,
  CheckCircleOutlined,
  ClockCircleOutlined,
  TruckOutlined,
  EyeOutlined
} from '@ant-design/icons'
import ExcelJS from 'exceljs'
import { saveAs } from 'file-saver'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import dayjs from 'dayjs'
import 'dayjs/locale/tr'
dayjs.locale('tr')

const Shipments = () => {
  const navigate = useNavigate()
  const { message } = App.useApp()
  const { isDarkMode } = useTheme()
  const [pendingRequests, setPendingRequests] = useState([])
  const [pendingFaults, setPendingFaults] = useState([])
  const [verifoneShipments, setVerifoneShipments] = useState([])
  const [loading, setLoading] = useState(false)
  const [verifoneLoading, setVerifoneLoading] = useState(false)
  const [shipModalVisible, setShipModalVisible] = useState(false)
  const [selectedPackage, setSelectedPackage] = useState(null)

  const [shipForm] = Form.useForm()
  const [selectedItems, setSelectedItems] = useState([])
  const [shipLoading, setShipLoading] = useState(false)
  const [checkedItems, setCheckedItems] = useState(new Set()) // Seçili öğelerin ID'leri (type-id formatında)
  const [shipSuccess, setShipSuccess] = useState(false) // Gönderim başarılı ekranı için
  const [verifoneSelectedKeys, setVerifoneSelectedKeys] = useState([]) // Verifone tablosu seçimleri
  const [createDraftForms, setCreateDraftForms] = useState(false) // Taslak form oluştur seçeneği
  const [pagination, setPagination] = useState({
    current: 1,
    pageSize: 25
  })
  const [isMobile, setIsMobile] = useState(window.innerWidth < 768)

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < 768)
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  useEffect(() => {
    fetchPendingRequests()
    fetchPendingFaults()
    fetchVerifoneShipments()
  }, [])

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < 768)
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  const fetchPendingRequests = async () => {
    try {
      setLoading(true)
      const response = await api.get('/requests')
      // Sadece Pending ve InProgress olanları al
      const pending = response.data.filter(r => r.status === 'Pending' || r.status === 'InProgress')
      setPendingRequests(pending)
    } catch (error) {
      console.error('Talepler yüklenirken hata:', error)
    } finally {
      setLoading(false)
    }
  }

  // Arızaları da getir (ShippingToCustomer durumunda olanlar)
  const fetchPendingFaults = async () => {
    try {
      const response = await api.get('/faults')
      // ShippingToCustomer durumundaki arızaları al
      const shippingFaults = response.data.filter(f => f.status === 'ShippingToCustomer')
      setPendingFaults(shippingFaults)
    } catch (error) {
      console.error('Arızalar yüklenirken hata:', error)
    }
  }

  const fetchVerifoneShipments = async () => {
    try {
      setVerifoneLoading(true)
      const response = await api.get('/shipments/verifone')
      setVerifoneShipments(response.data)
    } catch (error) {
      console.error('Verifone gönderimleri yüklenirken hata:', error)
    } finally {
      setVerifoneLoading(false)
    }
  }

  // Adres normalize fonksiyonu - küçük farklılıkları yok sayar
  const normalizeAddress = (str) => {
    if (!str) return ''
    return str
      .toLowerCase()
      .trim()
      .replace(/\s+/g, ' ')  // Çoklu boşlukları tek boşluğa çevir
      .replace(/[.,\/#!$%\^&\*;:{}=\-_`~()]/g, '') // Özel karakterleri kaldır
  }

  // Adreslere göre grupla (hem request hem fault)
  const groupByAddress = (items) => {
    const groups = {}

    items.forEach(item => {
      // Adres anahtarı oluştur - normalleştirilmiş
      const cityNorm = normalizeAddress(item.shippingCity)
      const districtNorm = normalizeAddress(item.shippingDistrict)
      const addressNorm = normalizeAddress(item.shippingAddressLine)

      // Şehir + İlçe ile grupla (adres satırı farklı olabilir)
      const addressKey = `${cityNorm}|${districtNorm}`

      if (!groups[addressKey]) {
        groups[addressKey] = {
          id: addressKey,
          shippingCity: item.shippingCity,
          shippingDistrict: item.shippingDistrict,
          shippingAddressLine: item.shippingAddressLine,
          contactPerson: item.contactPerson,
          contactPhone: item.contactPhone,
          items: []
        }
      }

      // _sourceType zaten eklenmiş durumda
      groups[addressKey].items.push({
        ...item,
        itemType: item._sourceType || 'request'
      })

      // En son iletişim bilgisini kullan
      if (item.contactPerson) {
        groups[addressKey].contactPerson = item.contactPerson
      }
      if (item.contactPhone) {
        groups[addressKey].contactPhone = item.contactPhone
      }
    })

    return Object.values(groups)
  }

  // Talep içeriğini (ürünleri) formatlayan yardımcı fonksiyon
  const getRequestContent = (item) => {
    const products = []

    // Stok ürünleri
    if (item.stockItems && item.stockItems.length > 0) {
      item.stockItems.forEach(si => {
        if (!si.isCancelled && !si.isShipped) {
          products.push(`${si.quantity}x ${si.itemName}`)
        }
      })
    }

    // Ekstra ürünler
    if (item.extraItems) {
      const extras = item.extraItems.split(',').map(s => s.trim()).filter(Boolean)
      const shippedExtras = (item.shippedExtraItems || '').split(',').map(s => s.trim())

      extras.forEach(extra => {
        if (!shippedExtras.includes(extra)) {
          products.push(extra)
        }
      })
    }

    if (products.length > 0) {
      return products.join(', ')
    }

    return item.description || 'İçerik belirtilmemiş'
  }

  // Kuruma göre grupla (Ankara içi)
  const groupByInstitution = (items) => {
    const groups = {}

    items.forEach(item => {
      const instName = item.institutionName || 'Kurum Belirtilmemiş'
      const key = normalizeAddress(instName)

      if (!groups[key]) {
        groups[key] = {
          id: key,
          title: instName,
          isHandDelivery: true, // Elden teslim işareti
          shippingCity: item.shippingCity,
          shippingDistrict: item.shippingDistrict,
          shippingAddressLine: item.shippingAddressLine,
          contactPerson: item.contactPerson,
          contactPhone: item.contactPhone,
          items: []
        }
      }

      groups[key].items.push({
        ...item,
        itemType: item._sourceType || 'request'
      })
    })

    return Object.values(groups)
  }

  // Tüm öğeleri birleştir (requests + faults) - _sourceType ekle
  const allPendingItems = [
    ...pendingRequests.map(r => ({ ...r, _sourceType: 'request' })),
    ...pendingFaults.map(f => ({ ...f, _sourceType: 'fault' }))
  ]

  // Ankara ve Diğerleri olarak ayır
  const ankaraItems = allPendingItems.filter(item =>
    normalizeAddress(item.shippingCity).includes('ankara')
  )
  const otherItems = allPendingItems.filter(item =>
    !normalizeAddress(item.shippingCity).includes('ankara')
  )

  const groupedCargoPackages = groupByAddress(otherItems)
  const groupedAnkaraPackages = groupByInstitution(ankaraItems)

  const handleShipPackage = (pkg) => {
    setSelectedPackage(pkg)

    const flatItems = []

    pkg.items.forEach(item => {
      if (item.itemType === 'fault') {
        flatItems.push({
          key: `fault-${item.id}`,
          originalId: item.id,
          type: 'fault',
          target: item.target || 'customer',
          title: `Cihaz: ${item.deviceSerialNumber || 'Cihaz'}`,
          subtitle: item.description || 'Arıza detayı yok',
          isHandDelivery: pkg.isHandDelivery
        })
      } else if (item.itemType === 'request') {
        // Talebin stok öğelerini parçala
        const stockItems = item.stockItems || []
        const activeStockItems = stockItems.filter(si => !si.isCancelled && !si.isShipped)
        let hasContent = false

        // 1. Stok Öğelerini Ekle
        if (activeStockItems.length > 0) {
          activeStockItems.forEach(si => {
            flatItems.push({
              key: `req-${item.id}-si-${si.id}`,
              originalId: si.id, // RequestStockItem ID
              requestId: item.id,
              type: 'requestStockItem',
              target: item.target || 'customer',
              title: `Talep #${item.id}: ${si.itemName} (${si.quantity} adet)`,
              subtitle: item.description || 'Talep açıklaması yok',
              isHandDelivery: pkg.isHandDelivery
            })
          })
          hasContent = true
        }

        // 2. Ekstra Ürünleri Ekle
        // 2. Ekstra Ürünleri Ekle
        if (item.extraItems) {
          const extras = item.extraItems.split(',').map(s => s.trim()).filter(Boolean)
          const shippedExtras = (item.shippedExtraItems || '').split(',').map(s => s.trim())
          let extraAdded = false

          extras.forEach((extra, idx) => {
            if (shippedExtras.includes(extra)) return

            flatItems.push({
              key: `req-${item.id}-extra-${idx}`,
              originalId: item.id,
              type: 'requestExtraItem',
              target: item.target || 'customer',
              title: `Talep #${item.id}: ${extra}`,
              subtitle: 'Stok dışı ekstra ürün',
              isHandDelivery: pkg.isHandDelivery,
              extraName: extra,
              requestId: item.id
            })
            extraAdded = true
          })
          if (extraAdded) hasContent = true
        }

        // 3. Fallback (Eğer hiçbişi yoksa)
        if (!hasContent) {
          flatItems.push({
            key: `req-${item.id}`,
            originalId: item.id,
            type: 'request',
            target: item.target || 'customer',
            title: `Talep #${item.id} (İçerik Yok)`,
            subtitle: item.description || 'İçerik belirtilmemiş',
            isHandDelivery: pkg.isHandDelivery
          })
        }
      }
    })

    setSelectedItems(flatItems)

    // Varsayılan olarak hepsini seç
    const allKeys = new Set(flatItems.map(item => item.key))
    setCheckedItems(allKeys)
    setShipSuccess(false)

    shipForm.resetFields()
    setShipModalVisible(true)
  }

  const handleShipVerifone = () => {
    if (verifoneSelectedKeys.length === 0) {
      message.warning('Lütfen Verifone tablosundan en az bir cihaz seçin.')
      return
    }

    // Seçili cihazları bul
    const selectedDevices = verifoneShipments.filter(item => verifoneSelectedKeys.includes(item.id))

    // Verifone için sanal bir paket oluştur
    const verifonePackage = {
      target: 'verifone', // Paket seviyesinde target belirtilmeli
      shippingCity: 'İstanbul',
      shippingDistrict: 'Şişli',
      shippingAddressLine: 'Verifone Yetkili Servis Merkezi',
      contactPerson: 'Teknik Servis',
      contactPhone: '444 0 123',
      items: selectedDevices.map(d => ({
        id: d.faultId || d.id, // ID eşleştirmesi
        itemType: 'fault',
        deviceSerialNumber: d.deviceSerialNumber,
        description: d.errorDescription,
        target: 'verifone' // Özel hedef işareti
      }))
    }

    handleShipPackage(verifonePackage)
  }

  const handleToggleItem = (key) => {
    const newChecked = new Set(checkedItems)
    if (newChecked.has(key)) {
      newChecked.delete(key)
    } else {
      newChecked.add(key)
    }
    setCheckedItems(newChecked)
  }

  const handleSelectAll = (e) => {
    if (e.target.checked) {
      const allKeys = new Set(selectedItems.map(item => item.key))
      setCheckedItems(allKeys)
    } else {
      setCheckedItems(new Set())
    }
  }

  const handleConfirmShipment = async () => {
    if (!selectedPackage) return
    if (checkedItems.size === 0) {
      message.warning('Lütfen en az bir öğe seçin')
      return
    }

    setShipLoading(true)
    try {
      // Seçili öğeleri grupla
      const selectedList = selectedItems.filter(item => checkedItems.has(item.key))

      // Request'leri ve Fault'ları ayır
      const faultIds = []
      const requestStockItemMap = {} // requestId -> [stockItemId, ...]
      const fullRequestIds = [] // Eğer eski tip request gönderimi varsa

      selectedList.forEach(item => {
        if (item.type === 'fault') {
          faultIds.push(item.originalId)
        } else if (item.type === 'requestStockItem') {
          if (!requestStockItemMap[item.requestId]) {
            requestStockItemMap[item.requestId] = []
          }
          requestStockItemMap[item.requestId].push(item.originalId)
        } else if (item.type === 'requestExtraItem') {
          // Ekstra ürünler için backend update yok, sadece görsel
          // Ancak kullanıcı seçtiyse "gönderildi" kabul ediyoruz (sözlü olarak)
          // İşlem yapmaya gerek yok
        } else if (item.type === 'request') {
          // Fallback (eski tip)
          fullRequestIds.push(item.originalId)
        }
      })

      const promises = []

      // 1. Fault Güncellemeleri
      faultIds.forEach(id => {
        const isVerifone = selectedPackage.target === 'verifone'
        const status = selectedPackage.isHandDelivery ? 'Delivered' : (
          isVerifone ? 'SentToService' : 'SentToCustomer'
        )

        // Verifone gönderildiğinde özel not ekle
        let notes = ''
        if (isVerifone) {
          notes = 'Verifone gönderildi'
        }

        const payload = {
          status,
          notes: notes || undefined,
          createDraftServiceForm: selectedPackage.isHandDelivery && createDraftForms
        }
        promises.push(api.put(`/faults/${id}/status`, payload))
      })

      // 2. Request Parçalar ve Ekstra Ürünler
      const requestPayloads = {}

      selectedList.forEach(item => {
        if (item.type === 'requestStockItem' || item.type === 'requestExtraItem') {
          if (!requestPayloads[item.requestId]) {
            requestPayloads[item.requestId] = { stockItemIds: [], extraItems: [] }
          }
          if (item.type === 'requestStockItem') {
            requestPayloads[item.requestId].stockItemIds.push(item.originalId)
          } else {
            requestPayloads[item.requestId].extraItems.push(item.extraName)
          }
        }
      })

      Object.keys(requestPayloads).forEach(reqId => {
        const payload = {
          requestStockItemIds: requestPayloads[reqId].stockItemIds,
          extraItems: requestPayloads[reqId].extraItems
        }
        promises.push(api.post(`/requests/${reqId}/ship`, payload))
      })

      // 3. Full Request Güncellemeleri (Fallback)
      fullRequestIds.forEach(id => {
        const payload = {
          status: 'Completed'
        }
        promises.push(api.put(`/requests/${id}/status`, payload))
      })

      await Promise.all(promises)

      setShipSuccess(true)
      message.success(selectedPackage.isHandDelivery ? 'Teslimat kaydedildi' : 'Kargo gönderimi kaydedildi')

      // Listeleri güncelle
      // Listeleri güncelle
      fetchPendingRequests()
      fetchPendingFaults()
      fetchVerifoneShipments()

    } catch (error) {
      console.error('Shipment error:', error)
      message.error('İşlem sırasında bir hata oluştu')
    } finally {
      setShipLoading(false)
    }
  }

  const handleCloseModal = () => {
    setShipModalVisible(false)
    setShipSuccess(false)
    setCreateDraftForms(false)
  }

  const exportToExcel = async () => {
    if (verifoneShipments.length === 0) {
      message.warning('Dışa aktarılacak veri bulunamadı')
      return
    }

    const workbook = new ExcelJS.Workbook()
    const worksheet = workbook.addWorksheet('Verifone Gönderimleri')

    worksheet.columns = [
      { header: 'Seri No', key: 'deviceSerialNumber', width: 20 },
      { header: 'Hata Kodu', key: 'errorCode', width: 15 },
      { header: 'Hata Açıklaması', key: 'errorDescription', width: 40 }
    ]

    verifoneShipments.forEach(item => {
      worksheet.addRow({
        deviceSerialNumber: item.deviceSerialNumber || '',
        errorCode: item.errorCode || '',
        errorDescription: item.errorDescription || ''
      })
    })

    const buffer = await workbook.xlsx.writeBuffer()
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    const fileName = `Verifone_Gonderimleri_${dayjs().format('YYYY-MM-DD_HH-mm-ss')}.xlsx`
    saveAs(blob, fileName)
    message.success('Excel dosyası indirildi')
  }

  const getTotalItemCount = (pkg) => {
    let total = 0
    pkg.items.forEach(item => {
      if (item.itemType === 'fault') {
        total += 1
      } else if (item.itemType === 'request') {
        // Stok ürünleri
        if (item.stockItems && item.stockItems.length > 0) {
          item.stockItems.forEach(si => {
            if (!si.isCancelled && !si.isShipped) {
              total += (parseInt(si.quantity) || 1)
            }
          })
        }
        // Ekstra ürünler
        if (item.extraItems) {
          const extras = item.extraItems.split(',').map(s => s.trim()).filter(Boolean)
          const shippedExtras = (item.shippedExtraItems || '').split(',').map(s => s.trim())

          extras.forEach(extra => {
            if (!shippedExtras.includes(extra)) {
              const match = extra.match(/^(\d+)x\s+(.+)$/)
              let qty = 1
              if (match) qty = parseInt(match[1]) || 1
              total += qty
            }
          })
        }
      }
    })
    return total
  }

  const mergeProducts = (pkg) => {
    const merged = {}
    pkg.items.forEach(item => {
      if (item.itemType === 'fault') {
        const key = `fault-${item.deviceSerialNumber}`
        if (!merged[key]) {
          merged[key] = { type: 'fault', name: item.deviceSerialNumber || 'Cihaz', count: 0, ids: [] }
        }
        merged[key].count += 1
        merged[key].ids.push(item.id)
      } else if (item.itemType === 'request') {
        if (item.stockItems && item.stockItems.length > 0) {
          item.stockItems.forEach(si => {
            if (!si.isCancelled && !si.isShipped) {
              const key = `stock-${si.itemName}`
              if (!merged[key]) {
                merged[key] = { type: 'stock', name: si.itemName, count: 0, ids: [] }
              }
              merged[key].count += (parseInt(si.quantity) || 1)
              merged[key].ids.push(item.id)
            }
          })
        }
        if (item.extraItems) {
          const extras = item.extraItems.split(',').map(s => s.trim()).filter(Boolean)
          const shippedExtras = (item.shippedExtraItems || '').split(',').map(s => s.trim())

          extras.forEach(extra => {
            if (!shippedExtras.includes(extra)) {
              const match = extra.match(/^(\d+)x\s+(.+)$/)
              let qty = 1
              let name = extra
              if (match) {
                qty = parseInt(match[1]) || 1
                name = match[2]
              }

              const key = `extra-${name}`
              if (!merged[key]) {
                merged[key] = { type: 'extra', name: name, count: 0, ids: [] }
              }
              merged[key].count += qty
              merged[key].ids.push(item.id)
            }
          })
        }
      }
    })
    return Object.values(merged)
  }

  const printShippingLabel = (pkg) => {
    const printWindow = window.open('', '_blank')

    const addressParts = [
      pkg.shippingAddressLine,
      pkg.shippingDistrict && pkg.shippingCity
        ? `${pkg.shippingDistrict} / ${pkg.shippingCity}`
        : pkg.shippingCity || pkg.shippingDistrict
    ].filter(Boolean)

    const itemDescriptions = pkg.items.map(item =>
      item.itemType === 'fault'
        ? `• [Arıza] ${item.deviceSerialNumber} - ${item.description || ''}`
        : `• [Talep #${item.id}] ${getRequestContent(item)}`
    ).join('<br>')

    const htmlContent = `
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset="UTF-8">
          <title>Kargo Etiketi</title>
          <style>
            @media print {
              @page { size: A4; margin: 10mm; }
              body { margin: 0; padding: 0; }
            }
            body { font-family: Arial, sans-serif; margin: 0; padding: 20px; }
            .shipping-label {
              width: 100%; max-width: 800px; margin: 0 auto;
              border: 2px solid #000; padding: 20px; box-sizing: border-box;
            }
            .sender-section { margin-bottom: 20px; }
            .logo { margin-bottom: 15px; }
            .label { font-weight: bold; margin-right: 5px; }
            .sender-info, .receiver-info { margin-bottom: 10px; }
            .divider { border-top: 2px dashed #000; margin: 20px 0; }
            .receiver-section { margin-top: 20px; }
            .info-line { margin-bottom: 8px; }
            .address-line { margin-bottom: 4px; }
            .package-contents { 
              margin-top: 15px; padding: 10px; 
              background: #f5f5f5; border-radius: 4px; 
            }
            .package-contents h4 { margin: 0 0 10px 0; }
            .package-count {
              position: absolute; top: 10px; right: 10px;
              background: #1890ff; color: white; padding: 5px 15px;
              border-radius: 4px; font-weight: bold;
            }
          </style>
        </head>
        <body>
          <div class="shipping-label" style="position: relative;">
            <div class="package-count">${getTotalItemCount(pkg)} Adet Öğe</div>
            <div class="sender-section">
              <div class="logo">
                <img src="/etisan-logo.png" alt="Etisan Proje" style="height: 50px; width: auto;" />
              </div>
              <div class="sender-info">
                <div class="info-line">
                  <span class="label">Gönderen:</span>
                  <span>Etisan Proje Bilgi ve Yazılım Teknolojileri San. Tic. A.Ş.</span>
                </div>
                <div class="info-line">
                  <span class="label">Adres:</span>
                  <span>Prof. Dr. Ahmet Taner Kışlalı Mah. 2853. Cad. Önder Sok. No: 4 Çankaya / Ankara</span>
                </div>
                <div class="info-line">
                  <span class="label">Tel:</span>
                  <span>+90 312 442 42 24</span>
                </div>
              </div>
            </div>
            
            <div class="divider"></div>
            
            <div class="receiver-section">
              <div class="receiver-info">
                <div class="info-line">
                  <span class="label">Alıcı:</span>
                  <span>${pkg.contactPerson ? pkg.contactPerson.toUpperCase() : ''}</span>
                </div>
                <div class="info-line">
                  <span class="label">Adres:</span>
                  <span>${addressParts.join(' - ')}</span>
                </div>
                ${pkg.contactPhone ? `
                <div class="info-line">
                  <span class="label">Tel:</span>
                  <span>${pkg.contactPhone}</span>
                </div>
                ` : ''}
              </div>
              
              <div class="package-contents">
                <h4>Paket İçeriği (${getTotalItemCount(pkg)} öğe):</h4>
                ${itemDescriptions}
              </div>
            </div>
          </div>
        </body>
      </html>
    `

    printWindow.document.write(htmlContent)
    printWindow.document.close()

    setTimeout(() => {
      printWindow.print()
    }, 250)
  }

  const verifoneColumns = [
    {
      title: 'Seri No',
      dataIndex: 'deviceSerialNumber',
      key: 'deviceSerialNumber',
      width: 150,
      responsive: ['md'], // Sadece medium ve üzeri ekranlarda göster
    },
    {
      title: 'Hata Kodu',
      dataIndex: 'errorCode',
      key: 'errorCode',
      width: 120,
      render: (code) => code || '-',
      responsive: ['md'], // Sadece medium ve üzeri ekranlarda göster
    },
    {
      title: 'Hata Açıklaması',
      dataIndex: 'errorDescription',
      key: 'errorDescription',
      ellipsis: true,
    },
    {
      title: 'İşlemler',
      key: 'actions',
      render: (_, record) => (
        <Tooltip title="Detay">
          <Button
            type="link"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/faults/${record.faultId}`)}
            style={{ color: '#1890ff' }}
          />
        </Tooltip>
      ),
    },
  ]

  // Paket kartı bileşeni - Kompakt ve Mobil Uyumlu Tasarım
  const PackageCard = ({ pkg }) => {
    const faultCount = pkg.items.filter(i => i.itemType === 'fault').length
    const requestCount = pkg.items.filter(i => i.itemType === 'request').length

    // Gece modu renkleri
    const cardBg = isDarkMode ? '#1f1f1f' : '#fff'
    const borderColor = isDarkMode ? '#303030' : '#e8e8e8'
    const textColor = isDarkMode ? '#e8e8e8' : '#333'
    const subTextColor = isDarkMode ? '#8c8c8c' : '#666'

    return (
      <Card
        size="small"
        style={{
          borderRadius: 12,
          border: `1px solid ${borderColor}`,
          background: cardBg,
          height: '100%',
        }}
        styles={{ body: { padding: 0 } }}
      >
        {/* Kompakt Başlık */}
        <div style={{
          padding: '12px 16px',
          borderBottom: `1px solid ${borderColor}`,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 8,
        }}>
          <div style={{ display: 'flex', gap: 10, alignItems: 'center', flex: 1 }}>
            <div style={{
              width: 36,
              height: 36,
              borderRadius: 8,
              background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0
            }}>
              <EnvironmentOutlined style={{ fontSize: 18, color: 'white' }} />
            </div>
            <div>
              <div style={{ fontWeight: 600, fontSize: 14, color: textColor }}>
                {pkg.title || (
                  <>
                    {pkg.shippingCity || 'Şehir Yok'}
                    {pkg.shippingDistrict && ` / ${pkg.shippingDistrict}`}
                  </>
                )}
              </div>
              <div style={{ fontSize: 12, color: subTextColor }}>
                {pkg.isHandDelivery ? 'Elden Teslim' : 'Kargo'} • {getTotalItemCount(pkg)} öğe
                {faultCount > 0 && ` • ${faultCount} arıza`}
                {requestCount > 0 && ` • ${requestCount} talep`}
              </div>
            </div>
          </div>

          <Space size="small" wrap>
            <Button
              type="primary"
              size="small"
              icon={pkg.isHandDelivery ? <CheckCircleOutlined /> : <SendOutlined />}
              onClick={() => handleShipPackage(pkg)}
              style={{ borderRadius: 6 }}
            >
              {pkg.isHandDelivery ? 'Teslim Et' : 'Gönder'}
            </Button>
            {!pkg.isHandDelivery && (
              <Button
                size="small"
                icon={<PrinterOutlined />}
                onClick={() => printShippingLabel(pkg)}
                style={{ borderRadius: 6 }}
              >
                Yazdır
              </Button>
            )}
          </Space>
        </div>

        {/* İçerik */}
        <div style={{ padding: 12 }}>
          {/* Adres ve İletişim - Tek satırda */}
          <div style={{
            fontSize: 12,
            color: subTextColor,
            marginBottom: 8,
            display: 'flex',
            flexWrap: 'wrap',
            gap: '8px 16px',
          }}>
            {pkg.shippingAddressLine && (
              <span><EnvironmentOutlined /> {pkg.shippingAddressLine.slice(0, 40)}{pkg.shippingAddressLine.length > 40 ? '...' : ''}</span>
            )}
            {pkg.contactPerson && (
              <span><UserOutlined style={{ color: '#1890ff' }} /> {pkg.contactPerson}</span>
            )}
            {pkg.contactPhone && (
              <span><PhoneOutlined style={{ color: '#52c41a' }} /> {pkg.contactPhone}</span>
            )}
          </div>

          {/* Öğeler - Kompakt Liste */}
          <div style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: 6,
          }}>
            {(() => {
              const mergedProducts = mergeProducts(pkg)
              return mergedProducts.map((product, index) => (
                <Tag
                  key={`${product.type}-${product.name}-${index}`}
                  color={product.type === 'fault' ? 'purple' : (product.type === 'extra' ? 'orange' : 'blue')}
                  style={{
                    margin: 0,
                    borderRadius: 6,
                    fontSize: 11,
                    padding: '2px 8px',
                    cursor: 'pointer'
                  }}
                  onClick={(e) => {
                    e.stopPropagation()
                    if (product.ids.length > 0) {
                      const targetId = product.ids[0]
                      navigate(product.type === 'fault' ? `/faults/${targetId}` : `/requests/${targetId}`)
                    }
                  }}
                >
                  {product.type === 'fault' ? '🔧' : '📦'} {product.count}x {product.name}
                </Tag>
              ))
            })()}
          </div>
        </div>
      </Card >
    )
  }

  return (
    <div>
      {/* Refined Header */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 20,
        marginBottom: 24,
        padding: '8px 4px'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <div style={{
            width: 42,
            height: 42,
            borderRadius: 12,
            background: isDarkMode ? '#722ed1' : '#722ed1',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            fontSize: 20,
            boxShadow: '0 4px 12px rgba(114, 46, 209, 0.3)'
          }}>
            <TruckOutlined />
          </div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
            Kargo Takip
          </h1>
        </div>
      </div>


      {/* Özet Kartları */}
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={8}>
          <Card style={{ borderRadius: 12 }}>
            <Statistic
              title="Bekleyen Paketler (Kargo)"
              value={groupedCargoPackages.length}
              prefix={<InboxOutlined style={{ color: '#faad14' }} />}
              valueStyle={{ color: '#faad14' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card style={{ borderRadius: 12 }}>
            <Statistic
              title="Ankara Teslimatları"
              value={groupedAnkaraPackages.length}
              prefix={<EnvironmentOutlined style={{ color: '#52c41a' }} />}
              valueStyle={{ color: '#52c41a' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={8}>
          <Card style={{ borderRadius: 12 }}>
            <Statistic
              title="Verifone'a Gönderilecek"
              value={verifoneShipments.length}
              prefix={<TruckOutlined style={{ color: '#722ed1' }} />}
              valueStyle={{ color: '#722ed1' }}
            />
          </Card>
        </Col>
      </Row>

      {/* Ankara Teslimatları */}
      <div style={{ marginBottom: 32 }}>
        <h2 style={{
          marginBottom: 16,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          color: isDarkMode ? 'var(--text-primary)' : undefined
        }}>
          <EnvironmentOutlined style={{ color: '#52c41a' }} />
          Ankara Teslim Edilecek
          <Tag color="green">{groupedAnkaraPackages.length} kurum</Tag>
        </h2>

        {groupedAnkaraPackages.length === 0 ? (
          <Card style={{ textAlign: 'center', padding: 20, borderRadius: 12, marginBottom: 24, border: '1px dashed #d9d9d9' }}>
            <div style={{ color: '#8c8c8c' }}>Ankara içi teslimat bulunmuyor</div>
          </Card>
        ) : (
          <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
            {groupedAnkaraPackages.map(pkg => (
              <Col xs={24} md={12} lg={8} key={pkg.id} style={{ display: 'flex' }}>
                <div style={{ width: '100%' }}>
                  <PackageCard pkg={pkg} />
                </div>
              </Col>
            ))}
          </Row>
        )}
      </div>

      {/* Bekleyen Kargo Paketleri */}
      <div style={{ marginBottom: 32 }}>
        <h2 style={{
          marginBottom: 16,
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          color: isDarkMode ? 'var(--text-primary)' : undefined
        }}>
          <InboxOutlined style={{ color: '#faad14' }} />
          Gönderilecek Paketler (Kargo)
          <Tag color="orange">{groupedCargoPackages.length} paket</Tag>
        </h2>

        {groupedCargoPackages.length === 0 ? (
          <Card style={{ textAlign: 'center', padding: 40, borderRadius: 12 }}>
            <CheckCircleOutlined style={{ fontSize: 48, color: '#52c41a', marginBottom: 16 }} />
            <div style={{ color: '#8c8c8c' }}>Gönderilecek kargo paketi bulunmuyor</div>
          </Card>
        ) : (
          <Row gutter={[16, 16]}>
            {groupedCargoPackages.map(pkg => (
              <Col xs={24} md={12} lg={8} key={pkg.id} style={{ display: 'flex' }}>
                <div style={{ width: '100%' }}>
                  <PackageCard pkg={pkg} />
                </div>
              </Col>
            ))}
          </Row>
        )}
      </div>

      {/* Verifone'a Gönderilecek Cihazlar */}
      <div style={{ marginBottom: 32 }}>
        <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <h2 style={{
            margin: 0,
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            fontSize: 'clamp(18px, 4vw, 24px)',
            color: isDarkMode ? 'var(--text-primary)' : undefined
          }}>
            <TruckOutlined style={{ color: '#722ed1' }} />
            Verifone'a Gönderilecek Cihazlar
          </h2>
          <div style={{ display: 'flex', gap: 12 }}>
            <Button
              type="default"
              icon={<DownloadOutlined />}
              onClick={exportToExcel}
              loading={verifoneLoading}
              disabled={verifoneShipments.length === 0}
              size={isMobile ? 'small' : 'middle'}
            >
              {isMobile ? 'Excel' : 'Excel\'e Aktar'}
            </Button>
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleShipVerifone}
              disabled={verifoneSelectedKeys.length === 0}
              style={{
                background: verifoneSelectedKeys.length > 0 ? 'linear-gradient(135deg, #722ed1 0%, #9254de 100%)' : undefined,
                border: 'none',
              }}
              size={isMobile ? 'small' : 'middle'}
            >
              {isMobile ? 'Gönder' : `${verifoneSelectedKeys.length > 0 ? verifoneSelectedKeys.length : ''} Cihazı Gönder`}
            </Button>
          </div>
        </div>
        <Card
          style={{
            background: isDarkMode
              ? 'linear-gradient(135deg, rgba(26, 31, 46, 0.6) 0%, rgba(22, 27, 34, 0.6) 100%)'
              : undefined,
            border: isDarkMode ? '1px solid rgba(45, 52, 65, 0.3)' : undefined,
            borderRadius: 12,
            boxShadow: isDarkMode
              ? '0 4px 16px rgba(0, 0, 0, 0.3)'
              : '0 2px 8px rgba(0, 0, 0, 0.1)',
            backdropFilter: isDarkMode ? 'blur(10px)' : undefined,
            WebkitBackdropFilter: isDarkMode ? 'blur(10px)' : undefined
          }}
        >
          <Table
            rowSelection={{
              selectedRowKeys: verifoneSelectedKeys,
              onChange: (keys) => setVerifoneSelectedKeys(keys),
            }}
            columns={verifoneColumns}
            dataSource={verifoneShipments.slice((pagination.current - 1) * pagination.pageSize, pagination.current * pagination.pageSize)}
            rowKey="id"
            loading={verifoneLoading}
            pagination={false}
            scroll={{ x: 'max-content' }}
            style={{
              background: 'transparent',
              borderRadius: 12,
              overflow: 'hidden'
            }}
          />
        </Card>

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 16, alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
          <span style={{ marginRight: isMobile ? 0 : 16, fontSize: isMobile ? 12 : 14 }}>
            {isMobile ? `${verifoneShipments.length} cihaz` : `Toplam ${verifoneShipments.length} cihaz`}
          </span>
          <Pagination
            current={pagination.current}
            total={verifoneShipments.length}
            pageSize={pagination.pageSize}
            onChange={(page) => setPagination(prev => ({ ...prev, current: page }))}
            showSizeChanger={false}
            size="small"
            simple={isMobile}
          />
          <Select
            value={pagination.pageSize.toString()}
            style={{ width: isMobile ? 60 : 70, marginLeft: isMobile ? 0 : 8 }}
            onChange={(value) => setPagination(prev => ({ ...prev, pageSize: parseInt(value), current: 1 }))}
            options={[
              { value: '25', label: '25' },
              { value: '50', label: '50' },
              { value: '100', label: '100' },
            ]}
            showSearch={false}
            size="small"
          />
        </div>
      </div>

      {/* Kargo Gönderim Modalı */}
      <Modal
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <SendOutlined style={{ color: '#52c41a' }} />
            Kargo Gönderimi Onayla
          </div>
        }
        open={shipModalVisible}
        onCancel={handleCloseModal}
        footer={null}
        width={600}
      >
        {shipSuccess ? (
          <div style={{ textAlign: 'center', padding: '20px 0' }}>
            <CheckCircleOutlined style={{ fontSize: 64, color: '#52c41a', marginBottom: 24 }} />
            <h2 style={{ marginBottom: 12 }}>Kargo İşlemi Başarılı!</h2>
            <p style={{ fontSize: 16, color: '#666', marginBottom: 32 }}>
              Seçilen öğeler "{selectedPackage?.isHandDelivery ? 'Teslim Edildi' : 'Gönderildi'}" olarak işaretlendi.<br />
              {!selectedPackage?.isHandDelivery && 'Kargo etiketini şimdi yazdırmak ister misiniz?'}
            </p>
            <Space size="middle">
              {!selectedPackage?.isHandDelivery && (
                <Button
                  type="primary"
                  size="large"
                  icon={<PrinterOutlined />}
                  onClick={() => printShippingLabel(selectedPackage)}
                  style={{ minWidth: 140 }}
                >
                  Etiketi Yazdır
                </Button>
              )}
              <Button
                size="large"
                onClick={handleCloseModal}
                style={{ minWidth: 140 }}
              >
                Kapat
              </Button>
            </Space>
          </div>
        ) : (
          selectedPackage && (
            <div>
              {/* Adres Özeti */}
              <Card size="small" style={{ marginBottom: 16, background: '#f6ffed', border: '1px solid #b7eb8f' }}>
                <div style={{ fontWeight: 600, marginBottom: 8 }}>
                  <EnvironmentOutlined /> Gönderim Adresi
                </div>
                <div>{selectedPackage.shippingAddressLine}</div>
                <div style={{ color: '#666' }}>
                  {[selectedPackage.shippingDistrict, selectedPackage.shippingCity].filter(Boolean).join(' / ')}
                </div>
                {selectedPackage.contactPerson && (
                  <div style={{ marginTop: 8 }}>
                    <UserOutlined /> {selectedPackage.contactPerson}
                    {selectedPackage.contactPhone && ` - ${selectedPackage.contactPhone}`}
                  </div>
                )}
              </Card>

              {/* Öğe Seçimi */}
              <div style={{ marginBottom: 24 }}>
                <div style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  marginBottom: 12,
                  padding: '0 4px'
                }}>
                  <div style={{ fontWeight: 600 }}>Gönderilecek Öğeler:</div>
                  <Checkbox
                    indeterminate={checkedItems.size > 0 && checkedItems.size < selectedItems.length}
                    checked={checkedItems.size === selectedItems.length}
                    onChange={handleSelectAll}
                  >
                    Tümünü Seç
                  </Checkbox>
                </div>

                <div style={{ maxHeight: 300, overflowY: 'auto', paddingRight: 4 }}>
                  {selectedItems.map((item, index) => {
                    const isChecked = checkedItems.has(item.key);
                    return (
                      <div
                        key={item.key}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: 12,
                          padding: '12px',
                          background: isDarkMode ? '#262626' : '#f5f5f5',
                          borderRadius: 8,
                          marginBottom: 8,
                          border: isChecked ? `1px solid #1890ff` : '1px solid transparent',
                          transition: 'all 0.3s',
                          cursor: 'pointer'
                        }}
                        onClick={() => handleToggleItem(item.key)} // key kullanıyoruz
                      >
                        <Checkbox
                          checked={isChecked}
                          style={{ transform: 'scale(1.1)' }}
                        />

                        <div style={{
                          width: 32,
                          height: 32,
                          borderRadius: 6,
                          background: item.type === 'fault'
                            ? 'linear-gradient(135deg, #722ed1 0%, #9254de 100%)'
                            : 'linear-gradient(135deg, #1890ff 0%, #40a9ff 100%)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          color: 'white',
                          fontSize: 14,
                          fontWeight: 600,
                          flexShrink: 0
                        }}>
                          {index + 1}
                        </div>

                        <div style={{ flex: 1, overflow: 'hidden' }}>
                          <div style={{ fontSize: 13, fontWeight: 500, color: isDarkMode ? '#e8e8e8' : '#333' }}>
                            {item.title}
                          </div>
                          <div style={{ fontSize: 11, color: '#888' }}>
                            {item.subtitle}
                          </div>

                          <Space size="small" style={{ marginTop: 4 }}>
                            <Tag
                              color={item.type === 'fault' ? 'purple' : 'blue'}
                              style={{ fontSize: 10, margin: 0 }}
                              bordered={false}
                            >
                              {item.type === 'fault' ? 'Arıza' : 'Talep'}
                            </Tag>
                            {item.type === 'requestStockItem' && (
                              <Tag color="cyan" style={{ fontSize: 10, margin: 0 }} bordered={false}>
                                Parça
                              </Tag>
                            )}
                          </Space>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>

              {/* Taslak Form Seçeneği - Sadece Ankara teslimatları ve POS arızaları için */}
              {selectedPackage?.isHandDelivery && selectedItems.some(item => item.type === 'fault') && (
                <Card size="small" style={{ marginBottom: 16, border: '1px solid #52c41a', background: '#f6ffed' }}>
                  <Checkbox
                    checked={createDraftForms}
                    onChange={(e) => setCreateDraftForms(e.target.checked)}
                  >
                    <strong>Taslak Form oluşturulsun mu?</strong>
                    <div style={{ fontSize: 12, color: '#595959', marginTop: 4, fontWeight: 'normal' }}>
                      İşaretlerseniz seçili POS arızaları için otomatik taslak servis formu oluşturulacaktır.
                    </div>
                  </Checkbox>
                </Card>
              )}

              <div style={{ marginTop: 24, textAlign: 'right' }}>
                {/* Kargo/Teslimat Bilgileri Formu */}
                <Form form={shipForm} layout="vertical" onFinish={handleConfirmShipment}>

                  <Form.Item>
                    <Space>
                      <Button
                        type="primary"
                        htmlType="submit"
                        icon={<CheckCircleOutlined />}
                        disabled={!selectedPackage?.items?.length}
                        style={{
                          background: 'linear-gradient(135deg, #52c41a 0%, #389e0d 100%)',
                          border: 'none',
                        }}
                      >
                        {selectedPackage?.isHandDelivery
                          ? `${checkedItems.size || 0} Öğeyi Teslim Edildi İşaretle`
                          : `${checkedItems.size || 0} Öğeyi Gönderildi Olarak İşaretle`
                        }
                      </Button>
                      <Button onClick={() => setShipModalVisible(false)}>İptal</Button>
                    </Space>
                  </Form.Item>
                </Form>
              </div>
            </div>
          )
        )}
      </Modal>
    </div>
  )
}

export default Shipments
