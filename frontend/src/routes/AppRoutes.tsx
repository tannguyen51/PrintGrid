import { Suspense, lazy } from 'react'
import { CircularProgress, Box } from '@mui/material'
import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'

const LoginPage = lazy(() => import('../features/auth/LoginPage'))
const RegisterPage = lazy(() => import('../features/auth/RegisterPage'))
const VerifyEmailPage = lazy(() => import('../features/auth/VerifyEmailPage'))
const HomePage = lazy(() => import('../features/home/HomePage'))
const ModelLibraryPage = lazy(() => import('../features/models/ModelLibraryPage'))
const StartOrderPage = lazy(() => import('../features/orders/StartOrderPage'))
const OrderConfigPage = lazy(() => import('../features/quotes/OrderConfigPage'))
const OrdersPage = lazy(() => import('../features/orders/OrdersPage'))
const LabQueuePage = lazy(() => import('../features/lab/LabQueuePage'))
const InventoryPage = lazy(() => import('../features/lab/InventoryPage'))
const HubQCPage = lazy(() => import('../features/hub/HubQCPage'))
const HubShipmentsPage = lazy(() => import('../features/hub/HubShipmentsPage'))
const SchedulingBoardPage = lazy(() => import('../features/scheduling/SchedulingBoardPage'))
const QcProofReviewPage = lazy(() => import('../features/qcProof/QcProofReviewPage'))
const QuoteReviewPage = lazy(() => import('../features/quotes/QuoteReviewPage'))
const EscalationQueuePage = lazy(() => import('../features/ops/EscalationQueuePage'))
const DecisionTracePage = lazy(() => import('../features/ops/DecisionTracePage'))
const OpsLabsPage = lazy(() => import('../features/ops/OpsLabsPage'))
const AdminUsersPage = lazy(() => import('../features/admin/AdminUsersPage'))
const AdminAuditLogPage = lazy(() => import('../features/admin/AdminAuditLogPage'))
const AdminCommercialConfigurationPage = lazy(() => import('../features/admin/AdminCommercialConfigurationPage'))
const AdminSharedCatalogPage = lazy(() => import('../features/admin/AdminSharedCatalogPage'))
const AdminQcKnowledgePage = lazy(() => import('../features/admin/AdminQcKnowledgePage'))
const AdminLabApprovalPage = lazy(() => import('../features/admin/AdminLabApprovalPage'))
const ForbiddenPage = lazy(() => import('../features/errors/ForbiddenPage'))
const NotFoundPage = lazy(() => import('../features/errors/NotFoundPage'))
const AccountPage = lazy(() => import('../features/account/AccountPage'))

function RouteFallback() {
  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}>
      <CircularProgress />
    </Box>
  )
}

/**
 * Role-based areas. Each demo account (demo@ / lab@ / qc@ / ops@) lands in its own
 * area only: customers see models/orders, lab staff see the lab queue, hub staff the
 * QC console, ops the scheduling board. Matches the strict backend role policies.
 */
export function AppRoutes() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/verify-email" element={<VerifyEmailPage />} />
        <Route path="/" element={<HomePage />} />
        <Route
          path="/models"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <ModelLibraryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/library"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <ModelLibraryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/account"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <AccountPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/order/new"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <StartOrderPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/models/:modelId/order"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <OrderConfigPage />
            </ProtectedRoute>
          }
        />
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
          path="/lab/:labId/inventory"
          element={
            <ProtectedRoute allowedRoles={['LabManager']}>
              <InventoryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/settings"
          element={<ProtectedRoute allowedRoles={['LabManager']}><OpsLabsPage /></ProtectedRoute>}
        />
        <Route
          path="/hub/qc"
          element={
            <ProtectedRoute allowedRoles={['HubQC', 'HubFulfillment']}>
              <HubQCPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/hub/shipments"
          element={
            <ProtectedRoute allowedRoles={['HubQC', 'HubFulfillment']}>
              <HubShipmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff/qc-proofs"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <QcProofReviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/quote-reviews"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <QuoteReviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/scheduling"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <SchedulingBoardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/escalations"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <EscalationQueuePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/decisions"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <DecisionTracePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/labs"
          element={
            <ProtectedRoute allowedRoles={['OpsManager']}>
              <OpsLabsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminUsersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/audit-logs"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminAuditLogPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/commercial-configurations"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminCommercialConfigurationPage /></ProtectedRoute>}
        />
        <Route
          path="/admin/catalogs"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminSharedCatalogPage /></ProtectedRoute>}
        />
        <Route
          path="/admin/qc-knowledge"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminQcKnowledgePage /></ProtectedRoute>}
        />
        <Route
          path="/admin/labs"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminLabApprovalPage /></ProtectedRoute>}
        />
        <Route path="/forbidden" element={<ForbiddenPage />} />
        {/* Catch-all must stay last — unknown URLs used to render a blank page. */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </Suspense>
  )
}
