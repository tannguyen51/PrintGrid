import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface ReprintRequest {
  requestId: string
  orderId: string
  reason: string
  description: string
  photos: string[]
  status: string
  createdAt: string
  updatedAt: string
  resolutionNote: string | null
}

export function useReprintRequests(orderId: string) {
  return useQuery({
    queryKey: ['reprint-requests', orderId],
    queryFn: async () => (await apiClient.get<ReprintRequest[]>(`/orders/${orderId}/reprint-requests`)).data,
  })
}

export function useCreateReprintRequest(orderId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: { reason: string; description: string; photos: string[] }) =>
      (await apiClient.post<ReprintRequest>(`/orders/${orderId}/reprint-request`, body)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['reprint-requests', orderId] }),
  })
}
