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
  | 'Admin'

export interface AuthUser {
  id: string
  email: string
  fullName: string
  roles: Role[]
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  register: (input: { fullName: string; email: string; password: string }) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

interface LoginResponse {
  accessToken: string
  refreshToken: string
  user: AuthUser
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await apiClient.post<LoginResponse>('/auth/login', { email, password })
    setTokens(data.accessToken, data.refreshToken)
    setUser(data.user)
  }, [])

  const register = useCallback(async (input: { fullName: string; email: string; password: string }) => {
    // Creates the account; the returned session (if any) is stored the same way
    // as login, so the user lands on the app immediately. If the API returns no
    // session, the caller should bounce to /login after a success.
    const { data } = await apiClient.post<LoginResponse | null>('/auth/register', input)
    if (data) {
      setTokens(data.accessToken, data.refreshToken)
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
