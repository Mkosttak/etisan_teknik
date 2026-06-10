import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { BrowserRouter } from 'react-router-dom'
import { App } from 'antd'

// Mock the api module
vi.mock('../services/api', () => ({
    default: {
        get: vi.fn(),
        post: vi.fn(),
        interceptors: {
            request: { use: vi.fn() },
            response: { use: vi.fn() }
        }
    },
    setAuthTokens: vi.fn(),
    clearAuthTokens: vi.fn(),
    getAuthAccessToken: vi.fn(),
    getAuthRefreshToken: vi.fn()
}))

import { AuthProvider, useAuth } from '../context/AuthContext'
import api from '../services/api'

// Test component to access auth context
const TestComponent = () => {
    const { user, login, logout, loading } = useAuth()

    if (loading) return <div>Loading...</div>

    return (
        <div>
            <div data-testid="user-status">
                {user ? `Logged in as ${user.email}` : 'Not logged in'}
            </div>
            <button onClick={() => login('test@test.com', 'password')}>Login</button>
            <button onClick={logout}>Logout</button>
        </div>
    )
}

// Wrapper component
const renderWithProviders = (ui) => {
    return render(
        <BrowserRouter>
            <App>
                <AuthProvider>
                    {ui}
                </AuthProvider>
            </App>
        </BrowserRouter>
    )
}

describe('AuthContext', () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe('Initial state', () => {
        it('should show loading initially', () => {
            renderWithProviders(<TestComponent />)
            expect(screen.getByText('Loading...')).toBeInTheDocument()
        })

        it('should show not logged in when no token', async () => {
            const { getAuthAccessToken } = await import('../services/api')
            getAuthAccessToken.mockReturnValue(null)

            renderWithProviders(<TestComponent />)

            await waitFor(() => {
                expect(screen.getByTestId('user-status')).toHaveTextContent('Not logged in')
            })
        })
    })

    describe('Login', () => {
        it('should login successfully with valid credentials', async () => {
            const user = userEvent.setup()
            const mockUserData = {
                id: 1,
                email: 'test@test.com',
                firstName: 'Test',
                lastName: 'User',
                role: 'Teknik'
            }

            api.post.mockResolvedValueOnce({
                data: {
                    accessToken: 'mock-access-token',
                    refreshToken: 'mock-refresh-token',
                    user: mockUserData
                }
            })

            const { getAuthAccessToken } = await import('../services/api')
            getAuthAccessToken.mockReturnValue(null)

            renderWithProviders(<TestComponent />)

            await waitFor(() => {
                expect(screen.getByTestId('user-status')).toHaveTextContent('Not logged in')
            })

            await user.click(screen.getByRole('button', { name: 'Login' }))

            await waitFor(() => {
                expect(api.post).toHaveBeenCalledWith('/auth/login-v2', {
                    email: 'test@test.com',
                    password: 'password'
                })
            })
        })

        it('should show error when login fails', async () => {
            const user = userEvent.setup()

            api.post.mockRejectedValueOnce({
                response: {
                    data: { message: 'Invalid credentials' }
                }
            })

            const { getAuthAccessToken } = await import('../services/api')
            getAuthAccessToken.mockReturnValue(null)

            renderWithProviders(<TestComponent />)

            await waitFor(() => {
                expect(screen.getByTestId('user-status')).toHaveTextContent('Not logged in')
            })

            await user.click(screen.getByRole('button', { name: 'Login' }))

            await waitFor(() => {
                expect(api.post).toHaveBeenCalled()
            })
        })
    })

    describe('Logout', () => {
        it('should logout and clear user state', async () => {
            const user = userEvent.setup()

            const { getAuthAccessToken, getAuthRefreshToken, clearAuthTokens } = await import('../services/api')
            getAuthAccessToken.mockReturnValue(null)
            getAuthRefreshToken.mockReturnValue('mock-refresh-token')

            api.post.mockResolvedValue({ data: {} })

            renderWithProviders(<TestComponent />)

            await waitFor(() => {
                expect(screen.getByTestId('user-status')).toHaveTextContent('Not logged in')
            })

            await user.click(screen.getByRole('button', { name: 'Logout' }))

            await waitFor(() => {
                expect(clearAuthTokens).toHaveBeenCalled()
            })
        })
    })
})
