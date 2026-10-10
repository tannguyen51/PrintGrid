import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { apiClient } from '../shared/api/apiClient'
import { clearTokens, getAccessToken, setTokens } from '../shared/api/tokenStore'

export type Role =
  | 'Customer'
  | 'LabManager'
  | 'LabOperator'
  | 'HubQC'
  | 'HubFulfillment'
  | 'OpsManager'
  | 'OrderStaff'
  | 'Admin'

export interface AuthUser {
  id: string
  email: string
  fullName: string
  roles: Role[]
  isEmailVerified: boolean
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
  login: (email: string, password: string, remember?: boolean) => Promise<AuthUser>
  register: (input: RegisterInput) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: AuthUser
}

const KNOWN_ROLES: Role[] = [
  'Customer', 'LabManager', 'LabOperator', 'HubQC', 'HubFulfillment',
  'OpsManager', 'OrderStaff', 'Admin',
]

/**
 * Backend RequireRole so sánh role KHÔNG phân biệt hoa thường, còn `roles.includes()`
 * phía UI thì có — một tài khoản role 'customer' trong DB sẽ qua API nhưng bị
 * ProtectedRoute đá sang /forbidden (403). Quy về dạng chuẩn ở một chỗ duy nhất.
 * Role lạ vẫn giữ nguyên để các trang chẩn đoán hiển thị được đúng giá trị.
 */
function normalizeRoles(roles: string[]): Role[] {
  return roles
    .map((r) => r.trim())
    .filter(Boolean)
    .map((r) => (KNOWN_ROLES.find((k) => k.toLowerCase() === r.toLowerCase()) ?? (r as Role)))
}

/** Đọc user từ access token (JWT payload) khi khôi phục phiên sau khi reload trang. */
function userFromAccessToken(token: string): AuthUser | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1] ?? ''))
    const id = payload.sub ?? payload.nameidentifier
    const rolesRaw =
      payload.role ??
      payload.roles ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    const roles: string[] = typeof rolesRaw === 'string' ? rolesRaw.split(',') : Array.isArray(rolesRaw) ? rolesRaw : []
    if (!id || !payload.email) return null
    return {
      id,
      email: payload.email,
      fullName: payload.unique_name ?? payload.name ?? '',
      roles: normalizeRoles(roles),
      isEmailVerified: payload.email_verified === 'true' || payload.email_verified === true
    }
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // Khôi phục phiên ngay từ state khởi tạo khi có access token.
  const [user, setUser] = useState<AuthUser | null>(() => {
    const token = getAccessToken()
    return token ? userFromAccessToken(token) : null
  })

  const login = useCallback(async (email: string, password: string, remember = false): Promise<AuthUser> => {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', { email, password })
    const user: AuthUser = { ...data.user, roles: normalizeRoles(data.user.roles ?? []) }
    setTokens(data.accessToken, data.refreshToken, remember)
    setUser(user)
    return user
  }, [])

  const register = useCallback(async (input: RegisterInput) => {
    const { data } = await apiClient.post<LoginResponse | null>('/auth/register', input)
    if (data) {
      setTokens(data.accessToken, data.refreshToken, false)
      setUser({ ...data.user, roles: normalizeRoles(data.user.roles ?? []) })
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
