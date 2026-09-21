import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Job, JobStatus } from './jobTypes'

const JOBS_KEY = ['jobs'] as const

export function useJobs(status: JobStatus) {
  return useQuery({
    queryKey: [...JOBS_KEY, status],
    queryFn: () => apiClient.get<Job[]>(`/jobs?status=${status}`).then((r) => r.data),
  })
}

function useInvalidate() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: JOBS_KEY })
}

export function useAcceptJob() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (id: string) => apiClient.post(`/jobs/${id}/accept`), onSuccess: invalidate })
}

export function useStartJob() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (id: string) => apiClient.post(`/jobs/${id}/start`), onSuccess: invalidate })
}

export function useCompleteJob() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, actualMinutes }: { id: string; actualMinutes: number }) =>
      apiClient.post(`/jobs/${id}/complete`, { actualPrintMinutes: actualMinutes }),
    onSuccess: invalidate,
  })
}

export function useInspectJob() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, passed, note }: { id: string; passed: boolean; note?: string }) =>
      apiClient.post(`/jobs/${id}/inspect`, { passed, note }),
    onSuccess: invalidate,
  })
}