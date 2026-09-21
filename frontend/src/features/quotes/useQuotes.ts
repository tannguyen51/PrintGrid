import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { Quote, QuoteConfigInput } from './quoteTypes'
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

export interface PlaceOrderInput {
  quoteId: string
  street: string
  ward: string
  district: string
  city: string
  postalCode: string
}

export function usePlaceOrder() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: PlaceOrderInput) => apiClient.post<Order>('/orders', input).then((r) => r.data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  })
}