import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../shared/api/apiClient'
import { clearTokens, getAccessToken, setTokens } from '../shared/api/tokenStore'
import { queryClient } from './queryClient'

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
  googleLogin: (credential: string, remember?: boolean) => Promise<AuthUser>
  register: (input: RegisterInput) => Promise<void>
  /** Đồng bộ lại tài khoản (và role) từ server — dùng khi token đang mang role cũ. */
  refreshUser: () => Promise<void>
  /** False trong lúc phiên đang được đồng bộ lại từ server ngay sau khi mở app. */
  sessionReady: boolean
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
  'OpsManager', 'Admin',
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
  const navigate = useNavigate()
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

  const googleLogin = useCallback(async (credential: string, remember = true): Promise<AuthUser> => {
    const { data } = await apiClient.post<LoginResponse>('/auth/google', { credential })
    const user: AuthUser = { ...data.user, roles: normalizeRoles(data.user.roles ?? []) }
    setTokens(data.accessToken, data.refreshToken, remember)
    setUser(user)
    return user
  }, [])

  /**
   * Role là dữ liệu của server, token chỉ là bản cache: một access token được cấp trước khi
   * tài khoản nhận role mới vẫn khai role cũ, khiến chính chủ bị đá khỏi console của mình (403)
   * cho tới khi token hết hạn. Đọc lại /auth/me để lấy role hiện tại trong DB.
   */
  const refreshUser = useCallback(async () => {
    if (!getAccessToken()) return
    try {
      const { data } = await apiClient.get<AuthUser>('/auth/me')
      setUser({ ...data, roles: normalizeRoles(data.roles ?? []) })
    } catch {
      // 401 đã được interceptor của apiClient xử lý (clear token + về trang chủ);
      // lỗi mạng thì giữ nguyên user suy ra từ token.
    }
  }, [])

  // Đồng bộ ngay khi mở app, trước khi ProtectedRoute kịp chặn theo role cũ trong token.
  // Chưa có token thì phiên đã "sẵn sàng" (khách), khỏi gọi server.
  const [sessionReady, setSessionReady] = useState(() => !getAccessToken())
  useEffect(() => {
    if (sessionReady) return
    void refreshUser().finally(() => setSessionReady(true))
  }, [sessionReady, refreshUser])

  /**
   * Signing out always lands on the guest homepage ("/"), for every role — staff consoles
   * are gone from the navbar once the tokens are cleared, so returning to /login only made
   * sense for the customer app. The query cache is dropped too: otherwise the next person
   * to sign in on this tab would briefly see the previous user's queued data.
   */
  const logout = useCallback(() => {
    clearTokens()
    setUser(null)
    queryClient.clear()
    setSessionReady(true)
    navigate('/', { replace: true })
  }, [navigate])

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null && getAccessToken() !== null, login, googleLogin, register, refreshUser, sessionReady, logout }),
    [user, login, googleLogin, register, refreshUser, sessionReady, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
