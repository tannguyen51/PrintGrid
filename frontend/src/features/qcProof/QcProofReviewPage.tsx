import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Divider, ImageList, ImageListItem, Stack, TextField, Typography,
} from '@mui/material'
import { CheckCircleRounded, CloseRounded, FactCheckRounded, LogoutRounded } from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import type { QcProofQueueItem } from '../jobs/jobTypes'
import { useQcProofQueue, useReviewQcProof } from '../jobs/useJobs'
import { PageBackButton } from '../../shared/components/PageBackButton'

export default function QcProofReviewPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const queue = useQcProofQueue()
  const review = useReviewQcProof()
  const [selected, setSelected] = useState<QcProofQueueItem | null>(null)
  const [rejecting, setRejecting] = useState<QcProofQueueItem | null>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function decide(item: QcProofQueueItem, approved: boolean, rejectionReason?: string) {
    try {
      setError(null)
      await review.mutateAsync({ id: item.job.id, approved, reason: rejectionReason })
      setSelected(null)
      setRejecting(null)
      setReason('')
    } catch (err: any) {
      setError(err?.response?.data?.error?.message ?? 'Không thể lưu quyết định QC.')
    }
  }

  const items = queue.data ?? []
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack spacing={2.5} sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Duyệt QC xưởng</Typography>
              <Typography color="text.secondary">Kiểm tra ảnh và báo cáo trước khi cho phép bàn giao về hub</Typography>
            </Box>
          </Stack>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>Đăng xuất</Button>
        </Stack>

        {error && <Alert severity="error" onClose={() => setError(null)}>{error}</Alert>}
        {queue.isError && <Alert severity="error">Không tải được hàng đợi QC.</Alert>}
        {queue.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}

        {!queue.isLoading && items.length === 0 && (
          <Box sx={{ py: 8, textAlign: 'center', border: '1px dashed', borderColor: 'divider', borderRadius: 2 }}>
            <FactCheckRounded sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Không có bằng chứng QC đang chờ duyệt.</Typography>
          </Box>
        )}

        <Stack spacing={1.5}>
          {items.map((item) => (
            <Box key={item.job.id} sx={{ p: 2.5, bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} spacing={2}>
                <Box>
                  <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                    <Typography fontWeight={700}>{item.job.materialCode} · {item.job.colorCode}</Typography>
                    <Chip label="Chờ duyệt" color="warning" size="small" />
                  </Stack>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                    Job {item.job.id.slice(0, 8)} · {item.photoUrls.length} ảnh · In thực tế {item.job.actualPrintMinutes ?? '—'} phút
                  </Typography>
                </Box>
                <Stack direction="row" spacing={1}>
                  <Button variant="outlined" onClick={() => setSelected(item)}>Xem bằng chứng</Button>
                  <Button variant="contained" color="success" startIcon={<CheckCircleRounded />} disabled={review.isPending} onClick={() => decide(item, true)}>Duyệt</Button>
                  <Button variant="outlined" color="error" startIcon={<CloseRounded />} onClick={() => setRejecting(item)}>Từ chối</Button>
                </Stack>
              </Stack>
            </Box>
          ))}
        </Stack>
      </Stack>

      <Dialog open={selected !== null} onClose={() => setSelected(null)} fullWidth maxWidth="md">
        <DialogTitle>Bằng chứng QC · {selected?.job.id.slice(0, 8)}</DialogTitle>
        <DialogContent>
          <Typography variant="overline" color="text.secondary">Báo cáo của xưởng</Typography>
          <Typography sx={{ whiteSpace: 'pre-wrap', mb: 2 }}>{selected?.job.qcSelfReport}</Typography>
          <Divider sx={{ mb: 2 }} />
          <ImageList cols={selected && selected.photoUrls.length > 1 ? 2 : 1} gap={12}>
            {(selected?.photoUrls ?? []).map((url, index) => (
              <ImageListItem key={url}><img src={url} alt={`Ảnh QC ${index + 1}`} loading="lazy" style={{ maxHeight: 420, objectFit: 'contain' }} /></ImageListItem>
            ))}
          </ImageList>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelected(null)}>Đóng</Button>
          {selected && <Button color="success" variant="contained" onClick={() => decide(selected, true)}>Duyệt bằng chứng</Button>}
        </DialogActions>
      </Dialog>

      <Dialog open={rejecting !== null} onClose={() => setRejecting(null)} fullWidth maxWidth="sm">
        <DialogTitle>Từ chối bằng chứng QC</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth multiline minRows={3} label="Lý do từ chối" value={reason} onChange={(e) => setReason(e.target.value)} sx={{ mt: 1 }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRejecting(null)}>Hủy</Button>
          <Button color="error" variant="contained" disabled={!reason.trim() || review.isPending} onClick={() => rejecting && decide(rejecting, false, reason.trim())}>Xác nhận từ chối</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
