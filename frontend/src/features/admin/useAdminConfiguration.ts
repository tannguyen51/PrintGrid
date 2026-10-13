import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface CommercialParameters {
  materialRates: Record<string, number>
  materialDensities: Record<string, number>
  pricePerPrintHour: number
  hubHandlingFee: number
  platformMarginPercent: number
  prioritySurchargePercent: number
  autoLaneThreshold: number
  quoteValidityHours: number
  assignmentWeights: Record<string, number>
  maxBatchSize: number
  schedulingTimeBudgetSeconds: number
  slaRiskThresholdHours: number
}

export interface CommercialConfiguration {
  id: string; versionNumber: number; name: string; status: 'Draft' | 'Active' | 'Archived'
  parameters: CommercialParameters; changeSummary: string; createdAtUtc: string; activatedAtUtc?: string
}

export interface CatalogItem {
  id: string; catalogType: string; code: string; revision: number; name: string
  description?: string; unitRate?: number; isPremium: boolean; properties: Record<string, unknown>
  isDeprecated: boolean; createdAtUtc: string
}

export const configurationKeys = { commercial: ['admin', 'commercial-configurations'] as const, catalog: ['admin', 'catalog-items'] as const }

export function useCommercialConfigurations() {
  return useQuery({ queryKey: configurationKeys.commercial, queryFn: async () => (await apiClient.get<CommercialConfiguration[]>('/admin/commercial-configurations')).data })
}

export function useCreateCommercialConfiguration() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (body: { name: string; changeSummary: string; parameters: CommercialParameters }) =>
      (await apiClient.post<CommercialConfiguration>('/admin/commercial-configurations', body)).data,
    onSuccess: () => client.invalidateQueries({ queryKey: configurationKeys.commercial }),
  })
}

export function useActivateCommercialConfiguration() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => (await apiClient.post<CommercialConfiguration>(`/admin/commercial-configurations/${id}/activate`)).data,
    onSuccess: () => client.invalidateQueries({ queryKey: configurationKeys.commercial }),
  })
}

export function useCatalogItems() {
  return useQuery({ queryKey: configurationKeys.catalog, queryFn: async () => (await apiClient.get<CatalogItem[]>('/admin/catalog-items?includeDeprecated=true')).data })
}

export function useSaveCatalogItem() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async ({ type, code, ...body }: { type: string; code: string; name: string; description?: string; unitRate?: number; isPremium: boolean; properties: Record<string, unknown>; isDeprecated: boolean }) =>
      (await apiClient.put<CatalogItem>(`/admin/catalog-items/${encodeURIComponent(type)}/${encodeURIComponent(code)}`, body)).data,
    onSuccess: () => client.invalidateQueries({ queryKey: configurationKeys.catalog }),
  })
}

export function useDeleteCatalogItem() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: ({ type, code }: { type: string; code: string }) => apiClient.delete(`/admin/catalog-items/${encodeURIComponent(type)}/${encodeURIComponent(code)}`),
    onSuccess: () => client.invalidateQueries({ queryKey: configurationKeys.catalog }),
  })
}
