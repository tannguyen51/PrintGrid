import { useState } from 'react'
import {
  Alert, Box, Button, Chip, CircularProgress, LinearProgress, MenuItem, Paper, Stack,
  Tab, Tabs, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination,
  TableRow, TextField, Typography,
} from '@mui/material'
import { AccountBalanceWalletOutlined, Inventory2Outlined, PaymentsOutlined } from '@mui/icons-material'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { JOB_LABELS } from '../jobs/jobTypes'
import { useLabFinance } from './useLabFinance'

const money = (value: number) => new Intl.NumberFormat('vi-VN', {
  style: 'currency', currency: 'VND', maximumFractionDigits: 2,
}).format(value)
const number = (value: number) => value.toLocaleString('vi-VN')
const date = (value: string | null) => value ? new Date(value).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : '—'
const jobType: Record<string, string> = {
  Standard: 'In theo đơn', LabReprint: 'In lại · Lab chịu lỗi', PaidReprint: 'In lại có chi trả',
}
const payment: Record<string, { label: string; color: 'default' | 'warning' | 'info' | 'success' }> = {
  PendingSync: { label: 'Chờ đối soát', color: 'warning' },
  Eligible: { label: 'Chờ duyệt chi', color: 'warning' },
  Approved: { label: 'Đã duyệt · chưa trả', color: 'info' },
  Paid: { label: 'Đã thanh toán', color: 'success' },
  Estimated: { label: 'Dự kiến', color: 'default' },
}

export default function LabFinancePage() {
  const [tab, setTab] = useState(0)
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [search, setSearch] = useState('')
  const [paymentStatus, setPaymentStatus] = useState('all')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(20)
  const [example, setExample] = useState('100000')
  const invalidRange = Boolean(from && to && from > to)
  const report = useLabFinance({ from: from || undefined, to: to || undefined,
    scope: 'completed', search: tab === 0 ? search || undefined : undefined,
    paymentStatus: tab === 0 ? paymentStatus : 'all', page: tab === 0 ? page + 1 : 1, pageSize }, !invalidRange)
  const prices = useLabFinance({ scope: 'all', search: search || undefined, page: page + 1, pageSize }, tab === 1)
  const active = tab === 0 ? report : prices
  const summary = invalidRange || report.isError ? undefined : report.data?.summary
  const exampleValue = Number(example)
  const exampleValid = example.trim() !== '' && Number.isFinite(exampleValue) && exampleValue >= 0
  const labRate = report.data?.labRate ?? .7
  const hubRate = report.data?.hubRate ?? .3
  const cards = [
    { label: 'Job đã nghiệm thu', value: summary ? number(summary.completedJobs) : '—', hint: 'Đã hoàn tất kiểm tra tại Hub', icon: <Inventory2Outlined />, color: 'primary.main' },
    { label: 'Sản phẩm đã thực hiện', value: summary ? number(summary.completedProducts) : '—', hint: 'Theo số lượng của các job đã nghiệm thu', icon: <Inventory2Outlined />, color: 'primary.main' },
    { label: 'Tổng thu nhập Lab', value: summary ? money(summary.totalIncome) : '—', hint: 'Phần Lab được hưởng sau khấu trừ', icon: <AccountBalanceWalletOutlined />, color: 'primary.main' },
    { label: 'Đã nhận thanh toán', value: summary ? money(summary.totalReceived) : '—', hint: 'Các khoản đã được ghi nhận trả cho Lab', icon: <PaymentsOutlined />, color: 'success.main' },
    { label: 'Còn chưa thanh toán', value: summary ? money(summary.outstanding) : '—', hint: 'Gồm chờ đối soát, chờ duyệt và chờ trả', icon: <AccountBalanceWalletOutlined />, color: 'warning.main' },
  ]

  return <Box sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1400, mx: 'auto' }}>
    <Stack spacing={3}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start"><PageBackButton /><Box>
          <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Thống kê & Thu nhập Lab</Typography>
          <Typography color="text.secondary">{report.data?.labName ?? 'Xưởng của bạn'} · Sản lượng, thu nhập và bảng giá từng job.</Typography>
        </Box></Stack>
      </Stack>

      <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
          <TextField type="date" label="Nghiệm thu từ ngày" InputLabelProps={{ shrink: true }} value={from}
            onChange={e => { setFrom(e.target.value); setPage(0) }} />
          <TextField type="date" label="Đến ngày" InputLabelProps={{ shrink: true }} value={to}
            onChange={e => { setTo(e.target.value); setPage(0) }} />
          <Button onClick={() => { setFrom(''); setTo(''); setPage(0) }}>Toàn bộ thời gian</Button>
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
          Thống kê theo ngày nghiệm thu, giờ Việt Nam. Tiền đã nhận và còn phải trả được tính cho các job trong khoảng này, kể cả khi được thanh toán sau đó.
        </Typography>
      </Paper>
      {invalidRange && <Alert severity="error">Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.</Alert>}
      {report.isError && <Alert severity="error">{getApiErrorMessage(report.error, 'Không tải được thống kê Lab.')}</Alert>}
      {report.isLoading && <LinearProgress aria-label="Đang tải thống kê Lab" />}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(5, 1fr)' }, gap: 2 }}>
        {cards.map(card => <Paper key={card.label} variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
          <Box sx={{ color: card.color, mb: 1 }}>{card.icon}</Box>
          <Typography variant="body2" color="text.secondary">{card.label}</Typography>
          <Typography sx={{ fontSize: '1.55rem', fontWeight: 800, my: .75, overflowWrap: 'anywhere' }}>{invalidRange || report.isError ? '—' : card.value}</Typography>
          <Typography variant="body2" color="text.secondary">{card.hint}</Typography>
        </Paper>)}
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1.4fr 1fr' }, gap: 2 }}>
        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
          <Typography variant="h6" fontWeight={800}>Chia doanh thu theo hợp đồng</Typography>
          <Stack direction="row" justifyContent="space-between" sx={{ mt: 2, mb: 1 }}>
            <Typography fontWeight={700} color="primary.main">Lab {labRate * 100}%</Typography>
            <Typography fontWeight={700} color="warning.main">Hub {hubRate * 100}%</Typography>
          </Stack>
          <Box sx={{ display: 'flex', height: 14, borderRadius: 2, overflow: 'hidden', mb: 2 }} aria-label="Lab nhận 70%, Hub nhận 30%">
            <Box sx={{ width: `${labRate * 100}%`, bgcolor: 'primary.main' }} /><Box sx={{ flex: 1, bgcolor: 'warning.main' }} />
          </Box>
          <Typography variant="body2">Giá trị job = giá trị dòng đơn đã chốt × số lượng job / số lượng dòng đơn.</Typography>
          <Typography variant="body2" sx={{ mt: .75 }}>Lab được hưởng = giá trị job × {labRate * 100}% − khấu trừ. Hub được hưởng {hubRate * 100}%.</Typography>
          {summary && <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
            Phần Lab trước khấu trừ: {money(summary.labGrossAmount)} · Khấu trừ: {money(summary.deductions)} · Phần Hub: {money(summary.hubAmount)}.
          </Typography>}
        </Paper>
        <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
          <Typography variant="h6" fontWeight={800}>Tỷ lệ tiền Lab đã nhận</Typography>
          <Typography sx={{ fontSize: '2rem', fontWeight: 800, my: 1 }}>{!invalidRange && summary ? `${number(summary.collectionRate)}%` : '—'}</Typography>
          <LinearProgress variant="determinate" value={Math.min(100, summary?.collectionRate ?? 0)} sx={{ height: 10, borderRadius: 2, mb: 2 }} />
          <Typography variant="body2" color="text.secondary">Tiền đã nhận / tổng thu nhập Lab sau khấu trừ × 100%. Tỷ lệ này thay đổi theo việc thanh toán; tỷ lệ hợp đồng luôn là 70% / 30%.</Typography>
        </Paper>
      </Box>

      {!!summary?.pendingSyncJobs && <Alert severity="info">Có {summary.pendingSyncJobs} job đã nghiệm thu đang chờ Ops đối soát. Thu nhập của các job này đã được tính vào tổng và số còn phải trả.</Alert>}
      <Tabs value={tab} onChange={(_, value) => { setTab(value); setPage(0); setSearch(''); setPaymentStatus('all') }} aria-label="Thống kê và bảng giá Lab">
        <Tab label="Sản lượng & Công nợ" /><Tab label="Bảng giá theo job" />
      </Tabs>

      {tab === 0 && summary && Boolean(report.data?.byType.length) && <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
        <Typography variant="h6" fontWeight={800} sx={{ p: 2.5 }}>Thống kê theo loại job</Typography>
        <TableContainer><Table size="small" aria-label="Thống kê sản lượng và thu nhập theo loại job"><TableHead><TableRow>
          {['Loại job / Vật liệu', 'Số job', 'Sản phẩm', 'Thu nhập Lab', 'Đã nhận', 'Còn phải trả'].map(label => <TableCell key={label}>{label}</TableCell>)}
        </TableRow></TableHead><TableBody>{report.data!.byType.map(row => <TableRow key={`${row.materialCode}-${row.technology}-${row.jobType}`}>
          <TableCell>{jobType[row.jobType]} · {row.technology} / {row.materialCode}</TableCell>
          <TableCell>{number(row.jobCount)}</TableCell><TableCell>{number(row.quantity)}</TableCell>
          <TableCell>{money(row.income)}</TableCell><TableCell>{money(row.received)}</TableCell><TableCell>{money(row.income - row.received)}</TableCell>
        </TableRow>)}</TableBody></Table></TableContainer>
      </Paper>}

      {tab === 1 && <Paper variant="outlined" sx={{ p: 3, borderRadius: 3 }}>
        <Typography variant="h6" fontWeight={800} sx={{ mb: 1 }}>Bảng giá nhận việc</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>Mỗi job có giá theo báo giá đã chốt, vật liệu, thời gian in và số lượng. Danh sách bên dưới gồm các job được giao cho Lab trong toàn bộ thời gian. Job chưa nghiệm thu chỉ hiển thị tiền dự kiến.</Typography>
        <TextField type="number" label="Giá trị job để xem cách chia (VND)" value={example} onChange={e => setExample(e.target.value)}
          inputProps={{ min: 0, step: 1000 }} error={!exampleValid} helperText={!exampleValid ? 'Nhập số tiền không âm hợp lệ.' : 'Ví dụ minh họa, không thay đổi giá hoặc công nợ.'} sx={{ width: { xs: '100%', sm: 360 }, mb: 2 }} />
        <TableContainer><Table size="small" aria-label="Quy tắc bảng giá theo loại job"><TableHead><TableRow>
          <TableCell>Loại job</TableCell><TableCell>Cách tính tiền công</TableCell><TableCell>Lab được hưởng</TableCell>
        </TableRow></TableHead><TableBody>
          {[
            ['In theo đơn / in lại do khách đặt', 'Theo giá trị job đã chốt', false],
            ['In lại do Hub hoặc hệ thống chịu chi phí', 'Theo giá trị job đã chốt', false],
            ['Lab tự in lại, chưa ghi nhận tiền công gốc', 'Nhận tiền công một lần khi nghiệm thu đạt; Lab tự chịu chi phí in lại', false],
            ['Lab tự in lại, đã ghi nhận đủ tiền công gốc', 'Tiền công đã nằm ở job gốc; không cộng thêm cho lần in lại', true],
          ].map(([label, rule, deducted]) => <TableRow key={String(label)}>
            <TableCell>{label}</TableCell><TableCell>{rule}</TableCell><TableCell sx={{ fontWeight: 700 }}>{exampleValid ? money(deducted ? 0 : Math.round(exampleValue * labRate * 100) / 100) : '—'}</TableCell>
          </TableRow>)}
        </TableBody></Table></TableContainer>
      </Paper>}

      <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
        <Stack spacing={2} sx={{ p: 2.5 }}>
          <Typography variant="h6" fontWeight={800}>{tab === 0 ? 'Chi tiết thu nhập từng job' : 'Giá trị và phần Lab được nhận từng job'}</Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} gap={2}>
            <TextField size="small" label="Tìm mã đơn, job hoặc vật liệu" value={search} onChange={e => { setSearch(e.target.value); setPage(0) }} sx={{ minWidth: { sm: 300 } }} />
            {tab === 0 && <TextField select size="small" label="Thanh toán" value={paymentStatus} onChange={e => { setPaymentStatus(e.target.value); setPage(0) }} sx={{ minWidth: 220 }}>
              <MenuItem value="all">Tất cả trạng thái</MenuItem>{Object.entries(payment).filter(([key]) => key !== 'Estimated').map(([key, item]) => <MenuItem key={key} value={key}>{item.label}</MenuItem>)}
            </TextField>}
          </Stack>
          {tab === 1 && prices.isError && <Alert severity="error">{getApiErrorMessage(prices.error, 'Không tải được bảng giá job.')}</Alert>}
        </Stack>
        <TableContainer><Table aria-label={tab === 0 ? 'Chi tiết công nợ Lab' : 'Bảng giá job của Lab'} sx={{ minWidth: 900 }}>
          <TableHead><TableRow><TableCell>Job / Đơn hàng</TableCell><TableCell>Loại job</TableCell><TableCell align="right">Số lượng</TableCell>
            <TableCell align="right">Giá trị job</TableCell>
            <TableCell align="right">Khấu trừ</TableCell><TableCell align="right">Lab được hưởng</TableCell><TableCell>Thanh toán</TableCell>
          </TableRow></TableHead>
          <TableBody>{active.isLoading ? <TableRow><TableCell colSpan={7} sx={{ textAlign: 'center', py: 5 }}><CircularProgress aria-label="Đang tải danh sách job" /></TableCell></TableRow>
            : active.isError || (tab === 0 && invalidRange) ? <TableRow><TableCell colSpan={7}>Chưa thể hiển thị danh sách. Vui lòng kiểm tra bộ lọc hoặc thử tải lại.</TableCell></TableRow>
            : !active.data?.items.length ? <TableRow><TableCell colSpan={7} sx={{ py: 5, textAlign: 'center' }}>{tab === 0 ? 'Chưa có job đã nghiệm thu phù hợp bộ lọc.' : 'Chưa có job được giao cho Lab.'}</TableCell></TableRow>
            : active.data.items.map(row => <TableRow key={row.jobId} hover>
              <TableCell><Typography fontWeight={700}>{row.orderNumber}</Typography><Typography variant="body2" color="text.secondary" title={row.jobId}>Job #{row.jobId.slice(0, 8)}</Typography>
                <Typography variant="body2" color="text.secondary">{row.completedAtUtc ? `Nghiệm thu ${date(row.completedAtUtc)}` : (JOB_LABELS as Record<string, string>)[row.jobStatus] ?? row.jobStatus}</Typography></TableCell>
              <TableCell>{jobType[row.jobType]}<Typography variant="body2" color="text.secondary">{row.technology} · {row.materialCode}</Typography>
                {row.jobType === 'LabReprint' && <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 220 }}>
                  {row.deductionAmount > 0 ? 'Trừ phần tiền công đã ghi nhận ở lần trước' : 'Nhận tiền công gốc một lần khi đạt'}
                </Typography>}
              </TableCell>
              <TableCell align="right">{number(row.quantity)}</TableCell><TableCell align="right">{money(row.jobValue)}</TableCell>
              <TableCell align="right">{money(row.deductionAmount)}</TableCell><TableCell align="right" sx={{ fontWeight: 800 }}>{money(row.netAmount)}</TableCell>
              <TableCell><Chip size="small" color={payment[row.paymentStatus]?.color ?? 'default'} label={payment[row.paymentStatus]?.label ?? row.paymentStatus} />
                {row.paidAtUtc && <Typography variant="body2" sx={{ mt: .75 }}>{date(row.paidAtUtc)}</Typography>}
                {row.paymentReference && <Typography variant="body2" color="text.secondary" sx={{ overflowWrap: 'anywhere', maxWidth: 180 }}>{row.paymentReference}</Typography>}
              </TableCell>
            </TableRow>)}
          </TableBody>
        </Table></TableContainer>
        <TablePagination component="div" count={active.data?.totalCount ?? 0} page={page} rowsPerPage={pageSize}
          rowsPerPageOptions={[10, 20, 50]} onPageChange={(_, value) => setPage(value)}
          onRowsPerPageChange={e => { setPageSize(Number(e.target.value)); setPage(0) }} labelRowsPerPage="Job mỗi trang"
          labelDisplayedRows={({ from: start, to: end, count }) => `${start}–${end} / ${count} job`} />
      </Paper>
    </Stack>
  </Box>
}
