import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface LabFinanceJob {
  jobId: string; orderId: string; orderNumber: string; materialCode: string; technology: string
  jobType: string; quantity: number; jobStatus: string; completedAtUtc: string | null; createdAtUtc: string
  jobValue: number; labAmount: number; hubAmount: number; deductionAmount: number; netAmount: number
  currency: string; paymentStatus: string; paidAtUtc: string | null; paymentReference: string | null
}
export interface LabFinanceReport {
  labId: string; labName: string; labRate: number; hubRate: number
  summary: {
    completedJobs: number; completedProducts: number; reprintJobs: number; productionValue: number
    labGrossAmount: number; hubAmount: number; deductions: number; totalIncome: number
    totalReceived: number; outstanding: number; collectionRate: number; pendingSyncJobs: number
  }
  byType: Array<{ materialCode: string; technology: string; jobType: string; jobCount: number; quantity: number; income: number; received: number }>
  items: LabFinanceJob[]; totalCount: number
}
export interface LabFinanceFilters {
  from?: string; to?: string; scope?: 'completed' | 'all'; paymentStatus?: string
  search?: string; page: number; pageSize: number
}
export function useLabFinance(filters: LabFinanceFilters, enabled = true) {
  return useQuery({
    queryKey: ['lab-finance', filters], enabled,
    queryFn: () => apiClient.get<LabFinanceReport>('/lab/finance', { params: filters }).then(r => r.data),
    refetchOnWindowFocus: true,
  })
}
