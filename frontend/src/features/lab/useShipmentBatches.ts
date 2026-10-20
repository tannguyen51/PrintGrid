import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface ShipmentJob {
  id: string
  internalDueDate: string
  weightGrams: number
  quantity: number
  materialCode: string
  proofApprovedAtUtc?: string | null
}

export interface ShipmentBatchSummary {
  id: string
  scheduledTransferDate: string
  waybillNumber: string
  status: string
  jobCount: number
  totalWeightGrams: number
  transportAllowance: number
  currency: string
  sealedAtUtc: string
}

export interface ShipmentPlanner {
  labId: string
  labName: string
  transportZone: string
  transferDaysOfWeek: string
  nextTransferDate: string
  eligibleJobs: ShipmentJob[]
  recentBatches: ShipmentBatchSummary[]
}

export interface AllowancePreview {
  zone: string
  weightClass: string
  totalWeightGrams: number
  zoneRate: number
  weightMultiplier: number
  allowance: number
  currency: string
}

const key = (labId: string) => ['lab-shipment-planner', labId] as const

export function useShipmentPlanner(labId: string) {
  return useQuery({
    queryKey: key(labId),
    queryFn: () => apiClient.get<ShipmentPlanner>(`/lab/shipment-batches/planner?labId=${labId}`).then(r => r.data),
    enabled: Boolean(labId),
  })
}

export function usePreviewShipment() {
  return useMutation({
    mutationFn: (selection: { labId: string; jobIds: string[] }) =>
      apiClient.post<AllowancePreview>('/lab/shipment-batches/preview', selection).then(r => r.data),
  })
}

export function useSealShipment(labId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (jobIds: string[]) =>
      apiClient.post<ShipmentBatchSummary>('/lab/shipment-batches/seal', { labId, jobIds }).then(r => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(labId) }),
  })
}
