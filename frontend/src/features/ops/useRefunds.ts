import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/** FR-ANAL-005 — refund queue row returned by GET /refunds. */
export interface RefundRequestRow {
  id: string
  orderId: string
  orderItemId: string
  dateChangeRequestId: string
  amount: number
  currency: string
  paymentTransactionId: string
  reason: string
  status: string
  createdAtUtc: string
}

const REFUNDS_KEY = ['refunds'] as const

export function useRefunds(status = 'Pending') {
  return useQuery({
    queryKey: [...REFUNDS_KEY, status],
    queryFn: () =>
      apiClient
        .get<RefundRequestRow[]>('/refunds', { params: { status } })
        .then((r) => r.data),
  })
}

/** POST /refunds/{id}/process — approve executes the refund within the original order's scope. */
export function useProcessRefund() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { id: string; approve: boolean; note?: string }) =>
      apiClient
        .post<RefundRequestRow>(`/refunds/${input.id}/process`, {
          approve: input.approve,
          note: input.note ?? null,
        })
        .then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: REFUNDS_KEY }),
  })
}

/** GET /refunds/export — CSV download with a UTF-8 BOM for Excel. */
export function useExportRefunds() {
  return useMutation({
    mutationFn: async (status: string) => {
      const response = await apiClient.get<Blob>('/refunds/export', {
        params: { status },
        responseType: 'blob',
      })

      const disposition = response.headers['content-disposition'] as string | undefined
      const match = disposition?.match(/filename="?([^"]+)"?/)
      const fileName = match?.[1] ?? `printgrid-refunds-${new Date().toISOString().slice(0, 10)}.csv`

      const url = window.URL.createObjectURL(new Blob([response.data], { type: 'text/csv;charset=utf-8' }))
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    },
  })
}
