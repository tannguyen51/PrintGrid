import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { OrderPayment } from '../orders/useOrderPayment'
import { PENDING_PAYMENTS_KEY } from './usePendingPayments'

export interface MarkPaidInput {
  orderId: string
  amount: number
  /** Optional bank-transfer reference shown to ops; the backend may ignore it for now. */
  paymentReference?: string
}

/**
 * POST /orders/{orderId}/mark-paid (Ops) — record a received deposit/settlement.
 * The order is confirmed once the deposit threshold is covered.
 */
export function useMarkPaid() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ orderId, amount, paymentReference }: MarkPaidInput) =>
      apiClient
        .post<OrderPayment>(`/orders/${orderId}/mark-paid`, {
          amount,
          paymentReference: paymentReference?.trim() || undefined,
        })
        .then((r) => r.data),
    onSuccess: (_payment, variables) => {
      queryClient.invalidateQueries({ queryKey: PENDING_PAYMENTS_KEY })
      queryClient.invalidateQueries({ queryKey: ['orders'] })
      queryClient.invalidateQueries({ queryKey: ['orders', variables.orderId, 'payment'] })
    },
  })
}
