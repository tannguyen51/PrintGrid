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
  MenuItem,
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
import { CheckRounded, CloseRounded, DownloadRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useExportRefunds, useProcessRefund, useRefunds, type RefundRequestRow } from './useRefunds'

const fmtVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(amount)

const statusColor = (status: string): 'warning' | 'success' | 'error' | 'default' =>
  status === 'Pending' ? 'warning' : status === 'Approved' ? 'success' : status === 'Rejected' ? 'error' : 'default'

/**
 * FR-ANAL-005 — refund processing & reconciliation. Ops works the pending queue; approving
 * executes the refund within the original order's scope (BR-LOG-002 / AC02) and the CSV export
 * feeds the finance reconciliation step.
 */
export default function RefundsPage() {
  const [status, setStatus] = useState('Pending')
  const [target, setTarget] = useState<RefundRequestRow | null>(null)
  const [note, setNote] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const { data, isLoading, isError, error } = useRefunds(status)
  const processRefund = useProcessRefund()
  const exportCsv = useExportRefunds()

  const rows = data ?? []

  async function decide(approve: boolean) {
    if (!target) return
    setActionError(null)
    try {
      await processRefund.mutateAsync({ id: target.id, approve, note: note.trim() || undefined })
      setTarget(null)
      setNote('')
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Không xử lý được yêu cầu hoàn tiền.')
    }
  }

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Xử lý hoàn tiền
              </Typography>
              <Typography color="text.secondary">
                Duyệt hoặc từ chối yêu cầu hoàn tiền đang chờ. Mọi khoản hoàn chỉ áp dụng trong phạm vi giá trị của đơn gốc.
              </Typography>
            </Box>
          </Stack>
          <Button
            variant="contained"
            startIcon={<DownloadRounded />}
            disabled={exportCsv.isPending || isLoading}
            onClick={() => exportCsv.mutate(status)}
          >
            {exportCsv.isPending ? 'Đang xuất…' : 'Xuất CSV'}
          </Button>
        </Stack>

        <Stack direction="row" spacing={2} alignItems="center">
          <TextField
            select
            size="small"
            label="Trạng thái"
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            sx={{ minWidth: 200 }}
          >
            <MenuItem value="Pending">Chờ xử lý</MenuItem>
            <MenuItem value="Approved">Đã duyệt</MenuItem>
            <MenuItem value="Rejected">Đã từ chối</MenuItem>
          </TextField>
          <Typography variant="body2" color="text.disabled">
            {rows.length} yêu cầu
          </Typography>
        </Stack>

        {(isError || actionError) && (
          <Alert severity="error">
            {actionError ?? 'Không tải được danh sách hoàn tiền.'}
            {!actionError && error instanceof Error ? ` (${error.message})` : ''}
          </Alert>
        )}
        {exportCsv.isError && <Alert severity="error">Không xuất được tệp CSV.</Alert>}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center' }} />
        ) : rows.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <Typography color="text.secondary">Không có yêu cầu hoàn tiền nào ở trạng thái này.</Typography>
          </Box>
        ) : (
          <TableContainer sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Mã đơn</TableCell>
                  <TableCell align="right">Số tiền</TableCell>
                  <TableCell>Lý do</TableCell>
                  <TableCell>Mã thanh toán</TableCell>
                  <TableCell>Ngày tạo</TableCell>
                  <TableCell>Trạng thái</TableCell>
                  <TableCell align="right">Thao tác</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} hover>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.78rem' }}>{row.orderId.slice(0, 8)}…</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700 }}>{fmtVnd(row.amount)}</TableCell>
                    <TableCell sx={{ maxWidth: 280 }}>{row.reason}</TableCell>
                    <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{row.paymentTransactionId}</TableCell>
                    <TableCell>{new Date(row.createdAtUtc).toLocaleString('vi-VN')}</TableCell>
                    <TableCell>
                      <Chip size="small" color={statusColor(row.status)} label={row.status} />
                    </TableCell>
                    <TableCell align="right">
                      {row.status === 'Pending' && (
                        <Button
                          size="small"
                          variant="outlined"
                          onClick={() => {
                            setTarget(row)
                            setNote('')
                            setActionError(null)
                          }}
                        >
                          Xử lý
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Stack>

      <Dialog open={target !== null} onClose={() => !processRefund.isPending && setTarget(null)} fullWidth maxWidth="sm">
        <DialogTitle>Xử lý yêu cầu hoàn tiền</DialogTitle>
        <DialogContent>
          {target && (
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Typography variant="body2" color="text.secondary">
                Số tiền: <strong style={{ color: '#fff' }}>{fmtVnd(target.amount)}</strong> · Đơn gốc:{' '}
                <strong style={{ color: '#fff' }}>{target.orderId}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary">{target.reason}</Typography>
              <TextField
                label="Ghi chú xử lý (không bắt buộc)"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                multiline
                minRows={2}
                inputProps={{ maxLength: 500 }}
              />
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button
            color="error"
            startIcon={<CloseRounded />}
            disabled={processRefund.isPending}
            onClick={() => decide(false)}
          >
            Từ chối
          </Button>
          <Button
            variant="contained"
            color="success"
            startIcon={processRefund.isPending ? <CircularProgress size={16} color="inherit" /> : <CheckRounded />}
            disabled={processRefund.isPending}
            onClick={() => decide(true)}
          >
            {processRefund.isPending ? 'Đang xử lý…' : 'Duyệt & hoàn tiền'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
