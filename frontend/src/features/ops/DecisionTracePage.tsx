import { useState, type FormEvent, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import type { AxiosError } from 'axios'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { FactCheckRounded, LogoutRounded, SearchRounded } from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import {
  useDecisionLog,
  type CandidatesSnapshot,
  type DecisionEntry,
  type RankingEntry,
  type ScoringConfigSnapshot,
} from './useOps'

const TRIGGER_LABELS: Record<string, string> = {
  initial_assign: 'Gán ban đầu',
  lab_decline: 'Lab từ chối',
  print_failure: 'In hỏng',
  urgent_reprint: 'In lại khẩn',
  ops_override: 'Ops can thiệp',
  date_change_probe: 'Thăm dò đổi ngày',
}

const OUTCOME_META: Record<string, { label: string; color: 'success' | 'error' | 'warning' | 'info' | 'default' }> = {
  assigned: { label: 'Đã gán', color: 'success' },
  no_capable_machine: { label: 'Không có máy đáp ứng', color: 'error' },
  no_feasible_slot: { label: 'Không còn khung giờ', color: 'error' },
  placement_race: { label: 'Xung đột khi đặt lịch', color: 'warning' },
  rejected_by_aggregate: { label: 'Bị job từ chối', color: 'default' },
  probe: { label: 'Chỉ thăm dò', color: 'info' },
}

const ACTOR_LABELS: Record<string, string> = {
  system: 'Hệ thống',
  ops: 'Nhân viên ops',
}

/** Breakdown keys written by AssignmentScorer.Score (snake_case). */
const CRITERION_LABELS: Record<string, string> = {
  due_date_slack: 'Dư thời gian',
  lab_reliability: 'Độ tin cậy lab',
  load_balance: 'Cân tải',
  cost: 'Chi phí',
  transit: 'Vận chuyển',
}

/** Weights keys written by DecisionSnapshotJson.ScoringConfig (camelCase). */
const WEIGHT_LABELS: Record<string, string> = {
  dueDateSlack: 'Dư thời gian',
  labReliability: 'Độ tin cậy lab',
  loadBalance: 'Cân tải',
  cost: 'Chi phí',
  transit: 'Vận chuyển',
}

const fmtDateTime = (d: string | null | undefined) =>
  d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'
const fmtScore = (n: number | null | undefined) => (typeof n === 'number' ? n.toFixed(3) : '—')
const fmtMoney = (n: number) => `${n.toLocaleString('vi-VN', { maximumFractionDigits: 0 })} đ`
const fmtPercent = (n: number) => `${Math.round(n * 100)}%`
const shortId = (id: string | null | undefined) => (id ? id.slice(0, 8) : '—')

// ── Defensive parsing of the audit JSON snapshots ────────────────────────────

function asCandidates(value: unknown): CandidatesSnapshot | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  const c = value as CandidatesSnapshot
  if (c.capable === undefined && c.rejected === undefined) return null
  if (c.capable !== undefined && !Array.isArray(c.capable)) return null
  if (c.rejected !== undefined && !Array.isArray(c.rejected)) return null
  return c
}

function asRanking(value: unknown): RankingEntry[] | null {
  if (!Array.isArray(value)) return null
  return value.every((x) => x && typeof x === 'object' && typeof (x as RankingEntry).score === 'number')
    ? (value as RankingEntry[])
    : null
}

function asScoringConfig(value: unknown): ScoringConfigSnapshot | null {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return null
  return value as ScoringConfigSnapshot
}

/** Raw JSON escape hatch — used whenever a snapshot does not match the audit shape. */
function JsonPre({ value }: { value: unknown }) {
  return (
    <Box
      component="pre"
      sx={{
        m: 0,
        p: 1.5,
        borderRadius: 2,
        border: '1px solid',
        borderColor: 'divider',
        bgcolor: 'rgba(255,255,255,0.04)',
        color: 'text.secondary',
        fontSize: '0.75rem',
        overflowX: 'auto',
      }}
    >
      {JSON.stringify(value, null, 2)}
    </Box>
  )
}

function SectionTitle({ children }: { children: ReactNode }) {
  return (
    <Typography variant="overline" color="text.secondary" sx={{ letterSpacing: '0.12em' }}>
      {children}
    </Typography>
  )
}

const cellNowrap = { whiteSpace: 'nowrap' as const }

/** One decision card: outcome, choice, timing, then candidates / ranking / weights. */
function DecisionCard({ decision: d }: { decision: DecisionEntry }) {
  const candidates = asCandidates(d.candidates)
  const ranking = asRanking(d.ranking)
  const config = asScoringConfig(d.scoringConfig)
  const outcome = OUTCOME_META[d.outcome]
  const chosen = candidates?.capable?.find((c) => c.machineId === d.chosenMachineId) ?? null
  const nameFor = (labId: string, machineId: string): string => {
    const hit = candidates?.capable?.find((c) => c.machineId === machineId)
    return hit ? `${hit.labName} · ${hit.machineName}` : `${shortId(labId)} · ${shortId(machineId)}`
  }

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        {/* ── Header: outcome + trigger + time ── */}
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} spacing={1}>
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
            <Chip size="small" label={`Lần thử #${d.attemptNumber}`} />
            <Chip size="small" color={outcome?.color ?? 'default'} label={outcome?.label ?? d.outcome} />
            <Typography variant="body2" color="text.secondary">
              {TRIGGER_LABELS[d.trigger] ?? d.trigger} · {ACTOR_LABELS[d.actorType] ?? d.actorType}
            </Typography>
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={cellNowrap}>
            {fmtDateTime(d.createdAtUtc)}
          </Typography>
        </Stack>

        {/* ── Choice + timing summary ── */}
        <Box sx={{ p: 1.5, borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
          <Typography sx={{ fontWeight: 700 }}>
            {d.chosenMachineId ? `Máy chốt: ${chosen ? `${chosen.labName} · ${chosen.machineName}` : shortId(d.chosenMachineId)} — điểm ${fmtScore(d.chosenScore)}` : 'Không có máy nào được chốt'}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Kỳ cấu hình {d.scoringConfigVersion} · chạy {d.elapsedMs} ms / ngân sách {d.timeBudgetMs} ms
            {d.actorId ? ` · người tác động ${shortId(d.actorId)}` : ''}
          </Typography>
          {d.budgetExceeded ? <Chip size="small" color="warning" label="Vượt ngân sách thời gian" sx={{ mt: 1 }} /> : null}
          {d.reason ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              Lý do: {d.reason}
            </Typography>
          ) : null}
        </Box>

        {/* ── Candidates ── */}
        <Box>
          <SectionTitle>Máy đủ điều kiện</SectionTitle>
          {candidates ? (
            <Stack spacing={1.5} sx={{ mt: 0.5 }}>
              {candidates.excludedLabId ? (
                <Typography variant="caption" color="warning.main">
                  Lab bị loại trừ theo yêu cầu: {shortId(candidates.excludedLabId)}
                </Typography>
              ) : null}
              {(candidates.capable?.length ?? 0) > 0 ? (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Lab</TableCell>
                      <TableCell>Máy</TableCell>
                      <TableCell align="right">Hệ số tốc độ</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {candidates.capable?.map((c) => (
                      <TableRow key={c.machineId}>
                        <TableCell>{c.labName}</TableCell>
                        <TableCell>{c.machineName}</TableCell>
                        <TableCell align="right">{c.speedFactor.toFixed(2)}×</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <Typography variant="body2" color="text.secondary">Không máy nào qua bộ lọc năng lực.</Typography>
              )}
              {(candidates.rejected?.length ?? 0) > 0 ? (
                <>
                  <SectionTitle>Máy bị loại</SectionTitle>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Máy</TableCell>
                        <TableCell>Lý do</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {candidates.rejected?.map((r, i) => (
                        <TableRow key={`${r.machineId}-${i}`}>
                          <TableCell sx={cellNowrap}>{shortId(r.machineId)}</TableCell>
                          <TableCell>
                            <Typography variant="body2">{r.reason}</Typography>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </>
              ) : null}
            </Stack>
          ) : (
            <Box sx={{ mt: 0.5 }}>
              <JsonPre value={d.candidates} />
            </Box>
          )}
        </Box>

        {/* ── Ranking ── */}
        <Box>
          <SectionTitle>Hạng xếp ứng viên</SectionTitle>
          {ranking ? (
            ranking.length > 0 ? (
              <Table size="small" sx={{ mt: 0.5 }}>
                <TableHead>
                  <TableRow>
                    <TableCell align="right">#</TableCell>
                    <TableCell>Lab · Máy</TableCell>
                    <TableCell sx={cellNowrap}>Bắt đầu</TableCell>
                    <TableCell sx={cellNowrap}>Kết thúc</TableCell>
                    <TableCell align="right">Chi phí</TableCell>
                    <TableCell align="right">Tải máy</TableCell>
                    <TableCell align="right">Điểm</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {ranking.map((r, i) => {
                    const breakdownText = Object.entries(r.breakdown ?? {})
                      .map(([k, v]) => `${CRITERION_LABELS[k] ?? k} ${Number(v).toFixed(2)}`)
                      .join(' · ')
                    return (
                      <TableRow key={`${r.machineId}-${i}`} sx={i === 0 ? { bgcolor: 'rgba(255,255,255,0.04)' } : undefined}>
                        <TableCell align="right">{i + 1}</TableCell>
                        <TableCell>{nameFor(r.labId, r.machineId)}</TableCell>
                        <TableCell sx={cellNowrap}>{fmtDateTime(r.plannedStartUtc)}</TableCell>
                        <TableCell sx={cellNowrap}>{fmtDateTime(r.plannedEndUtc)}</TableCell>
                        <TableCell align="right" sx={cellNowrap}>{fmtMoney(r.estimatedCost)}</TableCell>
                        <TableCell align="right">{fmtPercent(r.machineUtilization)}</TableCell>
                        <TableCell align="right">
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>{r.score.toFixed(3)}</Typography>
                          {breakdownText ? (
                            <Typography variant="caption" color="text.secondary" display="block">
                              {breakdownText}
                            </Typography>
                          ) : null}
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            ) : (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>Lần chạy này không có ứng viên nào được chấm điểm.</Typography>
            )
          ) : (
            <Box sx={{ mt: 0.5 }}>
              <JsonPre value={d.ranking} />
            </Box>
          )}
        </Box>

        {/* ── Frozen scoring weights ── */}
        <Box>
          <SectionTitle>Trọng số đóng băng (kỳ {config?.version ?? d.scoringConfigVersion})</SectionTitle>
          {config?.weights ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {Object.entries(config.weights).map(([k, v]) => `${WEIGHT_LABELS[k] ?? k}: ${Number(v).toFixed(2)}`).join(' · ')}
            </Typography>
          ) : (
            <Box sx={{ mt: 0.5 }}>
              <JsonPre value={d.scoringConfig} />
            </Box>
          )}
        </Box>
      </Stack>
    </Paper>
  )
}

/**
 * FR-SCHED-009 audit surface: why the assignment engine chose a lab/machine for a job,
 * with the frozen candidate set, per-criterion scores and the config era of each run.
 */
export default function DecisionTracePage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const activeJobId = searchParams.get('jobId')
  const [draft, setDraft] = useState(activeJobId ?? '')
  const log = useDecisionLog(activeJobId)

  const serverMessage = (() => {
    if (!log.isError) return null
    const err = log.error as AxiosError<{ error?: { message?: string } }>
    if (err.response?.status === 404) {
      return err.response.data?.error?.message ?? 'Không tìm thấy vết quyết định cho job này — có thể job chưa đi qua engine.'
    }
    return 'Không tải được vết quyết định.'
  })()

  function submit(e: FormEvent) {
    e.preventDefault()
    const v = draft.trim()
    setSearchParams(v ? { jobId: v } : {})
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Vết quyết định — Ops
              </Typography>
              <Typography color="text.secondary">Lý do engine xếp lịch chọn lab/máy cho từng job (FR-SCHED-009) — chỉ đọc</Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1} alignItems="center">
            <Button variant="text" color="inherit" onClick={() => navigate('/ops/escalations')}>
              Hàng đợi cảnh báo
            </Button>
            <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/', { replace: true }) }}>
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {/* ── Lookup ── */}
        <Stack component="form" direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ xs: 'stretch', sm: 'flex-start' }} onSubmit={submit}>
          <TextField
            label="Mã job"
            placeholder="ví dụ 3f2a9c1e-…"
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            size="small"
            sx={{ flex: 1, minWidth: 260 }}
            helperText="Lấy từ hàng đợi cảnh báo hoặc danh sách job"
          />
          <Button type="submit" variant="contained" startIcon={<SearchRounded />} disabled={!draft.trim()}>
            Tra vết
          </Button>
        </Stack>

        {serverMessage ? <Alert severity={log.error && (log.error as AxiosError).response?.status === 404 ? 'info' : 'error'}>{serverMessage}</Alert> : null}
        {log.isFetching && !log.isError ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!activeJobId && !log.isFetching ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactCheckRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Nhập mã job để xem vì sao engine xếp lịch như vậy.</Typography>
          </Box>
        ) : null}

        {log.data && log.data.decisions.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactCheckRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có quyết định nào được ghi cho job {shortId(log.data.jobId)}.</Typography>
          </Box>
        ) : null}

        {log.data && log.data.decisions.length > 0 ? (
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              Job {log.data.jobId} · {log.data.decisions.length} quyết định được ghi
            </Typography>
            {log.data.decisions.map((d) => (
              <DecisionCard key={d.id} decision={d} />
            ))}
          </Stack>
        ) : null}
      </Stack>
    </Box>
  )
}
