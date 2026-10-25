import { Suspense, lazy } from 'react'
import { CircularProgress, Box } from '@mui/material'
import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './ProtectedRoute'

const LoginPage = lazy(() => import('../features/auth/LoginPage'))
const RegisterPage = lazy(() => import('../features/auth/RegisterPage'))
const VerifyEmailPage = lazy(() => import('../features/auth/VerifyEmailPage'))
const ForgotPasswordPage = lazy(() => import('../features/auth/ForgotPasswordPage'))
const ResetPasswordPage = lazy(() => import('../features/auth/ResetPasswordPage'))
const HomePage = lazy(() => import('../features/home/HomePage'))
const ModelLibraryPage = lazy(() => import('../features/models/ModelLibraryPage'))
const StartOrderPage = lazy(() => import('../features/orders/StartOrderPage'))
const OrderConfigPage = lazy(() => import('../features/quotes/OrderConfigPage'))
const OrdersPage = lazy(() => import('../features/orders/OrdersPage'))
const DateChangeDecisionPage = lazy(() => import('../features/orders/DateChangeDecisionPage'))
const LabQueuePage = lazy(() => import('../features/lab/LabQueuePage'))
const LabRegisterPage = lazy(() => import('../features/lab/LabRegisterPage'))
const LabMachinesPage = lazy(() => import('../features/lab/LabMachinesPage'))
const LabTimelineScorePage = lazy(() => import('../features/lab/LabTimelineScorePage'))
const InventoryPage = lazy(() => import('../features/lab/InventoryPage'))
const ShipmentBatchPlannerPage = lazy(() => import('../features/lab/ShipmentBatchPlannerPage'))
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
const AdminOverviewPage = lazy(() => import('../features/admin/AdminOverviewPage'))
const AdminScoringConfigurationPage = lazy(() => import('../features/admin/AdminScoringConfigurationPage'))
const AdminLayout = lazy(() => import('../features/admin/AdminLayout').then((m) => ({ default: m.AdminLayout })))
const StaffLayout = lazy(() => import('./StaffLayout').then((m) => ({ default: m.StaffLayout })))
const CustomerLayout = lazy(() => import('./CustomerLayout').then((m) => ({ default: m.CustomerLayout })))
const ModelPreviewPage = lazy(() => import('../features/models/ModelPreviewPage'))
const SlaReportPage = lazy(() => import('../features/ops/SlaReportPage'))
const RefundsPage = lazy(() => import('../features/ops/RefundsPage'))
const PaymentsPage = lazy(() => import('../features/ops/PaymentsPage'))
const DesignRequestsPage = lazy(() => import('../features/ops/DesignRequestsPage'))
const DesignRequestPage = lazy(() => import('../features/design/DesignRequestPage'))

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
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/" element={<HomePage />} />
        {/* Customer pages share the customer navbar so navigation never disappears. */}
        <Route element={<CustomerLayout />}>
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
          path="/models/:modelId/preview"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <ModelPreviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/design-request"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <DesignRequestPage />
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
          path="/date-change/:token"
          element={
            <ProtectedRoute allowedRoles={['Customer']}>
              <DateChangeDecisionPage />
            </ProtectedRoute>
          }
        />
        </Route>
        {/* Staff consoles share one navbar shell so navigation never disappears. */}
        <Route element={<StaffLayout />}>
        <Route
          path="/lab/queue"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabQueuePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/register"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabRegisterPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/machines"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabMachinesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/timeline"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabTimelineScorePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/lab/score"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <LabTimelineScorePage />
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
          path="/lab/:labId/shipments"
          element={
            <ProtectedRoute allowedRoles={['LabManager', 'LabOperator']}>
              <ShipmentBatchPlannerPage />
            </ProtectedRoute>
          }
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
          path="/hub/complete-orders"
          element={
            <ProtectedRoute allowedRoles={['HubQC', 'HubFulfillment']}>
              <HubShipmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/staff/qc-proofs"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <QcProofReviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/quote-reviews"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <QuoteReviewPage />
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
        <Route
          path="/ops/escalations"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <EscalationQueuePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/decisions"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <DecisionTracePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/labs"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <OpsLabsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/sla"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <SlaReportPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/refunds"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <RefundsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/payments"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <PaymentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/ops/design-requests"
          element={
            <ProtectedRoute allowedRoles={['OpsManager', 'Admin']}>
              <DesignRequestsPage />
            </ProtectedRoute>
          }
        />
        <Route element={<AdminLayout />}>
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
        <Route
          path="/admin"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminOverviewPage /></ProtectedRoute>}
        />
        <Route
          path="/admin/scoring-configurations"
          element={<ProtectedRoute allowedRoles={['Admin']}><AdminScoringConfigurationPage /></ProtectedRoute>}
        />
        </Route>
        </Route>
        <Route path="/forbidden" element={<ForbiddenPage />} />
        {/* Catch-all must stay last — unknown URLs used to render a blank page. */}
        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </Suspense>
  )
}
