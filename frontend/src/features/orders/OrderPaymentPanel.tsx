import { Alert, Box, Button, Chip, CircularProgress, Divider, Stack, Typography } from '@mui/material'
import { AccountBalanceWalletRounded, ContentCopyRounded } from '@mui/icons-material'
import { useOrderPayment } from './useOrderPayment'

const fmtVnd = (amount: number) => new Intl.NumberFormat('vi-VN', {
  style: 'currency', currency: 'VND', maximumFractionDigits: 0,
}).format(amount)

function AmountRow({ label, value, emphasis }: { label: string; value: number; emphasis?: boolean }) {
  return <Stack direction="row" justifyContent="space-between" alignItems="baseline">
    <Typography variant="body2" color="text.secondary">{label}</Typography>
    <Typography variant="body2" sx={{ fontWeight: emphasis ? 800 : 600, color: emphasis ? 'warning.main' : 'text.primary' }}>
      {fmtVnd(value)}
    </Typography>
  </Stack>
}

const copy = (value: string) => navigator.clipboard.writeText(value)

export function OrderPaymentPanel({ orderId }: { orderId: string }) {
  const { data, isLoading, isError, error } = useOrderPayment(orderId)

  return <Box>
    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
      <AccountBalanceWalletRounded fontSize="small" sx={{ color: 'warning.main' }} />
      <Typography sx={{ fontWeight: 800 }}>Thanh toán đơn hàng</Typography>
      {data && <Chip size="small" color={data.isSettled ? 'success' : data.isDeposited ? 'info' : 'warning'}
        label={data.isSettled ? 'Đã thanh toán đủ' : data.isDeposited ? 'Đã đặt cọc' : 'Chờ thanh toán'} />}
    </Stack>

    {isLoading && <Stack direction="row" spacing={1.5} alignItems="center" sx={{ py: 2 }}>
      <CircularProgress size={22} /><Typography variant="body2">Đang tải thông tin thanh toán…</Typography>
    </Stack>}
    {isError && <Alert severity="error">Không tải được thông tin thanh toán.{error instanceof Error ? ` (${error.message})` : ''}</Alert>}

    {data && <Stack spacing={1.5}>
      <Box sx={{ p: 2, borderRadius: 2, border: '1px solid #E8EBF2', bgcolor: 'background.secondary' }}>
        <Stack spacing={1}>
          <AmountRow label="Tổng giá trị đơn" value={data.totalAmount} />
          <AmountRow label="Tiền cọc" value={data.depositAmount} />
          <AmountRow label="Đã thanh toán" value={data.paidAmount} />
          <Divider /><AmountRow label="Cần thanh toán lúc này" value={data.amountDue} emphasis />
        </Stack>
      </Box>

      {!data.isDeposited && data.paymentEnabled && data.qrUrl && <Box sx={{ p: 2, border: '1px solid #E8EBF2', borderRadius: 2 }}>
        <Stack alignItems="center" spacing={1.25}>
          <Box component="img" src={data.qrUrl} alt="Mã QR thanh toán SePay" sx={{ width: 240, maxWidth: '100%', borderRadius: 1 }} />
          <Typography variant="body2"><strong>{data.bankCode}</strong> · {data.accountNumber}</Typography>
          {data.accountName && <Typography variant="body2">{data.accountName}</Typography>}
          <Button size="small" startIcon={<ContentCopyRounded />} onClick={() => void copy(data.accountNumber ?? '')}>Sao chép số tài khoản</Button>
          <Typography variant="caption" color="text.secondary">Nội dung chuyển khoản bắt buộc</Typography>
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography sx={{ fontFamily: 'monospace', fontWeight: 800, wordBreak: 'break-all' }}>{data.transferDescription}</Typography>
            <Button size="small" onClick={() => void copy(data.transferDescription)}>Sao chép</Button>
          </Stack>
          <Alert severity="warning">Chuyển đúng <strong>{fmtVnd(data.amountDue)}</strong> và không sửa nội dung. Trạng thái sẽ tự cập nhật sau khi SePay xác nhận.</Alert>
        </Stack>
      </Box>}

      {!data.isDeposited && !data.paymentEnabled && <Alert severity="warning">
        Thanh toán SePay chưa được cấu hình. Vui lòng liên hệ PrintGrid để được hướng dẫn chuyển khoản.
      </Alert>}
      {data.isDeposited && !data.isSettled && <Alert severity="success">
        Đã nhận đủ tiền cọc. Số tiền còn lại là <strong>{fmtVnd(data.remaining)}</strong>.
      </Alert>}
      {data.isSettled && <Alert severity="success">Đơn hàng đã được thanh toán đầy đủ.</Alert>}
    </Stack>}
  </Box>
}
