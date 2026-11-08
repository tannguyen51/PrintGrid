import { useState } from 'react'
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Paper, Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TableRow, TextField, ToggleButton, ToggleButtonGroup, Typography,
} from '@mui/material'
import { CheckCircleRounded, CancelRounded, OpenInNewRounded, ThreeDRotationRounded } from '@mui/icons-material'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useApproveModel, useModelReviews, useRejectModel } from './useModelReviews'
import { useModelFileUrl } from '../models/useModelFile'
import { ModelViewer } from '../../shared/components/ModelViewer'

const FILTERS: Array<'Pending' | 'Approved' | 'Rejected' | 'All'> = ['Pending', 'Approved', 'Rejected', 'All']
const PREVIEWABLE_FORMATS = ['GLB', 'GLTF', 'STL', 'OBJ', '3MF']

function statusChip(status: string) {
  const color = status === 'Approved' ? 'success' : status === 'Rejected' ? 'error' : 'warning'
  const label = status === 'Approved' ? 'Đã duyệt' : status === 'Rejected' ? 'Bị từ chối' : 'Chờ duyệt'
  return <Chip size="small" color={color} label={label} sx={{ fontWeight: 600 }} />
}

export function ModelReviewQueuePage() {
  const [filter, setFilter] = useState<'Pending' | 'Approved' | 'Rejected' | 'All'>('Pending')
  const [rejectTarget, setRejectTarget] = useState<{ modelId: string; name: string } | null>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [snack, setSnack] = useState<{ open: boolean; msg: string; type: 'success' | 'error' }>({ open: false, msg: '', type: 'success' })
  const [preview3d, setPreview3d] = useState<string | null>(null)

  const reviews = useModelReviews(filter)
  const approve = useApproveModel()
  const reject = useRejectModel()

  // Track which model we're viewing in 3D
  const previewModelId = preview3d

  async function act(action: () => Promise<void>) {
    setError(null)
    try {
      await action()
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không thực hiện được thao tác. Vui lòng thử lại.'))
    }
  }

  async function submitReject() {
    if (!rejectTarget || reason.trim().length === 0) return
    await act(async () => {
      await reject.mutateAsync({ modelId: rejectTarget.modelId, reason: reason.trim() })
      setRejectTarget(null)
      setReason('')
      showSuccess('Đã từ chối model.')
    })
  }

  function showSuccess(msg: string) {
    setSnack({ open: true, msg, type: 'success' })
    setTimeout(() => setSnack(s => ({ ...s, open: false })), 4000)
  }

  return (
    <Box sx={{ maxWidth: 1200, mx: 'auto', px: { xs: 2, md: 4 }, py: 4 }}>
      <Stack spacing={3}>
        <Box>
          <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Kiểm duyệt model</Typography>
          <Typography color="text.secondary">
            Duyệt nội dung trước khi model được báo giá và đưa vào sản xuất. File xem tại đây được ghi audit.
          </Typography>
        </Box>

        <ToggleButtonGroup
          exclusive
          size="small"
          value={filter}
          onChange={(_, value) => { if (value) setFilter(value) }}
        >
          {FILTERS.map((f) => <ToggleButton key={f} value={f}>{f === 'All' ? 'Tất cả' : statusChip(f)}</ToggleButton>)}
        </ToggleButtonGroup>

        {error && <Alert severity="error">{error}</Alert>}

        <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
          {reviews.isLoading ? (
            <Box sx={{ display: 'grid', placeItems: 'center', py: 8 }}><CircularProgress /></Box>
          ) : reviews.isError ? (
            <Alert severity="error" sx={{ m: 2 }}>{getApiErrorMessage(reviews.error, 'Không tải được hàng đợi.')}</Alert>
          ) : (reviews.data?.length ?? 0) === 0 ? (
            <Box sx={{ py: 8, textAlign: 'center' }}>
              <Typography color="text.secondary">Không có model nào ở trạng thái này.</Typography>
            </Box>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Model</TableCell>
                    <TableCell>Khách hàng</TableCell>
                    <TableCell>File</TableCell>
                    <TableCell>Hình học</TableCell>
                    <TableCell>Trạng thái</TableCell>
                    <TableCell align="right">Thao tác</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reviews.data!.map((m) => {
                    const canPreview = PREVIEWABLE_FORMATS.includes((m.fileFormat ?? '').toUpperCase())
                    return (
                      <TableRow key={m.modelId} hover>
                        <TableCell>
                          <Typography fontWeight={600}>{m.name}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {new Date(m.createdAt).toLocaleString('vi-VN')}
                          </Typography>
                        </TableCell>
                        <TableCell>{m.customerEmail || m.customerId.slice(0, 8)}</TableCell>
                        <TableCell>
                          <Typography variant="body2">{m.fileName}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {m.fileFormat} · {(m.sizeBytes / 1024 / 1024).toFixed(2)} MB
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{m.geometryStatus}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {m.isPrintable === false ? 'Không in được' : 'Có thể in'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          {statusChip(m.reviewStatus)}
                          {m.reviewReason && (
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
                              {m.reviewReason}
                            </Typography>
                          )}
                        </TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <Button size="small" startIcon={<ThreeDRotationRounded />}
                              disabled={!canPreview}
                              onClick={() => setPreview3d(preview3d === m.modelId ? null : m.modelId)}>
                              Preview 3D
                            </Button>
                            <Button size="small" startIcon={<OpenInNewRounded />}
                              onClick={() => window.open(`/api/v1/model-reviews/${m.modelId}/file`, '_blank', 'noopener,noreferrer')}>
                              Xem file
                            </Button>
                            {m.reviewStatus === 'Pending' && (
                              <>
                                <Button size="small" color="success" startIcon={<CheckCircleRounded />}
                                  disabled={approve.isPending}
                                  onClick={() => act(async () => {
                                    await approve.mutateAsync({ modelId: m.modelId })
                                    showSuccess(`Đã duyệt model "${m.name}".`)
                                  })}>
                                  Duyệt
                                </Button>
                                <Button size="small" color="error" startIcon={<CancelRounded />}
                                  onClick={() => { setRejectTarget({ modelId: m.modelId, name: m.name }); setReason('') }}>
                                  Từ chối
                                </Button>
                              </>
                            )}
                          </Stack>
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Paper>
      </Stack>

      {/* ── Reject dialog ── */}
      <Dialog open={rejectTarget !== null} onClose={() => setRejectTarget(null)} fullWidth maxWidth="sm">
        <DialogTitle>Từ chối model</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Lý do sẽ được gửi tới khách hàng và lưu vào audit.
          </Typography>
          <TextField
            autoFocus fullWidth multiline minRows={3} label="Lý do từ chối"
            value={reason} onChange={(e) => setReason(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRejectTarget(null)}>Huỷ</Button>
          <Button color="error" variant="contained" disabled={reason.trim().length === 0 || reject.isPending}
            onClick={submitReject}>
            Từ chối
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Inline 3D Preview dialog ── */}
      <Dialog
        open={preview3d !== null}
        onClose={() => setPreview3d(null)}
        fullWidth
        maxWidth="lg"
        fullScreen
      >
        <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography variant="h6">Xem trước 3D</Typography>
          <Button onClick={() => setPreview3d(null)}>Đóng</Button>
        </DialogTitle>
        <DialogContent sx={{ flex: 1, minHeight: 0, p: 2 }}>
          {previewModelId && (() => {
            const review = reviews.data?.find(r => r.modelId === previewModelId)
            const format = (review?.fileFormat ?? '').toUpperCase()
            const previewable = PREVIEWABLE_FORMATS.includes(format)
            const { url, failed } = useModelFileUrl(previewModelId, previewable, '/model-reviews')
            if (!review) return <Typography color="text.secondary">Loading...</Typography>
            return (
              <Box sx={{ height: '100%', bgcolor: '#F5F2FF', borderRadius: 2, overflow: 'hidden', position: 'relative' }}>
                {!url ? (
                  <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} spacing={2}>
                    <CircularProgress size={36} />
                    <Typography color="text.secondary">Đang tải mô hình…</Typography>
                  </Stack>
                ) : failed ? (
                  <Stack sx={{ position: 'absolute', inset: 0, alignItems: 'center', justifyContent: 'center' }} spacing={2} px={4}>
                    <ThreeDRotationRounded sx={{ fontSize: 48, color: 'text.disabled' }} />
                    <Typography color="text.secondary" align="center">Không tải được file để xem trước.</Typography>
                  </Stack>
                ) : (
                  <ModelViewer url={url} format={format} fit background="#F5F2FF" sx={{ bgcolor: '#F5F2FF' }} />
                )}
              </Box>
            )
          })()}
        </DialogContent>
      </Dialog>

      {/* ── Toast notification ── */}
      <Snackbar
        open={snack.open}
        autoHideDuration={4000}
        onClose={() => setSnack(s => ({ ...s, open: false }))}
      >
        <Alert severity={snack.type} onClose={() => setSnack(s => ({ ...s, open: false }))}>
          {snack.msg}
        </Alert>
      </Snackbar>
    </Box>
  )
}
