import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/**
 * Confirms (or rejects) COD receipt for an order (BR-PAY-008/010, FR-CUST-012).
 * The customer-side UI reads the current COD state from the order list payload, so this
 * only needs the mutation — no separate GET endpoint. After a success we refresh the
 * orders list and the open order's timeline so the panel and stepper update immediately.
 */
export function useConfirmCodReceipt(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (accepted: boolean) => {
      const { data } = await apiClient.post(`/orders/${orderId}/cod-confirm`, { accepted })
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['orders'] })
      queryClient.invalidateQueries({ queryKey: ['order-timeline', orderId] })
    },
  })
}
