const ACCESS_TOKEN_KEY = 'printgrid.accessToken'
const REFRESH_TOKEN_KEY = 'printgrid.refreshToken'
const REMEMBER_KEY = 'printgrid.remember'

/** "Ghi nhớ đăng nhập": lưu token vào localStorage (qua nhiều phiên) thay vì sessionStorage. */
export function isRemembered(): boolean {
  return localStorage.getItem(REMEMBER_KEY) === '1'
}

export function getAccessToken(): string | null {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY) ?? localStorage.getItem(ACCESS_TOKEN_KEY)
}

export function getRefreshToken(): string | null {
  return sessionStorage.getItem(REFRESH_TOKEN_KEY) ?? localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setTokens(accessToken: string, refreshToken: string, remember = false): void {
  const storage = remember ? localStorage : sessionStorage
  storage.setItem(ACCESS_TOKEN_KEY, accessToken)
  storage.setItem(REFRESH_TOKEN_KEY, refreshToken)
  if (remember) {
    localStorage.setItem(REMEMBER_KEY, '1')
    // Rời sessionStorage để tránh trùng phiên không nhất quán.
    sessionStorage.removeItem(ACCESS_TOKEN_KEY)
    sessionStorage.removeItem(REFRESH_TOKEN_KEY)
  } else {
    localStorage.removeItem(REMEMBER_KEY)
    localStorage.removeItem(ACCESS_TOKEN_KEY)
    localStorage.removeItem(REFRESH_TOKEN_KEY)
  }
}

export function clearTokens(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY)
  sessionStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(ACCESS_TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
  localStorage.removeItem(REMEMBER_KEY)
}