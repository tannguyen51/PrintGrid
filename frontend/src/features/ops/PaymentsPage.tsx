import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { CheckRounded, PaymentsRounded, RefreshRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { usePendingPayments, type PendingPaymentRow } from './usePendingPayments'
import { useMarkPaid } from './useMarkPaid'

const fmtVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount)

const fmtDateTime = (value: string) => {
  if (!value) return '—'
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('vi-VN')
}

const customerOf = (row: PendingPaymentRow) =>
  row.customerName?.trim() ||
  row.customerEmail?.trim() ||
  (row.customerId ? `${row.customerId.slice(0, 8)}…` : '—')

/**
 * FR-CUST-012 / BR-PAY-007/008 — ops records deposits. Placement no longer confirms an
 * order: it becomes Confirmed only once the deposit has been received, so this queue is
 * the ops surface that unblocks production.
 */
export default function PaymentsPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = usePendingPayments()
  const markPaid = useMarkPaid()

  const [target, setTarget] = useState<PendingPaymentRow | null>(null)
  const [amount, setAmount] = useState('')
  const [reference, setReference] = useState('')
  const [notice, setNotice] = useState<string | null>(null)

  const rows = data ?? []

  const parsedAmount = Number(amount)
  const amountError =
    !amount.trim()
      ? 'Nhập số tiền đã nhận.'
      : !Number.isFinite(parsedAmount)
        ? 'Số tiền không hợp lệ.'
        : parsedAmount <= 0
          ? 'Số tiền phải lớn hơn 0.'
          : target && parsedAmount > target.remaining
            ? `Số tiền vượt quá số còn lại (${fmtVnd(target.remaining)}).`
            : null

  function openDialog(row: PendingPaymentRow) {
    markPaid.reset()
    setTarget(row)
    setAmount(String(row.remaining))
    setReference('')
    setNotice(null)
  }

  function closeDialog() {
    if (markPaid.isPending) return
    setTarget(null)
    setAmount('')
    setReference('')
  }

  async function confirmPaid() {
    if (!target || amountError) return
    try {
      await markPaid.mutateAsync({
        orderId: target.orderId,
        amount: parsedAmount,
        paymentReference: reference,
      })
      setNotice(`Đã ghi nhận ${fmtVnd(parsedAmount)} cho đơn ${target.orderNumber}.`)
      setTarget(null)
      setAmount('')
      setReference('')
    } catch {
      // The mutation's error is rendered inside the dialog via getApiErrorMessage.
    }
  }

  const serverError = markPaid.isError
    ? getApiErrorMessage(markPaid.error, 'Không ghi nhận được thanh toán. Vui lòng thử lại.')
    : null

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }} spacing={2.5}>
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
                Thanh toán & đặt cọc
              </Typography>
              <Typography color="text.secondary">
                Các đơn chưa thanh toán đủ tiền cọc. Xác nhận khoản tiền đã nhận để đơn chuyển sang trạng thái Đã xác nhận.
              </Typography>
            </Box>
          </Stack>
          <Button
            startIcon={<RefreshRounded />}
            onClick={() => refetch()}
            disabled={isFetching}
            sx={{ alignSelf: { xs: 'stretch', sm: 'center' } }}
          >
            Làm mới
          </Button>
        </Stack>

        {notice && (
          <Alert severity="success" onClose={() => setNotice(null)}>
            {notice}
          </Alert>
        )}
        {isError && (
          <Alert severity="error">
            Không tải được danh sách đơn chờ thanh toán.
            {error instanceof Error ? ` (${error.message})` : ''}
          </Alert>
        )}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center' }} />
        ) : rows.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <PaymentsRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Không có đơn nào đang chờ thanh toán cọc.</Typography>
          </Box>
        ) : (
          <TableContainer sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Mã đơn</TableCell>
                  <TableCell>Khách</TableCell>
                  <TableCell align="right">Tổng</TableCell>
                  <TableCell align="right">Cọc</TableCell>
                  <TableCell align="right">Đã trả</TableCell>
                  <TableCell align="right">Còn lại</TableCell>
                  <TableCell>Ngày tạo</TableCell>
                  <TableCell align="right">Hành động</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.orderId} hover>
                    <TableCell sx={{ fontFamily: 'monospace', fontWeight: 700 }}>{row.orderNumber}</TableCell>
                    <TableCell sx={{ maxWidth: 220 }}>{customerOf(row)}</TableCell>
                    <TableCell align="right">{fmtVnd(row.totalPrice)}</TableCell>
                    <TableCell align="right">{fmtVnd(row.depositAmount)}</TableCell>
                    <TableCell align="right">{fmtVnd(row.paidAmount)}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 800, color: 'warning.main' }}>
                      {fmtVnd(row.remaining)}
                    </TableCell>
                    <TableCell>{fmtDateTime(row.createdAt)}</TableCell>
                    <TableCell align="right">
                      <Button size="small" variant="outlined" onClick={() => openDialog(row)}>
                        Xác nhận
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Stack>

      <Dialog open={target !== null} onClose={closeDialog} fullWidth maxWidth="sm">
        <DialogTitle>Xác nhận đã nhận tiền</DialogTitle>
        <DialogContent>
          {target && (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Đơn <strong style={{ color: '#fff' }}>{target.orderNumber}</strong> · Khách{' '}
                <strong style={{ color: '#fff' }}>{customerOf(target)}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Tổng {fmtVnd(target.totalPrice)} · Cọc {fmtVnd(target.depositAmount)} · Đã trả{' '}
                {fmtVnd(target.paidAmount)} · Còn lại{' '}
                <strong style={{ color: '#fff' }}>{fmtVnd(target.remaining)}</strong>
              </Typography>

              <TextField
                label="Số tiền đã nhận"
                type="number"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                required
                error={Boolean(amountError) && amount.length > 0}
                helperText={amount.length > 0 ? amountError ?? 'Mặc định bằng số tiền còn lại.' : `Mặc định bằng số tiền còn lại (${fmtVnd(target.remaining)}).`}
                inputProps={{ min: 0, step: 1000 }}
              />
              <TextField
                label="Mã tham chiếu (không bắt buộc)"
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                placeholder="VD: FT25102412345"
                inputProps={{ maxLength: 100 }}
                helperText="Mã giao dịch ngân hàng để đối soát sau này."
              />

              {serverError && <Alert severity="error">{serverError}</Alert>}
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={closeDialog} disabled={markPaid.isPending}>
            Hủy
          </Button>
          <Button
            variant="contained"
            color="success"
            startIcon={markPaid.isPending ? <CircularProgress size={16} color="inherit" /> : <CheckRounded />}
            disabled={markPaid.isPending || Boolean(amountError)}
            onClick={confirmPaid}
          >
            {markPaid.isPending ? 'Đang ghi nhận…' : 'Xác nhận đã nhận tiền'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
