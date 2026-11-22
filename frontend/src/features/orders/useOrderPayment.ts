import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/**
 * FR-CUST-012 — payment snapshot of one order (the customer's own order only).
 * Mirrors OrderPaymentDto in the Customer module.
 */
export interface OrderPayment {
  totalAmount: number
  depositAmount: number
  paidAmount: number
  remaining: number
  amountDue: number
  isDeposited: boolean
  isSettled: boolean
  currency: string
  paymentCode: string
  transferDescription: string
  qrUrl: string | null
  bankCode: string | null
  accountNumber: string | null
  accountName: string | null
  paymentEnabled: boolean
}

/** GET /orders/{orderId}/payment */
export function useOrderPayment(orderId: string | null) {
  return useQuery({
    queryKey: ['orders', orderId, 'payment'],
    queryFn: async () => {
      const { data } = await apiClient.get<OrderPayment>(`/orders/${orderId}/payment`)
      return data
    },
    enabled: Boolean(orderId),
    retry: false,
    refetchInterval: (query) => query.state.data?.isDeposited ? false : 4000,
  })
}
