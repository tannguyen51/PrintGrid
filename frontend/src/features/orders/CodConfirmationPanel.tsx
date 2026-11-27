import { useEffect, useState } from 'react'
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
import { useConfirmCodReceipt } from './useCodConfirmation'
import type { Order } from '../../shared/types/order'
import { getApiErrorMessage } from '../../shared/api/apiError'

const fmtDateTime = (d: string | null | undefined) =>
  d ? new Date(d).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' }) : '—'

function getTimeRemaining(deadline: string | null | undefined, now: number) {
  if (!deadline) return { hours: 0, minutes: 0, seconds: 0, expired: false, known: false }
  const diff = new Date(deadline).getTime() - now
  if (diff <= 0) return { hours: 0, minutes: 0, seconds: 0, expired: true, known: true }
  return {
    hours: Math.floor(diff / 3_600_000),
    minutes: Math.floor((diff % 3_600_000) / 60_000),
    seconds: Math.floor((diff % 60_000) / 1000),
    expired: false,
    known: true,
  }
}

/**
 * COD (Cash on Delivery) receipt confirmation panel for the customer (BR-PAY-008 / FR-CUST-012).
 * Reads its state straight from the order list payload — no separate fetch. Renders nothing for
 * non-COD orders; for COD orders waiting in QualityCheck it asks the customer to confirm they will
 * receive the parcel so the hub is allowed to ship it.
 */
export function CodConfirmationPanel({ order }: { order: Order }) {
  const confirmMutation = useConfirmCodReceipt(order.id)
  const [dialog, setDialog] = useState<'none' | 'confirm' | 'reject'>('none')
  const [now, setNow] = useState(Date.now)

  const isCod = (order.paymentMethod ?? 'COD').toUpperCase() === 'COD'
  useEffect(() => {
    if (!isCod || order.status !== 'QualityCheck' || order.isCodConfirmed || !order.codConfirmationDeadlineUtc) return
    setNow(Date.now())
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [isCod, order.id, order.status, order.isCodConfirmed, order.codConfirmationDeadlineUtc])
  // Only relevant while the parcel is ready at the hub, before the waybill is written.
  if (!isCod || order.status !== 'QualityCheck') return null

  const t = getTimeRemaining(order.codConfirmationDeadlineUtc, now)

  if (order.isCodConfirmed) {
    return (
      <Alert severity="success" icon={<LocalShippingRounded />}>
        Bạn đã xác nhận sẽ nhận hàng. Đơn hàng sẵn sàng để hub ghi vận đơn và giao đi.
      </Alert>
    )
  }

  const submit = async (accepted: boolean) => {
    try {
      await confirmMutation.mutateAsync(accepted)
      setDialog('none')
    } catch {
      // Keep the dialog open; MUI Dialog shows nothing special — errors surface via the Alert below.
    }
  }

  return (
    <>
      <Box
        sx={{
          p: 2.5,
          borderRadius: 2,
          border: '1px solid',
          borderColor: t.expired ? 'error.main' : 'warning.main',
          bgcolor: t.expired ? 'error.50' : 'warning.50',
        }}
      >
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <LocalShippingRounded sx={{ color: 'warning.main' }} />
            <Typography sx={{ fontWeight: 800, fontSize: '1.1rem' }}>Xác nhận nhận hàng (COD)</Typography>
            <Chip size="small" color={t.expired ? 'error' : 'warning'} label={t.expired ? 'Quá hạn' : 'Chờ xác nhận'} />
          </Stack>

          <Typography variant="body2" color="text.secondary">
            Hàng của bạn đã xong kiểm tra chất lượng. Xác nhận bạn sẽ nhận đơn để hub tiến hành giao. Bạn thanh toán{' '}
            <strong>{new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(Math.max(0, order.totalAmount - (order.depositAmount ?? 0)))}</strong>{' '}
            còn lại khi nhận hàng.
          </Typography>

          {t.known && !t.expired && (
            <Box sx={{ p: 2, borderRadius: 1, bgcolor: 'background.paper' }}>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                <ScheduleRounded color="warning" />
                <Typography variant="body2" sx={{ fontWeight: 600 }}>Thời gian còn lại để xác nhận:</Typography>
              </Stack>
              <Typography variant="h6" sx={{ fontWeight: 800, fontFamily: 'monospace', color: t.hours < 2 ? 'error.main' : 'warning.main' }}>
                {String(t.hours).padStart(2, '0')}:{String(t.minutes).padStart(2, '0')}:{String(t.seconds).padStart(2, '0')}
              </Typography>
              <Typography variant="caption" color="text.secondary">Hạn: {fmtDateTime(order.codConfirmationDeadlineUtc)}</Typography>
            </Box>
          )}

          {!t.known && <Alert severity="info">Hub đang chuẩn bị yêu cầu xác nhận. Vui lòng chờ thông báo và hạn xác nhận của đơn.</Alert>}
          {t.expired ? (
            <Alert severity="error">
              Đã quá thời gian xác nhận. Đơn sẽ sớm bị hủy tự động và tiền cọc không hoàn lại. Liên hệ hỗ trợ nếu bạn vẫn muốn nhận.
            </Alert>
          ) : (
            <>
              <Alert severity="warning">
                Nếu không xác nhận trong thời gian quy định, đơn tự động hủy và <strong>tiền cọc không được hoàn lại</strong>.
              </Alert>
              {confirmMutation.isError && (
                <Alert severity="error">{getApiErrorMessage(confirmMutation.error, 'Không xử lý được yêu cầu COD. Vui lòng thử lại.')}</Alert>
              )}
              <Stack direction="row" spacing={2}>
                <Button
                  variant="contained"
                  color="success"
                  startIcon={<LocalShippingRounded />}
                  onClick={() => setDialog('confirm')}
                  disabled={confirmMutation.isPending || !t.known}
                  sx={{ flex: 1 }}
                >
                  Xác nhận sẽ nhận hàng
                </Button>
                <Button
                  variant="outlined"
                  color="error"
                  startIcon={<CancelRounded />}
                  onClick={() => setDialog('reject')}
                  disabled={confirmMutation.isPending || !t.known}
                  sx={{ flex: 1 }}
                >
                  Từ chối nhận hàng
                </Button>
              </Stack>
            </>
          )}
        </Stack>
      </Box>

      <Dialog open={dialog !== 'none'} onClose={() => setDialog('none')} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ fontWeight: 800, color: dialog === 'reject' ? 'error.main' : undefined }}>
          {dialog === 'confirm' ? 'Xác nhận nhận hàng' : 'Từ chối nhận hàng'}
        </DialogTitle>
        <DialogContent>
          {t.expired && <Alert severity="error" sx={{ mb: 2 }}>Đã quá hạn xác nhận COD. Yêu cầu này không thể tiếp tục.</Alert>}
          {confirmMutation.isError && <Alert severity="error" sx={{ mb: 2 }}>{getApiErrorMessage(confirmMutation.error, 'Không xử lý được yêu cầu COD.')}</Alert>}
          {dialog === 'confirm' ? (
            <Alert severity="info">
              Sau khi xác nhận, hub sẽ ghi vận đơn và giao hàng. Bạn trả phần tiền còn lại khi nhận (COD).
            </Alert>
          ) : (
            <Alert severity="error">
              Từ chối nhận hàng: tiền cọc sẽ <strong>không được hoàn lại</strong> (BR-PAY-010). Nếu hàng lỗi, hãy liên hệ hỗ trợ thay vì từ chối.
            </Alert>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setDialog('none')} color="inherit" disabled={confirmMutation.isPending}>Hủy</Button>
          <Button
            variant="contained"
            color={dialog === 'confirm' ? 'success' : 'error'}
            disabled={confirmMutation.isPending || t.expired || !t.known}
            onClick={() => void submit(dialog === 'confirm')}
            startIcon={confirmMutation.isPending ? <CircularProgress size={18} color="inherit" /> : (dialog === 'confirm' ? <LocalShippingRounded /> : <CancelRounded />)}
          >
            {confirmMutation.isPending ? 'Đang xử lý…' : dialog === 'confirm' ? 'Xác nhận' : 'Từ chối'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
