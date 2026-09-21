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
import { CheckCircleRounded, CancelRounded, FactoryRounded, LogoutRounded } from '@mui/icons-material'
import { useState } from 'react'
import { JOB_LABELS } from '../jobs/jobTypes'
import type { Job } from '../jobs/jobTypes'
import { useJobs, useInspectJob } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')

/**
 * Hub quality control (FR-HUB-002) — inspect jobs that finished printing:
 * PASS → Completed; FAIL → classifies defect, triggers reprint/reassignment.
 */
export default function HubQCPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const awaiting = useJobs('AwaitingInspection')
  const [rejectTarget, setRejectTarget] = useState<Job | null>(null)
  const [reason, setReason] = useState('')
  const inspect = useInspectJob()

  const jobs = awaiting.data ?? []

  async function handlePass(job: Job) {
    try {
      await inspect.mutateAsync({ id: job.id, passed: true })
    } catch { /* no-op */ }
  }

  async function handleFail() {
    if (!rejectTarget) return
    try {
      await inspect.mutateAsync({ id: rejectTarget.id, passed: false, note: reason.trim() || 'Failed QC' })
      setRejectTarget(null)
      setReason('')
    } catch { /* no-op */ }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Kiểm tra chất lượng — Hub
            </Typography>
            <Typography color="text.secondary">Duyệt job đã in xong (FR-HUB-002)</Typography>
          </Box>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>
            Đăng xuất
          </Button>
        </Stack>

        {awaiting.isError ? <Alert severity="error">Không tải được danh sách chờ kiểm tra.</Alert> : null}

        {awaiting.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!awaiting.isLoading && jobs.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có job nào chờ kiểm tra chất lượng.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {jobs.map((j) => (
              <Box key={j.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{j.materialCode} · {j.colorCode}</Typography>
                      <Chip label={JOB_LABELS[j.status]} size="small" color="warning" />
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Ước tính {j.estimatedPrintMinutes} phút · Hạn nội bộ {fmtDate(j.internalDueDate)}
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={1.5} justifyContent={{ xs: 'flex-start', md: 'flex-end' }}>
                    <Button variant="contained" color="success" startIcon={<CheckCircleRounded />} onClick={() => handlePass(j)} disabled={inspect.isPending}>
                      PASS
                    </Button>
                    <Button variant="outlined" color="error" startIcon={<CancelRounded />} onClick={() => { setRejectTarget(j); setReason('') }}>
                      FAIL
                    </Button>
                  </Stack>
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>

      {/* FAIL dialog — defect classification */}
      <Dialog open={rejectTarget !== null} onClose={() => setRejectTarget(null)} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>Đánh trượt kiểm tra (FAIL)</DialogTitle>
        <DialogContent>
          <DialogContentText sx={{ color: 'text.secondary', mb: 2 }}>
            Mô tả khiếm khuyết. Job sẽ được chuyển về gán lại cho lab khác (tự động tạo lệnh in lại).
          </DialogContentText>
          <TextField
            label="Mô tả khiếm khuyết"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            fullWidth
            multiline
            minRows={2}
            autoFocus
            placeholder="VD: Lệch kích thước, bề mặt lỗi..."
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setRejectTarget(null)} color="inherit" sx={{ color: 'text.secondary' }}>Hủy</Button>
          <Button onClick={handleFail} variant="contained" color="error" disabled={inspect.isPending}>
            {inspect.isPending ? 'Đang xử lý…' : 'Xác nhận FAIL & yêu cầu in lại'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}