import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as signalR from '@microsoft/signalr'
import { apiClient } from '../../shared/api/apiClient'
import { getAccessToken } from '../../shared/api/tokenStore'
import { startHubConnection } from '../../shared/api/hubLifecycle'

export interface OrderStage {
  stageName: string
  completedAt: string | null
  isCurrent: boolean
}

export interface CustomerOrderItem {
  id: string
  modelName: string
  quantity: number
  status: string
}

export interface CustomerTimelineDto {
  orderId: string
  orderNumber: string
  currentStage: string
  promisedDeliveryDate: string
  isDelayed: boolean
  stages: OrderStage[]
  items: CustomerOrderItem[]
  trackingNumber: string | null
  deliveredAt: string | null
  canConfirmReceipt: boolean
  contractNumber?: string | null
}

export function useOrderTimeline(orderId: string | null) {
  const queryClient = useQueryClient()
  const [isConnected, setIsConnected] = useState(false)

  const queryKey = ['order-timeline', orderId]

  const query = useQuery({
    queryKey,
    queryFn: async () => {
      if (!orderId) return null
      const { data } = await apiClient.get<CustomerTimelineDto>(`/orders/${orderId}/timeline`)
      return data
    },
    enabled: !!orderId,
    refetchInterval: isConnected ? false : 15000,
  })

  useEffect(() => {
    setIsConnected(false)
    if (!orderId) return
    let disposed = false

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/orders', {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect()
      .build()

    const onTimelineUpdated = (updatedOrderId: string) => {
      if (!disposed && updatedOrderId === orderId) {
        queryClient.invalidateQueries({ queryKey: ['order-timeline', orderId] })
      }
    }
    connection.on('TimelineUpdated', onTimelineUpdated)

    connection.onreconnecting(() => { if (!disposed) setIsConnected(false) })
    connection.onreconnected(() => {
      if (!disposed) {
        setIsConnected(true)
        void queryClient.invalidateQueries({ queryKey: ['order-timeline', orderId] })
      }
    })
    connection.onclose(() => { if (!disposed) setIsConnected(false) })

    const stopConnection = startHubConnection(connection,
      () => setIsConnected(true),
      (error) => console.error('Unable to connect to the order timeline hub.', error))

    return () => {
      disposed = true
      connection.off('TimelineUpdated', onTimelineUpdated)
      stopConnection()
    }
  }, [orderId, queryClient])

  return query
}

/** Customer confirms they received the goods — stamps the 30-day warranty anchor on the backend. */
export function useConfirmOrderReceipt(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiClient.post(`/orders/${orderId}/confirm-receipt`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['order-timeline', orderId] })
      queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
  })
}
