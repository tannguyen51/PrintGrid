import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { PagedResponse } from '../../shared/types/pagination'
import type {
  Lab,
  LabOption,
  LabPerformance,
  LabTimeline,
  Machine,
  MachineListItem,
  MachineStatus,
  PagedLabsFilter,
  PagedMachinesFilter,
  RegisterLabInput,
  RegisterMachineInput,
  ScheduleMaintenanceInput,
  SelfRegisterLabInput,
  UpdateMachineInput,
} from './labTypes'

const LABS_KEY = ['labs'] as const
const MY_LAB_KEY = ['labs', 'me'] as const

function compactParams(record: Record<string, string | number | boolean | undefined>) {
  const out: Record<string, string | number | boolean> = {}
  for (const [key, value] of Object.entries(record)) {
    if (value !== undefined && value !== '') {
      out[key] = typeof value === 'string' ? value.trim() : value
    }
  }
  return out
}

export function useLabs(includeInactive = false) {
  return useQuery({
    queryKey: [...LABS_KEY, includeInactive],
    queryFn: () =>
      apiClient.get<Lab[]>(`/labs?includeInactive=${includeInactive}`).then((r) => r.data),
  })
}

export function useLabOptions(enabled = true) {
  return useQuery({
    queryKey: [...LABS_KEY, 'options'],
    enabled,
    queryFn: () => apiClient.get<LabOption[]>('/labs/options').then((r) => r.data),
  })
}

export function useLabsPaged(filters: PagedLabsFilter, enabled = true) {
  return useQuery({
    queryKey: [...LABS_KEY, 'paged', filters],
    enabled,
    placeholderData: (previous) => previous,
    queryFn: () =>
      apiClient
        .get<PagedResponse<Lab>>('/labs/paged', {
          params: compactParams({
            search: filters.search,
            approvalStatus: filters.approvalStatus,
            isActive: filters.isActive === '' ? undefined : filters.isActive,
            isInGoodStanding: filters.isInGoodStanding === '' ? undefined : filters.isInGoodStanding,
            manufacturerOrModel: filters.manufacturerOrModel,
            material: filters.material,
            includeInactive: filters.includeInactive ?? true,
            sortBy: filters.sortBy ?? 'pendingFirst',
            page: filters.page,
            pageSize: filters.pageSize,
          }),
        })
        .then((r) => r.data),
  })
}

export function useMachinesPaged(filters: PagedMachinesFilter, enabled = true) {
  return useQuery({
    queryKey: [...LABS_KEY, 'machines', 'paged', filters],
    enabled,
    placeholderData: (previous) => previous,
    queryFn: () =>
      apiClient
        .get<PagedResponse<MachineListItem>>('/labs/machines/paged', {
          params: compactParams({
            search: filters.search,
            labId: filters.labId,
            profileStatus: filters.profileStatus,
            operationalStatus: filters.operationalStatus,
            labStatus: filters.labStatus,
            manufacturerOrModel: filters.manufacturerOrModel,
            material: filters.material,
            technology: filters.technology,
            sortBy: filters.sortBy ?? 'pendingFirst',
            page: filters.page,
            pageSize: filters.pageSize,
          }),
        })
        .then((r) => r.data),
  })
}

export function useMyLab(allowUnclaimedFallback = true) {
  return useQuery({
    queryKey: [...MY_LAB_KEY, allowUnclaimedFallback],
    queryFn: () =>
      apiClient
        .get<Lab | null>(`/labs/me?allowUnclaimedFallback=${allowUnclaimedFallback}`)
        .then((r) => r.data || null),
  })
}

export function useLabTimeline(labId?: string | null) {
  return useQuery({
    queryKey: [...LABS_KEY, labId, 'timeline'],
    enabled: Boolean(labId),
    queryFn: () => apiClient.get<LabTimeline>(`/labs/${labId}/timeline`).then((r) => r.data),
  })
}

export function useLabPerformance(labId?: string | null) {
  return useQuery({
    queryKey: [...LABS_KEY, labId, 'performance'],
    enabled: Boolean(labId),
    queryFn: () => apiClient.get<LabPerformance>(`/labs/${labId}/performance`).then((r) => r.data),
  })
}

function useInvalidateLabs() {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: LABS_KEY })
  }
}

export function useCreateLab() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: (input: RegisterLabInput) => apiClient.post<Lab>('/labs', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useSelfRegisterLab() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: (input: SelfRegisterLabInput) =>
      apiClient.post<Lab>('/labs/self-register', input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useApproveLab() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: (labId: string) => apiClient.post<Lab>(`/labs/${labId}/approve`).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useRejectLab() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, reason }: { labId: string; reason: string }) =>
      apiClient.post<Lab>(`/labs/${labId}/reject`, { reason }).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useAddMachine() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, ...input }: RegisterMachineInput) =>
      apiClient.post<Machine>(`/labs/${labId}/machines`, input).then(r => r.data),
    onSuccess: invalidate,
  })
}

export function useUpdateMachine() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, machineId, ...input }: UpdateMachineInput) =>
      apiClient.put<Machine>(`/labs/${labId}/machines/${machineId}`, input).then(r => r.data),
    onSuccess: invalidate,
  })
}

export function useSyncMachineProfiles() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: (machineId: string) => apiClient.post<{
      machineId: string; status: string; errors: string[]
    }>(`/cura-library/machines/${machineId}/sync`).then(r => r.data),
    onSuccess: invalidate,
  })
}

export function useDeleteMachine() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, machineId }: { labId: string; machineId: string }) =>
      apiClient.delete(`/labs/${labId}/machines/${machineId}`),
    onSuccess: invalidate,
  })
}

export function useScheduleMachineMaintenance() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, machineId, ...input }: ScheduleMaintenanceInput) =>
      apiClient.put(`/labs/${labId}/machines/${machineId}/maintenance`, input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useSetLabActive() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, isActive }: { labId: string; isActive: boolean }) =>
      apiClient.patch(`/labs/${labId}/active`, { isActive }),
    onSuccess: invalidate,
  })
}

export function useSetMachineStatus() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, machineId, status }: { labId: string; machineId: string; status: MachineStatus }) =>
      apiClient.patch(`/labs/${labId}/machines/${machineId}/status`, { status }),
    onSuccess: invalidate,
  })
}

export function useSetLabStanding() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, isInGoodStanding }: { labId: string; isInGoodStanding: boolean }) =>
      apiClient.patch(`/labs/${labId}/standing`, { isInGoodStanding }),
    onSuccess: invalidate,
  })
}

export function useSetLabCapacityLimit() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, maxConcurrentJobs }: { labId: string; maxConcurrentJobs: number | null }) =>
      apiClient.patch(`/labs/${labId}/capacity-limit`, { maxConcurrentJobs }),
    onSuccess: invalidate,
  })
}
