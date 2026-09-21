import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { clearTokens, getAccessToken, getRefreshToken, setTokens, isRemembered } from './tokenStore'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api/v1',
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

let refreshInFlight: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) throw new Error('No refresh token available')

  const { data } = await axios.post<{ accessToken: string; refreshToken: string }>(
    `${apiClient.defaults.baseURL}/auth/refresh`,
    { refreshToken },
  )

  setTokens(data.accessToken, data.refreshToken, isRemembered())
  return data.accessToken
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as InternalAxiosRequestConfig & { _retried?: boolean }

    if (error.response?.status !== 401 || request?._retried) {
      return Promise.reject(error)
    }

    request._retried = true

    try {
      refreshInFlight ??= refreshAccessToken().finally(() => {
        refreshInFlight = null
      })
      const token = await refreshInFlight
      request.headers.Authorization = `Bearer ${token}`
      return apiClient(request)
    } catch (refreshError) {
      clearTokens()
      window.location.assign('/login')
      return Promise.reject(refreshError)
    }
  },
)
