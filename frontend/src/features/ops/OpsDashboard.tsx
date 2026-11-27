import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { OpsPageHeader, OpsStatCard } from './OpsComponents'
import { AccountBalanceWalletRounded, TrendingUpRounded, WarningRounded, CompareArrowsRounded } from '@mui/icons-material'

interface LabSummary {
  labId: string; labName: string; totalPayable: number; paid: number; outstanding: number; transactionCount: number
}
interface RevenuePoint { period: string; total: number; count: number }

const money = (n: number) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(n)

export default function OpsDashboard() {
  const [labId, setLabId] = useState<string>('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [period, setPeriod] = useState<'week' | 'month' | 'day'>('week')

  const labSummary = useQuery({
    queryKey: ['ops-lab-summary', labId, from, to],
    queryFn: () => apiClient.get<LabSummary[]>('/finance/lab-summary', {
      params: { labId: labId || undefined, fromUtc: from || undefined, toUtc: to || undefined }
    }).then(r => r.data)
  })

  const revenue = useQuery({
    queryKey: ['ops-revenue', period, labId, from, to],
    queryFn: () => apiClient.get<RevenuePoint[]>('/finance/revenue-by-period', {
      params: { period, labId: labId || undefined, fromUtc: from || undefined, toUtc: to || undefined }
    }).then(r => r.data)
  })

  const totals = useMemo(() => {
    if (!labSummary.data) return { payable: 0, paid: 0, outstanding: 0, labs: 0 }
    return {
      payable: labSummary.data.reduce((s, l) => s + l.totalPayable, 0),
      paid: labSummary.data.reduce((s, l) => s + l.paid, 0),
      outstanding: labSummary.data.reduce((s, l) => s + l.outstanding, 0),
      labs: labSummary.data.length
    }
  }, [labSummary.data])

  const maxRevenue = useMemo(() => Math.max(...(revenue.data?.map(r => r.total) ?? [1]), 1), [revenue.data])

  return (
    <Box className="ops-page">
      <Stack className="ops-page-body" spacing={3}>
        <OpsPageHeader
          eyebrow="Tổng quan tài chính"
          title="Dashboard vận hành"
          description="Theo dõi doanh thu, công nợ và hiệu suất tài chính của từng Lab. Lọc theo Lab, Hub hoặc khoảng thời gian."
        />

        {/* Filters */}
        <Paper variant="outlined" sx={{ p: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} gap={2} alignItems="flex-end">
            <TextField
              select
              label="Lab"
              value={labId}
              onChange={e => setLabId(e.target.value)}
              sx={{ minWidth: 200 }}
            >
              <MenuItem value="">Tất cả Labs</MenuItem>
              {labSummary.data?.map(l => (
                <MenuItem key={l.labId} value={l.labId}>{l.labName}</MenuItem>
              ))}
            </TextField>
            <TextField type="date" label="Từ ngày" InputLabelProps={{ shrink: true }} value={from} onChange={e => setFrom(e.target.value)} />
            <TextField type="date" label="Đến ngày" InputLabelProps={{ shrink: true }} value={to} onChange={e => setTo(e.target.value)} />
            <TextField select label="Khoảng thời gian" value={period} onChange={e => setPeriod(e.target.value as 'week' | 'month' | 'day')} sx={{ minWidth: 150 }}>
              <MenuItem value="day">Theo ngày</MenuItem>
              <MenuItem value="week">Theo tuần</MenuItem>
              <MenuItem value="month">Theo tháng</MenuItem>
            </TextField>
            <Button onClick={() => { labSummary.refetch(); revenue.refetch() }} disabled={labSummary.isFetching}>Làm mới</Button>
          </Stack>
        </Paper>

        {(labSummary.isError || revenue.isError) && (
          <Alert severity="error">{getApiErrorMessage(labSummary.error || revenue.error, 'Không tải được dữ liệu tài chính.')}</Alert>
        )}

        {/* KPI Cards */}
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' }, gap: 2 }}>
          <OpsStatCard
            label="Tổng phải trả"
            value={money(totals.payable)}
            hint={`${totals.labs} Lab đang có công nợ`}
            icon={<AccountBalanceWalletRounded />}
            tone="warning"
            loading={labSummary.isLoading}
          />
          <OpsStatCard
            label="Đã trả"
            value={money(totals.paid)}
            hint={`${totals.payable > 0 ? ((totals.paid / totals.payable) * 100).toFixed(1) : 0}% hoàn thành`}
            icon={<TrendingUpRounded />}
            tone="success"
            loading={labSummary.isLoading}
          />
          <OpsStatCard
            label="Còn nợ"
            value={money(totals.outstanding)}
            hint={totals.outstanding > 0 ? 'Cần xử lý' : 'Đã thanh toán đủ'}
            icon={<WarningRounded />}
            tone={totals.outstanding > 0 ? 'error' : 'success'}
            loading={labSummary.isLoading}
          />
          <OpsStatCard
            label="Doanh thu gần nhất"
            value={money(revenue.data?.[revenue.data.length - 1]?.total ?? 0)}
            hint={`Theo ${period === 'week' ? 'tuần' : period === 'month' ? 'tháng' : 'ngày'}`}
            icon={<CompareArrowsRounded />}
            tone="primary"
            loading={revenue.isLoading}
          />
        </Box>

        {/* Revenue Chart */}
        <Paper variant="outlined" sx={{ p: 2.5 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" mb={2}>
            <Typography variant="h5">Doanh thu theo {period === 'week' ? 'tuần' : period === 'month' ? 'tháng' : 'ngày'}</Typography>
            <Chip label={`Tổng ${money(revenue.data?.reduce((s, r) => s + r.total, 0) ?? 0)}`} color="primary" />
          </Stack>
          {revenue.isLoading ? (
            <Typography color="text.secondary" align="center" sx={{ py: 4 }}>Đang tải biểu đồ...</Typography>
          ) : revenue.data?.length === 0 ? (
            <Typography color="text.secondary" align="center" sx={{ py: 4 }}>Không có dữ liệu doanh thu trong khoảng thời gian này.</Typography>
          ) : (
            <Box sx={{ width: '100%', overflowX: 'auto' }}>
              <svg width="100%" height="280" viewBox={`0 0 ${Math.max(600, (revenue.data?.length ?? 0) * 60)} 280`} preserveAspectRatio="none">
                {/* Grid lines */}
                {[0, 0.25, 0.5, 0.75, 1].map((ratio, i) => (
                  <g key={i}>
                    <line x1="0" y1={250 - ratio * 220} x2="100%" y2={250 - ratio * 220} stroke="#E8EBF2" strokeWidth="1" />
                    <text x="0" y={250 - ratio * 220 + 4} fontSize="11" fill="#888">{money(maxRevenue * ratio)}</text>
                  </g>
                ))}
                {/* Bars */}
                {revenue.data?.map((point, i) => {
                  const barHeight = (point.total / maxRevenue) * 220
                  const x = i * 60 + 20
                  return (
                    <g key={point.period}>
                      <rect
                        x={x}
                        y={250 - barHeight}
                        width="40"
                        height={barHeight}
                        fill="url(#barGradient)"
                        rx="4"
                      />
                      <text x={x + 20} y="268" textAnchor="middle" fontSize="10" fill="#666">
                        {point.period.slice(5)}
                      </text>
                      <title>{point.period}: {money(point.total)} ({point.count} giao dịch)</title>
                    </g>
                  )
                })}
                <defs>
                  <linearGradient id="barGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                    <stop offset="0%" stopColor="#6D58BC" />
                    <stop offset="100%" stopColor="#8C7CAF" />
                  </linearGradient>
                </defs>
              </svg>
            </Box>
          )}
        </Paper>

        {/* Lab Comparison Table */}
        <Paper variant="outlined">
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Lab</TableCell>
                  <TableCell align="right">Tổng phải trả</TableCell>
                  <TableCell align="right">Đã trả</TableCell>
                  <TableCell align="right">Còn nợ</TableCell>
                  <TableCell align="right">Tỷ lệ thanh toán</TableCell>
                  <TableCell align="right">Số giao dịch</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {labSummary.isLoading ? (
                  <TableRow><TableCell colSpan={6} align="center"><Typography color="text.secondary">Đang tải...</Typography></TableCell></TableRow>
                ) : labSummary.data?.length === 0 ? (
                  <TableRow><TableCell colSpan={6} align="center"><Typography color="text.secondary">Không có dữ liệu</Typography></TableCell></TableRow>
                ) : (
                  labSummary.data?.map(lab => {
                    const paidPercent = lab.totalPayable > 0 ? (lab.paid / lab.totalPayable) * 100 : 0
                    return (
                      <TableRow key={lab.labId} hover>
                        <TableCell>
                          <Typography fontWeight={600}>{lab.labName}</Typography>
                          <Typography variant="caption" color="text.secondary">{lab.labId.slice(0, 8)}</Typography>
                        </TableCell>
                        <TableCell align="right">{money(lab.totalPayable)}</TableCell>
                        <TableCell align="right" sx={{ color: 'success.main', fontWeight: 600 }}>{money(lab.paid)}</TableCell>
                        <TableCell align="right" sx={{ color: lab.outstanding > 0 ? 'error.main' : 'text.primary', fontWeight: 600 }}>{money(lab.outstanding)}</TableCell>
                        <TableCell align="right">
                          <Chip
                            label={`${paidPercent.toFixed(1)}%`}
                            color={paidPercent >= 100 ? 'success' : paidPercent >= 50 ? 'warning' : 'error'}
                            size="small"
                          />
                        </TableCell>
                        <TableCell align="right">{lab.transactionCount}</TableCell>
                      </TableRow>
                    )
                  })
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      </Stack>
    </Box>
  )
}
