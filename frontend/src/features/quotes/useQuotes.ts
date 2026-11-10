import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Quote, QuoteConfigInput, ModelSummary } from './quoteTypes'
import type { Order } from '../../shared/types/order'

const QUOTES_KEY = ['quotes'] as const

export function useQuotes() {
  return useQuery({
    queryKey: QUOTES_KEY,
    queryFn: () => apiClient.get<Quote[]>('/quotes').then((r) => r.data),
  })
}

export function useCreateQuote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: QuoteConfigInput) => apiClient.post<Quote>('/quotes', input).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: QUOTES_KEY }),
  })
}

/** BR-04: fetch eligible machine models (≥3 machines from ≥2 labs). */
export function useEligibleModels(materialCode?: string) {
  return useQuery<ModelSummary[]>({
    queryKey: ['eligible-models', materialCode],
    queryFn: () => apiClient.get<ModelSummary[]>('/quotes/models', { params: materialCode ? { materialCode } : {} }).then(r => r.data),
    enabled: !!materialCode,
  })
}

export interface PlaceOrderInput {
  quoteId: string
  street: string
  ward: string
  district: string
  city: string
  postalCode: string
  /** FR-CUST-007 — server rejects the order without this (validation_error). */
  acceptTerms: boolean
  // Doc §6.1 / §6.2 — machine selection context (BR-03/04/05/09)
  techConstraintsJson?: string | null
  modelBindingKind?: string
  pinnedModelId?: string | null
  referenceLineId?: string | null
}

export function usePlaceOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: PlaceOrderInput) => apiClient.post<Order>('/orders', input).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  })
}