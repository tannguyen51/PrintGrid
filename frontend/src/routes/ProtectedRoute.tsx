import { Navigate, useLocation } from 'react-router-dom'
import type { ReactElement } from 'react'
import { useAuth, type Role } from '../app/AuthContext'

interface ProtectedRouteProps {
  children: ReactElement
  allowedRoles?: Role[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { user, isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  if (allowedRoles && !allowedRoles.some((role) => user?.roles.includes(role))) {
    // Mang theo ngữ cảnh để /forbidden in ra NGUYÊN NHÂN: trang nào, role nào có,
    // role nào cần — thay vì chỉ báo "không có quyền" chung chung.
    return (
      <Navigate
        to="/forbidden"
        replace
        state={{ from: location.pathname, requiredRoles: allowedRoles, userRoles: user?.roles ?? [] }}
      />
    )
  }

  return children
}
