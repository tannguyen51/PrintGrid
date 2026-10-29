import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { AdminSample } from '../samples/sampleTypes'

const ADMIN_SAMPLES_KEY = ['admin', 'samples'] as const

export interface SampleFormInput {
  id?: string
  name: string
  slug?: string
  description?: string
  category: string
  tags?: string
  coverImageUrl?: string
  sortOrder: number
  isPublished: boolean
  /** Required on create; optional on update (leave empty to keep the current binary). */
  file?: File | null
}

export function useAdminSamples() {
  return useQuery({
    queryKey: ADMIN_SAMPLES_KEY,
    queryFn: async () => (await apiClient.get<AdminSample[]>('/admin/samples')).data,
  })
}

function toFormData(input: SampleFormInput): FormData {
  const form = new FormData()
  form.append('Name', input.name)
  if (input.slug) form.append('Slug', input.slug)
  if (input.description) form.append('Description', input.description)
  form.append('Category', input.category)
  if (input.tags) form.append('Tags', input.tags)
  if (input.coverImageUrl) form.append('CoverImageUrl', input.coverImageUrl)
  form.append('SortOrder', String(input.sortOrder))
  form.append('IsPublished', String(input.isPublished))
  if (input.file) form.append('File', input.file)
  return form
}

const MULTIPART = { headers: { 'Content-Type': 'multipart/form-data' } }

export function useSaveSample() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (input: SampleFormInput) => {
      const form = toFormData(input)
      const { data } = input.id
        ? await apiClient.put<AdminSample>(`/admin/samples/${input.id}`, form, MULTIPART)
        : await apiClient.post<AdminSample>('/admin/samples', form, MULTIPART)
      return data
    },
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ADMIN_SAMPLES_KEY })
      client.invalidateQueries({ queryKey: ['samples'] })
    },
  })
}

export function useSetSamplePublished() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, published }: { id: string; published: boolean }) =>
      (await apiClient.post<AdminSample>(`/admin/samples/${id}/publish`, { published })).data,
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ADMIN_SAMPLES_KEY })
      client.invalidateQueries({ queryKey: ['samples'] })
    },
  })
}

export function useDeleteSample() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/admin/samples/${id}`),
    onSuccess: () => {
      client.invalidateQueries({ queryKey: ADMIN_SAMPLES_KEY })
      client.invalidateQueries({ queryKey: ['samples'] })
    },
  })
}
