import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { apiClient } from '../shared/api/apiClient'
import { clearTokens, getAccessToken, setTokens, isRemembered } from '../shared/api/tokenStore'

export type Role =
  | 'Customer'
  | 'LabManager'
  | 'LabOperator'
  | 'HubQC'
  | 'HubFulfillment'
  | 'OpsManager'
  | 'Admin'

export interface AuthUser {
  id: string
  email: string
  fullName: string
  roles: Role[]
}

export interface RegisterInput {
  fullName: string
  email: string
  password: string
  phoneNumber?: string | null
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  /** remember=true lưu token vào localStorage (giữ qua nhiều phiên mở trình duyệt). */
  login: (email: string, password: string, remember?: boolean) => Promise<void>
  register: (input: RegisterInput) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: AuthUser
}

/** Đọc user từ access token (JWT payload) khi khôi phục phiên sau khi reload trang. */
function userFromAccessToken(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''))
    const id = payload.sub ?? payload.nameidentifier
    const rolesRaw = payload.role ?? payload.roles
    const roles: Role[] = typeof rolesRaw === 'string' ? rolesRaw.split(',') : rolesRaw ?? []
    if (!id || !payload.email) return null
    return {
      id,
      email: payload.email,
      fullName: payload.unique_name ?? payload.name ?? '',
      roles,
    }
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Khôi phục phiên đã ghi nhớ ngay từ state khởi tạo (sau khi reload trang).
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = getAccessToken()
    return token && isRemembered() ? userFromAccessToken(token) : null
  })

  const login = useCallback(async (email: string, password: string, remember = false) => {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', { email, password })
    setTokens(data.accessToken, data.refreshToken, remember)
    setUser(data.user)
  }, [])

  const register = useCallback(async (input: RegisterInput) => {
    const { data } = await apiClient.post<LoginResponse | null>('/auth/register', input)
    if (data) {
      setTokens(data.accessToken, data.refreshToken, false)
      setUser(data.user)
    }
  }, [])

  const logout = useCallback(() => {
    clearTokens()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null && getAccessToken() !== null, login, register, logout }),
    [user, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}