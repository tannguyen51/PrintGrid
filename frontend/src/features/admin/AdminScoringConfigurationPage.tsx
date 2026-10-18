import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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
  Grid,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { InsightsRounded, LogoutRounded } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'

interface ScoringConfiguration {
  id?: string | null
  version: string
  dueDateSlack: number
  labReliability: number
  loadBalance: number
  cost: number
  transit: number
  total: number
  isActive: boolean
  reason?: string | null
  changedBy?: string | null
  createdAtUtc?: string | null
}

const QUERY_KEY = ['admin', 'scoring-configurations'] as const

function useScoringConfigurations() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => (await apiClient.get<ScoringConfiguration[]>('/admin/scoring-configurations')).data,
  })
}

interface ActivateForm {
  version: string
  dueDateSlack: string
  labReliability: string
  loadBalance: string
  cost: string
  transit: string
  reason: string
}

const EMPTY_FORM: ActivateForm = {
  version: '',
  dueDateSlack: '0.35',
  labReliability: '0.25',
  loadBalance: '0.20',
  cost: '0.10',
  transit: '0.10',
  reason: '',
}

/**
 * Admin: versioned assignment scoring weights ("kỳ cấu hình", FR-SCHED-009 / BR-CONFIG-003).
 * Activating a version is forward-only — every decision log entry keeps the version that was
 * in force, so changing weights never rewrites history.
 */
export function AdminScoringConfigurationPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const client = useQueryClient()
  const configurations = useScoringConfigurations()

  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState<ActivateForm>(EMPTY_FORM)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const activate = useMutation({
    mutationFn: async () =>
      (
        await apiClient.post<ScoringConfiguration>('/admin/scoring-configurations', {
          version: form.version.trim(),
          dueDateSlack: Number(form.dueDateSlack),
          labReliability: Number(form.labReliability),
          loadBalance: Number(form.loadBalance),
          cost: Number(form.cost),
          transit: Number(form.transit),
          reason: form.reason.trim(),
        })
      ).data,
    onSuccess: (created) => {
      client.invalidateQueries({ queryKey: QUERY_KEY })
      setFormOpen(false)
      setForm(EMPTY_FORM)
      setNotice(`Đã kích hoạt phiên bản ${created.version}. Các quyết định mới sẽ dùng trọng số này.`)
    },
    onError: (err) => setError(getApiErrorMessage(err)),
  })

  const sum =
    Number(form.dueDateSlack) + Number(form.labReliability) + Number(form.loadBalance) +
    Number(form.cost) + Number(form.transit)
  const formValid =
    form.version.trim().length > 0 && form.reason.trim().length > 0 &&
    [form.dueDateSlack, form.labReliability, form.loadBalance, form.cost, form.transit]
      .every((value) => Number.isFinite(Number(value))) &&
    Math.abs(sum - 1) < 0.0001

  return (
    <Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2} sx={{ mb: 3 }}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <PageBackButton />
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>
              Trọng số chấm điểm gán việc
            </Typography>
            <Typography color="text.secondary">
              Kỳ cấu hình có phiên bản — mỗi quyết định gán giữ lại phiên bản đã dùng
            </Typography>
          </Box>
        </Stack>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <Button
            variant="contained"
            startIcon={<InsightsRounded />}
            onClick={() => { setError(null); setFormOpen(true) }}
          >
            Kích hoạt phiên bản mới
          </Button>
          <Button
            variant="outlined"
            color="error"
            startIcon={<LogoutRounded />}
            onClick={() => { logout(); navigate('/login', { replace: true }) }}
          >
            Đăng xuất
          </Button>
        </Stack>
      </Stack>

      {notice ? <Alert severity="success" sx={{ mb: 2 }} onClose={() => setNotice(null)}>{notice}</Alert> : null}
      {configurations.isError ? <Alert severity="error" sx={{ mb: 2 }}>Không tải được danh sách phiên bản trọng số.</Alert> : null}

      <Paper variant="outlined">
        {configurations.isLoading ? (
          <Box sx={{ p: 4, display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Phiên bản</TableCell>
                <TableCell align="right">Slack hạn</TableCell>
                <TableCell align="right">Uy tín lab</TableCell>
                <TableCell align="right">Cân tải</TableCell>
                <TableCell align="right">Chi phí</TableCell>
                <TableCell align="right">Vận chuyển</TableCell>
                <TableCell align="right">Tổng</TableCell>
                <TableCell>Trạng thái</TableCell>
                <TableCell>Lý do thay đổi</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(configurations.data ?? []).length === 0 ? (
                <TableRow>
                  <TableCell colSpan={9} sx={{ py: 5, textAlign: 'center', color: 'text.secondary' }}>
                    Chưa có phiên bản nào — engine đang dùng bộ trọng số mặc định.
                  </TableCell>
                </TableRow>
              ) : (
                (configurations.data ?? []).map((config) => (
                  <TableRow key={`${config.version}-${config.id ?? 'default'}`} hover>
                    <TableCell sx={{ fontWeight: 700 }}>{config.version}</TableCell>
                    <TableCell align="right">{config.dueDateSlack}</TableCell>
                    <TableCell align="right">{config.labReliability}</TableCell>
                    <TableCell align="right">{config.loadBalance}</TableCell>
                    <TableCell align="right">{config.cost}</TableCell>
                    <TableCell align="right">{config.transit}</TableCell>
                    <TableCell align="right">{config.total}</TableCell>
                    <TableCell>
                      {config.isActive
                        ? <Chip label="Đang áp dụng" size="small" color="success" />
                        : <Chip label="Lịch sử" size="small" variant="outlined" />}
                    </TableCell>
                    <TableCell sx={{ maxWidth: 260, color: 'text.secondary' }}>{config.reason ?? '—'}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        )}
      </Paper>

      <Dialog open={formOpen} onClose={() => setFormOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Kích hoạt phiên bản trọng số mới</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Tổng năm trọng số phải bằng 1.0. Phiên bản mới chỉ ảnh hưởng các quyết định sau thời
            điểm kích hoạt; vết quyết định cũ giữ nguyên phiên bản của nó.
          </Typography>
          {error ? <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>{error}</Alert> : null}
          <Grid container spacing={2}>
            <Grid size={12}>
              <TextField
                label="Tên phiên bản"
                placeholder="vd: 2026.11"
                value={form.version}
                onChange={(e) => setForm({ ...form, version: e.target.value })}
                fullWidth
              />
            </Grid>
            {([
              ['dueDateSlack', 'Slack hạn giao'],
              ['labReliability', 'Uy tín lab'],
              ['loadBalance', 'Cân tải'],
              ['cost', 'Chi phí'],
              ['transit', 'Vận chuyển'],
            ] as const).map(([key, label]) => (
              <Grid key={key} size={{ xs: 6, sm: 4 }}>
                <TextField
                  label={label}
                  type="number"
                  value={form[key]}
                  onChange={(e) => setForm({ ...form, [key]: e.target.value })}
                  fullWidth
                />
              </Grid>
            ))}
            <Grid size={12}>
              <Typography
                variant="body2"
                sx={{ fontWeight: 600, color: Math.abs(sum - 1) < 0.0001 ? 'success.main' : 'warning.main' }}
              >
                Tổng hiện tại: {Number.isFinite(sum) ? sum.toFixed(3) : '—'} (cần bằng 1.000)
              </Typography>
            </Grid>
            <Grid size={12}>
              <TextField
                label="Lý do thay đổi"
                value={form.reason}
                onChange={(e) => setForm({ ...form, reason: e.target.value })}
                fullWidth
                multiline
                minRows={2}
                required
              />
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setFormOpen(false)} color="inherit">Hủy</Button>
          <Button
            onClick={() => activate.mutate()}
            variant="contained"
            disabled={!formValid || activate.isPending}
          >
            {activate.isPending ? 'Đang kích hoạt…' : 'Kích hoạt'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

export default AdminScoringConfigurationPage
