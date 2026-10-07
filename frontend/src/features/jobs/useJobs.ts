import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Job, JobStatus, QcProofQueueItem } from './jobTypes'

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

export function useDeclineJob() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      apiClient.post(`/jobs/${id}/decline`, { reason }),
    onSuccess: invalidate,
  })
}

export function useStartJob() {
  const invalidate = useInvalidate()
  return useMutation({ mutationFn: (id: string) => apiClient.post(`/jobs/${id}/start`), onSuccess: invalidate })
}

export function useCompleteJob() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, actualMinutes, selfReport, photos }: { id: string; actualMinutes: number; selfReport: string; photos: File[] }) => {
      const form = new FormData()
      form.append('actualPrintMinutes', String(actualMinutes))
      form.append('selfReport', selfReport)
      photos.forEach((photo) => form.append('photos', photo))
      return apiClient.post(`/jobs/${id}/complete`, form, { headers: { 'Content-Type': 'multipart/form-data' } })
    },
    onSuccess: invalidate,
  })
}

export function useQcProofQueue() {
  return useQuery({
    queryKey: [...JOBS_KEY, 'qc-proofs'],
    queryFn: () => apiClient.get<QcProofQueueItem[]>('/jobs/qc-proofs').then((r) => r.data),
  })
}

export function useReviewQcProof() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, approved, reason }: { id: string; approved: boolean; reason?: string }) =>
      apiClient.post(`/jobs/${id}/qc-proof/review`, { approved, reason }),
    onSuccess: invalidate,
  })
}

export interface InspectJobPayload {
  id: string
  passed: boolean
  checklistResults: { itemName: string; status: 'Pass' | 'Fail' | 'NotApplicable'; note?: string }[]
  photoUrls: string[]
  faultAttribution?: 'Lab' | 'Hub' | 'Customer'
  note?: string
}

export function useInspectJob() {
  const invalidate = useInvalidate()
  return useMutation({
    mutationFn: ({ id, passed, checklistResults, photoUrls, faultAttribution, note }: InspectJobPayload) =>
      apiClient.post(`/jobs/${id}/inspect`, { passed, checklistResults, photoUrls, faultAttribution, note }),
    onSuccess: invalidate,
  })
}
