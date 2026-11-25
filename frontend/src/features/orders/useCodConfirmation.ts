import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'

interface CodConfirmationData {
  isCodConfirmed: boolean
  codConfirmationRequestedAtUtc: string | null
  codConfirmationDeadlineUtc: string | null
  codConfirmationRequestCount: number
  paymentMethod: string
}

export function useCodConfirmation(orderId: string) {
  return useQuery({
    queryKey: ['cod-confirmation', orderId],
    queryFn: async () => {
      const response = await axios.get(`/api/v1/orders/${orderId}`)
      return response.data as CodConfirmationData
    },
    enabled: !!orderId,
    refetchInterval: 60000, // Refetch every minute for countdown
  })
}

export function useConfirmCodReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ orderId, accepted }: { orderId: string; accepted: boolean }) => {
      const response = await axios.post(`/api/v1/orders/${orderId}/cod-confirm`, { accepted })
      return response.data
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['cod-confirmation', variables.orderId] })
      queryClient.invalidateQueries({ queryKey: ['orders'] })
    },
  })
}

export function useRequestCodConfirmation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (orderId: string) => {
      const response = await axios.post(`/api/v1/orders/${orderId}/request-cod-confirmation`)
      return response.data
    },
    onSuccess: (_, orderId) => {
      queryClient.invalidateQueries({ queryKey: ['cod-confirmation', orderId] })
    },
  })
}
