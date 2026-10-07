import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface ShipmentOrder {
  id: string
  orderNumber: string
  status: 'QualityCheck' | 'Shipping' | 'Delivered'
  trackingNumber: string | null
  promisedDeliveryDate: string
  deliveredAt: string | null
  receiptConfirmed: boolean
}

const QUEUE_KEY = ['hub-shipment-queue'] as const

export function useShipmentQueue() {
  return useQuery({
    queryKey: QUEUE_KEY,
    queryFn: () => apiClient.get<ShipmentOrder[]>('/hub/orders/shipment-queue').then((r) => r.data),
  })
}

function useInvalidate() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: QUEUE_KEY })
}

/** Hub goods-out: attach a carrier tracking number and move the order to Shipping. */
export function useShipOrder() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, trackingNumber }: { id: string; trackingNumber: string }) =>
      apiClient.post(`/hub/orders/${id}/ship`, { trackingNumber }),
    onSuccess: invalidate,
  })
}

/** Hub physical handover: move a shipped order to Delivered (warranty clock does NOT start here). */
export function useDeliverOrder() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: (id: string) => apiClient.post(`/hub/orders/${id}/deliver`),
    onSuccess: invalidate,
  })
}
