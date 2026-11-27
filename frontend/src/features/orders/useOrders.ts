import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Order } from '../../shared/types/order'

export function useOrders() {
  return useQuery({
    queryKey: ['orders'],
    refetchInterval: 30_000,
    refetchOnWindowFocus: true,
    queryFn: async () => {
      const { data } = await apiClient.get<Order[]>('/orders')
      return data
    },
  })
}
