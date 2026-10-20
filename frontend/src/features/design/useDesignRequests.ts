import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/** FR-CUST-013 / BR-IP-004 — a design intake request. */
export interface DesignRequestRow {
  id: string
  customerId: string
  title: string
  description: string
  status: string
  quoteId: string | null
  referenceImageKeys: string[]
  createdAtUtc: string
}

export const DESIGN_STATUSES = ['Pending', 'InReview', 'Quoted', 'Rejected', 'Accepted'] as const

const DESIGN_KEY = ['design-requests'] as const

export function useMyDesignRequests() {
  return useQuery({
    queryKey: [...DESIGN_KEY, 'mine'],
    queryFn: () =>
      apiClient.get<DesignRequestRow[]>('/design-requests/mine').then((r) => r.data),
  })
}

export function useDesignRequests(status?: string) {
  return useQuery({
    queryKey: [...DESIGN_KEY, 'all', status ?? 'all'],
    queryFn: () =>
      apiClient
        .get<DesignRequestRow[]>('/design-requests', { params: status ? { status } : undefined })
        .then((r) => r.data),
  })
}

/** Uploads 1–5 reference images and returns the object keys to attach to the brief. */
export function useUploadDesignImages() {
  return useMutation({
    mutationFn: async (files: File[]) => {
      const form = new FormData()
      files.forEach((file) => form.append('images', file))
      const { data } = await apiClient.post<{ imageKeys: string[] }>('/design-requests/images', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      return data.imageKeys
    },
  })
}

export function useCreateDesignRequest() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { title: string; description: string; referenceImageKeys: string[] }) =>
      apiClient.post<DesignRequestRow>('/design-requests', input).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: DESIGN_KEY }),
  })
}

export function useChangeDesignStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { id: string; status: string }) =>
      apiClient.post<DesignRequestRow>(`/design-requests/${input.id}/status`, { status: input.status }).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: DESIGN_KEY }),
  })
}
