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
  customerId?: string
  deliveryAddressSummary?: string
  readyForPackingAtUtc?: string | null
  consolidatedShipmentId?: string | null
  suggestedConsolidationGroupKey?: string | null
  suggestedCompanionOrderNumbers?: string[] | null
  totalAmount?: number
  refundedAmount?: number
  itemCount?: number
}

export interface ConsolidatedShipmentResult {
  consolidatedShipmentId: string
  trackingNumber: string
  orderIds: string[]
  orderNumbers: string[]
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

/** FR-HUB-006 / BR-LOG-001: Consolidate multiple eligible orders (same customer + same address + <=24h) under 1 waybill. */
export function useShipConsolidatedOrders() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ orderIds, trackingNumber }: { orderIds: string[]; trackingNumber: string }) =>
      apiClient
        .post<ConsolidatedShipmentResult>('/hub/orders/ship-consolidated', { orderIds, trackingNumber })
        .then((r) => r.data),
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

/** Hub physical handover for all orders in a consolidated package. */
export function useDeliverConsolidatedOrders() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: (orderIds: string[]) => apiClient.post('/hub/orders/deliver-consolidated', { orderIds }),
    onSuccess: invalidate,
  })
}

/** BR-LOG-002 / AC02: Refund strictly within the original order's scope (TotalAmount). */
export function useRefundOrderWithinScope() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ orderId, amount, reason }: { orderId: string; amount: number; reason?: string }) =>
      apiClient.post(`/hub/orders/${orderId}/refund`, { amount, reason }),
    onSuccess: invalidate,
  })
}
