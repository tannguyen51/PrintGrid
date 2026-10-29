import { Navigate, useLocation } from 'react-router-dom'
import { Box, CircularProgress } from '@mui/material'
import type { ReactElement } from 'react'
import { useAuth, type Role } from '../app/AuthContext'

interface ProtectedRouteProps {
  children: ReactElement
  allowedRoles?: Role[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { user, isAuthenticated, sessionReady } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  // Chờ AuthContext đồng bộ role từ /auth/me: role nhúng trong token có thể đã cũ, và
  // chặn theo bản cũ sẽ đá đúng người có quyền sang /forbidden.
  if (!sessionReady) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
        <CircularProgress />
      </Box>
    )
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
