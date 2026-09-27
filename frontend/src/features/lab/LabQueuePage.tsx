import { useState } from 'react'
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
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { CheckRounded, FactoryRounded, PlayArrowRounded, LibraryBooksRounded, LogoutRounded } from '@mui/icons-material'
import type { Job } from '../jobs/jobTypes'
import { JOB_LABELS } from '../jobs/jobTypes'
import { useJobs, useAcceptJob, useStartJob, useCompleteJob } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')
const fmtTime = (d: string | null | undefined) => (d ? new Date(d).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) : '—')

/**
 * Lab operator queue — works a job through Assigned → Accepted → In Progress → Completed.
 * (FR-LAB-004/005 — production workflow.)
 */
export default function LabQueuePage() {
  const navigate = useNavigate()
  const { logout } = useAuth()

  // Three concurrent views; each uses the same cache key namespace.
  const assigned = useJobs('Assigned')
  const accepted = useJobs('Accepted')
  const inProgress = useJobs('InProgress')

  const accept = useAcceptJob()
  const start = useStartJob()
  const complete = useCompleteJob()

  const [completeTarget, setCompleteTarget] = useState<Job | null>(null)
  const [actualMinutes, setActualMinutes] = useState(0)

  const jobs: { j: Job; action: 'complete' | 'start' | 'accept' }[] = [
    ...(inProgress.data ?? []).map((j) => ({ j, action: 'complete' as const })),
    ...(accepted.data ?? []).map((j) => ({ j, action: 'start' as const })),
    ...(assigned.data ?? []).map((j) => ({ j, action: 'accept' as const })),
  ]

  const loading = assigned.isLoading || accepted.isLoading || inProgress.isLoading
  const error = assigned.isError || accepted.isError || inProgress.isError

  async function handleAction(action: 'accept' | 'start', id: string) {
    try {
      if (action === 'accept') await accept.mutateAsync(id)
      else await start.mutateAsync(id)
    } catch { /* invalid transition; refresh will settle */ }
  }

  async function handleComplete() {
    if (!completeTarget) return
    try {
      await complete.mutateAsync({ id: completeTarget.id, actualMinutes: Math.max(actualMinutes, 1) })
      setCompleteTarget(null)
    } catch { /* no-op */ }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1100, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Hàng đợi sản xuất — Lab
            </Typography>
            <Typography color="text.secondary">Nhận job → bắt đầu in → báo hoàn thành (FR-LAB)</Typography>
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

        {error ? <Alert severity="error">Không tải được hàng đợi.</Alert> : null}
        {loading && jobs.length === 0 ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {jobs.length === 0 && !loading ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có job nào được gán cho lab.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {jobs.map(({ j, action }) => (
              <Box key={j.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{j.materialCode} · {j.colorCode}</Typography>
                      <Chip label={JOB_LABELS[j.status]} size="small" color={j.status === 'InProgress' ? 'primary' : j.status === 'Accepted' ? 'info' : 'warning'} />
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Ước tính {j.estimatedPrintMinutes} phút · Lớp {j.layerHeightMm}mm · Hạn nội bộ {fmtDate(j.internalDueDate)}
                      {j.plannedStartUtc ? ` · Bắt đầu ${fmtTime(j.plannedStartUtc)}` : ''}
                    </Typography>
                  </Box>

                  <Box>
                    {action === 'accept' && (
                      <Button variant="contained" color="primary" startIcon={<CheckRounded />} onClick={() => handleAction('accept', j.id)} disabled={accept.isPending}>
                        Nhận job
                      </Button>
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

      {/* Complete dialog — enter the real print time */}
      <Dialog open={completeTarget !== null} onClose={() => setCompleteTarget(null)} fullWidth maxWidth="xs"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>Hoàn thành job</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ color: 'text.secondary', mb: 2 }}>
            Ghi thời gian in thực tế (phút) để hệ thống hiệu chuẩn ước lượng (FR-SCHED-008).
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