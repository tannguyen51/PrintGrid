import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as signalR from '@microsoft/signalr'
import { apiClient } from '../../shared/api/apiClient'
import { getAccessToken } from '../../shared/api/tokenStore'

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
    if (!orderId) return

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/orders', {
        accessTokenFactory: () => getAccessToken() || '',
      })
      .withAutomaticReconnect()
      .build()

    connection.on('TimelineUpdated', (updatedOrderId: string) => {
      if (updatedOrderId === orderId) {
        queryClient.invalidateQueries({ queryKey: ['order-timeline', orderId] })
      }
    })

    connection.onreconnecting(() => setIsConnected(false))
    connection.onreconnected(() => setIsConnected(true))
    connection.onclose(() => setIsConnected(false))

    connection.start()
      .then(() => setIsConnected(true))
      .catch(console.error)

    return () => {
      connection.stop()
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
