import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { PagedResponse } from '../../shared/types/pagination'
import type {
  CuraProfileDetail,
  CuraProfileListItem,
  Machine,
  MyProposalsFilter,
  PagedProfilesFilter,
} from '../ops/labTypes'

export interface SubmitProfileProposalInput {
  labId: string
  name: string
  manufacturer: string
  machineModel: string
  materialCode: string
  version: string
  extruderCount: number
  settingsJson: string
  targetMachineId?: string | null
  submissionNotes?: string | null
  sourceFileName?: string | null
}

export interface ResubmitProfileProposalInput {
  id: string
  name: string
  manufacturer: string
  machineModel: string
  materialCode: string
  version: string
  extruderCount: number
  settingsJson: string
  targetMachineId?: string | null
  submissionNotes?: string | null
  sourceFileName?: string | null
}

export interface ImportCuraProfileInput {
  name: string
  manufacturer: string
  machineModel: string
  materialCode: string
  version: string
  engineVersion: string
  extruderCount: number
  settingsJson: string
}

function cleanParams(record: Record<string, string | number | boolean | undefined>) {
  const out: Record<string, string | number | boolean> = {}
  for (const [k, v] of Object.entries(record)) {
    if (v !== undefined && v !== '') out[k] = typeof v === 'string' ? v.trim() : v
  }
  return out
}

export function useCuraProfilesPaged(filters: PagedProfilesFilter, enabled = true) {
  return useQuery({
    queryKey: ['cura-profiles', 'paged', filters],
    enabled,
    placeholderData: (previous) => previous,
    queryFn: () =>
      apiClient
        .get<PagedResponse<CuraProfileListItem>>('/cura-profiles/paged', {
          params: cleanParams({
            scope: filters.scope ?? 'all',
            search: filters.search,
            labId: filters.labId,
            approvalStatus: filters.approvalStatus,
            isActive: filters.isActive === '' ? undefined : filters.isActive,
            manufacturerOrModel: filters.manufacturerOrModel,
            material: filters.material,
            page: filters.page,
            pageSize: filters.pageSize,
          }),
        })
        .then((r) => r.data),
  })
}

export function useMyCuraProfileProposals(filters: MyProposalsFilter, enabled = true) {
  return useQuery({
    queryKey: ['my-cura-profile-proposals', filters],
    enabled,
    placeholderData: (previous) => previous,
    queryFn: () =>
      apiClient
        .get<PagedResponse<CuraProfileListItem>>('/cura-profiles/my-proposals', {
          params: cleanParams({
            search: filters.search,
            approvalStatus: filters.approvalStatus,
            material: filters.material,
            page: filters.page,
            pageSize: filters.pageSize,
          }),
        })
        .then((r) => r.data),
  })
}

export function useCuraProfileProposalDetail(id?: string | null) {
  return useQuery({
    queryKey: ['cura-profile-proposal-detail', id],
    enabled: Boolean(id),
    queryFn: () =>
      apiClient.get<CuraProfileDetail>(`/cura-profiles/proposals/${id}`).then((r) => r.data),
  })
}

function useInvalidateProfileQueries() {
  const client = useQueryClient()
  return () => {
    client.invalidateQueries({ queryKey: ['cura-profiles'] })
    client.invalidateQueries({ queryKey: ['my-cura-profile-proposals'] })
    client.invalidateQueries({ queryKey: ['cura-profile-proposal-detail'] })
    client.invalidateQueries({ queryKey: ['labs'] })
  }
}

export function useSubmitCuraProfileProposal() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: (input: SubmitProfileProposalInput) =>
      apiClient.post<CuraProfileDetail>('/cura-profiles/proposals', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useResubmitCuraProfileProposal() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: ({ id, ...input }: ResubmitProfileProposalInput) =>
      apiClient.put<CuraProfileDetail>(`/cura-profiles/proposals/${id}`, input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useReviewCuraProfileProposal() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: ({ id, approve, reason }: { id: string; approve: boolean; reason?: string | null }) =>
      apiClient
        .post<CuraProfileDetail>(`/cura-profiles/proposals/${id}/review`, { approve, reason: reason || null })
        .then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useSetCuraProfileActive() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      apiClient.patch<CuraProfileDetail>(`/cura-profiles/${id}/active`, { isActive }).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useImportCuraProfile() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: (input: ImportCuraProfileInput) =>
      apiClient.post<{ id: string; name: string }>('/cura-profiles', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useMatchMachineProfiles() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: (machineId: string) =>
      apiClient.post<Machine>(`/cura-profiles/machines/${machineId}/match`, {}).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useReviewMachineProfile() {
  const invalidate = useInvalidateProfileQueries()
  return useMutation({
    mutationFn: ({ machineId, approve, reason }: { machineId: string; approve: boolean; reason?: string | null }) =>
      apiClient
        .post<Machine>(`/cura-profiles/machines/${machineId}/review`, { approve, reason: reason || null })
        .then((r) => r.data),
    onSuccess: invalidate,
  })
}
