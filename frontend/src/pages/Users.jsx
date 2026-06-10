import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Table, Button, Input, App, Space, Popconfirm, Tag, Tooltip } from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, UserOutlined, SearchOutlined } from '@ant-design/icons'
import { ROLES } from '../constants/roles'
import api from '../services/api'
import { useTheme } from '../context/ThemeContext'
import { getErrorMessage } from '../utils/errorHelper'

const Users = () => {
    const navigate = useNavigate()
    const { isDarkMode } = useTheme()
    const { message } = App.useApp()
    const [users, setUsers] = useState([])
    const [filteredUsers, setFilteredUsers] = useState([])
    const [searchText, setSearchText] = useState('')
    const [loading, setLoading] = useState(false)

    useEffect(() => {
        fetchUsers()
    }, [])

    const fetchUsers = async () => {
        try {
            setLoading(true)
            const response = await api.get('/users')
            setUsers(response.data)
            setFilteredUsers(response.data)
        } catch (error) {
            message.error(getErrorMessage(error))
        } finally {
            setLoading(false)
        }
    }

    const handleSearch = (value) => {
        setSearchText(value)
        if (!value) {
            setFilteredUsers(users)
            return
        }
        const searchLower = value.toLocaleLowerCase('tr-TR')
        const filtered = users.filter(user =>
            user.firstName.toLocaleLowerCase('tr-TR').includes(searchLower) ||
            user.lastName.toLocaleLowerCase('tr-TR').includes(searchLower) ||
            user.email.toLocaleLowerCase('tr-TR').includes(searchLower)
        )
        setFilteredUsers(filtered)
    }

    const handleDelete = async (id) => {
        try {
            await api.delete(`/users/${id}`)
            message.success('Kullanıcı silindi')
            fetchUsers()
        } catch (error) {
            message.error(getErrorMessage(error))
        }
    }

    const getRoleColor = (role) => {
        const colorMap = {
            [ROLES.ADMIN]: 'red',
            [ROLES.TEKNIK]: 'blue',
            [ROLES.PROJE]: 'purple',
            [ROLES.KULLANICI]: 'green',
            [ROLES.SAHA_ELEMANI]: 'orange'
        }
        return colorMap[role] || 'default'
    }

    const columns = [
        {
            title: 'Ad Soyad',
            key: 'fullName',
            render: (_, record) => (
                <Space>
                    <UserOutlined />
                    <span style={{ fontWeight: 500 }}>{`${record.firstName} ${record.lastName}`}</span>
                </Space>
            )
        },
        {
            title: 'Email',
            dataIndex: 'email',
            key: 'email'
        },
        {
            title: 'Telefon',
            dataIndex: 'phoneNumber',
            key: 'phoneNumber',
            render: (phone) => phone || '-'
        },
        {
            title: 'Rol',
            dataIndex: 'role',
            key: 'role',
            render: (role) => <Tag color={getRoleColor(role)}>{role}</Tag>
        },
        {
            title: 'Durum',
            dataIndex: 'isActive',
            key: 'isActive',
            render: (isActive) => (
                <Tag color={isActive ? 'success' : 'default'}>
                    {isActive ? 'Aktif' : 'Pasif'}
                </Tag>
            )
        },
        {
            title: 'Oluşturma Tarihi',
            dataIndex: 'createdAt',
            key: 'createdAt',
            render: (date) => new Date(date).toLocaleDateString('tr-TR')
        },
        {
            title: 'İşlemler',
            key: 'actions',
            render: (_, record) => (
                <Space>
                    <Tooltip title="Düzenle">
                        <Button
                            type="link"
                            icon={<EditOutlined />}
                            onClick={() => navigate(`/users/${record.id}/edit`)}
                            style={{ color: '#52c41a' }}
                        />
                    </Tooltip>
                    <Popconfirm
                        title="Kullanıcıyı silmek istediğinizden emin misiniz?"
                        onConfirm={() => handleDelete(record.id)}
                        okText="Evet"
                        cancelText="Hayır"
                    >
                        <Tooltip title="Sil">
                            <Button
                                type="link"
                                danger
                                icon={<DeleteOutlined />}
                            />
                        </Tooltip>
                    </Popconfirm>
                </Space>
            )
        }
    ]

    return (
        <div>
            {/* Refined Header & Search Area */}
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
                        background: isDarkMode ? '#177ddc' : '#1890ff',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: '#fff',
                        fontSize: 20,
                        boxShadow: '0 4px 12px rgba(24, 144, 255, 0.3)'
                    }}>
                        <UserOutlined />
                    </div>
                    <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: isDarkMode ? '#fff' : '#262626' }}>
                        Kullanıcı Yönetimi
                    </h1>
                </div>

                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 16,
                    flex: 1,
                    justifyContent: 'flex-end',
                    minWidth: 300
                }}>
                    <Input
                        placeholder="Ad, soyad veya email ile ara..."
                        prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
                        value={searchText}
                        onChange={(e) => handleSearch(e.target.value)}
                        allowClear
                        style={{
                            maxWidth: 400,
                            height: 42,
                            borderRadius: 12,
                            border: isDarkMode ? '1px solid #333' : '1px solid #f0f0f0',
                            boxShadow: '0 4px 12px rgba(0,0,0,0.03)',
                            background: isDarkMode ? '#1f1f1f' : '#fff'
                        }}
                    />
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => navigate('/users/new')}
                        style={{
                            height: 42,
                            borderRadius: 12,
                            padding: '0 24px',
                            fontWeight: 600,
                            boxShadow: '0 4px 12px rgba(24, 144, 255, 0.2)'
                        }}
                    >
                        Yeni Kullanıcı
                    </Button>
                </div>
            </div>

            <Table
                columns={columns}
                dataSource={filteredUsers}
                rowKey="id"
                loading={loading}
                pagination={{ pageSize: 25, pageSizeOptions: ['25', '50', '100'], showSizeChanger: true }}
                scroll={{ x: true }}
            />
        </div>
    )
}

export default Users
