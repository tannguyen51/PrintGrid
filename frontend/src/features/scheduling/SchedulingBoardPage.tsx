import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Typography,
} from '@mui/material'
import { FactoryRounded, LogoutRounded, RocketLaunchRounded } from '@mui/icons-material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../../shared/api/apiClient'
import { JOB_LABELS } from '../jobs/jobTypes'
import { useJobs } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')

/**
 * Ops scheduling board (FR-ANALYTICS-001) — pending jobs, trigger the assignment engine
 * to place each job on the best capable lab/machine (FR-SCHED-004/006).
 */
export default function SchedulingBoardPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const queryClient = useQueryClient()
  const pending = useJobs('Pending')

  const assign = useMutation({
    mutationFn: (jobId: string) => apiClient.post(`/scheduling/jobs/${jobId}/assign`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['jobs'] }),
  })

  const jobs = pending.data ?? []

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Bảng điều phối — Ops
            </Typography>
            <Typography color="text.secondary">Gán job cho lab/máy phù hợp nhất (FR-SCHED-004)</Typography>
          </Box>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>
            Đăng xuất
          </Button>
        </Stack>

        {pending.isError ? <Alert severity="error">Không tải được danh sách job chờ.</Alert> : null}
        {pending.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!pending.isLoading && jobs.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Không có job nào chờ gán. Đặt hàng mới sẽ sinh job tự động.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {jobs.map((j) => (
              <Box key={j.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{j.materialCode} · {j.colorCode}</Typography>
                      <Chip label={JOB_LABELS[j.status]} size="small" color="default" />
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Ước tính {j.estimatedPrintMinutes} phút · Lớp {j.layerHeightMm}mm · Hạn nội bộ {fmtDate(j.internalDueDate)}
                    </Typography>
                  </Box>
                  <Button
                    variant="contained"
                    color="primary"
                    startIcon={assign.isPending ? <CircularProgress size={16} color="inherit" /> : <RocketLaunchRounded />}
                    onClick={() => assign.mutate(j.id)}
                    disabled={assign.isPending}
                    sx={{ alignSelf: { xs: 'stretch', md: 'auto' } }}
                  >
                    Gán cho lab
                  </Button>
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>
    </Box>
  )
}