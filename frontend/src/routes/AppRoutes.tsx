import { Suspense, lazy } from 'react'
import { CircularProgress, Box } from '@mui/material'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'

const LoginPage = lazy(() => import('../features/auth/LoginPage'))
const OrdersPage = lazy(() => import('../features/orders/OrdersPage'))
const LabQueuePage = lazy(() => import('../features/lab/LabQueuePage'))
const SchedulingBoardPage = lazy(() => import('../features/scheduling/SchedulingBoardPage'))

function RouteFallback() {
  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
      <CircularProgress />
    </Box>
  )
}

export function AppRoutes() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          path="/orders"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <OrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/queue"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabQueuePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/scheduling"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <SchedulingBoardPage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/orders" replace />} />
      </Routes>
    </Suspense>
  )
}
