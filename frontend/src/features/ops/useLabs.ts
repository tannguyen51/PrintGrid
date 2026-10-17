import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type {
  Lab,
  LabPerformance,
  LabTimeline,
  MachineStatus,
  RegisterLabInput,
  RegisterMachineInput,
  ScheduleMaintenanceInput,
  SelfRegisterLabInput,
  UpdateMachineInput,
} from './labTypes'

const LABS_KEY = ['labs'] as const
const MY_LAB_KEY = ['labs', 'me'] as const

export function useLabs(includeInactive = false) {
  return useQuery({
    queryKey: [...LABS_KEY, includeInactive],
    queryFn: () =>
      apiClient.get<Lab[]>(`/labs?includeInactive=${includeInactive}`).then((r) => r.data),
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
      apiClient.post(`/labs/${labId}/machines`, input).then((r) => r.data),
    onSuccess: invalidate,
  })
}

export function useUpdateMachine() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: ({ labId, machineId, ...input }: UpdateMachineInput) =>
      apiClient.put(`/labs/${labId}/machines/${machineId}`, input).then((r) => r.data),
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