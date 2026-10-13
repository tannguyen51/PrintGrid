import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface InspectionChecklistItem {
  code: string
  name: string
  passCriteria: string
  isMandatory: boolean
  displayOrder: number
}

export interface InspectionChecklistVersion {
  id: string
  qualityGradeCode: string
  versionNumber: number
  items: InspectionChecklistItem[]
  isActive: boolean
  createdAtUtc: string
}

export interface DefectTaxonomyItem {
  id: string
  code: string
  name: string
  description?: string | null
  defaultFaultAttribution?: 'Lab' | 'Hub' | 'Customer' | null
  isDeprecated: boolean
  createdAtUtc: string
  updatedAtUtc?: string | null
}

const keys = {
  checklists: ['admin', 'qc', 'checklists'] as const,
  defects: ['admin', 'qc', 'defects'] as const,
}

export function useInspectionChecklists() {
  return useQuery({
    queryKey: keys.checklists,
    queryFn: async () => (await apiClient.get<InspectionChecklistVersion[]>('/admin/qc/checklists')).data,
  })
}

export function useCreateInspectionChecklist() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async ({ grade, items }: { grade: string; items: InspectionChecklistItem[] }) =>
      (await apiClient.post<InspectionChecklistVersion>(`/admin/qc/checklists/${encodeURIComponent(grade)}`, { items })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: keys.checklists }),
  })
}

export function useDefectTaxonomy() {
  return useQuery({
    queryKey: keys.defects,
    queryFn: async () => (await apiClient.get<DefectTaxonomyItem[]>('/admin/qc/defects?includeDeprecated=true')).data,
  })
}

export function useSaveDefectTaxonomyItem() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: async ({ code, ...body }: Omit<DefectTaxonomyItem, 'id' | 'createdAtUtc' | 'updatedAtUtc'>) =>
      (await apiClient.put<DefectTaxonomyItem>(`/admin/qc/defects/${encodeURIComponent(code)}`, body)).data,
    onSuccess: () => client.invalidateQueries({ queryKey: keys.defects }),
  })
}

export function useDeleteDefectTaxonomyItem() {
  const client = useQueryClient()
  return useMutation({
    mutationFn: (code: string) => apiClient.delete(`/admin/qc/defects/${encodeURIComponent(code)}`),
    onSuccess: () => client.invalidateQueries({ queryKey: keys.defects }),
  })
}
