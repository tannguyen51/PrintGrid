import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Lab, MachineStatus, RegisterLabInput, RegisterMachineInput, UpdateMachineInput } from './labTypes'

const LABS_KEY = ['labs'] as const

export function useLabs(includeInactive = false) {
  return useQuery({
    queryKey: [...LABS_KEY, includeInactive],
    queryFn: () =>
      apiClient.get<Lab[]>(`/labs?includeInactive=${includeInactive}`).then((r) => r.data),
  })
}

function useInvalidateLabs() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: LABS_KEY })
}

export function useCreateLab() {
  const invalidate = useInvalidateLabs()
  return useMutation({
    mutationFn: (input: RegisterLabInput) => apiClient.post('/labs', input).then((r) => r.data),
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
