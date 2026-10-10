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

/**
 * Streams the job's model file with the auth header and hands it to the browser as a
 * download — a plain <a href> could not carry the bearer token (GET /jobs/{id}/file).
 */
export async function downloadJobFile(jobId: string, fileName?: string | null) {
  const response = await apiClient.get(`/jobs/${jobId}/file`, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data as Blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName || 'model-file'
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  // Give the browser a moment to start the download before revoking.
  window.setTimeout(() => URL.revokeObjectURL(url), 10_000)
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
    mutationFn: ({ id, actualMinutes, selfReport, photos, actualMaterialGrams }: { id: string; actualMinutes: number; selfReport: string; photos: File[]; actualMaterialGrams?: number }) => {
      const form = new FormData()
      form.append('actualPrintMinutes', String(actualMinutes))
      form.append('selfReport', selfReport)
      // Drives the stock ledger when the lab reports what it actually used.
      if (actualMaterialGrams !== undefined) form.append('actualMaterialGrams', String(actualMaterialGrams))
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

export interface InspectionPhotoUploadResult {
  photoKeys: string[]
}

/** Uploads 1–5 QC evidence photos for a hub inspection and returns their storage object keys. */
export function useUploadInspectionPhotos() {
  return useMutation({
    mutationFn: ({ id, photos }: { id: string; photos: File[] }) => {
      const form = new FormData()
      photos.forEach((photo) => form.append('photos', photo))
      return apiClient
        .post<InspectionPhotoUploadResult>(`/jobs/${id}/inspection-photos`, form, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
        .then((r) => r.data.photoKeys)
    },
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
