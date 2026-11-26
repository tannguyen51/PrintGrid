import { useMemo, useState } from 'react'
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControl, InputLabel, MenuItem, Select, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material'
import { CheckRounded, RefreshRounded, ReceiptLongOutlined, SyncRounded } from '@mui/icons-material'
import { useQueryClient } from '@tanstack/react-query'
import { OpsStatCard } from './OpsComponents'
import { useMarkPaid } from './useMarkPaid'
import { getPaymentDetail, PENDING_PAYMENTS_KEY, syncPaymentReconciliation, type PaymentDetail, type PaymentOverviewRow, type PaymentStatus, usePendingPayments } from './usePendingPayments'

const money = (value: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(value)
const dateTime = (value: string) => new Date(value).toLocaleString('vi-VN')
const labels: Record<PaymentStatus, string> = { Unpaid: 'Chưa thanh toán', Partial: 'Thanh toán một phần', Paid: 'Đã thanh toán đầy đủ' }

export default function PaymentsPage() {
  const queryClient = useQueryClient()
  const [filter, setFilter] = useState<'All' | PaymentStatus>('All')
  const [search, setSearch] = useState('')
  const [detail, setDetail] = useState<PaymentDetail | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)
  const [payTarget, setPayTarget] = useState<PaymentOverviewRow | null>(null)
  const [amount, setAmount] = useState('')
  const [reference, setReference] = useState('')
  const [syncing, setSyncing] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const { data = [], isLoading, isError, refetch, isFetching } = usePendingPayments(filter, search)
  const markPaid = useMarkPaid()
  const totals = useMemo(() => ({ unpaid: data.filter(x => x.paymentStatus === 'Unpaid').length, partial: data.filter(x => x.paymentStatus === 'Partial').length, paid: data.filter(x => x.paymentStatus === 'Paid').length }), [data])

  async function showDetail(row: PaymentOverviewRow) {
    setDetailLoading(true)
    try { setDetail(await getPaymentDetail(row.orderId)) } finally { setDetailLoading(false) }
  }
  async function sync() {
    setSyncing(true)
    try {
      const result = await syncPaymentReconciliation()
      await queryClient.invalidateQueries({ queryKey: PENDING_PAYMENTS_KEY })
      setNotice(`Đã đồng bộ: bổ sung ${result.ledgerCreated} giao dịch ledger, tạo ${result.labCreated} và điều chỉnh ${result.labCorrected} khoản công nợ Lab.`)
    } finally { setSyncing(false) }
  }
  async function recordPayment() {
    if (!payTarget || Number(amount) <= 0 || Number(amount) > payTarget.remaining) return
    await markPaid.mutateAsync({ orderId: payTarget.orderId, amount: Number(amount), paymentReference: reference })
    setPayTarget(null); setAmount(''); setReference(''); setNotice('Đã ghi nhận giao dịch và cập nhật trạng thái thanh toán.')
  }

  return <Box className="ops-page"><Stack className="ops-page-body" spacing={2.5}>
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
      <Box><Typography variant="h1">Quản lý thanh toán đơn hàng</Typography><Typography color="text.secondary">Theo dõi toàn bộ đơn, lịch sử giao dịch và trạng thái đối soát với Lab.</Typography></Box>
      <Stack direction="row" spacing={1}><Button startIcon={syncing ? <CircularProgress size={16} /> : <SyncRounded />} onClick={sync} disabled={syncing}>Đồng bộ đối soát</Button><Button startIcon={<RefreshRounded />} onClick={() => refetch()} disabled={isFetching}>Làm mới</Button></Stack>
    </Stack>
    {notice && <Alert severity="success" onClose={() => setNotice(null)}>{notice}</Alert>}
    {isError && <Alert severity="error">Không tải được danh sách thanh toán.</Alert>}
    <Box className="ops-summary-grid">
      <OpsStatCard label="Chưa thanh toán" value={totals.unpaid} hint="Chưa ghi nhận giao dịch" icon={<ReceiptLongOutlined />} loading={isLoading} />
      <OpsStatCard label="Thanh toán một phần" value={totals.partial} hint="Đã trả nhưng chưa đủ" icon={<ReceiptLongOutlined />} loading={isLoading} tone="warning" />
      <OpsStatCard label="Đã thanh toán" value={totals.paid} hint="Đã thanh toán đầy đủ" icon={<CheckRounded />} loading={isLoading} tone="success" />
    </Box>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><FormControl sx={{ minWidth: 230 }}><InputLabel>Trạng thái thanh toán</InputLabel><Select value={filter} label="Trạng thái thanh toán" onChange={e => setFilter(e.target.value as 'All' | PaymentStatus)}><MenuItem value="All">Tất cả</MenuItem><MenuItem value="Unpaid">Chưa thanh toán</MenuItem><MenuItem value="Partial">Thanh toán một phần</MenuItem><MenuItem value="Paid">Đã thanh toán đầy đủ</MenuItem></Select></FormControl><TextField fullWidth label="Tìm theo mã đơn hoặc mã khách hàng" value={search} onChange={e => setSearch(e.target.value)} /></Stack>
    {isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : <TableContainer sx={{ border: '1px solid #E8EBF2', borderRadius: 2 }}><Table size="small">
      <TableHead><TableRow><TableCell>Đơn hàng / Khách hàng</TableCell><TableCell>Trạng thái</TableCell><TableCell align="right">Cần thanh toán</TableCell><TableCell align="right">Đã thanh toán</TableCell><TableCell align="right">Còn thiếu</TableCell><TableCell>Đối soát</TableCell><TableCell align="right">Thao tác</TableCell></TableRow></TableHead>
      <TableBody>{data.map(row => <TableRow key={row.orderId} hover><TableCell><Typography fontWeight={800}>{row.orderNumber}</Typography><Typography variant="caption" color="text.secondary">{row.customerName || row.customerEmail || row.customerId}</Typography></TableCell><TableCell><Chip size="small" color={row.paymentStatus === 'Paid' ? 'success' : row.paymentStatus === 'Partial' ? 'warning' : 'default'} label={labels[row.paymentStatus]} /></TableCell><TableCell align="right">{money(row.totalAmount)}</TableCell><TableCell align="right">{money(row.paidAmount)}</TableCell><TableCell align="right" sx={{ fontWeight: 800, color: row.remaining ? 'warning.main' : 'success.main' }}>{money(row.remaining)}</TableCell><TableCell><Chip size="small" variant="outlined" color={row.reconciliationStatus === 'Synced' ? 'success' : 'error'} label={row.reconciliationStatus === 'Synced' ? 'Đồng bộ' : 'Có chênh lệch'} />{row.reconciliationIssues.map(x => <Typography key={x} display="block" variant="caption" color="error">{x}</Typography>)}</TableCell><TableCell align="right"><Stack direction="row" justifyContent="flex-end"><Button size="small" onClick={() => showDetail(row)}>Chi tiết</Button>{row.remaining > 0 && <Button size="small" variant="outlined" onClick={() => { setPayTarget(row); setAmount(String(row.remaining)); setReference('') }}>Ghi nhận tiền</Button>}</Stack></TableCell></TableRow>)}</TableBody>
    </Table></TableContainer>}
  </Stack>
  <Dialog open={detail !== null || detailLoading} onClose={() => setDetail(null)} fullWidth maxWidth="md"><DialogTitle>Chi tiết thanh toán {detail?.orderNumber}</DialogTitle><DialogContent>{detailLoading ? <CircularProgress /> : detail && <Stack spacing={2}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={3}><Typography>Tổng: <b>{money(detail.totalAmount)}</b></Typography><Typography>Đã trả: <b>{money(detail.paidAmount)}</b></Typography><Typography>Còn thiếu: <b>{money(detail.remaining)}</b></Typography><Typography>Đã hoàn: <b>{money(detail.refundedAmount)}</b></Typography></Stack><Typography variant="h6">Lịch sử giao dịch</Typography>{detail.transactions.length === 0 ? <Typography color="text.secondary">Chưa có giao dịch trong ledger.</Typography> : detail.transactions.map(t => <Box key={t.id} sx={{ p: 1.5, border: '1px solid #E8EBF2', borderRadius: 1 }}><Stack direction="row" justifyContent="space-between"><Typography>{t.type} · {t.reference}</Typography><Typography fontWeight={800}>{money(t.amount)}</Typography></Stack><Typography variant="caption" color="text.secondary">{dateTime(t.confirmedAtUtc)} · {t.status}</Typography></Box>)}<Typography variant="h6">Công nợ Lab</Typography>{detail.labPayables.length === 0 ? <Typography color="text.secondary">Chưa phát sinh công nợ Lab.</Typography> : detail.labPayables.map(p => <Box key={p.id} sx={{ p: 1.5, border: '1px solid #E8EBF2', borderRadius: 1 }}><Stack direction="row" justifyContent="space-between"><Typography>Lab {p.labId.slice(0, 8)} · {p.status}</Typography><Typography fontWeight={800}>{money(p.netAmount)}</Typography></Stack></Box>)}</Stack>}</DialogContent><DialogActions><Button onClick={() => setDetail(null)}>Đóng</Button></DialogActions></Dialog>
  <Dialog open={payTarget !== null} onClose={() => setPayTarget(null)} fullWidth maxWidth="sm"><DialogTitle>Ghi nhận thanh toán</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}><Typography>Đơn <b>{payTarget?.orderNumber}</b> · còn thiếu {money(payTarget?.remaining ?? 0)}</Typography><TextField label="Số tiền đã nhận" type="number" value={amount} onChange={e => setAmount(e.target.value)} /><TextField label="Mã tham chiếu giao dịch" value={reference} onChange={e => setReference(e.target.value)} />{markPaid.isError && <Alert severity="error">Không thể ghi nhận giao dịch.</Alert>}</Stack></DialogContent><DialogActions><Button onClick={() => setPayTarget(null)}>Hủy</Button><Button variant="contained" startIcon={<CheckRounded />} disabled={markPaid.isPending || Number(amount) <= 0 || Number(amount) > (payTarget?.remaining ?? 0)} onClick={recordPayment}>Xác nhận</Button></DialogActions></Dialog>
  </Box>
}
