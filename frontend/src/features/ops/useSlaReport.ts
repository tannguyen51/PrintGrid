import { useMutation, useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

/** FR-ANAL-002/003 — 90-day SLA report (BR-PERF-001/002). Rates are 0..1 fractions. */
export interface SlaLabRow {
  labId: string | null
  labName: string
  onTimeDeliveryRate: number
  firstPassYield: number
  acceptanceRate: number
  utilizationRate: number
  completedJobs: number
  assignedJobs: number
  lateJobs: number
  isProvisional: boolean
}

export interface SlaReport {
  generatedAtUtc: string
  windowStartUtc: string
  windowEndUtc: string
  windowDays: number
  labFilter: string | null
  network: SlaLabRow
  labs: SlaLabRow[]
}

const SLA_KEY = ['sla', 'report'] as const

export function useSlaReport(labId?: string | null) {
  return useQuery({
    queryKey: [...SLA_KEY, labId ?? 'all'],
    queryFn: () =>
      apiClient
        .get<SlaReport>('/sla/report', { params: labId ? { labId } : undefined })
        .then((r) => r.data),
  })
}

/**
 * GET /sla/report/export?labId= — triggers a file download. The backend returns the CSV
 * with a UTF-8 BOM so Excel renders Vietnamese lab names correctly.
 */
export function useExportSlaReport() {
  return useMutation({
    mutationFn: async (labId?: string | null) => {
      const response = await apiClient.get<Blob>('/sla/report/export', {
        params: labId ? { labId } : undefined,
        responseType: 'blob',
      })

      const disposition = response.headers['content-disposition'] as string | undefined
      const match = disposition?.match(/filename="?([^"]+)"?/)
      const fileName = match?.[1] ?? `printgrid-sla-${new Date().toISOString().slice(0, 10)}.csv`

      const url = window.URL.createObjectURL(new Blob([response.data], { type: 'text/csv;charset=utf-8' }))
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
    },
  })
}
