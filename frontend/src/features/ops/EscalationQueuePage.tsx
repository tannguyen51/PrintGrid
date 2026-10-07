import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
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
} from '@mui/material'
import { WarningRounded, LogoutRounded } from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
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
  const navigate = useNavigate()
  const { logout } = useAuth()
  const escalations = useOpsEscalations()
  const rows = escalations.data ?? []

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Hàng đợi cảnh báo — Ops
              </Typography>
              <Typography color="text.secondary">Việc xếp lịch cần người xử lý: in lại vượt trần và job không còn khung giờ (BR-SCHED-011)</Typography>
            </Box>
          </Stack>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>
            Đăng xuất
          </Button>
        </Stack>

        {escalations.isError ? <Alert severity="error">Không tải được hàng đợi cảnh báo.</Alert> : null}
        {escalations.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!escalations.isLoading && !escalations.isError && rows.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <WarningRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Không có cảnh báo nào đang mở. Hệ thống tự xử lý được hết.</Typography>
          </Box>
        ) : null}

        {rows.length > 0 ? (
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
                {rows.map((e) => (
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
