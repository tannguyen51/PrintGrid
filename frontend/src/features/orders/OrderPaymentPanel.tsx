import { Alert, Box, Chip, CircularProgress, Divider, Stack, Typography } from '@mui/material'
import { AccountBalanceWalletRounded } from '@mui/icons-material'
import { useOrderPayment } from './useOrderPayment'

const fmtVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount)

function AmountRow({ label, value, emphasis }: { label: string; value: number; emphasis?: boolean }) {
  return (
    <Stack direction="row" justifyContent="space-between" alignItems="baseline">
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
      <Typography
        variant="body2"
        sx={{ fontWeight: emphasis ? 800 : 600, color: emphasis ? 'warning.main' : 'text.primary' }}
      >
        {fmtVnd(value)}
      </Typography>
    </Stack>
  )
}

/**
 * Deposit/settlement summary shown inside the customer's order drawer while the order
 * is PaymentPending. The customer pays by bank transfer and ops confirms it with
 * POST /orders/{orderId}/mark-paid — there is no QR flow.
 */
export function OrderPaymentPanel({ orderId }: { orderId: string }) {
  const { data, isLoading, isError, error } = useOrderPayment(orderId)

  return (
    <Box>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
        <AccountBalanceWalletRounded fontSize="small" sx={{ color: 'warning.main' }} />
        <Typography sx={{ fontWeight: 800, color: 'text.primary' }}>Thanh toán tiền cọc</Typography>
        {data && (
          <Chip
            size="small"
            color={data.isSettled ? 'success' : data.isDeposited ? 'info' : 'warning'}
            label={data.isSettled ? 'Đã thanh toán đủ' : data.isDeposited ? 'Đã đặt cọc' : 'Chưa đặt cọc'}
          />
        )}
      </Stack>

      {isLoading && (
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ py: 2 }}>
          <CircularProgress size={22} />
          <Typography variant="body2" color="text.secondary">Đang tải thông tin thanh toán…</Typography>
        </Stack>
      )}

      {isError && (
        <Alert severity="error">
          Không tải được thông tin thanh toán.
          {error instanceof Error ? ` (${error.message})` : ''}
        </Alert>
      )}

      {data && (
        <Stack spacing={1.25}>
          <Box
            sx={{
              p: 2,
              borderRadius: 2,
              border: '1px solid rgba(255,255,255,0.1)',
              bgcolor: 'rgba(255,255,255,0.02)',
            }}
          >
            <Stack spacing={1}>
              <AmountRow label="Tổng giá trị đơn" value={data.totalAmount} />
              <AmountRow label="Tiền cọc cần thanh toán" value={data.depositAmount} />
              <AmountRow label="Đã thanh toán" value={data.paidAmount} />
              <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
              <AmountRow label="Còn lại" value={data.remaining} emphasis />
            </Stack>
          </Box>

          {!data.isDeposited ? (
            <Alert severity="warning">
              Vui lòng chuyển khoản số tiền cọc <strong>{fmtVnd(data.depositAmount)}</strong> cho PrintGrid.
              Sau khi nhận được tiền, bộ phận vận hành sẽ xác nhận và đơn của bạn chuyển sang trạng thái
              “Đã xác nhận”. Ghi rõ mã đơn hàng trong nội dung chuyển khoản.
            </Alert>
          ) : !data.isSettled ? (
            <Alert severity="info">
              Bạn đã thanh toán đủ tiền cọc. Số tiền còn lại <strong>{fmtVnd(data.remaining)}</strong> sẽ
              được thanh toán khi đơn hoàn tất.
            </Alert>
          ) : (
            <Alert severity="success">Đơn hàng đã được thanh toán đầy đủ.</Alert>
          )}
        </Stack>
      )}
    </Box>
  )
}
