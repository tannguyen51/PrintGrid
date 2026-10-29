import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { SampleDetail, SampleSummary } from './sampleTypes'

export const sampleKeys = {
  list: (search: string, category: string) => ['samples', search, category] as const,
  detail: (slug: string) => ['samples', 'detail', slug] as const,
}

/** Public library — anonymous, no token required. */
export function useSamples(search = '', category = '') {
  return useQuery({
    queryKey: sampleKeys.list(search, category),
    queryFn: async () => {
      const params = new URLSearchParams()
      if (search.trim()) params.set('search', search.trim())
      if (category) params.set('category', category)
      const query = params.toString()
      const { data } = await apiClient.get<SampleSummary[]>(`/samples${query ? `?${query}` : ''}`)
      return data
    },
  })
}

export function useSample(slug: string | undefined) {
  return useQuery({
    queryKey: sampleKeys.detail(slug ?? ''),
    queryFn: async () => {
      const { data } = await apiClient.get<SampleDetail>(`/samples/${encodeURIComponent(slug!)}`)
      return data
    },
    enabled: Boolean(slug),
  })
}

/**
 * "Đặt in mẫu này" — the backend copies the file into the caller's library and
 * returns the new private model. The wizard then takes over at /models/:id/order.
 */
export function useCopySample() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (slug: string) => {
      const { data } = await apiClient.post<{ id: string }>(`/samples/${encodeURIComponent(slug)}/copy`)
      return data
    },
    onSuccess: () => client.invalidateQueries({ queryKey: ['models'] }),
  })
}
