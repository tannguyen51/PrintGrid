import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Quote, QuoteConfigInput, ModelSummary } from './quoteTypes'
import type { Order } from '../../shared/types/order'

const QUOTES_KEY = ['quotes'] as const

export function useQuotes() {
  return useQuery({
    queryKey: QUOTES_KEY,
    queryFn: () => apiClient.get<Quote[]>('/quotes').then((r) => r.data),
    refetchOnWindowFocus: true,
    refetchInterval: query => query.state.data?.some(q => q.status === 'PendingReview' || q.status === 'Pending') ? 15_000 : false,
  })
}

/** Keep an open quote current while Ops reviews it, without changing the customer's form. */
export function useQuote(quoteId: string | null) {
  return useQuery({
    queryKey: ['quote', quoteId],
    enabled: !!quoteId,
    queryFn: () => apiClient.get<Quote>(`/quotes/${quoteId}`).then(r => r.data),
    refetchOnWindowFocus: true,
    refetchInterval: query => ['PendingReview', 'Pending'].includes(query.state.data?.status ?? '') ? 5_000 : false,
  })
}

export function useCreateQuote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: QuoteConfigInput) => apiClient.post<Quote>('/quotes', input).then((r) => r.data),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: QUOTES_KEY }); void queryClient.invalidateQueries({ queryKey: ['quote-history'] }) },
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
  requestedDeliveryDate: string
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
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['orders'] }); void queryClient.invalidateQueries({ queryKey: ['quote-history'] }); void queryClient.invalidateQueries({ queryKey: ['quote'] }) },
  })
}

export interface QuoteHistoryFilters {
  status: string; search: string; from: string; to: string; sort: string; page: number; pageSize: number
}
export interface QuoteHistoryPage {
  items: Quote[]; totalCount: number; page: number; pageSize: number
  totalQuotes: number; draftQuotes: number; pendingReviewQuotes: number; readyQuotes: number
}
export function useQuoteHistory(modelId: string, filters: QuoteHistoryFilters) {
  return useQuery({
    queryKey: ['quote-history', modelId, filters],
    placeholderData: keepPreviousData,
    enabled: !filters.from || !filters.to || filters.from <= filters.to,
    queryFn: () => apiClient.get<QuoteHistoryPage>('/quotes/history', { params: { modelId, ...filters,
      from: filters.from || undefined, to: filters.to || undefined, search: filters.search || undefined } }).then(r => r.data),
    refetchOnWindowFocus: true,
    refetchInterval: query => query.state.data?.items.some(q => q.status === 'PendingReview') ? 15_000 : false,
  })
}
