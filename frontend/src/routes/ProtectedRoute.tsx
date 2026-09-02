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
    return <Navigate to="/forbidden" replace />
  }

  return children
}
