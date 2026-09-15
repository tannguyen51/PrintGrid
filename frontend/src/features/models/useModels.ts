import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { ModelInput, ThreeDModel } from './modelTypes'

const MODELS_KEY = ['models'] as const
const searchParam = (s: string) => (s.trim() ? `?search=${encodeURIComponent(s.trim())}` : '')

export function useModels(search: string) {
  return useQuery({
    queryKey: [...MODELS_KEY, search],
    queryFn: () => apiClient.get<ThreeDModel[]>(`/models${searchParam(search)}`).then((r) => r.data),
  })
}

function useInvalidateModels() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: MODELS_KEY })
}

export function useCreateModel() {
  const invalidate = useInvalidateModels()
  return useMutation({
    mutationFn: (input: ModelInput) => apiClient.post<ThreeDModel>('/models', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useUpdateModel() {
  const invalidate = useInvalidateModels()
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ModelInput }) =>
      apiClient.put<ThreeDModel>(`/models/${id}`, input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useDeleteModel() {
  const invalidate = useInvalidateModels()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/models/${id}`),
    onSuccess: invalidate,
  })
}