import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export type ModelReviewStatus = 'Pending' | 'Approved' | 'Rejected'

export interface ModelReview {
  modelId: string
  customerId: string
  customerEmail: string
  name: string
  fileName: string
  fileFormat: string
  sizeBytes: number
  sha256?: string | null
  geometryStatus: string
  isPrintable?: boolean | null
  createdAt: string
  reviewStatus: ModelReviewStatus
  reviewReason?: string | null
  reviewedAtUtc?: string | null
}

export function useModelReviews(status: ModelReviewStatus | 'All') {
  return useQuery({
    queryKey: ['model-reviews', status],
    queryFn: async () => {
      const params = status === 'All' ? {} : { status }
      const { data } = await apiClient.get<ModelReview[]>('/model-reviews', { params })
      return data
    },
  })
}

export function useApproveModel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ modelId, note }: { modelId: string; note?: string }) => {
      await apiClient.post(`/model-reviews/${modelId}/approve`, { note })
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['model-reviews'] }),
  })
}

export function useRejectModel() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ modelId, reason }: { modelId: string; reason: string }) => {
      await apiClient.post(`/model-reviews/${modelId}/reject`, { reason })
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['model-reviews'] }),
  })
}

/** Opens the reviewer-scoped file stream in a new tab (the API logs the access). */
export async function openModelFile(modelId: string) {
  const response = await apiClient.get(`/model-reviews/${modelId}/file`, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data as Blob)
  window.open(url, '_blank', 'noopener,noreferrer')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}
