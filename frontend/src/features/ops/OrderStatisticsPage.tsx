import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { JOB_LABELS } from '../jobs/jobTypes'
import { OpsPageHeader } from './OpsComponents'

interface OrderRow {
  id: string; orderNumber: string; status: string; quantity: number; createdAt: string
  requestedDeliveryDate: string | null; promisedDeliveryDate: string; dispatchMode: string
  totalAmount: number; paidAmount: number; refundedAmount: number; refundKind: 'None' | 'Partial' | 'Full'
  hasIncident: boolean; hasOpenIncident: boolean
}
interface Report {
  summary: { totalOrders: number; deliveredOrders: number; cancelledOrders: number; incidentOrders: number
    openIncidentOrders: number; refundedOrders: number; partialRefundOrders: number; fullyRefundedOrders: number }
  items: OrderRow[]; totalCount: number
}
interface Detail {
  orderNumber: string; status: string; requestedDeliveryDate: string | null; promisedDeliveryDate: string
  paidAmount: number; refundedAmount: number
  items: { id: string; quantity: number; completedQuantity: number; itemStatus: string }[]
  batches: { id: string; orderItemId: string; quantity: number; status: string; labName: string | null
    parentJobId: string | null; reprintIndex: number; failureReason: string | null; plannedEndUtc: string | null }[]
}
const statuses: Record<string, string> = { PaymentPending: 'Chờ thanh toán', Confirmed: 'Đã xác nhận', InProduction: 'Đang sản xuất', QualityCheck: 'Kiểm tra chất lượng', Shipping: 'Đang giao', Delivered: 'Đã giao', Cancelled: 'Đã hủy' }
const date = (value: string | null) => value ? new Date(value.includes('T') ? value : `${value}T00:00:00`).toLocaleDateString('vi-VN') : '—'
const money = (value: number) => `${value.toLocaleString('vi-VN')} đ`

export default function OrderStatisticsPage() {
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [category, setCategory] = useState('all')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(20)
  const [selected, setSelected] = useState<string | null>(null)
  const invalidRange = !!from && !!to && from > to
  const report = useQuery({ queryKey: ['ops-orders', from, to, category, search, page, pageSize], enabled: !invalidRange,
    queryFn: () => apiClient.get<Report>('/ops/orders', { params: { from: from || undefined, to: to || undefined, category, search: search || undefined, page: page + 1, pageSize } }).then(r => r.data) })
  const detail = useQuery({ queryKey: ['ops-order-detail', selected], enabled: !!selected,
    queryFn: () => apiClient.get<Detail>(`/ops/orders/${selected}`).then(r => r.data) })
  const summary = report.data?.summary
  const cards = [['Tổng đơn', summary?.totalOrders, 'all'], ['Giao thành công', summary?.deliveredOrders, 'delivered'],
    ['Đã hủy', summary?.cancelledOrders, 'cancelled'], ['Có lịch sử sự cố', summary?.incidentOrders, 'incident'],
    ['Sự cố chưa xử lý', summary?.openIncidentOrders, 'open-incident'], ['Có hoàn tiền', summary?.refundedOrders, 'refunded']] as const
  return <Box className="ops-page"><Stack spacing={3}>
    <OpsPageHeader eyebrow="Báo cáo vận hành" title="Thống kê đơn hàng" description="Theo ngày tạo đơn (giờ Việt Nam). Mỗi đơn được tính một lần dù chia nhiều xưởng hoặc in lại." />
    <Paper variant="outlined" sx={{ p: 2 }}><Stack direction={{ xs: 'column', md: 'row' }} gap={2}>
      <TextField type="date" label="Từ ngày tạo đơn" InputLabelProps={{ shrink: true }} value={from} onChange={e => { setFrom(e.target.value); setPage(0) }} />
      <TextField type="date" label="Đến ngày" InputLabelProps={{ shrink: true }} value={to} onChange={e => { setTo(e.target.value); setPage(0) }} />
      <TextField label="Tìm mã đơn" value={search} onChange={e => { setSearch(e.target.value); setPage(0) }} />
      <TextField select label="Nhóm đơn" value={category} onChange={e => { setCategory(e.target.value); setPage(0) }} sx={{ minWidth: 200 }}>
        {[['all', 'Tất cả'], ['delivered', 'Giao thành công'], ['cancelled', 'Đã hủy'], ['incident', 'Có lịch sử sự cố'], ['open-incident', 'Sự cố chưa xử lý'], ['refunded', 'Có hoàn tiền'], ['partial-refund', 'Hoàn một phần'], ['full-refund', 'Hoàn toàn bộ tiền đã thu']].map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
      </TextField><Button onClick={() => void report.refetch()} disabled={invalidRange || report.isFetching}>Làm mới</Button>
    </Stack></Paper>
    {invalidRange && <Alert severity="error">Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.</Alert>}
    {report.isError && <Alert severity="error">{getApiErrorMessage(report.error, 'Không tải được thống kê đơn hàng.')}</Alert>}
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', lg: 'repeat(3, 1fr)' }, gap: 2 }}>
      {cards.map(([label, value, filter]) => <Paper key={filter} variant="outlined" sx={{ p: 2 }}><Button onClick={() => { setCategory(filter); setPage(0) }}>{label}</Button><Typography variant="h4" sx={{ px: 1 }}>{value ?? '—'}</Typography></Paper>)}
    </Box>
    <Alert severity="info">Sự cố đã khắc phục vẫn được ghi nhận trong lịch sử và không biến đơn đã giao thành đơn thất bại. Các nhóm có thể giao nhau.
      {' '}Hoàn một phần: {summary?.partialRefundOrders ?? '—'} đơn; hoàn toàn bộ tiền đã thu: {summary?.fullyRefundedOrders ?? '—'} đơn.</Alert>
    <Paper variant="outlined"><TableContainer><Table aria-label="Danh sách đơn hàng theo thống kê">
      <TableHead><TableRow>{['Đơn hàng', 'Số lượng', 'Trạng thái', 'Ngày mong muốn / cam kết', 'Sự cố / hoàn tiền', ''].map((label, i) => <TableCell key={i}>{label}</TableCell>)}</TableRow></TableHead>
      <TableBody>{report.isLoading ? <TableRow><TableCell colSpan={6}><CircularProgress aria-label="Đang tải thống kê" /></TableCell></TableRow> : !report.data?.items.length ? <TableRow><TableCell colSpan={6}>Không có đơn phù hợp bộ lọc.</TableCell></TableRow> : report.data.items.map(order => <TableRow key={order.id}>
        <TableCell><Typography fontWeight={700}>{order.orderNumber}</Typography><Typography variant="body2">Tạo {date(order.createdAt)}</Typography><Chip size="small" label={order.dispatchMode === 'Automatic' ? 'Xét tự động' : 'Ops điều phối'} /></TableCell>
        <TableCell>{order.quantity}</TableCell><TableCell>{statuses[order.status] ?? order.status}</TableCell>
        <TableCell>{date(order.requestedDeliveryDate)} / {date(order.promisedDeliveryDate)}</TableCell>
        <TableCell><Stack gap={1}>{order.hasIncident && <Chip size="small" color={order.hasOpenIncident ? 'error' : 'warning'} label={order.hasOpenIncident ? 'Cần xử lý sự cố' : 'Có lịch sử sự cố'} />}
          {order.refundKind !== 'None' && <Typography variant="body2">{order.refundKind === 'Full' ? 'Hoàn toàn bộ tiền đã thu' : 'Hoàn một phần'}: {money(order.refundedAmount)}</Typography>}</Stack></TableCell>
        <TableCell><Button onClick={() => setSelected(order.id)}>Chi tiết</Button></TableCell>
      </TableRow>)}</TableBody>
    </Table></TableContainer><TablePagination component="div" count={report.data?.totalCount ?? 0} page={page} rowsPerPage={pageSize} rowsPerPageOptions={[10, 20, 50]}
      onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0) }} labelRowsPerPage="Số đơn mỗi trang" />
    </Paper>
  </Stack>
    <Dialog open={!!selected} onClose={() => setSelected(null)} fullWidth maxWidth="md"><DialogTitle>{detail.data?.orderNumber ?? 'Chi tiết đơn hàng'}</DialogTitle>
      <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>{detail.isLoading && <CircularProgress />}{detail.isError && <Alert severity="error">Không tải được chi tiết đơn.</Alert>}
        {detail.data && <><Typography>Ngày mong muốn: {date(detail.data.requestedDeliveryDate)} · Ngày cam kết: {date(detail.data.promisedDeliveryDate)}</Typography>
          <Typography>Đã thu: {money(detail.data.paidAmount)} · Đã hoàn: {money(detail.data.refundedAmount)}</Typography>
          {detail.data.items.map(item => <Paper key={item.id} variant="outlined" sx={{ p: 2 }}><Typography fontWeight={700}>Đã nghiệm thu {item.completedQuantity}/{item.quantity} sản phẩm</Typography>
            {detail.data!.batches.filter(batch => batch.orderItemId === item.id).map(batch => <Box key={batch.id} sx={{ mt: 2 }}>
              <Typography>{batch.labName ?? 'Chưa gán xưởng'} · {batch.quantity} sản phẩm · {(JOB_LABELS as Record<string, string>)[batch.status] ?? batch.status}</Typography>
              <Typography variant="body2" color="text.secondary">#{batch.id.slice(0, 8)}{batch.parentJobId ? ' · Lô đã chia' : ''}{batch.reprintIndex ? ` · In lại lần ${batch.reprintIndex}` : ''} · Dự kiến xong: {date(batch.plannedEndUtc)}</Typography>
              {batch.failureReason && <Typography variant="body2">{batch.failureReason}</Typography>}
            </Box>)}
          </Paper>)}</>}
      </Stack></DialogContent><DialogActions><Button onClick={() => setSelected(null)}>Đóng</Button></DialogActions>
    </Dialog>
  </Box>
}
