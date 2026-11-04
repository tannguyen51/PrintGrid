import { useState } from 'react'
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead,
  TableRow, TextField, ToggleButton, ToggleButtonGroup, Typography,
} from '@mui/material'
import { CheckCircleRounded, CancelRounded, OpenInNewRounded } from '@mui/icons-material'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  openModelFile, useApproveModel, useModelReviews, useRejectModel,
  type ModelReview, type ModelReviewStatus,
} from './useModelReviews'

const FILTERS: Array<ModelReviewStatus | 'All'> = ['Pending', 'Approved', 'Rejected', 'All']

function statusChip(status: ModelReviewStatus) {
  const color = status === 'Approved' ? 'success' : status === 'Rejected' ? 'error' : 'warning'
  const label = status === 'Approved' ? 'Đã duyệt' : status === 'Rejected' ? 'Bị từ chối' : 'Chờ duyệt'
  return <Chip size="small" color={color} label={label} sx={{ fontWeight: 600 }} />
}

export function ModelReviewQueuePage() {
  const [filter, setFilter] = useState<ModelReviewStatus | 'All'>('Pending')
  const [rejectTarget, setRejectTarget] = useState<ModelReview | null>(null)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const reviews = useModelReviews(filter)
  const approve = useApproveModel()
  const reject = useRejectModel()

  const act = async (action: () => Promise<void>) => {
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
    })
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
          {FILTERS.map((f) => <ToggleButton key={f} value={f}>{f === 'All' ? 'Tất cả' : statusChip(f as ModelReviewStatus)}</ToggleButton>)}
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
                  {reviews.data!.map((m) => (
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
                          <Button size="small" startIcon={<OpenInNewRounded />}
                            onClick={() => act(() => openModelFile(m.modelId))}>
                            Xem file
                          </Button>
                          {m.reviewStatus === 'Pending' && (
                            <>
                              <Button size="small" color="success" startIcon={<CheckCircleRounded />}
                                disabled={approve.isPending}
                                onClick={() => act(async () => { await approve.mutateAsync({ modelId: m.modelId }) })}>
                                Duyệt
                              </Button>
                              <Button size="small" color="error" startIcon={<CancelRounded />}
                                onClick={() => { setRejectTarget(m); setReason('') }}>
                                Từ chối
                              </Button>
                            </>
                          )}
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Paper>
      </Stack>

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
    </Box>
  )
}
