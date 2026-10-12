import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef, type GridPaginationModel } from '@mui/x-data-grid'
import { DownloadRounded, FactCheckRounded, VisibilityRounded } from '@mui/icons-material'
import { StaffNavbar } from '../home/components/StaffNavbar'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { exportAuditLogs, useAuditLogs, type AuditLogEntry, type AuditLogFilters } from './useAuditLogs'

const ACTION_CLASSES = [
  { value: 'authentication', label: 'Đăng nhập & phiên' },
  { value: 'configuration', label: 'Thay đổi cấu hình' },
  { value: 'manual_intervention', label: 'Can thiệp thủ công' },
  { value: 'model_access', label: 'Truy cập model' },
  { value: 'export', label: 'Xuất dữ liệu' },
]

const classLabel = (value: string) => ACTION_CLASSES.find((item) => item.value === value)?.label ?? value
const formatDate = (value: string) => new Date(value).toLocaleString('vi-VN', { dateStyle: 'short', timeStyle: 'medium' })

function JsonBlock({ title, value }: { title: string; value?: string | null }) {
  let display = '—'
  if (value) {
    try { display = JSON.stringify(JSON.parse(value), null, 2) } catch { display = value }
  }
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', fontWeight: 700 }}>{title}</Typography>
      <Box component="pre" sx={{ m: 0, mt: 0.75, p: 1.5, borderRadius: 2, bgcolor: 'rgba(255,255,255,0.04)', whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', fontSize: '0.78rem' }}>
        {display}
      </Box>
    </Box>
  )
}

export default function AdminAuditLogPage() {
  const [filters, setFilters] = useState<AuditLogFilters>({ actor: '', actionClass: '', fromUtc: '', toUtc: '', page: 0, pageSize: 25 })
  const [selected, setSelected] = useState<AuditLogEntry | null>(null)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)
  const query = useAuditLogs(filters)
  const rows = useMemo(() => query.data?.items ?? [], [query.data])

  const columns: GridColDef<AuditLogEntry>[] = [
    { field: 'createdAtUtc', headerName: 'Thời điểm', width: 175, valueFormatter: (value: string) => formatDate(value) },
    { field: 'actorDisplay', headerName: 'Actor', flex: 1.2, minWidth: 190 },
    { field: 'actorRole', headerName: 'Role', width: 125 },
    { field: 'actionClass', headerName: 'Nhóm hành động', width: 175, valueFormatter: (value: string) => classLabel(value) },
    { field: 'action', headerName: 'Hành động', flex: 1.1, minWidth: 180 },
    {
      field: 'entity', headerName: 'Đối tượng', flex: 1, minWidth: 170, sortable: false,
      renderCell: ({ row }) => <Typography variant="body2">{row.entityType}{row.entityId ? ` · ${row.entityId.slice(0, 8)}` : ''}</Typography>,
    },
    {
      field: 'details', headerName: '', width: 70, sortable: false, filterable: false,
      renderCell: ({ row }) => <Button aria-label="Xem chi tiết" onClick={() => setSelected(row)} sx={{ minWidth: 0 }}><VisibilityRounded fontSize="small" /></Button>,
    },
  ]

  const setFilter = (key: keyof AuditLogFilters, value: string) => setFilters((current) => ({ ...current, [key]: value, page: 0 }))
  const pagination = (model: GridPaginationModel) => setFilters((current) => ({ ...current, page: model.page, pageSize: model.pageSize }))

  async function handleExport() {
    setExportError(null)
    setExporting(true)
    try { await exportAuditLogs(filters) }
    catch (error) { setExportError(getApiErrorMessage(error, 'Không xuất được nhật ký.')) }
    finally { setExporting(false) }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <StaffNavbar />
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1400, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Nhật ký hệ thống</Typography>
              <Typography color="text.secondary">Theo dõi các hoạt động quan trọng phục vụ kiểm tra và điều tra</Typography>
            </Box>
          </Stack>
          <Button variant="outlined" startIcon={<DownloadRounded />} onClick={handleExport} disabled={exporting}>
            {exporting ? 'Đang xuất…' : 'Xuất CSV'}
          </Button>
        </Stack>

        <Alert severity="info" icon={<FactCheckRounded />}>
          Nhật ký chỉ được ghi thêm, không cho phép sửa hoặc xóa và được lưu trữ dài hạn.
        </Alert>
        {query.isError && <Alert severity="error">Không tải được nhật ký hệ thống.</Alert>}
        {exportError && <Alert severity="error" onClose={() => setExportError(null)}>{exportError}</Alert>}

        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
          <TextField label="Actor / email" size="small" value={filters.actor} onChange={(event) => setFilter('actor', event.target.value)} sx={{ minWidth: 240 }} />
          <FormControl size="small" sx={{ minWidth: 210 }}>
            <InputLabel id="action-class-label">Nhóm hành động</InputLabel>
            <Select labelId="action-class-label" label="Nhóm hành động" value={filters.actionClass} onChange={(event) => setFilter('actionClass', event.target.value)}>
              <MenuItem value="">Tất cả</MenuItem>
              {ACTION_CLASSES.map((item) => <MenuItem key={item.value} value={item.value}>{item.label}</MenuItem>)}
            </Select>
          </FormControl>
          <TextField label="Từ ngày" type="datetime-local" size="small" value={filters.fromUtc} onChange={(event) => setFilter('fromUtc', event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <TextField label="Đến ngày" type="datetime-local" size="small" value={filters.toUtc} onChange={(event) => setFilter('toUtc', event.target.value)} slotProps={{ inputLabel: { shrink: true } }} />
          <Button color="inherit" onClick={() => setFilters({ actor: '', actionClass: '', fromUtc: '', toUtc: '', page: 0, pageSize: filters.pageSize })}>Xóa lọc</Button>
        </Stack>

        <Box sx={{ border: '1px solid rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper' }}>
          <DataGrid
            rows={rows}
            columns={columns}
            loading={query.isLoading || query.isFetching}
            rowCount={query.data?.total ?? 0}
            paginationMode="server"
            paginationModel={{ page: filters.page, pageSize: filters.pageSize }}
            onPaginationModelChange={pagination}
            pageSizeOptions={[25, 50, 100]}
            disableRowSelectionOnClick
            autoHeight
            getRowHeight={() => 'auto'}
            sx={{ border: 'none', '& .MuiDataGrid-cell': { py: 0.5 }, '& .MuiDataGrid-columnHeaders': { bgcolor: 'rgba(255,255,255,0.03)' } }}
          />
        </Box>
      </Stack>

      <Dialog open={selected !== null} onClose={() => setSelected(null)} fullWidth maxWidth="md" PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none' } }}>
        <DialogTitle>Chi tiết bản ghi bất biến</DialogTitle>
        <DialogContent>
          {selected && <Stack spacing={2.5} sx={{ pt: 1 }}>
            <Typography><b>{selected.action}</b> · {classLabel(selected.actionClass)} · {formatDate(selected.createdAtUtc)}</Typography>
            <Typography color="text.secondary">Actor: {selected.actorDisplay} ({selected.actorRole}) · IP: {selected.ipAddress ?? '—'}</Typography>
            <Typography color="text.secondary">Đối tượng: {selected.entityType} / {selected.entityId ?? '—'} · Trace: {selected.traceId ?? '—'}</Typography>
            {selected.reason && <Typography>Lý do: {selected.reason}</Typography>}
            <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
              <Box flex={1}><JsonBlock title="Giá trị trước" value={selected.oldValuesJson} /></Box>
              <Box flex={1}><JsonBlock title="Giá trị sau" value={selected.newValuesJson} /></Box>
            </Stack>
          </Stack>}
        </DialogContent>
      </Dialog>
    </Box>
  )
}
