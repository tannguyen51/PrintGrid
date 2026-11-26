import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export type PaymentStatus = 'Unpaid' | 'Partial' | 'Paid'

export interface PaymentOverviewRow {
  orderId: string; orderNumber: string; customerId: string; customerName?: string | null; customerEmail?: string | null
  orderStatus: string; paymentStatus: PaymentStatus; totalAmount: number; depositAmount: number; paidAmount: number
  remaining: number; refundedAmount: number; currency: string; createdAt: string; promisedDeliveryDate: string
  transactionCount: number; ledgerReceived: number; labJobCount: number; labPayableCount: number
  labPayableStatuses: string[]; reconciliationStatus: 'Synced' | 'Mismatch'; reconciliationIssues: string[]
}

export interface PaymentDetail {
  orderId: string; orderNumber: string; paymentStatus: PaymentStatus; totalAmount: number; depositAmount: number
  paidAmount: number; remaining: number; refundedAmount: number; currency: string
  transactions: Array<{ id: string; type: string; amount: number; currency: string; reference: string; status: string; confirmedAtUtc: string; note?: string | null }>
  labPayables: Array<{ id: string; jobId: string; labId: string; grossAmount: number; deductionAmount: number; netAmount: number; currency: string; status: string; eligibleAtUtc: string; paidAtUtc?: string | null; paymentReference?: string | null }>
}

export const PENDING_PAYMENTS_KEY = ['orders', 'payment-overview'] as const

export function usePendingPayments(paymentStatus = 'All', search = '') {
  return useQuery({
    queryKey: [...PENDING_PAYMENTS_KEY, paymentStatus, search],
    queryFn: () => apiClient.get<PaymentOverviewRow[]>('/orders/payment-overview', {
      params: { paymentStatus, search: search.trim() || undefined },
    }).then(r => r.data),
  })
}

export const getPaymentDetail = (orderId: string) =>
  apiClient.get<PaymentDetail>(`/orders/${orderId}/payment-detail`).then(r => r.data)

export const syncPaymentReconciliation = () =>
  Promise.all([
    apiClient.post<{ created: number }>('/orders/payment-reconciliation/sync').then(r => r.data),
    apiClient.post<{ created: number; corrected: number }>('/finance/lab-payables/sync').then(r => r.data),
  ]).then(([ledger, lab]) => ({ ledgerCreated: ledger.created, labCreated: lab.created, labCorrected: lab.corrected }))
