// Vitest test setup file
import '@testing-library/jest-dom'

// Mock localStorage
const localStorageMock = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
}
Object.defineProperty(window, 'localStorage', {
    value: localStorageMock
})

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation(query => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
    })),
})

// Suppress console errors from antd during tests
const originalConsoleError = console.error
console.error = (...args) => {
    if (
        typeof args[0] === 'string' &&
        (args[0].includes('Warning:') || args[0].includes('validateDOMNesting'))
    ) {
        return
    }
    originalConsoleError.apply(console, args)
}
