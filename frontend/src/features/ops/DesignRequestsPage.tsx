import { useState } from 'react'
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  MenuItem,
  Select,
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
import { PageBackButton } from '../../shared/components/PageBackButton'
import { DESIGN_STATUSES, useChangeDesignStatus, useDesignRequests } from '../design/useDesignRequests'

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Chờ xử lý',
  InReview: 'Đang xem xét',
  Quoted: 'Đã báo giá',
  Rejected: 'Từ chối',
  Accepted: 'Chấp nhận',
}

const statusColor = (status: string): 'warning' | 'info' | 'success' | 'error' | 'default' =>
  status === 'Pending' ? 'warning'
    : status === 'InReview' ? 'info'
      : status === 'Accepted' ? 'success'
        : status === 'Rejected' ? 'error'
          : 'default'

/**
 * FR-CUST-013 / BR-IP-004 — ops design intake queue. Ops triages submitted briefs and moves
 * each request through Pending → InReview → Quoted/Rejected/Accepted.
 */
export default function DesignRequestsPage() {
  const [statusFilter, setStatusFilter] = useState('Pending')
  const [error, setError] = useState<string | null>(null)

  const { data, isLoading, isError } = useDesignRequests(statusFilter || undefined)
  const changeStatus = useChangeDesignStatus()
  const rows = data ?? []

  async function setStatus(id: string, status: string) {
    setError(null)
    try {
      await changeStatus.mutateAsync({ id, status })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không cập nhật được trạng thái.')
    }
  }

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1180, mx: 'auto' }} spacing={2.5}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <PageBackButton />
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Hàng đợi yêu cầu thiết kế
            </Typography>
            <Typography color="text.secondary">
              Xem xét yêu cầu thiết kế của khách hàng và cập nhật trạng thái xử lý.
            </Typography>
          </Box>
        </Stack>

        <Stack direction="row" spacing={2} alignItems="center">
          <TextField
            select
            size="small"
            label="Trạng thái"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            sx={{ minWidth: 200 }}
          >
            <MenuItem value="">Tất cả</MenuItem>
            {DESIGN_STATUSES.map((status) => (
              <MenuItem key={status} value={status}>{STATUS_LABELS[status]}</MenuItem>
            ))}
          </TextField>
          <Typography variant="body2" color="text.disabled">{rows.length} yêu cầu</Typography>
        </Stack>

        {(isError || error) && (
          <Alert severity="error">{error ?? 'Không tải được danh sách yêu cầu thiết kế.'}</Alert>
        )}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center' }} />
        ) : rows.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <Typography color="text.secondary">Không có yêu cầu thiết kế nào ở trạng thái này.</Typography>
          </Box>
        ) : (
          <TableContainer sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Tiêu đề</TableCell>
                  <TableCell>Mô tả</TableCell>
                  <TableCell align="center">Ảnh</TableCell>
                  <TableCell>Ngày gửi</TableCell>
                  <TableCell>Trạng thái</TableCell>
                  <TableCell>Cập nhật</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map((row) => (
                  <TableRow key={row.id} hover>
                    <TableCell sx={{ fontWeight: 600 }}>{row.title}</TableCell>
                    <TableCell sx={{ maxWidth: 320 }}>
                      <Typography variant="body2" color="text.secondary" noWrap>{row.description}</Typography>
                    </TableCell>
                    <TableCell align="center">{row.referenceImageKeys.length}</TableCell>
                    <TableCell>{new Date(row.createdAtUtc).toLocaleString('vi-VN')}</TableCell>
                    <TableCell>
                      <Chip size="small" color={statusColor(row.status)} label={STATUS_LABELS[row.status] ?? row.status} />
                    </TableCell>
                    <TableCell>
                      <Select
                        size="small"
                        value={row.status}
                        disabled={changeStatus.isPending}
                        onChange={(e) => setStatus(row.id, e.target.value)}
                        sx={{ minWidth: 150 }}
                      >
                        {DESIGN_STATUSES.map((status) => (
                          <MenuItem key={status} value={status}>{STATUS_LABELS[status]}</MenuItem>
                        ))}
                      </Select>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}
      </Stack>
    </Box>
  )
}
