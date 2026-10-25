import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/**
 * GET /orders/pending-payment (Ops) — orders that have not covered their deposit yet.
 * Mirrors PendingOrderPaymentDto in the Customer module. The queue currently exposes the
 * customer id only (no display name), so the UI falls back to a short id. The name/email
 * fields are optional forward-compatible extras rendered when the backend adds them.
 */
export interface PendingPaymentRow {
  orderId: string
  orderNumber: string
  customerId?: string | null
  customerName?: string | null
  customerEmail?: string | null
  totalPrice: number
  depositAmount: number
  paidAmount: number
  remaining: number
  currency?: string | null
  createdAt: string
  promisedDeliveryDate: string
}

export const PENDING_PAYMENTS_KEY = ['orders', 'pending-payment'] as const

export function usePendingPayments() {
  return useQuery({
    queryKey: PENDING_PAYMENTS_KEY,
    queryFn: () => apiClient.get<PendingPaymentRow[]>('/orders/pending-payment').then((r) => r.data),
  })
}
