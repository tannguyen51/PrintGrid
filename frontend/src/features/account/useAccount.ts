import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { AddressInput, CustomerAddress, CustomerProfile } from './accountTypes'

const ACCOUNT_KEY = ['account'] as const
const ADDRESSES_KEY = ['account', 'addresses'] as const

export function useProfile() {
  return useQuery({ queryKey: ACCOUNT_KEY, queryFn: () => apiClient.get<CustomerProfile>('/me').then(r => r.data) })
}

export function useUpdateProfile() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (input: Pick<CustomerProfile, 'fullName' | 'phoneNumber'>) => apiClient.put<CustomerProfile>('/me', input).then(r => r.data),
    onSuccess: data => client.setQueryData(ACCOUNT_KEY, data),
  })
}

export function useAddresses() {
  return useQuery({ queryKey: ADDRESSES_KEY, queryFn: () => apiClient.get<CustomerAddress[]>('/me/addresses').then(r => r.data) })
}

function useRefreshAddresses() {
  const client = useQueryClient()
  return () => client.invalidateQueries({ queryKey: ADDRESSES_KEY })
}

export function useSaveAddress() {
  const refresh = useRefreshAddresses()
  return useMutation({
    mutationFn: ({ id, input }: { id?: string; input: AddressInput }) => id
      ? apiClient.put<CustomerAddress>(`/me/addresses/${id}`, input).then(r => r.data)
      : apiClient.post<CustomerAddress>('/me/addresses', input).then(r => r.data),
    onSuccess: refresh,
  })
}

export function useDeleteAddress() {
  const refresh = useRefreshAddresses()
  return useMutation({ mutationFn: (id: string) => apiClient.delete(`/me/addresses/${id}`), onSuccess: refresh })
}
