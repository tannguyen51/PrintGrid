import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, InputAdornment, LinearProgress, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow, TextField, Typography } from '@mui/material'
import { FilterAltOffRounded, HistoryRounded, SearchRounded, VisibilityOutlined } from '@mui/icons-material'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { QuotePriceDetails } from './QuotePriceDetails'
import { QUOTE_STATUS_LABELS, type Quote, type QuoteStatus } from './quoteTypes'
import { useQuoteHistory } from './useQuotes'
import './quote-history.css'

const statusColor: Record<QuoteStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Pending: 'info', Draft: 'default', PendingReview: 'warning', Ready: 'success', Expired: 'default', Converted: 'info', Failed: 'error',
}
const formatTime = (value: string) => new Date(value).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })

export function QuoteHistoryPanel({ modelId, selectedQuoteId, onSelect }: { modelId: string; selectedQuoteId?: string; onSelect: (quote: Quote) => void }) {
  const [search, setSearch] = useState('')
  const [term, setTerm] = useState('')
  const [status, setStatus] = useState('all')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(5)
  const [detail, setDetail] = useState<Quote | null>(null)
  useEffect(() => { const timer = setTimeout(() => { setTerm(search.trim()); setPage(0) }, 300); return () => clearTimeout(timer) }, [search])
  useEffect(() => { setPage(0); setStatus('all'); setSearch(''); setFrom(''); setTo('') }, [modelId])
  const query = useQuoteHistory(modelId, { search: term, status, from, to, sort, page: page + 1, pageSize })
  const invalidRange = !!from && !!to && from > to
  useEffect(() => {
    if (query.data && !query.isPlaceholderData && page > Math.max(0, Math.ceil(query.data.totalCount / pageSize) - 1)) setPage(Math.max(0, Math.ceil(query.data.totalCount / pageSize) - 1))
  }, [query.data, query.isPlaceholderData, page, pageSize])
  const reset = () => { setSearch(''); setTerm(''); setStatus('all'); setFrom(''); setTo(''); setSort('newest'); setPage(0) }
  const selectQuote = (quote: Quote) => { onSelect(quote); setDetail(null) }
  const stats = [
    { label: 'Tất cả báo giá', value: query.data?.totalQuotes, filter: 'all' },
    { label: 'Bản nháp', value: query.data?.draftQuotes, filter: 'Draft' },
    { label: 'Đang chờ duyệt', value: query.data?.pendingReviewQuotes, filter: 'PendingReview' },
    { label: 'Sẵn sàng đặt hàng', value: query.data?.readyQuotes, filter: 'Ready' },
  ]
  return <Paper variant="outlined" className="quote-history">
    <Box className="quote-history-heading"><Box><Stack direction="row" gap={1} alignItems="center"><HistoryRounded color="primary" /><Typography component="h2" variant="h6">Báo giá đã tạo cho model này</Typography></Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mt: .75 }}>So sánh các lần tính giá và tiếp tục với báo giá bạn đã chọn.</Typography></Box>
      <Chip size="small" variant="outlined" label={`${query.data?.totalQuotes ?? '—'} báo giá`} />
    </Box>
    <Box className="quote-history-stats">{stats.map(item => <Button key={item.filter} className={`quote-history-stat${status === item.filter ? ' selected' : ''}`}
      onClick={() => { setStatus(item.filter); setPage(0) }}><span>{item.label}</span><strong>{item.value ?? '—'}</strong></Button>)}</Box>
    <Box className="quote-history-filters">
      <TextField size="small" label="Tìm mã, vật liệu, màu" value={search} onChange={e => setSearch(e.target.value)} inputProps={{ maxLength: 100 }}
        InputProps={{ startAdornment: <InputAdornment position="start"><SearchRounded fontSize="small" /></InputAdornment> }} />
      <TextField size="small" select label="Trạng thái" value={status} onChange={e => { setStatus(e.target.value); setPage(0) }}>
        <MenuItem value="all">Tất cả trạng thái</MenuItem>{Object.entries(QUOTE_STATUS_LABELS).map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
      </TextField>
      <TextField size="small" select label="Sắp xếp" value={sort} onChange={e => { setSort(e.target.value); setPage(0) }}>
        {[['newest', 'Mới nhất'], ['oldest', 'Cũ nhất'], ['price-low', 'Giá tăng dần'], ['price-high', 'Giá giảm dần']].map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
      </TextField>
      <TextField size="small" type="date" label="Tạo từ ngày" value={from} onChange={e => { setFrom(e.target.value); setPage(0) }} InputLabelProps={{ shrink: true }} />
      <TextField size="small" type="date" label="Đến ngày" value={to} onChange={e => { setTo(e.target.value); setPage(0) }} InputLabelProps={{ shrink: true }} error={invalidRange} />
      <Button size="small" startIcon={<FilterAltOffRounded />} onClick={reset}>Xóa bộ lọc</Button>
    </Box>
    {invalidRange && <Alert severity="error" sx={{ mx: 2.5, mb: 2 }}>Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.</Alert>}
    {query.isError && <Alert severity="error" sx={{ mx: 2.5, mb: 2 }} action={<Button onClick={() => void query.refetch()}>Thử lại</Button>}>{getApiErrorMessage(query.error, 'Không tải được lịch sử báo giá.')}</Alert>}
    <Box sx={{ height: 3 }}>{query.isFetching && <LinearProgress aria-label="Đang cập nhật lịch sử báo giá" />}</Box>
    <TableContainer aria-busy={query.isFetching}><Table aria-label="Báo giá đã tạo cho model này" size="small" sx={{ minWidth: 650 }}>
      <TableHead><TableRow><TableCell>Báo giá / ngày tạo</TableCell><TableCell>Cấu hình in</TableCell><TableCell align="right">Tổng giá in</TableCell><TableCell>Trạng thái</TableCell><TableCell align="right">Thao tác</TableCell></TableRow></TableHead>
      <TableBody>{!query.isLoading && !query.data?.items.length ? <TableRow><TableCell colSpan={5}><Box className="quote-history-empty"><HistoryRounded /><Typography fontWeight={650}>{query.data?.totalQuotes ? 'Không có báo giá phù hợp' : 'Chưa có báo giá cho model này'}</Typography><Typography variant="body2" color="text.secondary">{query.data?.totalQuotes ? 'Thử thay đổi trạng thái, từ khóa hoặc khoảng ngày.' : 'Chọn cấu hình in và bấm Nhận báo giá để xem giá dự kiến.'}</Typography></Box></TableCell></TableRow>
        : query.data?.items.map(quote => <TableRow key={quote.id} selected={quote.id === selectedQuoteId} hover>
          <TableCell><Typography className="quote-history-id">#{quote.id.slice(0, 8)}</Typography><Typography variant="body2" color="text.secondary" sx={{ fontSize: 13 }}>{formatTime(quote.createdAt)}</Typography></TableCell>
          <TableCell><Typography variant="body2" fontWeight={650}>{[...new Set(quote.items.map(i => i.materialCode))].join(', ')} · {quote.items.reduce((sum, i) => sum + i.quantity, 0)} SP</Typography><Typography variant="body2" color="text.secondary" sx={{ fontSize: 13 }}>{[...new Set(quote.items.map(i => i.colorCode))].join(', ')}</Typography></TableCell>
          <TableCell align="right"><Typography variant="body2" fontWeight={750} whiteSpace="nowrap">{quote.totalPrice.toLocaleString('vi-VN')} {quote.currency}</Typography></TableCell>
          <TableCell><Chip size="small" color={statusColor[quote.status]} label={QUOTE_STATUS_LABELS[quote.status]} /></TableCell>
          <TableCell align="right"><Stack direction="row" gap={.25} justifyContent="flex-end" alignItems="center">
            <IconButton size="small" aria-label={`Xem chi tiết báo giá ${quote.id.slice(0, 8)}`} onClick={() => setDetail(quote)}><VisibilityOutlined fontSize="small" /></IconButton>
            {['Draft', 'Ready'].includes(quote.status) ? <Button size="small" onClick={() => selectQuote(quote)} aria-label={`Tiếp tục báo giá ${quote.id.slice(0, 8)}`}>Tiếp tục</Button>
              : ['PendingReview', 'Converted'].includes(quote.status) && <Button size="small" component={Link} to="/orders">Xem đơn</Button>}
          </Stack></TableCell>
        </TableRow>)}</TableBody>
    </Table></TableContainer>
    <TablePagination component="div" count={query.data?.totalCount ?? 0} page={page} rowsPerPage={pageSize} rowsPerPageOptions={[5, 10, 20]}
      onPageChange={(_, value) => setPage(value)} onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0) }} labelRowsPerPage="Mỗi trang"
      getItemAriaLabel={type => ({ first: 'Trang đầu', last: 'Trang cuối', next: 'Trang tiếp theo', previous: 'Trang trước' })[type]}
      labelDisplayedRows={({ from: start, to: end, count }) => `${start}–${end} / ${count} báo giá`} />
    <Dialog open={!!detail} onClose={() => setDetail(null)} fullWidth maxWidth="sm"><DialogTitle>Chi tiết báo giá #{detail?.id.slice(0, 8)}</DialogTitle>
      <DialogContent>{detail && <Stack spacing={2} sx={{ pt: 1 }}><Chip label={QUOTE_STATUS_LABELS[detail.status]} color={statusColor[detail.status]} sx={{ alignSelf: 'flex-start' }} /><QuotePriceDetails quote={detail} /></Stack>}</DialogContent>
      <DialogActions><Button onClick={() => setDetail(null)}>Đóng</Button>{detail && ['Draft', 'Ready'].includes(detail.status) && <Button variant="contained" onClick={() => selectQuote(detail)}>Tiếp tục với báo giá này</Button>}</DialogActions>
    </Dialog>
  </Paper>
}
