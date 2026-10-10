import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Lab, MachineStatus, RegisterLabInput, RegisterMachineInput } from './labTypes'

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