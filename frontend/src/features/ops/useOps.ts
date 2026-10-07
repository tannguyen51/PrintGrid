import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'

const OPS_KEY = ['ops'] as const

export type OpsEscalationKind = 'ReprintLimitExceeded' | 'NoFeasibleSlot'
export type OpsEscalationStatus = 'Open' | 'Resolved'

/** GET /ops/escalations — reprints past the cap and jobs with no feasible slot (BR-SCHED-011). */
export interface OpsEscalation {
  id: string
  jobId: string
  orderItemId: string
  kind: OpsEscalationKind
  reason: string
  status: OpsEscalationStatus
  createdAtUtc: string
}

export function useOpsEscalations() {
  return useQuery({
    queryKey: [...OPS_KEY, 'escalations'],
    queryFn: () => apiClient.get<OpsEscalation[]>('/ops/escalations').then((r) => r.data),
  })
}

// ── Decision trace (FR-SCHED-009) ────────────────────────────────────────────
// candidates/ranking/scoringConfig are stored as arbitrary JSON snapshots whose
// shape is an audit contract (DecisionSnapshotJson.cs). They are typed `unknown`
// here and parsed defensively at render time.

export interface CapableCandidate {
  labId: string
  labName: string
  machineId: string
  machineName: string
  speedFactor: number
}

export interface RejectedMachine {
  machineId: string
  reason: string
}

export interface CandidatesSnapshot {
  excludedLabId?: string | null
  capable?: CapableCandidate[]
  rejected?: RejectedMachine[]
}

export interface RankingEntry {
  labId: string
  machineId: string
  plannedStartUtc: string
  plannedEndUtc: string
  estimatedCost: number
  machineUtilization: number
  score: number
  breakdown?: Record<string, number>
}

export interface ScoringConfigSnapshot {
  version?: string
  weights?: Record<string, number>
}

export interface DecisionEntry {
  id: string
  createdAtUtc: string
  attemptNumber: number
  /** 'initial_assign' | 'lab_decline' | 'print_failure' | 'urgent_reprint' | 'ops_override' | 'date_change_probe' */
  trigger: string
  actorType: string
  actorId: string | null
  /** 'assigned' | 'no_capable_machine' | 'no_feasible_slot' | 'placement_race' | 'rejected_by_aggregate' | 'probe' */
  outcome: string
  reason: string | null
  chosenLabId: string | null
  chosenMachineId: string | null
  chosenScore: number | null
  scoringConfigVersion: string
  timeBudgetMs: number
  elapsedMs: number
  budgetExceeded: boolean
  candidates: unknown
  ranking: unknown
  scoringConfig: unknown
}

export interface DecisionLog {
  jobId: string
  decisions: DecisionEntry[]
}

/** GET /ops/decisions/{jobId}. The 404 when the job has no log is a normal audit answer, so no retry. */
export function useDecisionLog(jobId: string | null) {
  return useQuery({
    queryKey: [...OPS_KEY, 'decisions', jobId],
    queryFn: () => apiClient.get<DecisionLog>(`/ops/decisions/${jobId}`).then((r) => r.data),
    enabled: Boolean(jobId),
    retry: false,
  })
}
