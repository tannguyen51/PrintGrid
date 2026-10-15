import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import type { DefectTaxonomyItem, InspectionChecklistVersion } from '../admin/useQcKnowledge'

export interface QcKnowledge {
  checklist: InspectionChecklistVersion
  defects: DefectTaxonomyItem[]
}

export function useQcKnowledge(layerHeightMm?: number) {
  return useQuery({
    queryKey: ['qc-knowledge', layerHeightMm],
    enabled: !!layerHeightMm,
    queryFn: async () => (await apiClient.get<QcKnowledge>('/qc-knowledge', { params: { layerHeightMm } })).data,
  })
}
