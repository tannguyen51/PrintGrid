import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

export interface AuditLogEntry {
  id: string
  actorId?: string | null
  actorDisplay: string
  actorRole: string
  actionClass: string
  action: string
  entityType: string
  entityId?: string | null
  oldValuesJson?: string | null
  newValuesJson?: string | null
  reason?: string | null
  ipAddress?: string | null
  traceId?: string | null
  createdAtUtc: string
}

export interface AuditLogPage {
  items: AuditLogEntry[]
  total: number
  page: number
  pageSize: number
}

export interface AuditLogFilters {
  actor: string
  actionClass: string
  fromUtc: string
  toUtc: string
  page: number
  pageSize: number
}

function params(filters: AuditLogFilters, includePaging = true) {
  const result: Record<string, string | number> = {}
  if (filters.actor.trim()) result.actor = filters.actor.trim()
  if (filters.actionClass) result.actionClass = filters.actionClass
  if (filters.fromUtc) result.fromUtc = new Date(filters.fromUtc).toISOString()
  if (filters.toUtc) result.toUtc = new Date(filters.toUtc).toISOString()
  if (includePaging) {
    result.page = filters.page + 1
    result.pageSize = filters.pageSize
  }
  return result
}

export function useAuditLogs(filters: AuditLogFilters) {
  return useQuery({
    queryKey: ['admin', 'audit-logs', filters],
    queryFn: () => apiClient.get<AuditLogPage>('/audit-logs', { params: params(filters) }).then((r) => r.data),
  })
}

export async function exportAuditLogs(filters: AuditLogFilters) {
  const response = await apiClient.get<Blob>('/audit-logs/export', {
    params: params(filters, false),
    responseType: 'blob',
  })
  const url = URL.createObjectURL(response.data)
  const link = document.createElement('a')
  link.href = url
  link.download = `printgrid-audit-${new Date().toISOString().slice(0, 10)}.csv`
  link.click()
  URL.revokeObjectURL(url)
}
