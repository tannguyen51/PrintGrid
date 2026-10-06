import { useState, useEffect } from 'react'
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
  DialogContentText,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  AccessTimeRounded,
  CheckRounded,
  CloseRounded,
  FactoryRounded,
  PlayArrowRounded,
  LibraryBooksRounded,
  LogoutRounded,
} from '@mui/icons-material'
import type { Job } from '../jobs/jobTypes'
import { JOB_LABELS } from '../jobs/jobTypes'
import { useJobs, useAcceptJob, useDeclineJob, useStartJob, useCompleteJob } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')
const fmtTime = (d: string | null | undefined) => (d ? new Date(d).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) : '—')

const PREDEFINED_REASONS = [
  'Hết vật liệu trong kho',
  'Máy đang bảo trì / gặp sự cố',
  'Quá tải công suất lab',
  'Không khả thi kỹ thuật / file lỗi',
  'Khác',
]

/**
 * Countdown timer component for job acceptance window (BR-ASSIGN-005, default 2 hours).
 */
function JobCountdown({ deadline, onExpire }: { deadline?: string | null; onExpire?: () => void }) {
  const [remaining, setRemaining] = useState<number | null>(null)

  useEffect(() => {
    if (!deadline) return
    const target = new Date(deadline).getTime()

    const update = () => {
      const diff = Math.floor((target - Date.now()) / 1000)
      setRemaining(diff)
      if (diff <= 0 && onExpire) {
        onExpire()
      }
    }

    update()
    const timer = setInterval(update, 1000)
    return () => clearInterval(timer)
  }, [deadline, onExpire])

  if (remaining === null) return null

  if (remaining <= 0) {
    return (
      <Chip
        icon={<AccessTimeRounded />}
        label="Quá hạn nhận việc (2 giờ)"
        size="small"
        color="error"
        sx={{ fontWeight: 600 }}
      />
    )
  }

  const hours = Math.floor(remaining / 3600)
  const minutes = Math.floor((remaining % 3600) / 60)
  const seconds = remaining % 60
  const pad = (n: number) => String(n).padStart(2, '0')
  const timeStr = `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`
  const isUrgent = remaining < 30 * 60

  return (
    <Chip
      icon={<AccessTimeRounded />}
      label={`Hạn nhận: còn ${timeStr}`}
      size="small"
      color={isUrgent ? 'error' : 'warning'}
      variant={isUrgent ? 'filled' : 'outlined'}
      sx={{ fontWeight: 600 }}
    />
  )
}

/**
 * Lab operator queue — works a job through Assigned → Accepted → In Progress → Completed.
 * (FR-LAB-004/005 — production workflow, BR-ASSIGN-005/006 — 2h acceptance window & rejection.)
 */
export default function LabQueuePage() {
  const navigate = useNavigate()
  const { logout } = useAuth()

  // Three concurrent views; each uses the same cache key namespace.
  const assigned = useJobs('Assigned')
  const accepted = useJobs('Accepted')
  const inProgress = useJobs('InProgress')

  const accept = useAcceptJob()
  const decline = useDeclineJob()
  const start = useStartJob()
  const complete = useCompleteJob()

  const [completeTarget, setCompleteTarget] = useState<Job | null>(null)
  const [actualMinutes, setActualMinutes] = useState(0)

  // Decline dialog state
  const [declineTarget, setDeclineTarget] = useState<Job | null>(null)
  const [selectedReason, setSelectedReason] = useState(PREDEFINED_REASONS[0])
  const [customReason, setCustomReason] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const jobs: { j: Job; action: 'complete' | 'start' | 'accept' }[] = [
    ...(inProgress.data ?? []).map((j) => ({ j, action: 'complete' as const })),
    ...(accepted.data ?? []).map((j) => ({ j, action: 'start' as const })),
    ...(assigned.data ?? []).map((j) => ({ j, action: 'accept' as const })),
  ]

  const loading = assigned.isLoading || accepted.isLoading || inProgress.isLoading
  const error = assigned.isError || accepted.isError || inProgress.isError

  async function handleAction(action: 'accept' | 'start', id: string) {
    try {
      setActionError(null)
      if (action === 'accept') await accept.mutateAsync(id)
      else await start.mutateAsync(id)
    } catch (err: any) {
      setActionError(err?.response?.data?.error?.message || 'Có lỗi xảy ra khi xử lý job.')
    }
  }

  async function handleDecline() {
    if (!declineTarget) return
    const reasonText = selectedReason === 'Khác' ? customReason.trim() : selectedReason
    if (!reasonText) return

    try {
      setActionError(null)
      await decline.mutateAsync({ id: declineTarget.id, reason: reasonText })
      setDeclineTarget(null)
      setCustomReason('')
      setSelectedReason(PREDEFINED_REASONS[0])
    } catch (err: any) {
      setActionError(err?.response?.data?.error?.message || 'Không thể từ chối job.')
    }
  }

  async function handleComplete() {
    if (!completeTarget) return
    try {
      setActionError(null)
      await complete.mutateAsync({ id: completeTarget.id, actualMinutes: Math.max(actualMinutes, 1) })
      setCompleteTarget(null)
    } catch (err: any) {
      setActionError(err?.response?.data?.error?.message || 'Không thể ghi nhận hoàn thành.')
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1100, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Hàng đợi sản xuất — Lab
            </Typography>
            <Typography color="text.secondary">Quy trình sản xuất: Nhận / Từ chối (hạn 2 giờ) → Bắt đầu in → Báo hoàn thành</Typography>
          </Box>
          <Stack direction="row" spacing={1.5}>
            <Button variant="outlined" color="inherit" startIcon={<LibraryBooksRounded />} onClick={() => navigate('/models')} sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}>
              Thư viện model
            </Button>
            <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {actionError ? <Alert severity="error" onClose={() => setActionError(null)}>{actionError}</Alert> : null}
        {error ? <Alert severity="error">Không tải được hàng đợi.</Alert> : null}
        {loading && jobs.length === 0 ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {jobs.length === 0 && !loading ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có job nào trong hàng đợi.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {jobs.map(({ j, action }) => (
              <Box key={j.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5, flexWrap: 'wrap', gap: 1 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{j.materialCode} · {j.colorCode}</Typography>
                      <Chip label={JOB_LABELS[j.status]} size="small" color={j.status === 'InProgress' ? 'primary' : j.status === 'Accepted' ? 'info' : 'warning'} />
                      {j.status === 'Assigned' && (
                        <JobCountdown
                          deadline={j.acceptanceDeadlineUtc}
                          onExpire={() => assigned.refetch()}
                        />
                      )}
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Ước tính {j.estimatedPrintMinutes} phút · Lớp {j.layerHeightMm}mm · Hạn nội bộ {fmtDate(j.internalDueDate)}
                      {j.plannedStartUtc ? ` · Bắt đầu ${fmtTime(j.plannedStartUtc)}` : ''}
                    </Typography>
                  </Box>

                  <Box>
                    {action === 'accept' && (
                      <Stack direction="row" spacing={1.5}>
                        <Button
                          variant="contained"
                          color="primary"
                          startIcon={<CheckRounded />}
                          onClick={() => handleAction('accept', j.id)}
                          disabled={accept.isPending || decline.isPending}
                        >
                          Nhận job
                        </Button>
                        <Button
                          variant="outlined"
                          color="error"
                          startIcon={<CloseRounded />}
                          onClick={() => {
                            setDeclineTarget(j)
                            setSelectedReason(PREDEFINED_REASONS[0])
                            setCustomReason('')
                          }}
                          disabled={accept.isPending || decline.isPending}
                        >
                          Từ chối
                        </Button>
                      </Stack>
                    )}
                    {action === 'start' && (
                      <Button variant="contained" color="primary" startIcon={<PlayArrowRounded />} onClick={() => handleAction('start', j.id)} disabled={start.isPending}>
                        Bắt đầu in
                      </Button>
                    )}
                    {action === 'complete' && (
                      <Button variant="contained" color="success" startIcon={<CheckRounded />} onClick={() => { setCompleteTarget(j); setActualMinutes(j.estimatedPrintMinutes) }}>
                        Báo hoàn thành
                      </Button>
                    )}
                  </Box>
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>

      {/* Decline Dialog — Reason selection (BR-ASSIGN-006) */}
      <Dialog
        open={declineTarget !== null}
        onClose={() => setDeclineTarget(null)}
        fullWidth
        maxWidth="xs"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>Từ chối nhận job</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ color: 'text.secondary', mb: 2 }}>
            Vui lòng chọn lý do từ chối. Job sẽ được đưa về hàng đợi để hệ thống tự động gán lại (BR-ASSIGN-006).
          </DialogContentText>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <FormControl fullWidth size="small">
              <InputLabel id="decline-reason-label">Lý do từ chối</InputLabel>
              <Select
                labelId="decline-reason-label"
                value={selectedReason}
                label="Lý do từ chối"
                onChange={(e) => setSelectedReason(e.target.value)}
              >
                {PREDEFINED_REASONS.map((r) => (
                  <MenuItem key={r} value={r}>{r}</MenuItem>
                ))}
              </Select>
            </FormControl>

            {selectedReason === 'Khác' && (
              <TextField
                label="Chi tiết lý do"
                value={customReason}
                onChange={(e) => setCustomReason(e.target.value)}
                multiline
                rows={2}
                fullWidth
                autoFocus
                placeholder="Nhập lý do cụ thể..."
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setDeclineTarget(null)} color="inherit" sx={{ color: 'text.secondary' }}>
            Hủy
          </Button>
          <Button
            onClick={handleDecline}
            variant="contained"
            color="error"
            disabled={decline.isPending || (selectedReason === 'Khác' && !customReason.trim())}
          >
            {decline.isPending ? 'Đang xử lý…' : 'Xác nhận từ chối'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Complete dialog — enter the real print time */}
      <Dialog
        open={completeTarget !== null}
        onClose={() => setCompleteTarget(null)}
        fullWidth
        maxWidth="xs"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>Hoàn thành job</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ color: 'text.secondary', mb: 2 }}>
            Nhập thời gian in thực tế (phút) để cập nhật dữ liệu vận hành sản xuất.
          </DialogContentText>
          <TextField
            label="Thời gian thực tế (phút)"
            type="number"
            inputProps={{ min: 1 }}
            value={actualMinutes}
            onChange={(e) => setActualMinutes(Number(e.target.value))}
            fullWidth
            autoFocus
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setCompleteTarget(null)} color="inherit" sx={{ color: 'text.secondary' }}>Hủy</Button>
          <Button onClick={handleComplete} variant="contained" color="primary" disabled={complete.isPending || actualMinutes < 1}>
            {complete.isPending ? 'Đang lưu…' : 'Hoàn thành'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}