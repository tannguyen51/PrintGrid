import { useState } from 'react'
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
  Stack,
  Typography,
} from '@mui/material'
import {
  LocalShippingRounded,
  CancelRounded,
  ScheduleRounded,
} from '@mui/icons-material'
import { useCodConfirmation, useConfirmCodReceipt } from './useCodConfirmation'

const fmtDateTime = (d: string | null | undefined) =>
  d ? new Date(d).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' }) : '—'

function getTimeRemaining(deadline: string | null): { hours: number; minutes: number; seconds: number; expired: boolean } {
  if (!deadline) return { hours: 0, minutes: 0, seconds: 0, expired: true }
  const now = new Date().getTime()
  const deadlineTime = new Date(deadline).getTime()
  const diff = deadlineTime - now

  if (diff <= 0) return { hours: 0, minutes: 0, seconds: 0, expired: true }

  const hours = Math.floor(diff / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  const seconds = Math.floor((diff % (1000 * 60)) / 1000)

  return { hours, minutes, seconds, expired: false }
}

export function CodConfirmationPanel({ orderId }: { orderId: string }) {
  const { data, isLoading, isError, error } = useCodConfirmation(orderId)
  const confirmMutation = useConfirmCodReceipt()

  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false)
  const [rejectDialogOpen, setRejectDialogOpen] = useState(false)
  const [actionType, setActionType] = useState<'confirm' | 'reject'>('confirm')

  if (isLoading) {
    return (
      <Box sx={{ p: 2, textAlign: 'center' }}>
        <CircularProgress size={24} />
        <Typography variant="body2" sx={{ mt: 1 }}>Đang tải thông tin xác nhận COD...</Typography>
      </Box>
    )
  }

  if (isError || !data) {
    return (
      <Alert severity="error">
        Không tải được thông tin xác nhận COD. {error instanceof Error ? error.message : ''}
      </Alert>
    )
  }

  // Không hiển thị nếu không phải COD hoặc đã xác nhận
  if (data.paymentMethod !== 'COD' || data.isCodConfirmed) {
    return null
  }

  const timeRemaining = getTimeRemaining(data.codConfirmationDeadlineUtc)

  const handleConfirm = () => {
    setActionType('confirm')
    setConfirmDialogOpen(true)
  }

  const handleReject = () => {
    setActionType('reject')
    setRejectDialogOpen(true)
  }

  const handleSubmit = async () => {
    try {
      await confirmMutation.mutateAsync({
        orderId,
        accepted: actionType === 'confirm',
      })
      setConfirmDialogOpen(false)
      setRejectDialogOpen(false)
    } catch (err) {
      console.error('COD confirmation failed:', err)
    }
  }

  return (
    <>
      <Box sx={{ p: 2.5, borderRadius: 2, border: '1px solid', borderColor: timeRemaining.expired ? 'error.main' : 'warning.main', bgcolor: timeRemaining.expired ? 'error.50' : 'warning.50' }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <LocalShippingRounded sx={{ color: 'warning.main' }} />
            <Typography sx={{ fontWeight: 800, fontSize: '1.1rem' }}>
              Xác nhận nhận hàng (COD)
            </Typography>
            <Chip
              size="small"
              color={data.isCodConfirmed ? 'success' : timeRemaining.expired ? 'error' : 'warning'}
              label={data.isCodConfirmed ? 'Đã xác nhận' : timeRemaining.expired ? 'Quá hạn' : 'Chờ xác nhận'}
            />
          </Stack>

          <Typography variant="body2" color="text.secondary">
            Đơn hàng của bạn đã hoàn thành kiểm tra chất lượng. Vui lòng xác nhận bạn sẽ nhận hàng để chúng tôi tiến hành giao hàng.
          </Typography>

          {!data.isCodConfirmed && !timeRemaining.expired && (
            <>
              <Box sx={{ p: 2, borderRadius: 1, bgcolor: 'background.paper' }}>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1 }}>
                  <ScheduleRounded color="warning" />
                  <Typography variant="body2" sx={{ fontWeight: 600 }}>
                    Thời gian còn lại để xác nhận:
                  </Typography>
                </Stack>
                <Typography variant="h6" sx={{ fontWeight: 800, color: timeRemaining.hours < 2 ? 'error.main' : 'warning.main', fontFamily: 'monospace' }}>
                  {String(timeRemaining.hours).padStart(2, '0')}:{String(timeRemaining.minutes).padStart(2, '0')}:{String(timeRemaining.seconds).padStart(2, '0')}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                 Deadline: {fmtDateTime(data.codConfirmationDeadlineUtc)}
                </Typography>
              </Box>

              <Alert severity="warning" sx={{ mt: 1 }}>
                <strong>Lưu ý:</strong> Nếu bạn không xác nhận trong thời gian quy định, đơn hàng sẽ tự động hủy và tiền cọc sẽ không được hoàn lại.
              </Alert>

              <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
                <Button
                  variant="contained"
                  color="success"
                  startIcon={<LocalShippingRounded />}
                  onClick={handleConfirm}
                  disabled={confirmMutation.isPending}
                  sx={{ flex: 1 }}
                >
                  {confirmMutation.isPending ? 'Đang xử lý...' : 'Xác nhận sẽ nhận hàng'}
                </Button>
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<CancelRounded />}
                  onClick={handleReject}
                  disabled={confirmMutation.isPending}
                  sx={{ flex: 1 }}
                >
                  Từ chối nhận hàng
                </Button>
              </Stack>
            </>
          )}

          {timeRemaining.expired && !data.isCodConfirmed && (
            <Alert severity="error">
              Đã quá thời gian xác nhận. Đơn hàng sẽ được tự động hủy. Vui lòng liên hệ hỗ trợ nếu bạn vẫn muốn nhận hàng.
            </Alert>
          )}

          {data.isCodConfirmed && (
            <Alert severity="success">
              Bạn đã xác nhận sẽ nhận hàng. Đơn hàng sẽ được giao trong thời gian sớm nhất.
            </Alert>
          )}
        </Stack>
      </Box>

      {/* Confirm Dialog */}
      <Dialog open={confirmDialogOpen} onClose={() => setConfirmDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 800 }}>Xác nhận nhận hàng</DialogTitle>
        <DialogContent>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Bạn có chắc chắn muốn nhận đơn hàng này không?
          </Typography>
          <Alert severity="info">
            Khi xác nhận, đơn hàng sẽ được chuyển sang trạng thái "Đang giao". Bạn sẽ thanh toán số tiền còn lại (50%) khi nhận hàng.
          </Alert>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setConfirmDialogOpen(false)} color="inherit" disabled={confirmMutation.isPending}>
            Hủy
          </Button>
          <Button
            variant="contained"
            color="success"
            onClick={handleSubmit}
            disabled={confirmMutation.isPending}
            startIcon={confirmMutation.isPending ? <CircularProgress size={20} /> : <LocalShippingRounded />}
          >
            {confirmMutation.isPending ? 'Đang xử lý...' : 'Xác nhận'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Reject Dialog */}
      <Dialog open={rejectDialogOpen} onClose={() => setRejectDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 800, color: 'error.main' }}>Từ chối nhận hàng</DialogTitle>
        <DialogContent>
          <Typography variant="body1" sx={{ mb: 2 }}>
            Bạn có chắc chắn muốn từ chối nhận đơn hàng này không?
          </Typography>
          <Alert severity="error" sx={{ mb: 2 }}>
            <strong>Cảnh báo:</strong> Khi từ chối nhận hàng, tiền cọc (50% giá trị đơn hàng) sẽ không được hoàn lại theo chính sách BR-PAY-010.
          </Alert>
          <Typography variant="body2" color="text.secondary">
            Nếu bạn gặp vấn đề với đơn hàng (hàng lỗi, sai sản phẩm, v.v.), vui lòng liên hệ hỗ trợ thay vì từ chối nhận hàng.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setRejectDialogOpen(false)} color="inherit" disabled={confirmMutation.isPending}>
            Hủy
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleSubmit}
            disabled={confirmMutation.isPending}
            startIcon={confirmMutation.isPending ? <CircularProgress size={20} /> : <CancelRounded />}
          >
            {confirmMutation.isPending ? 'Đang xử lý...' : 'Xác nhận từ chối'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
