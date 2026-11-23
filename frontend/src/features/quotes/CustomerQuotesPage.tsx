import { useMemo, useState } from 'react'
import {
  Alert, Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogTitle,
  Grid, InputAdornment, LinearProgress, MenuItem, Stack, Table, TableBody, TableCell, TableContainer,
  TableHead, TablePagination, TableRow, TextField, Typography,
} from '@mui/material'
import { ArrowForwardRounded, PriceCheckRounded, ReceiptLongRounded, SearchRounded, VisibilityOutlined } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useModels } from '../models/useModels'
import { QuotePriceDetails } from './QuotePriceDetails'
import { QUOTE_STATUS_LABELS, type Quote, type QuoteStatus } from './quoteTypes'
import { useQuotes } from './useQuotes'

const statusColor: Record<QuoteStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Pending: 'info', Draft: 'default', PendingReview: 'warning', Ready: 'success', Expired: 'default', Converted: 'info', Failed: 'error',
}
const activeStatuses: QuoteStatus[] = ['Pending', 'Draft', 'PendingReview', 'Ready']
const EMPTY_QUOTES: Quote[] = []

function displayStatus(quote: Quote): QuoteStatus {
  return quote.status === 'Ready' && quote.expiresAt && new Date(quote.expiresAt).getTime() <= Date.now() ? 'Expired' : quote.status
}

function modelIdOf(quote: Quote) {
  return quote.items[0]?.modelId
}

export default function CustomerQuotesPage() {
  const navigate = useNavigate()
  const quotes = useQuotes()
  const models = useModels('')
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('active')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(5)
  const [detail, setDetail] = useState<Quote | null>(null)
  const modelNames = useMemo(() => new Map((models.data ?? []).map(model => [model.id, model.name])), [models.data])
  const allQuotes = quotes.data ?? EMPTY_QUOTES
  const filtered = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi-VN')
    return allQuotes.filter(quote => {
      const effectiveStatus = displayStatus(quote)
      const matchesStatus = status === 'all' || status === 'active'
        ? status === 'all' || activeStatuses.includes(effectiveStatus)
        : effectiveStatus === status
      const modelId = modelIdOf(quote)
      const values = [quote.id, modelId ? modelNames.get(modelId) ?? modelId : '',
        ...quote.items.flatMap(item => [item.materialCode, item.colorCode])]
      return matchesStatus && (!keyword || values.some(value => value.toLocaleLowerCase('vi-VN').includes(keyword)))
    })
  }, [allQuotes, modelNames, search, status])
  const currentPage = Math.min(page, Math.max(0, Math.ceil(filtered.length / pageSize) - 1))
  const visible = filtered.slice(currentPage * pageSize, currentPage * pageSize + pageSize)
  const activeCount = allQuotes.filter(quote => activeStatuses.includes(displayStatus(quote))).length
  const readyCount = allQuotes.filter(quote => displayStatus(quote) === 'Ready').length
  const reviewCount = allQuotes.filter(quote => displayStatus(quote) === 'PendingReview').length

  const continueQuote = (quote: Quote) => {
    const modelId = modelIdOf(quote)
    if (!modelId) return
    navigate(`/models/${modelId}/order?quoteId=${quote.id}`)
  }

  return <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default', py: { xs: 3, md: 5 }, px: { xs: 2, md: 3 } }}>
    <Stack spacing={3} sx={{ maxWidth: 1180, mx: 'auto' }}>
      <Box><Stack direction="row" spacing={1.25} alignItems="center"><ReceiptLongRounded color="primary" /><Typography component="h1" variant="h4" fontWeight={800}>Báo giá của tôi</Typography></Stack>
        <Typography color="text.secondary" mt={.75}>Xem lại giá đã tính, trạng thái duyệt và tiếp tục đặt hàng dù bạn đã chuyển sang trang khác.</Typography></Box>

      {quotes.isError && <Alert severity="error" action={<Button onClick={() => void quotes.refetch()}>Thử lại</Button>}>{getApiErrorMessage(quotes.error, 'Không tải được danh sách báo giá.')}</Alert>}
      <Grid container spacing={2}>{[
        { label: 'Đang hiệu lực', value: activeCount, color: '#6D5CE7' },
        { label: 'Sẵn sàng đặt hàng', value: readyCount, color: '#16A34A' },
        { label: 'Đang chờ duyệt', value: reviewCount, color: '#D97706' },
      ].map(item => <Grid key={item.label} size={{ xs: 12, sm: 4 }}><Card variant="outlined" sx={{ borderRadius: 3, height: '100%' }}><CardContent><Typography variant="body2" color="text.secondary">{item.label}</Typography><Typography variant="h5" fontWeight={800} color={item.color}>{item.value}</Typography></CardContent></Card></Grid>)}</Grid>

      <Card variant="outlined" sx={{ borderRadius: 3 }}><CardContent sx={{ p: { xs: 2, md: 3 }, '&:last-child': { pb: { xs: 2, md: 3 } } }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} mb={2.5}>
          <TextField fullWidth size="small" label="Tìm báo giá" placeholder="Mã báo giá, model, vật liệu hoặc màu" value={search} onChange={event => { setSearch(event.target.value); setPage(0) }} InputProps={{ startAdornment: <InputAdornment position="start"><SearchRounded fontSize="small" /></InputAdornment> }} />
          <TextField select size="small" label="Trạng thái" value={status} onChange={event => { setStatus(event.target.value); setPage(0) }} sx={{ minWidth: { md: 220 } }}>
            <MenuItem value="active">Đang hiệu lực</MenuItem><MenuItem value="all">Tất cả trạng thái</MenuItem>
            {Object.entries(QUOTE_STATUS_LABELS).map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
          </TextField>
        </Stack>
        {quotes.isLoading && <LinearProgress aria-label="Đang tải báo giá" />}
        {!quotes.isLoading && filtered.length === 0 ? <Stack alignItems="center" textAlign="center" spacing={1} sx={{ py: 7 }}><PriceCheckRounded sx={{ fontSize: 46, color: 'text.disabled' }} /><Typography fontWeight={750}>{allQuotes.length ? 'Không có báo giá phù hợp' : 'Bạn chưa có báo giá nào'}</Typography><Typography variant="body2" color="text.secondary">{allQuotes.length ? 'Thử thay đổi từ khóa hoặc trạng thái.' : 'Chọn một model và nhận báo giá để bắt đầu.'}</Typography>{!allQuotes.length && <Button variant="contained" onClick={() => navigate('/models')}>Chọn model</Button>}</Stack>
          : <><TableContainer><Table size="small" aria-label="Danh sách báo giá của tôi" sx={{ minWidth: 760 }}><TableHead><TableRow><TableCell>Báo giá / model</TableCell><TableCell>Cấu hình</TableCell><TableCell align="right">Giá đang báo</TableCell><TableCell>Trạng thái</TableCell><TableCell align="right">Thao tác</TableCell></TableRow></TableHead>
            <TableBody>{visible.map(quote => { const effectiveStatus = displayStatus(quote); const modelId = modelIdOf(quote); const canContinue = ['Draft', 'Ready'].includes(effectiveStatus) && Boolean(modelId); return <TableRow key={quote.id} hover>
              <TableCell><Typography fontWeight={750}>{modelId ? modelNames.get(modelId) ?? 'Model đã lưu' : 'Model không xác định'}</Typography><Typography variant="caption" color="text.secondary">#{quote.id.slice(0, 8)} · {new Date(quote.createdAt).toLocaleString('vi-VN')}</Typography></TableCell>
              <TableCell><Typography variant="body2" fontWeight={650}>{[...new Set(quote.items.map(item => item.materialCode))].join(', ')} · {quote.items.reduce((sum, item) => sum + item.quantity, 0)} SP</Typography><Typography variant="caption" color="text.secondary">{[...new Set(quote.items.map(item => item.colorCode))].join(', ')}</Typography></TableCell>
              <TableCell align="right"><Typography fontWeight={800} whiteSpace="nowrap">{quote.totalPrice.toLocaleString('vi-VN')} {quote.currency}</Typography></TableCell>
              <TableCell><Chip size="small" color={statusColor[effectiveStatus]} label={QUOTE_STATUS_LABELS[effectiveStatus]} /></TableCell>
              <TableCell align="right"><Stack direction="row" justifyContent="flex-end" spacing={.5}><Button size="small" startIcon={<VisibilityOutlined />} onClick={() => setDetail(quote)}>Chi tiết</Button>{canContinue && <Button size="small" variant="contained" endIcon={<ArrowForwardRounded />} onClick={() => continueQuote(quote)}>Tiếp tục</Button>}{['PendingReview', 'Converted'].includes(effectiveStatus) && <Button size="small" onClick={() => navigate('/orders')}>Xem đơn</Button>}</Stack></TableCell>
            </TableRow> })}</TableBody></Table></TableContainer>
            <TablePagination component="div" count={filtered.length} page={currentPage} onPageChange={(_, value) => setPage(value)} rowsPerPage={pageSize} onRowsPerPageChange={event => { setPageSize(Number(event.target.value)); setPage(0) }} rowsPerPageOptions={[5, 10, 20]} labelRowsPerPage="Mỗi trang" labelDisplayedRows={({ from, to, count }) => `${from}–${to} / ${count} báo giá`} /></>}
      </CardContent></Card>
    </Stack>

    <Dialog open={Boolean(detail)} onClose={() => setDetail(null)} fullWidth maxWidth="sm"><DialogTitle sx={{ fontWeight: 800 }}>Chi tiết báo giá #{detail?.id.slice(0, 8)}</DialogTitle><DialogContent>{detail && <Stack spacing={2} pt={1}><Chip size="small" sx={{ alignSelf: 'flex-start' }} color={statusColor[displayStatus(detail)]} label={QUOTE_STATUS_LABELS[displayStatus(detail)]} /><QuotePriceDetails quote={detail} /></Stack>}</DialogContent><DialogActions><Button onClick={() => setDetail(null)}>Đóng</Button>{detail && ['Draft', 'Ready'].includes(displayStatus(detail)) && <Button variant="contained" onClick={() => continueQuote(detail)}>Tiếp tục đặt hàng</Button>}</DialogActions></Dialog>
  </Box>
}
