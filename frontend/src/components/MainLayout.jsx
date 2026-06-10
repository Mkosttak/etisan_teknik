/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: MainLayout.jsx
 * KONUM: frontend/src/components/
 * TİP: Frontend - React Layout Component
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulamanın ana layout component'i. 
 * Tüm private route'lar (giriş gerektiren sayfalar) bu layout içinde render edilir.
 * 
 * SORUMLULUKLAR:
 * - Sidebar (sol menü) yönetimi
 * - Header (üst bar) yönetimi
 * - Responsive tasarım (desktop/mobile)
 * - Kullanıcı dropdown menüsü
 * - Bildirim dropdown'u
 * - Tema değiştirme butonu
 * - Hoş geldin modalı
 * - Role-based menu filtering
 * 
 * BÖLÜMLER:
 * - Desktop: Fixed sidebar + Header + Content
 * - Mobile: Hamburger menü (Drawer) + Header + Content
 * 
 * BREAKPOINT: md (≥768px) desktop sidebar, <768px mobil hamburger menü
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { useState, useEffect } from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Button, Avatar, Dropdown, Drawer, Grid, Badge, List, Typography, Empty, Modal, Space, FloatButton, Popover } from 'antd'
import {
  DashboardOutlined,
  UserOutlined,
  LogoutOutlined,
  ToolOutlined,
  MenuOutlined,
  SunOutlined,
  MoonOutlined,
  AlertOutlined,
  TruckOutlined,
  InboxOutlined,
  FileTextOutlined,
  BellOutlined,
  CheckOutlined,
  TeamOutlined,
  FormOutlined,
  AppstoreOutlined,
  BankOutlined,
  LockOutlined,
  ArrowRightOutlined
} from '@ant-design/icons'
import { useAuth } from '../context/AuthContext'
import { ROLES } from '../constants/roles'
import { useTheme } from '../context/ThemeContext'
import { useNotifications } from '../context/NotificationContext'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/tr'

// Dayjs konfigürasyonu - Türkçe tarih formatı ve relative time desteği
dayjs.extend(relativeTime)
dayjs.locale('tr')

// Ant Design Layout bileşenleri
const { Header, Sider, Content } = Layout
const { useBreakpoint } = Grid

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * MAIN LAYOUT COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * STATE YÖNETİMİ:
 * - collapsed: Sidebar daraltılmış mı? (Desktop)
 * - mobileMenuOpen: Mobil menü açık mı? (Drawer)
 * - notificationOpen: Bildirim dropdown'u açık mı?
 * 
 * CONTEXT KULLANIMI:
 * - useAuth: Kullanıcı bilgisi, logout, welcome modal
 * - useTheme: Dark/light mode, toggleTheme
 * - useNotifications: Bildirim listesi, okundu işaretleme
 * 
 * SIDEBAR BOYUTLARI:
 * - İnce hali: 80px (collapsed)
 * - Geniş hali: 200px (expanded)
 * 
 * RESPONSIVE:
 * - md (≥768px): Desktop → Fixed sidebar (tablet dahil)
 * - <768px: Mobile → Drawer (sadece telefon)
 */
const MainLayout = () => {
  // State
  const [collapsed, setCollapsed] = useState(true) // Sidebar varsayılan olarak kapalı
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false) // Mobil menü açık/kapalı
  const [notificationOpen, setNotificationOpen] = useState(false) // Bildirim dropdown açık/kapalı

  // Hooks
  const navigate = useNavigate()
  const location = useLocation()
  const { user, logout, showWelcomeModal, setShowWelcomeModal } = useAuth()
  const { isDarkMode, toggleTheme } = useTheme()
  const { notifications, unreadCount, markAsRead, markAllAsRead, refresh } = useNotifications()
  const leadingNotifications = notifications.slice(0, 5) // Son 5 bildirim
  const screens = useBreakpoint() // Ant Design responsive breakpoints

  // Sabitler
  const sidebarWidth = 200 // Sidebar geniş hali
  const collapsedWidth = 80 // Sidebar dar hali

  /**
   * RESPONSIVE KONTROL
   * 
   * md breakpoint (≥768px) ve üstünde desktop sidebar, altında mobil hamburger menü.
   * Tablet cihazlar (768px+) desktop sidebar görecek.
   * Sadece telefon (<768px) hamburger menü kullanacak.
   */
  const isMobile = !screens.md

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * MOBİL MENÜ AÇIKKEN SCROLL ENGELLEME
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * AMAÇ:
   * Mobilde menü açıldığında body scroll'unu kapat, kapandığında aç.
   * 
   * Menü açıkken arka plan scroll olmasın diye document.body.overflow = 'hidden'
   */
  useEffect(() => {
    if (mobileMenuOpen) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = 'unset'
    }
    return () => {
      document.body.style.overflow = 'unset'
    }
  }, [mobileMenuOpen])

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * MENÜ ÖĞELERİ TANIMI - TÜM SAYFALAR
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * Uygulamadaki tüm sayfalar için menü öğeleri.
   * Her öğe:
   * - key: Route path (navigate ile kullanılacak)
   * - icon: Ant Design icon
   * - label: Menüde gösterilecek metin
   * 
   * NOT:
   * Bu liste role göre filtrelenecek (SahaElemani sadece bazı sayfalara erişebilir).
   */
  const allMenuItems = [
    {
      key: '/',
      icon: <DashboardOutlined style={{ fontSize: 18 }} />,
      label: 'Ana Sayfa',
    },
    {
      key: '/workflow',
      icon: <AppstoreOutlined style={{ fontSize: 18 }} />,
      label: 'İş Akışı',
    },
    {
      key: '/requests',
      icon: <FileTextOutlined style={{ fontSize: 18 }} />,
      label: 'Talepler',
    },
    {
      key: '/faults',
      icon: <AlertOutlined style={{ fontSize: 18 }} />,
      label: 'Arızalar',
    },
    {
      key: '/service-forms',
      icon: <FormOutlined style={{ fontSize: 18 }} />,
      label: 'Formlar',
    },
    {
      key: '/shipments',
      icon: <TruckOutlined style={{ fontSize: 18 }} />,
      label: 'Kargolar',
    },
    {
      key: '/stock',
      icon: <InboxOutlined style={{ fontSize: 18 }} />,
      label: 'Stok',
    },
    {
      key: '/institutions',
      icon: <BankOutlined style={{ fontSize: 18 }} />,
      label: 'Üniversiteler',
    },
    {
      key: '/users',
      icon: <TeamOutlined style={{ fontSize: 18 }} />,
      label: 'Kullanıcılar',
    },
    {
      key: '/institution-management',
      icon: <ToolOutlined style={{ fontSize: 18 }} />,
      label: 'Kurum Yönetimi',
    }
  ]

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * ROLE-BASED MENÜ FİLTRELEME
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * AMAÇ:
   * Kullanıcının rolüne göre menü öğelerini filtrele.
   * 
   * YETKİLENDİRME KURALLARI:
   * 
   * 1. SahaElemani (Saha personeli):
   *    - Sadece: /requests, /faults, /service-forms
   * 
   * 2. Admin:
   *    - Tüm sayfalara erişim
   * 
   * 3. Teknik, Proje, Kullanici:
   *    - Users ve InstitutionManagement hariç tüm sayfalara erişim
   * 
   * İŞLEYİŞ:
   * - allMenuItems array'ini filtrele
   * - Her öğe için kullanıcının rolüne göre erişim kontrolü yap
   * - İzin yoksa menüden çıkar
   */
  const menuItems = allMenuItems.filter(item => {
    if (!user?.role) return false

    // SahaElemani için kısıtlı erişim
    if (user.role === ROLES.SAHA_ELEMANI) {
      const allowedForSahaElemani = ['/requests', '/faults', '/service-forms']
      return allowedForSahaElemani.includes(item.key)
    }

    // Kullanıcılar ve Kurum Yönetimi sadece Admin'e görünsün
    if (item.key === '/users' || item.key === '/institution-management') {
      return user.role === ROLES.ADMIN
    }

    // Diğer tüm sayfalar (Üniversiteler ve İş Akışı dahil) SahaElemani hariç tüm rollere açık
    return true
  })

  /**
   * MENÜ TIKLAMA HANDLERı
   * 
   * Menü öğesine tıklandığında:
   * 1. key (route path) ile navigate et
   * 2. Mobilde menü açıksa kapat
   * 
   * @param {Object} param - MenuItem click event object
   * @param {string} param.key - Tıklanan menü öğesinin key'i (route path)
   */
  const handleMenuClick = ({ key }) => {
    navigate(key)
    if (isMobile) {
      setMobileMenuOpen(false)
    }
  }

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * KULLANICI DROPDOWN MENÜ ÖĞELERİ
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * Header'daki kullanıcı avatar'ına tıklandığında açılan dropdown içeriği:
   * 1. Tema değiştirme (Dark ↔ Light)
   * 2. Şifre değiştirme
   * 3. Divider (ayırıcı çizgi)
   * 4. Çıkış yap (danger style)
   */
  const userMenuItems = [

    {
      key: 'change-password',
      icon: <LockOutlined />,
      label: 'Sifre Degistir',
      onClick: () => navigate('/change-password')
    },
    {
      type: 'divider'
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Çıkış Yap',
      onClick: logout,
      danger: true
    }
  ]

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * SIDEBAR İÇERİĞİ COMPONENT
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * AMAÇ:
   * Sidebar (sol menü) içeriğini render eder.
   * Hem desktop Sider hem mobile Drawer içinde kullanılır.
   * 
   * İÇERİK:
   * 1. Logo (collapsed durumuna göre küçük/büyük)
   * 2. Menu component (Ant Design)
   * 
   * LOGO DAVRANIŞI:
   * - Collapsed && Desktop: Küçük logo (40x40)
   * - Diğer durumlar: Büyük logo (otomatik genişlik)
   */
  const SidebarContent = () => (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* ─────────────────────────────────────────────────────────── */}
      {/* LOGO ALANI                                                  */}
      {/* ─────────────────────────────────────────────────────────── */}
      <div style={{
        padding: collapsed && !isMobile ? '16px 8px' : '20px 12px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0 // Logo küçülmesin
      }}>
        {collapsed && !isMobile ? (
          <img
            src="/etisan-proje-logo-kucuk.png"
            alt="Etisan Proje"
            style={{
              height: 40,
              width: 40,
              objectFit: 'contain',
              borderRadius: 8,
            }}
          />
        ) : (
          <img
            src="/etisan-logo.png"
            alt="Etisan Proje"
            style={{
              height: 42,
              width: 'auto',
              objectFit: 'contain',
              background: 'white',
              padding: '8px 14px',
              borderRadius: 10,
              boxShadow: '0 2px 8px rgba(0,0,0,0.1)'
            }}
          />
        )}
      </div>

      {/* ─────────────────────────────────────────────────────────── */}
      {/* MENÜ ALANI (SCROLLABLE)                                     */}
      {/* ─────────────────────────────────────────────────────────── */}
      <div style={{ flex: 1, overflowY: 'auto', overflowX: 'hidden' }}>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname === '/' ? '/' : location.pathname]}
          items={menuItems}
          onClick={handleMenuClick}
          style={{
            border: 'none',
            background: 'transparent',
          }}
        />
      </div>

      {/* ─────────────────────────────────────────────────────────── */}
      {/* SIDEBAR FOOTER (PROFİL & AYARLAR)                           */}
      {/* ─────────────────────────────────────────────────────────── */}
      {!isMobile && (
        <div style={{
          padding: collapsed ? '12px 8px' : '16px',
          background: 'rgba(0, 0, 0, 0.15)',
          borderTop: '1px solid rgba(255, 255, 255, 0.05)',
          display: 'flex',
          flexDirection: collapsed ? 'column' : 'row',
          alignItems: 'center',
          gap: 12,
          justifyContent: 'space-between',
          flexShrink: 0
        }}>
          {/* Profil (Premium Kart) */}
          <Dropdown menu={{ items: userMenuItems }} placement={collapsed ? "rightBottom" : "topRight"} trigger={['click']}>
            {collapsed ? (
              <Avatar icon={<UserOutlined />} size={32} style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', cursor: 'pointer' }} />
            ) : (
              <div style={{
                display: 'flex',
                alignItems: 'center',
                gap: 12,
                cursor: 'pointer',
                padding: '10px 14px',
                borderRadius: 12,
                background: isDarkMode ? '#1f1f1f' : 'rgba(255,255,255,0.05)',
                border: isDarkMode ? '1px solid #333' : '1px solid rgba(0,0,0,0.05)',
                flex: 1,
                minWidth: 0,
                transition: 'all 0.3s'
              }}
                onMouseEnter={(e) => { if (!isMobile) e.currentTarget.style.borderColor = '#667eea' }}
                onMouseLeave={(e) => { if (!isMobile) e.currentTarget.style.borderColor = '#333' }}
              >
                <Avatar icon={<UserOutlined />} size={36} style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', flexShrink: 0 }} />
                <div style={{ display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
                  <span style={{ fontSize: 13, fontWeight: 600, color: '#fff', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {user?.firstName}
                  </span>
                  <span style={{ fontSize: 10, color: 'rgba(255,255,255,0.5)', lineHeight: 1, textTransform: 'uppercase' }}>
                    {user?.role}
                  </span>
                </div>
              </div>
            )}
          </Dropdown>

          {/* Tema Değiştirme (Sidebar Footer) */}
          {(!collapsed || isMobile) && (
            <Button
              type="text"
              icon={isDarkMode ? <SunOutlined style={{ color: '#faad14', fontSize: 18 }} /> : <MoonOutlined style={{ color: 'rgba(255,255,255,0.65)', fontSize: 18 }} />}
              onClick={toggleTheme}
              style={{
                width: 36,
                height: 36,
                borderRadius: 10,
                background: isDarkMode ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.1)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                border: isDarkMode ? '1px solid rgba(255,255,255,0.1)' : '1px solid rgba(0,0,0,0.05)'
              }}
            />
          )}
        </div>
      )}
    </div>
  )

  /**
   * ═══════════════════════════════════════════════════════════════════════
   * LAYOUT RENDER
   * ═══════════════════════════════════════════════════════════════════════
   * 
   * YAPISI:
   * 
   * Layout (minHeight: 100vh)
   * ├── Desktop Sidebar (isMobile=false) → Fixed Sider
   * ├── Mobile Drawer (isMobile=true) → Hamburger menü
   * └── Layout (İçerik alanı)
   *     ├── Header (Sticky)
   *     │   ├── Sol: Hamburger (mobil) + Başlık
   *     │   └── Sağ: Bildirim + Tema + Kullanıcı dropdown
   *     └── Content → <Outlet /> (React Router child routes burada render edilir)
   * 
   * STICKY HEADER:
   * position: sticky, top: 0 → Scroll'da üstte sabitlenir
   * 
   * OUTLET:
   * React Router'dan gelen child route'lar burada render edilir.
   * Örnek: /requests route'u → Requests component buraya gelir
   */
  return (
    <Layout style={{ minHeight: '100vh' }}>
      {/* ═══════════════════════════════════════════════════════════════ */}
      {/* DESKTOP SIDEBAR (FIXED SIDER)                                   */}
      {/* ═══════════════════════════════════════════════════════════════ */}
      {!isMobile && (
        <>
          <Sider
            trigger={null} // Otomatik daraltma butonunu gizle
            collapsed={collapsed}
            width={sidebarWidth} // Geniş halde 200px
            collapsedWidth={collapsedWidth} // Dar halde 80px
            style={{
              background: isDarkMode ? '#1a1a1a' : 'linear-gradient(180deg, #d84315 0%, #bf360c 100%)',
              boxShadow: '4px 0 24px rgba(0,0,0,0.1)',
              position: 'fixed', // Sol tarafa sabitle
              left: 0,
              top: 0,
              bottom: 0,
              height: '100vh',
              overflow: 'hidden', // İçerik kendi içinde scroll olacak
              zIndex: 200 // Header'ın üstünde
            }}
          >
            <SidebarContent />
          </Sider>

          {/* Toggle Button - Sidebar dışında, overflow sorunu olmasın */}
          <Button
            type="default"
            icon={<ArrowRightOutlined style={{
              fontSize: 14,
              color: '#fff',
              transition: 'transform 0.3s',
              transform: collapsed ? 'rotate(0deg)' : 'rotate(180deg)'
            }} />}
            onClick={() => setCollapsed(!collapsed)}
            style={{
              position: 'fixed',
              left: collapsed ? collapsedWidth - 16 : sidebarWidth - 16,
              top: '50%',
              transform: 'translateY(-50%)',
              width: 32,
              height: 32,
              padding: 0,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: 'rgba(96, 125, 139, 0.7)',
              border: 'none',
              borderRadius: '50%',
              boxShadow: '0 2px 6px rgba(0, 0, 0, 0.15)',
              zIndex: 201,
              cursor: 'pointer',
              transition: 'all 0.3s'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.background = 'rgba(96, 125, 139, 0.9)'
              e.currentTarget.style.boxShadow = '0 3px 8px rgba(0, 0, 0, 0.2)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.background = 'rgba(96, 125, 139, 0.7)'
              e.currentTarget.style.boxShadow = '0 2px 6px rgba(0, 0, 0, 0.15)'
            }}
          />
        </>
      )}

      {/* ═══════════════════════════════════════════════════════════════ */}
      {/* MOBİL DRAWER (HAMBURGER MENÜ)                                   */}
      {/* ═══════════════════════════════════════════════════════════════ */}
      <Drawer
        placement="left"
        open={mobileMenuOpen}
        onClose={() => setMobileMenuOpen(false)}
        width={280}
        className={isDarkMode ? 'dark-drawer' : 'light-drawer'}
        styles={{
          body: {
            padding: 0,
            background: 'transparent'
          },
          header: { display: 'none' }, // Drawer başlığını gizle
          content: {
            background: 'var(--bg-sidebar)'
          },
          wrapper: {
            background: 'var(--bg-sidebar)'
          }
        }}
      >
        <SidebarContent />
      </Drawer>

      {/* ═══════════════════════════════════════════════════════════════ */}
      {/* İÇERİK ALANI (HEADER + CONTENT)                                 */}
      {/* ═══════════════════════════════════════════════════════════════ */}
      <Layout style={{
        marginLeft: isMobile ? 0 : (collapsed ? collapsedWidth : sidebarWidth),
        minHeight: '100vh',
        background: 'var(--bg-content)',
        transition: 'margin-left 0.3s'
      }}>
        {/* CONTENT (SAYFA İÇERİĞİ) */}
        <Content style={{
          padding: isMobile ? '64px 16px 24px 16px' : '32px',
          minHeight: 280,
        }}>
          <Outlet />
        </Content>
      </Layout>

      {/* ═══════════════════════════════════════════════════════════════ */}
      {/* FLOATING ELEMENTS                                               */}
      {/* ═══════════════════════════════════════════════════════════════ */}

      {/* MOBILE HEADER */}
      {isMobile && (
        <Header style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          height: 64,
          padding: '0 16px',
          background: isDarkMode ? 'rgba(26, 26, 26, 0.85)' : 'rgba(255, 255, 255, 0.85)',
          backdropFilter: 'blur(10px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          borderBottom: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
          zIndex: 1000,
          boxShadow: '0 2px 8px rgba(0,0,0,0.05)'
        }}>
          {/* Left: Hamburger */}
          <Button
            type="text"
            icon={<MenuOutlined style={{ fontSize: 20 }} />}
            onClick={() => setMobileMenuOpen(true)}
            style={{
              width: 40,
              height: 40,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              borderRadius: 8
            }}
          />

          {/* Center: Logo */}
          <div style={{
            position: 'absolute',
            left: '50%',
            transform: 'translateX(-50%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}>
            <img
              src="/etisan-proje-logo-kucuk.png"
              alt="Etisan Proje"
              style={{
                height: 36,
                width: 36,
                objectFit: 'contain'
              }}
            />
          </div>

          {/* Right: Notification, Theme, Profile */}
          <Space size={8}>
            {/* 1. Notification */}
            <Popover
              content={
                <div style={{ width: 280 }}>
                  <div style={{
                    padding: '12px 16px',
                    borderBottom: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}>
                    <Typography.Text strong>Bildirimler</Typography.Text>
                    <Button type="link" size="small" onClick={() => markAllAsRead()}>Tümünü Oku</Button>
                  </div>
                  <List
                    dataSource={notifications.slice(0, 5)}
                    renderItem={(item) => (
                      <List.Item
                        style={{
                          padding: '12px 16px',
                          cursor: 'pointer',
                          background: item.isRead ? 'transparent' : (isDarkMode ? 'rgba(216, 67, 21, 0.1)' : 'rgba(216, 67, 21, 0.05)')
                        }}
                        onClick={() => { if (!item.isRead) markAsRead(item.id); if (item.link) navigate(item.link); }}
                      >
                        <List.Item.Meta
                          title={<div style={{ fontSize: 13, fontWeight: item.isRead ? 400 : 600 }}>{item.title}</div>}
                          description={<div style={{ fontSize: 11 }}>{dayjs(item.createdAt).fromNow()}</div>}
                        />
                      </List.Item>
                    )}
                    locale={{ emptyText: <div style={{ padding: 20, textAlign: 'center' }}>Yeni bildirim yok</div> }}
                  />
                </div>
              }
              trigger="click"
              placement="bottomRight"
              arrow={true}
              styles={{ body: { padding: 0, borderRadius: 12, overflow: 'hidden' } }}
            >
              <Badge count={unreadCount} size="small" offset={[-2, 6]}>
                <Button
                  type="text"
                  icon={<BellOutlined style={{ fontSize: 20 }} />}
                  style={{ width: 40, height: 40, borderRadius: 8 }}
                />
              </Badge>
            </Popover>

            {/* 2. Theme Toggle */}
            <Button
              type="text"
              icon={isDarkMode ? <SunOutlined style={{ color: '#faad14', fontSize: 20 }} /> : <MoonOutlined style={{ color: '#1890ff', fontSize: 20 }} />}
              onClick={toggleTheme}
              style={{ width: 40, height: 40, borderRadius: 8 }}
            />

            {/* 3. Profile */}
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" trigger={['click']}>
              <Avatar
                icon={<UserOutlined />}
                size={32}
                style={{
                  background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                  cursor: 'pointer',
                  boxShadow: '0 2px 6px rgba(118, 75, 162, 0.3)'
                }}
              />
            </Dropdown>
          </Space>
        </Header>
      )}

      {/* ───────────────────────────────────────────────────────────── */}
      {/* DESKTOP NOTIFICATIONS (BOTTOM RIGHT)                         */}
      {/* ───────────────────────────────────────────────────────────── */}
      {!isMobile && unreadCount > 0 && (
        <Popover
          content={
            <div style={{ width: 300 }}>
              <div style={{
                padding: '12px 16px',
                borderBottom: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center'
              }}>
                <Typography.Text strong>Bildirimler</Typography.Text>
                <Button type="link" size="small" onClick={() => markAllAsRead()}>Tümünü Oku</Button>
              </div>
              <List
                dataSource={notifications.slice(0, 5)}
                renderItem={(item) => (
                  <List.Item
                    style={{
                      padding: '12px 16px',
                      cursor: 'pointer',
                      background: item.isRead ? 'transparent' : (isDarkMode ? 'rgba(216, 67, 21, 0.1)' : 'rgba(216, 67, 21, 0.05)')
                    }}
                    onClick={() => { if (!item.isRead) markAsRead(item.id); if (item.link) navigate(item.link); }}
                  >
                    <List.Item.Meta
                      title={<div style={{ fontSize: 13, fontWeight: item.isRead ? 400 : 600 }}>{item.title}</div>}
                      description={<div style={{ fontSize: 11 }}>{dayjs(item.createdAt).fromNow()}</div>}
                    />
                  </List.Item>
                )}
                locale={{ emptyText: <div style={{ padding: 20, textAlign: 'center' }}>Yeni bildirim yok</div> }}
              />
            </div>
          }
          trigger="click"
          placement="topLeft"
          arrow={true}
          styles={{ body: { padding: 0, borderRadius: 12, overflow: 'hidden' } }}
        >
          <FloatButton
            badge={{ count: unreadCount, color: '#f5222d' }}
            icon={<BellOutlined />}
            type="primary"
            style={{
              right: 24,
              bottom: 24,
              width: 52,
              height: 52,
              boxShadow: '0 8px 24px rgba(216, 67, 21, 0.3)'
            }}
          />
        </Popover>
      )}

      {/* CONTENT (SAYFA İÇERİĞİ) */}
      <Modal
        open={showWelcomeModal}
        onCancel={() => setShowWelcomeModal(false)}
        footer={[
          <Button
            key="close"
            onClick={() => setShowWelcomeModal(false)}
            style={{
              borderRadius: 8,
              padding: '8px 24px',
              height: 'auto'
            }}
          >
            Kapat
          </Button>
        ]}
        width={isMobile ? '90%' : 700}
        centered
        closable={true}
        maskClosable={true}
        style={{
          top: 20
        }}
      >
        <div style={{
          padding: isMobile ? '16px' : '24px',
          textAlign: 'center'
        }}>
          <div style={{
            fontSize: isMobile ? 24 : 32,
            fontWeight: 700,
            marginBottom: 24,
            background: 'linear-gradient(135deg, #ff6f00 0%, #d84315 100%)',
            WebkitBackgroundClip: 'text',
            WebkitTextFillColor: 'transparent',
            backgroundClip: 'text',
          }}>
            Hoş geldiniz
          </div>

          <div style={{
            textAlign: 'left',
            fontSize: isMobile ? 14 : 16,
            lineHeight: 1.8,
            color: isDarkMode ? 'var(--text-secondary)' : '#333',
            marginBottom: 24
          }}>
            <p style={{ marginBottom: 16 }}>
              Teknik ekipte çalışırken sahada gözlemlediğim ihtiyaçlardan yola çıkarak, süreçleri hızlandırmak ve daha düzenli yönetilebilir hale getirmek için yapay zekâ destekli olarak geliştirdiğim <strong>Teknik Servis Yönetim Panelini</strong> test etmenizi rica ederim.
            </p>

            <p style={{ marginBottom: 16 }}>
              Bu sistem; <strong>Talep Yönetimi</strong>, <strong>Stok Takibi</strong>, <strong>Arıza/Tamir Süreçleri</strong>, <strong>Kargo işlemleri</strong> ve <strong>Teknik Servis Talep Formu</strong> gibi modülleri tek bir panelde toplayarak uçtan uca yönetim sağlamayı amaçlar.
            </p>

            <p style={{ marginBottom: 16 }}>
              <strong>Altyapı:</strong> .NET 8 (C#) + MySQL + EF Core<br />
              <strong>Arayüz:</strong> React
            </p>

            <p style={{
              marginTop: 24,
              fontStyle: 'italic',
              color: isDarkMode ? 'var(--text-secondary)' : '#666',
              fontSize: isMobile ? 13 : 15
            }}>
              Görüş ve önerileriniz, sistemi geliştirmem için çok değerli. Şimdiden teşekkür ederim.
            </p>

            <div style={{
              marginTop: 24,
              textAlign: 'right',
              fontWeight: 600,
              color: isDarkMode ? 'var(--text-primary)' : '#333',
              fontSize: isMobile ? 14 : 16
            }}>
              Murat KOSTAK
            </div>
          </div>
        </div>
      </Modal>
    </Layout>
  )
}

export default MainLayout
