/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: queryClient.js
 * KONUM: frontend/src/lib/
 * TİP: Frontend - React Query Configuration
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ:
 * TanStack React Query client'ını yapılandırır.
 * 
 * SORUMLULUKLAR:
 * - API state management
 * - Cache yönetimi
 * - Otomatik retry mantığı
 * - Stale time yapılandırması
 * 
 * KULLANIM:
 * import { queryClient } from '@/lib/queryClient'
 * // App.jsx'te QueryClientProvider ile sarmalanır
 * 
 * KONFİGÜRASYON:
 * - staleTime: 5 dakika (data ne kadar süre fresh kabul edilir)
 * - gcTime: 30 dakika (cache ne kadar süre saklanır)
 * - retry: 4xx hatalarda retry YOK, 5xx'te 3 retry
 * - retryDelay: Exponential backoff (1s, 2s, 4s, max 30s)
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'

/**
 * TANSTACK QUERY CLIENT KONFİGÜRASYONU
 * 
 * Production-ready default ayarlar.
 * 
 * QUERIES (GET istekleri):
 * - staleTime: 5 dakika - Bu süre boyunca cache'teki data fresh kabul edilir
 * - gcTime: 30 dakika - Cache'te bu süre boyunca tutulur (garbage collection)
 * - retry: Hata durumunda yeniden deneme stratejisi
 *   * 4xx hatalar (client error): Retry YOK (örn: 401, 404, 400)
 *   * 5xx hatalar (server error): 3 kereye kadar retry
 * - retryDelay: Exponential backoff - Her denemede süre artar
 *   * 1. deneme: 1 saniye
 *   * 2. deneme: 2 saniye
 *   * 3. deneme: 4 saniye
 *   * Max: 30 saniye
 * - refetchOnWindowFocus: false - Pencere focus olduğunda otomatik refetch YOK
 * 
 * MUTATIONS (POST/PUT/DELETE istekleri):
 * - retry: false - Mutation'lar default olarak retry YAPILMAZ
 */
const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 1000 * 60 * 5, // 5 minutes - data considered fresh
            gcTime: 1000 * 60 * 30, // 30 minutes - garbage collection (formerly cacheTime)
            retry: (failureCount, error) => {
                // Don't retry on 4xx errors (client errors)
                if (error?.response?.status >= 400 && error?.response?.status < 500) {
                    return false
                }
                // Retry up to 3 times for other errors
                return failureCount < 3
            },
            retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
            refetchOnWindowFocus: false, // Disable for better UX (can be enabled per-query)
        },
        mutations: {
            retry: false, // Don't retry mutations by default
        },
    },
})

export { queryClient, QueryClientProvider }
