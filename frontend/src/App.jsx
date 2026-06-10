/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: App.jsx
 * KONUM: frontend/src/  
 * TİP: Frontend - Root Application Component
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * Uygulamanın ana container component'i.
 * Tüm provider'ları (context, routing, UI library) yapılandırır.
 * 
 * SORUMLULUKLAR:
 * - Provider hierarchy (QueryClient, Ant Design, Router, Auth, Theme, Notification)
 * - Route tanımlamaları (public/private)
 * - Role-based access control (RBAC)
 * - Protected route wrapper'ları
 * 
 * PROVIDER HIERARCHY (Dıştan içe):
 * QueryClientProvider → ConfigProvider → AntdApp → ThemeProvider → 
 * Router → AuthProvider → NotificationProvider → AppRoutes
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { lazy, Suspense } from 'react'
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { ConfigProvider, App as AntdApp } from 'antd'
import trTR from 'antd/locale/tr_TR'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './lib/queryClient'
import { ROLES } from './constants/roles'
import 'dayjs/locale/tr'
import { AuthProvider, useAuth } from './context/AuthContext'
import { ThemeProvider } from './context/ThemeContext'
import { NotificationProvider } from './context/NotificationContext'
import MainLayout from './components/MainLayout'
import ScrollToTop from './components/ScrollToTop'

// Pages - Lazy loaded for better performance
const Login = lazy(() => import('./pages/Login'))
const Dashboard = lazy(() => import('./pages/Dashboard'))
const Requests = lazy(() => import('./pages/Requests'))
const RequestDetail = lazy(() => import('./pages/RequestDetail'))
const Faults = lazy(() => import('./pages/Faults'))
const FaultDetail = lazy(() => import('./pages/FaultDetail'))
const Shipments = lazy(() => import('./pages/Shipments'))
const Stock = lazy(() => import('./pages/Stock'))
const Users = lazy(() => import('./pages/Users'))
const ServiceForms = lazy(() => import('./pages/ServiceForms'))
const ServiceFormWizard = lazy(() => import('./pages/ServiceFormWizard'))
const ServiceFormDetail = lazy(() => import('./pages/ServiceFormDetail'))
const WorkFlow = lazy(() => import('./pages/WorkFlow'))
const Institutions = lazy(() => import('./pages/Institutions'))
const InstitutionManagement = lazy(() => import('./pages/InstitutionManagement'))
const ChangePassword = lazy(() => import('./pages/ChangePassword'))
const RequestForm = lazy(() => import('./pages/RequestForm'))
const FaultForm = lazy(() => import('./pages/FaultForm'))
const UserForm = lazy(() => import('./pages/UserForm'))
const StockForm = lazy(() => import('./pages/StockForm'))
const WorkItemForm = lazy(() => import('./pages/WorkItemForm'))

import LoadingSpinner, { SpinnerContent } from './components/LoadingSpinner'

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * PRIVATE ROUTE WRAPPER
 * ═══════════════════════════════════════════════════════════════════════════
 */
const PrivateRoute = ({ children }) => {
    const { user, loading } = useAuth()

    if (loading) {
        return <LoadingSpinner />
    }

    return user ? children : <Navigate to="/login" />
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * ROLE-BASED PROTECTED ROUTE WRAPPER
 * ═══════════════════════════════════════════════════════════════════════════
 */
const RoleProtectedRoute = ({ children, allowedRoles = [] }) => {
    const { user, loading } = useAuth()

    if (loading) {
        return <LoadingSpinner />
    }

    if (!user) {
        return <Navigate to="/login" />
    }

    if (allowedRoles.length > 0 && !allowedRoles.includes(user.role)) {
        if (user.role === 'SahaElemani') {
            return <Navigate to="/requests" />
        }
        return <Navigate to="/" />
    }

    return children
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * APPLICATION ROUTES
 * ═══════════════════════════════════════════════════════════════════════════
 */
function AppRoutes() {
    const { user } = useAuth()

    return (
        <Suspense fallback={<LoadingSpinner />}>
            <Routes>
                {/* PUBLIC ROUTE - Login sayfası */}
                <Route path="/login" element={user ? <Navigate to="/" /> : <Login />} />

                {/* PRIVATE ROUTES - MainLayout içinde */}
                <Route path="/" element={<PrivateRoute><MainLayout /></PrivateRoute>}>
                    {/* Dashboard - Ana sayfa */}
                    <Route index element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><Dashboard /></RoleProtectedRoute>} />

                    {/* Talepler */}
                    <Route path="requests" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><Requests /></RoleProtectedRoute>} />
                    <Route path="requests/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><RequestForm /></RoleProtectedRoute>} />
                    <Route path="requests/:id/edit" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><RequestForm /></RoleProtectedRoute>} />
                    <Route path="requests/:id" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><RequestDetail /></RoleProtectedRoute>} />

                    {/* Arızalar */}
                    <Route path="faults" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><Faults /></RoleProtectedRoute>} />
                    <Route path="faults/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><FaultForm /></RoleProtectedRoute>} />
                    <Route path="faults/:id/edit" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><FaultForm /></RoleProtectedRoute>} />
                    <Route path="faults/:id" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><FaultDetail /></RoleProtectedRoute>} />

                    {/* Sevkiyat */}
                    <Route path="shipments" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><Shipments /></RoleProtectedRoute>} />

                    {/* Stok */}
                    <Route path="stock" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><Stock /></RoleProtectedRoute>} />
                    <Route path="stock/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE]}><StockForm /></RoleProtectedRoute>} />
                    <Route path="stock/:id/edit" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE]}><StockForm /></RoleProtectedRoute>} />

                    {/* Kullanıcılar */}
                    <Route path="users" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN]}><Users /></RoleProtectedRoute>} />
                    <Route path="users/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN]}><UserForm /></RoleProtectedRoute>} />
                    <Route path="users/:id/edit" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN]}><UserForm /></RoleProtectedRoute>} />

                    {/* Institution Management */}
                    <Route path="institution-management" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN]}><InstitutionManagement /></RoleProtectedRoute>} />

                    {/* Servis Formları */}
                    <Route path="service-forms" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><ServiceForms /></RoleProtectedRoute>} />
                    <Route path="service-forms/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><ServiceFormWizard /></RoleProtectedRoute>} />
                    <Route path="service-forms/:id" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><ServiceFormWizard /></RoleProtectedRoute>} />
                    <Route path="service-forms/:id/view" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><ServiceFormDetail /></RoleProtectedRoute>} />

                    {/* Kurumlar */}
                    <Route path="institutions" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><Institutions /></RoleProtectedRoute>} />

                    {/* İş Akışı */}
                    <Route path="workflow" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><WorkFlow /></RoleProtectedRoute>} />
                    <Route path="workflow/new" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><WorkItemForm /></RoleProtectedRoute>} />
                    <Route path="workflow/:id/edit" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI]}><WorkItemForm /></RoleProtectedRoute>} />

                    {/* Şifre Değiştir */}
                    <Route path="change-password" element={<RoleProtectedRoute allowedRoles={[ROLES.ADMIN, ROLES.TEKNIK, ROLES.PROJE, ROLES.KULLANICI, ROLES.SAHA_ELEMANI]}><ChangePassword /></RoleProtectedRoute>} />
                </Route>
            </Routes>
        </Suspense>
    )
}

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * MAIN APP COMPONENT
 * ═══════════════════════════════════════════════════════════════════════════
 */
function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <ConfigProvider
                locale={trTR}
                spin={{
                    indicator: <SpinnerContent size="default" style={{ position: 'absolute', left: '50%', top: '50%', transform: 'translate(-50%, -50%)' }} />
                }}
                theme={{
                    token: {
                        colorPrimary: '#1890ff', // Blue
                        colorLink: '#1890ff',
                        borderRadius: 8,
                        fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
                    },
                    components: {
                        Button: {
                            borderRadius: 6,
                            controlHeight: 40,
                            algorithm: true,
                        },
                        Card: {
                            borderRadiusLG: 12,
                        },
                        Input: {
                            controlHeight: 40,
                            borderRadius: 6,
                        },
                        Select: {
                            controlHeight: 40,
                            borderRadius: 6,
                        },
                        Layout: {
                            headerBg: '#ffffff',
                            siderBg: '#141414',
                        },
                        Spin: {
                            contentHeight: 400
                        }
                    }
                }}
            >
                <AntdApp>
                    <ThemeProvider>
                        <Router future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
                            <ScrollToTop />
                            <AuthProvider>
                                <NotificationProvider>
                                    <AppRoutes />
                                </NotificationProvider>
                            </AuthProvider>
                        </Router>
                    </ThemeProvider>
                </AntdApp>
            </ConfigProvider>
        </QueryClientProvider>
    )
}

export default App
