import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { PricingDetail, PublicPricing } from './pricingTypes'

/** Anonymous ranges — always safe to call, guest or customer. */
export function usePricingPreview() {
  return useQuery({
    queryKey: ['pricing', 'preview'],
    queryFn: async () => (await apiClient.get<PublicPricing>('/pricing/preview')).data,
    staleTime: 60_000,
  })
}

/** Exact parameter table — Customer only (401 for guests, so the hook stays disabled). */
export function usePricingDetail(enabled: boolean) {
  return useQuery({
    queryKey: ['pricing', 'detail'],
    queryFn: async () => (await apiClient.get<PricingDetail>('/pricing')).data,
    enabled,
    staleTime: 60_000,
  })
}
