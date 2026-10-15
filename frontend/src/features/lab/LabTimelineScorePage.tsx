import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  Grid,
  InputLabel,
  LinearProgress,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import {
  AssessmentRounded,
  BuildCircleRounded,
  FactoryRounded,
  LogoutRounded,
  PrecisionManufacturingRounded,
  RefreshRounded,
  ScheduleRounded,
  ViewListRounded,
} from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useLabPerformance, useLabTimeline, useMyLab } from '../ops/useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  type MachineTimelineSlot,
} from '../ops/labTypes'

const fmtDateTime = (iso?: string | null) =>
  iso
    ? new Date(iso).toLocaleString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
      })
    : '—'

const pct = (v?: number) => `${Math.round((v ?? 0) * 100)}%`

/**
 * Machine Schedule Timeline (Gantt) + Lab Performance Score Dashboard
 * (FR-LAB-006 + FR-LAB-007 · BR-PERF-001/002/004):
 * - Gantt chart from scheduled machine slots (updated after rescheduling/repair).
 * - AC01: Completing a job immediately removes its slot from the future timeline.
 * - AC02: Performance score displays 4 metrics (ODR, FPY, Acceptance Rate, Utilization),
 *   marks < 10 completed jobs as Provisional Score (0.70), and shows anonymized network benchmark comparison.
 */
export default function LabTimelineScorePage() {
  const navigate = useNavigate()
  const { logout } = useAuth()

  const { data: myLab, isLoading: loadingLab } = useMyLab(true)
  const timelineQuery = useLabTimeline(myLab?.id)
  const perfQuery = useLabPerformance(myLab?.id)

  const [selectedMachineId, setSelectedMachineId] = useState<string>('ALL')
  const [selectedMaterial, setSelectedMaterial] = useState<string>('ALL')
  const [activeSlot, setActiveSlot] = useState<MachineTimelineSlot | null>(null)

  const timeline = timelineQuery.data
  const perf = perfQuery.data

  const materials = useMemo(() => {
    if (!timeline) return []
    const set = new Set<string>()
    for (const m of timeline.machines) {
      for (const s of m.slots) {
        set.add(s.materialCode)
      }
    }
    return Array.from(set)
  }, [timeline])

  const filteredMachines = useMemo(() => {
    if (!timeline) return []
    return timeline.machines
      .filter((m) => selectedMachineId === 'ALL' || m.machineId === selectedMachineId)
      .map((m) => ({
        ...m,
        slots: m.slots.filter((s) => selectedMaterial === 'ALL' || s.materialCode === selectedMaterial),
      }))
  }, [timeline, selectedMachineId, selectedMaterial])

  // Compute time window bounds for horizontal Gantt visualization
  const ganttBounds = useMemo(() => {
    const now = Date.now()
    let minMs = now - 60 * 60 * 1000
    let maxMs = now + 24 * 60 * 60 * 1000

    for (const m of filteredMachines) {
      for (const s of m.slots) {
        const start = new Date(s.plannedStartUtc).getTime()
        const end = new Date(s.plannedEndUtc).getTime()
        if (start < minMs) minMs = start
        if (end > maxMs) maxMs = end
      }
      if (m.maintenanceFromUtc && m.maintenanceUntilUtc) {
        const mf = new Date(m.maintenanceFromUtc).getTime()
        const mu = new Date(m.maintenanceUntilUtc).getTime()
        if (mf < minMs) minMs = mf
        if (mu > maxMs) maxMs = mu
      }
    }

    const span = Math.max(maxMs - minMs, 4 * 3600 * 1000)
    return { minMs, maxMs: minMs + span, span }
  }, [filteredMachines])

  function toPercent(iso: string) {
    const ms = new Date(iso).getTime()
    return Math.min(100, Math.max(0, ((ms - ganttBounds.minMs) / ganttBounds.span) * 100))
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }} spacing={3}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'center' }}
          spacing={2}
        >
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Lịch máy (Gantt) &amp; Điểm hiệu suất xưởng — Lab
              </Typography>
              <Typography color="text.secondary">
                Biểu đồ khung giờ in theo từng máy &amp; 4 chỉ số hiệu suất kèm mức trung bình mạng lưới
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.25} flexWrap="wrap" useFlexGap>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ViewListRounded />}
              onClick={() => navigate('/lab/queue')}
              sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
            >
              Hàng đợi Job
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<PrecisionManufacturingRounded />}
              onClick={() => navigate('/lab/machines')}
              sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
            >
              Khai máy
            </Button>
            <Button
              variant="outlined"
              color="primary"
              startIcon={<RefreshRounded />}
              onClick={() => {
                timelineQuery.refetch()
                perfQuery.refetch()
              }}
            >
              Làm mới
            </Button>
            <Button
              variant="outlined"
              color="error"
              startIcon={<LogoutRounded />}
              onClick={() => {
                logout()
                navigate('/', { replace: true })
              }}
            >
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {loadingLab ? (
          <CircularProgress sx={{ alignSelf: 'center', my: 4 }} />
        ) : !myLab ? (
          <Box
            sx={{
              p: 6,
              textAlign: 'center',
              borderRadius: 3,
              border: '1px dashed rgba(255,255,255,0.18)',
              bgcolor: 'background.paper',
            }}
          >
            <FactoryRounded sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
            <Typography sx={{ fontWeight: 700, mb: 1 }}>Chưa tìm thấy hồ sơ xưởng của bạn</Typography>
            <Button variant="contained" onClick={() => navigate('/lab/register')}>
              Đăng ký xưởng ngay
            </Button>
          </Box>
        ) : (
          <>
            {/* ── SECTION 1: LAB PERFORMANCE SCORE & 4 METRICS + ANONYMOUS BENCHMARK (FR-LAB-007) ── */}
            <Box
              sx={{
                p: { xs: 2.5, md: 3 },
                borderRadius: 3,
                border: '1px solid rgba(255,255,255,0.12)',
                bgcolor: 'background.paper',
              }}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'flex-start', md: 'center' }}
                spacing={2}
                sx={{ mb: 2.5 }}
              >
                <Box>
                  <Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
                    <AssessmentRounded color="primary" />
                    <Typography sx={{ fontWeight: 700, fontSize: '1.15rem', color: 'text.primary' }}>
                      Điểm hiệu suất xưởng: {myLab.name}
                    </Typography>
                    {perf && (
                      <Chip
                        size="small"
                        color={perf.isProvisional ? 'warning' : perf.standingScore >= 0.7 ? 'success' : 'error'}
                        label={
                          perf.isProvisional
                            ? `Điểm tạm tính: ${pct(perf.standingScore)} (Chưa đủ 10 đơn hoàn tất)`
                            : `Điểm chính thức: ${pct(perf.standingScore)} (${perf.standingLabel})`
                        }
                      />
                    )}
                  </Stack>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                    Số đơn hoàn tất (90 ngày): <b>{perf?.completedJobsCount ?? 0}</b> / tổng {perf?.totalAssignedJobsCount ?? 0} đơn được giao · So sánh ẩn danh với{' '}
                    <b>{perf?.activeLabsInBenchmark ?? 1} xưởng</b> đang hoạt động trên toàn hệ thống
                  </Typography>
                </Box>

                {perf && (
                  <Box
                    sx={{
                      px: 2.5,
                      py: 1.5,
                      borderRadius: 2.5,
                      border: '1px solid rgba(139,92,246,0.35)',
                      bgcolor: 'rgba(139,92,246,0.08)',
                      textAlign: 'center',
                    }}
                  >
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      Điểm xưởng / TB mạng lưới ẩn danh
                    </Typography>
                    <Typography sx={{ fontWeight: 800, fontSize: '1.4rem', color: '#A78BFA' }}>
                      {pct(perf.standingScore)}{' '}
                      <Typography component="span" variant="body2" color="text.secondary">
                        vs {pct(perf.networkAverageStandingScore)}
                      </Typography>
                    </Typography>
                  </Box>
                )}
              </Stack>

              {perf?.isProvisional && (
                <Alert severity="info" sx={{ mb: 2.5 }}>
                  Xưởng hiện mới hoàn tất <b>{perf.completedJobsCount}/10 đơn in</b>. Hệ thống áp dụng <b>điểm tạm tính trung lập 70%</b> để đảm bảo công bằng cho xưởng mới, đồng thời vẫn theo dõi và hiển thị đầy đủ 4 chỉ số thực tế bên dưới.
                </Alert>
              )}

              {perfQuery.isLoading ? (
                <CircularProgress size={28} />
              ) : perf ? (
                <Grid container spacing={2}>
                  {[
                    {
                      title: '1. Giao đúng hạn (ODR)',
                      subtitle: 'On-Time Delivery Rate (trọng số 35%)',
                      own: perf.labMetrics.onTimeDeliveryRate,
                      bench: perf.networkAnonymousAverage.onTimeDeliveryRate,
                    },
                    {
                      title: '2. Đạt chuẩn lần đầu (FPY)',
                      subtitle: 'First-Pass Yield qua QC (trọng số 30%)',
                      own: perf.labMetrics.firstPassYield,
                      bench: perf.networkAnonymousAverage.firstPassYield,
                    },
                    {
                      title: '3. Tỷ lệ nhận Job (Acceptance)',
                      subtitle: 'Nhận việc trong hạn 2 giờ (trọng số 15%)',
                      own: perf.labMetrics.acceptanceRate,
                      bench: perf.networkAnonymousAverage.acceptanceRate,
                    },
                    {
                      title: '4. Hiệu suất tải máy (Utilization)',
                      subtitle: 'Tỷ lệ khai thác công suất máy (trọng số 10%)',
                      own: perf.labMetrics.utilizationRate,
                      bench: perf.networkAnonymousAverage.utilizationRate,
                    },
                  ].map((metric) => {
                    const diff = Math.round((metric.own - metric.bench) * 100)
                    return (
                      <Grid key={metric.title} size={{ xs: 12, sm: 6, md: 3 }}>
                        <Box
                          sx={{
                            p: 2,
                            borderRadius: 2.5,
                            border: '1px solid rgba(255,255,255,0.09)',
                            bgcolor: 'rgba(255,255,255,0.02)',
                            height: '100%',
                          }}
                        >
                          <Typography sx={{ fontWeight: 700, fontSize: '0.95rem', color: 'text.primary' }}>
                            {metric.title}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1.5 }}>
                            {metric.subtitle}
                          </Typography>
                          <Stack direction="row" justifyContent="space-between" alignItems="baseline" sx={{ mb: 0.75 }}>
                            <Typography sx={{ fontWeight: 800, fontSize: '1.5rem', color: 'text.primary' }}>
                              {pct(metric.own)}
                            </Typography>
                            <Chip
                              size="small"
                              label={`${diff >= 0 ? '+' : ''}${diff}% so với mạng`}
                              color={diff >= 0 ? 'success' : 'warning'}
                              variant="outlined"
                            />
                          </Stack>
                          <LinearProgress
                            variant="determinate"
                            value={Math.min(100, Math.round(metric.own * 100))}
                            sx={{ height: 6, borderRadius: 3, mb: 1 }}
                          />
                          <Typography variant="caption" color="text.secondary">
                            Mốc trung bình ẩn danh toàn mạng: <b>{pct(metric.bench)}</b>
                          </Typography>
                        </Box>
                      </Grid>
                    )
                  })}
                </Grid>
              ) : null}
            </Box>

            {/* ── SECTION 2: MACHINE GANTT TIMELINE (FR-LAB-006) ── */}
            <Box
              sx={{
                p: { xs: 2.5, md: 3 },
                borderRadius: 3,
                border: '1px solid rgba(255,255,255,0.12)',
                bgcolor: 'background.paper',
              }}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'stretch', md: 'center' }}
                spacing={2}
                sx={{ mb: 2 }}
              >
                <Box>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <ScheduleRounded color="primary" />
                    <Typography sx={{ fontWeight: 700, fontSize: '1.15rem', color: 'text.primary' }}>
                      Dòng thời gian lịch máy (Biểu đồ Gantt)
                    </Typography>
                  </Stack>
                  <Typography variant="body2" color="text.secondary">
                    Hiển thị các khung giờ in trên từng máy. Khi đơn in được xác nhận hoàn tất, lịch in sẽ tự động cập nhật ngay lập tức.
                  </Typography>
                </Box>

                <Stack direction="row" spacing={1.5}>
                  <FormControl size="small" sx={{ minWidth: 170 }}>
                    <InputLabel id="gantt-machine-filter">Lọc theo máy</InputLabel>
                    <Select
                      labelId="gantt-machine-filter"
                      label="Lọc theo máy"
                      value={selectedMachineId}
                      onChange={(e) => setSelectedMachineId(e.target.value)}
                    >
                      <MenuItem value="ALL">Tất cả máy ({timeline?.machines.length ?? 0})</MenuItem>
                      {(timeline?.machines ?? []).map((m) => (
                        <MenuItem key={m.machineId} value={m.machineId}>
                          {m.machineName}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>

                  <FormControl size="small" sx={{ minWidth: 150 }}>
                    <InputLabel id="gantt-material-filter">Vật liệu</InputLabel>
                    <Select
                      labelId="gantt-material-filter"
                      label="Vật liệu"
                      value={selectedMaterial}
                      onChange={(e) => setSelectedMaterial(e.target.value)}
                    >
                      <MenuItem value="ALL">Tất cả vật liệu</MenuItem>
                      {materials.map((mat) => (
                        <MenuItem key={mat} value={mat}>
                          {mat}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                </Stack>
              </Stack>

              <Divider sx={{ my: 2, borderColor: 'rgba(255,255,255,0.08)' }} />

              {timelineQuery.isLoading ? (
                <CircularProgress size={28} />
              ) : filteredMachines.length === 0 ? (
                <Typography color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
                  Chưa có máy nào trong danh sách. Hãy khai báo máy tại Màn khai máy.
                </Typography>
              ) : (
                <Stack spacing={2.25}>
                  {/* Time Axis Header */}
                  <Stack
                    direction="row"
                    justifyContent="space-between"
                    sx={{ px: 1, color: 'text.secondary', fontSize: '0.78rem' }}
                  >
                    <span>Bắt đầu khung: {fmtDateTime(new Date(ganttBounds.minMs).toISOString())}</span>
                    <span>Giữa khung: {fmtDateTime(new Date(ganttBounds.minMs + ganttBounds.span / 2).toISOString())}</span>
                    <span>Kết thúc khung: {fmtDateTime(new Date(ganttBounds.maxMs).toISOString())}</span>
                  </Stack>

                  {filteredMachines.map((m) => (
                    <Box
                      key={m.machineId}
                      sx={{
                        p: 2,
                        borderRadius: 2.5,
                        border: '1px solid rgba(255,255,255,0.09)',
                        bgcolor: 'rgba(255,255,255,0.015)',
                      }}
                    >
                      <Stack
                        direction={{ xs: 'column', sm: 'row' }}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', sm: 'center' }}
                        spacing={1}
                        sx={{ mb: 1.5 }}
                      >
                        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                          <PrecisionManufacturingRounded fontSize="small" color="primary" />
                          <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{m.machineName}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            · {m.model} ({PRINT_TECHNOLOGY_LABELS[m.technology] ?? 'FDM'})
                          </Typography>
                          <Chip
                            size="small"
                            label={MACHINE_STATUS_LABELS[m.status]}
                            color={
                              m.status === 0
                                ? 'success'
                                : m.status === 1
                                  ? 'info'
                                  : m.status === 2
                                    ? 'warning'
                                    : 'error'
                            }
                          />
                        </Stack>
                        <Typography variant="caption" color="text.secondary">
                          Tổng thời lượng hàng đợi: <b>{m.totalQueueMinutes} phút</b> ({m.slots.length} slot đang đặt)
                        </Typography>
                      </Stack>

                      {/* Horizontal Gantt Bar Track */}
                      <Box
                        sx={{
                          position: 'relative',
                          height: 46,
                          borderRadius: 2,
                          bgcolor: 'rgba(255,255,255,0.04)',
                          border: '1px solid rgba(255,255,255,0.08)',
                          overflow: 'hidden',
                        }}
                      >
                        {/* Maintenance Block */}
                        {m.maintenanceFromUtc && m.maintenanceUntilUtc && (
                          <Tooltip
                            title={`Bảo trì: ${fmtDateTime(m.maintenanceFromUtc)} → ${fmtDateTime(m.maintenanceUntilUtc)} (${m.maintenanceReason ?? 'Bảo trì'})`}
                          >
                            <Box
                              sx={{
                                position: 'absolute',
                                top: 4,
                                bottom: 4,
                                left: `${toPercent(m.maintenanceFromUtc)}%`,
                                width: `${Math.max(4, toPercent(m.maintenanceUntilUtc) - toPercent(m.maintenanceFromUtc))}%`,
                                bgcolor: 'rgba(245,158,11,0.35)',
                                border: '1px dashed #F59E0B',
                                borderRadius: 1.5,
                                display: 'flex',
                                alignItems: 'center',
                                px: 1,
                                overflow: 'hidden',
                                whiteSpace: 'nowrap',
                              }}
                            >
                              <BuildCircleRounded sx={{ fontSize: 14, mr: 0.5, color: '#FBBF24' }} />
                              <Typography variant="caption" sx={{ color: '#FDE68A', fontWeight: 600 }}>
                                Bảo trì
                              </Typography>
                            </Box>
                          </Tooltip>
                        )}

                        {/* Job Slots */}
                        {m.slots.map((slot) => {
                          const left = toPercent(slot.plannedStartUtc)
                          const right = toPercent(slot.plannedEndUtc)
                          const width = Math.max(6, right - left)
                          const isReprint = slot.attemptNumber > 1
                          const bg = isReprint
                            ? 'linear-gradient(90deg, #DC2626, #EA580C)'
                            : slot.status === 'InProgress'
                              ? 'linear-gradient(90deg, #7C3AED, #6366F1)'
                              : slot.status === 'Accepted'
                                ? 'linear-gradient(90deg, #0284C7, #2563EB)'
                                : 'linear-gradient(90deg, #D97706, #F59E0B)'

                          return (
                            <Tooltip
                              key={slot.jobId}
                              title={`${slot.orderNumber ?? slot.jobId.slice(0, 8)} · ${slot.materialCode} ${slot.colorCode} · ${fmtDateTime(slot.plannedStartUtc)} → ${fmtDateTime(slot.plannedEndUtc)} (${slot.estimatedPrintMinutes}p)`}
                            >
                              <Box
                                onClick={() => setActiveSlot(slot)}
                                sx={{
                                  position: 'absolute',
                                  top: 5,
                                  bottom: 5,
                                  left: `${left}%`,
                                  width: `${width}%`,
                                  background: bg,
                                  borderRadius: 1.5,
                                  px: 1,
                                  display: 'flex',
                                  alignItems: 'center',
                                  cursor: 'pointer',
                                  overflow: 'hidden',
                                  whiteSpace: 'nowrap',
                                  boxShadow: '0 2px 6px rgba(0,0,0,0.35)',
                                }}
                              >
                                <Typography variant="caption" sx={{ color: '#fff', fontWeight: 700 }}>
                                  {slot.orderNumber ?? slot.jobId.slice(0, 6)} · {slot.materialCode}
                                  {isReprint ? ` (In lại #${slot.attemptNumber})` : ''}
                                </Typography>
                              </Box>
                            </Tooltip>
                          )
                        })}

                        {m.slots.length === 0 && !m.maintenanceUntilUtc && (
                          <Stack
                            alignItems="center"
                            justifyContent="center"
                            sx={{ height: '100%', color: 'text.disabled', fontSize: '0.8rem' }}
                          >
                            Trống lịch — Sẵn sàng nhận slot mới
                          </Stack>
                        )}
                      </Box>

                      {/* Slot List Table under the bar for crystal clarity */}
                      {m.slots.length > 0 && (
                        <Stack spacing={0.75} sx={{ mt: 1.25 }}>
                          {m.slots.map((slot) => (
                            <Stack
                              key={slot.jobId}
                              direction={{ xs: 'column', sm: 'row' }}
                              justifyContent="space-between"
                              alignItems={{ xs: 'flex-start', sm: 'center' }}
                              onClick={() => setActiveSlot(slot)}
                              sx={{
                                px: 1.5,
                                py: 0.75,
                                borderRadius: 1.5,
                                bgcolor: 'rgba(255,255,255,0.025)',
                                cursor: 'pointer',
                                '&:hover': { bgcolor: 'rgba(255,255,255,0.05)' },
                              }}
                            >
                              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.primary' }}>
                                  {slot.orderNumber ?? `Job ${slot.jobId.slice(0, 8)}`}
                                </Typography>
                                {slot.modelFileName && (
                                  <Typography variant="caption" color="text.secondary">
                                    ({slot.modelFileName})
                                  </Typography>
                                )}
                                <Chip size="small" label={`${slot.materialCode} · ${slot.colorCode}`} />
                                <Chip
                                  size="small"
                                  color={
                                    slot.attemptNumber > 1
                                      ? 'error'
                                      : slot.status === 'InProgress'
                                        ? 'primary'
                                        : slot.status === 'Accepted'
                                          ? 'info'
                                          : 'warning'
                                  }
                                  label={
                                    slot.attemptNumber > 1
                                      ? `In lại khẩn #${slot.attemptNumber}`
                                      : slot.status
                                  }
                                />
                              </Stack>
                              <Typography variant="caption" color="text.secondary">
                                Slot: {fmtDateTime(slot.plannedStartUtc)} → {fmtDateTime(slot.plannedEndUtc)} (
                                {slot.estimatedPrintMinutes} phút · Hạn nội bộ: {slot.internalDueDate})
                              </Typography>
                            </Stack>
                          ))}
                        </Stack>
                      )}
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
          </>
        )}
      </Stack>

      {/* ── Slot Detail Popup ── */}
      <Dialog
        open={activeSlot !== null}
        onClose={() => setActiveSlot(null)}
        maxWidth="xs"
        fullWidth
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          Chi tiết Slot — {activeSlot?.orderNumber ?? activeSlot?.jobId.slice(0, 8)}
        </DialogTitle>
        <DialogContent>
          {activeSlot && (
            <Stack spacing={1.25}>
              <Typography variant="body2">
                <b>Job ID:</b> {activeSlot.jobId}
              </Typography>
              {activeSlot.modelFileName && (
                <Typography variant="body2">
                  <b>File mẫu in:</b> {activeSlot.modelFileName}
                </Typography>
              )}
              <Typography variant="body2">
                <b>Vật liệu &amp; màu:</b> {activeSlot.materialCode} · {activeSlot.colorCode}
              </Typography>
              <Typography variant="body2">
                <b>Trạng thái:</b> {activeSlot.status} (Lần in #{activeSlot.attemptNumber})
              </Typography>
              <Typography variant="body2">
                <b>Khung giờ dự kiến:</b> {fmtDateTime(activeSlot.plannedStartUtc)} →{' '}
                {fmtDateTime(activeSlot.plannedEndUtc)} ({activeSlot.estimatedPrintMinutes} phút)
              </Typography>
              <Typography variant="body2">
                <b>Hạn nội bộ:</b> {activeSlot.internalDueDate}
              </Typography>
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setActiveSlot(null)} color="inherit">
            Đóng
          </Button>
          <Button
            variant="contained"
            onClick={() => {
              setActiveSlot(null)
              navigate('/lab/queue')
            }}
          >
            Tới Hàng đợi xử lý Job
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

