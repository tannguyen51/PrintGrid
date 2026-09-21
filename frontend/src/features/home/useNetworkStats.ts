import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface NetworkStats {
  totalLabs: number
  activeLabs: number
  totalMachines: number
  fdmMachines: number
  slaMachines: number
  slsMachines: number
  averageOnTimeDeliveryRate: number
  averageFirstPassYield: number
}

/**
 * Live network summary from the public stats endpoint — no seeded or
 * hardcoded marketing figures.
 */
export function useNetworkStats() {
  return useQuery({
    queryKey: ['network-stats'],
    queryFn: async () => {
      const { data } = await apiClient.get<NetworkStats>('/network/stats')
      return data
    },
    staleTime: 60_000,
  })
}

export function formatNumber(value: number | undefined): string {
  if (value === undefined) return '–'
  return value.toLocaleString('vi-VN')
}