import { Link as RouterLink } from 'react-router-dom'
import { useState } from 'react'
import {
  Alert,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  TextField,
  MenuItem,
} from '@mui/material'
import { WarningRounded } from '@mui/icons-material'
import { useOpsEscalations, type OpsEscalationKind, type OpsEscalationStatus } from './useOps'

const KIND_LABELS: Record<OpsEscalationKind, string> = {
  ReprintLimitExceeded: 'In lại vượt trần',
  NoFeasibleSlot: 'Hết khung giờ khả dụng',
}

const STATUS_META: Record<OpsEscalationStatus, { label: string; color: 'warning' | 'success' }> = {
  Open: { label: 'Đang mở', color: 'warning' },
  Resolved: { label: 'Đã xử lý', color: 'success' },
}

const fmtDateTime = (d: string | null | undefined) =>
  d ? new Date(d).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'short' }) : '—'

/**
 * Ops escalation queue (BR-SCHED-011): reprints past the cap and jobs the engine
 * could not place before the committed date. Read-only — resolution happens in the
 * decision trace / scheduling board, not here.
 */
export default function EscalationQueuePage() {
  const escalations = useOpsEscalations()
  const rows = escalations.data ?? []
  const [statusFilter, setStatusFilter] = useState('')
  const [kindFilter, setKindFilter] = useState('')
  const filteredRows = rows.filter((row) => (!statusFilter || row.status === statusFilter) && (!kindFilter || row.kind === kindFilter))

  return (
    <Box className="ops-page">
      <Stack className="ops-page-body" spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <Box>
              <Typography variant="h1">
                Cảnh báo & can thiệp
              </Typography>
              <Typography color="text.secondary">Danh sách đơn in cần can thiệp xử lý: in lại vượt giới hạn hoặc không còn khung giờ khả dụng</Typography>
            </Box>
          </Stack>
        </Stack>

        {escalations.isError ? <Alert severity="error">Không tải được hàng đợi cảnh báo.</Alert> : null}
        {escalations.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}
        <Paper variant="outlined" sx={{ p: 2 }}><Stack direction={{ xs: 'column', sm: 'row' }} useFlexGap flexWrap="wrap" gap={2} alignItems={{ sm: 'center' }}>
          <TextField select size="small" label="Trạng thái cảnh báo" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} sx={{ minWidth: 180 }}>
            <MenuItem value="">Tất cả trạng thái</MenuItem><MenuItem value="Open">Đang mở</MenuItem><MenuItem value="Resolved">Đã xử lý</MenuItem>
          </TextField>
          <TextField select size="small" label="Loại cảnh báo" value={kindFilter} onChange={(event) => setKindFilter(event.target.value)} sx={{ minWidth: 220 }}>
            <MenuItem value="">Tất cả loại</MenuItem><MenuItem value="ReprintLimitExceeded">In lại vượt trần</MenuItem><MenuItem value="NoFeasibleSlot">Hết khung giờ khả dụng</MenuItem>
          </TextField>
          <Typography role="status" variant="body2" color="text.secondary">{escalations.data ? filteredRows.length + ' cảnh báo trong danh sách' : 'Chưa có dữ liệu'}</Typography>
        </Stack></Paper>

        {!escalations.isLoading && !escalations.isError && filteredRows.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed #E8EBF2' }}>
            <WarningRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">{rows.length ? 'Không có cảnh báo phù hợp với bộ lọc đã chọn.' : 'Chưa có cảnh báo nào được ghi nhận.'}</Typography>
          </Box>
        ) : null}

        {filteredRows.length > 0 ? (
          <TableContainer component={Paper} variant="outlined" sx={{ bgcolor: 'background.paper' }}>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Thời điểm</TableCell>
                  <TableCell>Loại</TableCell>
                  <TableCell>Lý do</TableCell>
                  <TableCell>Job</TableCell>
                  <TableCell>Trạng thái</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filteredRows.map((e) => (
                  <TableRow key={e.id}>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{fmtDateTime(e.createdAtUtc)}</TableCell>
                    <TableCell>
                      <Chip size="small" color="warning" label={KIND_LABELS[e.kind] ?? e.kind} />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">{e.reason}</Typography>
                    </TableCell>
                    <TableCell>
                      <RouterLink
                        to={`/ops/decisions?jobId=${e.jobId}`}
                        style={{ color: 'inherit', textDecoration: 'underline' }}
                      >
                        {e.jobId.slice(0, 8)}
                      </RouterLink>
                    </TableCell>
                    <TableCell>
                      <Chip size="small" color={STATUS_META[e.status]?.color ?? 'default'} label={STATUS_META[e.status]?.label ?? e.status} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : null}
      </Stack>
    </Box>
  )
}
