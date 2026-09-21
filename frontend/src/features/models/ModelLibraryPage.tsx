import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { AddRounded, DeleteOutlineRounded, EditRounded, LogoutRounded, SearchRounded, VisibilityRounded } from '@mui/icons-material'
import type { ThreeDModel } from './modelTypes'
import { useDeleteModel, useCreateModel, useUpdateModel, useModels, useUploadModel } from './useModels'
import type { ModelFormValues } from './modelSchema'
import { toModelInput } from './modelSchema'
import { ModelFormDialog } from './components/ModelFormDialog'
import { ModelDetailDrawer } from './components/ModelDetailDrawer'
import { DeleteConfirmDialog } from './components/DeleteConfirmDialog'
import { useAuth } from '../../app/AuthContext'
import { useNavigate } from 'react-router-dom'

function formatSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${bytes} B`
}

/**
 * Customer's 3D model library — a full CRUD demo (list / create / read / update / delete / search).
 */
export default function ModelLibraryPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [formOpen, setFormOpen] = useState(false)
  const [editing, setEditing] = useState<ThreeDModel | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [detail, setDetail] = useState<ThreeDModel | null>(null)
  const [deleting, setDeleting] = useState<ThreeDModel | null>(null)

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const { data, isLoading, isError } = useModels(search)
  const createMutation = useCreateModel()
  const uploadMutation = useUploadModel()
  const updateMutation = useUpdateModel()
  const deleteMutation = useDeleteModel()

  const rows = useMemo(() => data ?? [], [data])

  const columns: GridColDef<ThreeDModel>[] = [
    { field: 'name', headerName: 'Tên model', flex: 1.6, minWidth: 180 },
    { field: 'fileFormat', headerName: 'Định dạng', width: 110 },
    {
      field: 'sizeBytes',
      headerName: 'Kích thước',
      width: 120,
      valueFormatter: (value: number) => formatSize(value),
    },
    {
      field: 'tags',
      headerName: 'Tags',
      flex: 1,
      minWidth: 160,
      sortable: false,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
          {params.row.tags.length === 0
            ? <Typography variant="caption" color="text.disabled">—</Typography>
            : params.row.tags.slice(0, 3).map((t) => (
              <Typography key={t} component="span" sx={{ px: 0.75, py: 0.2, borderRadius: 1, fontSize: '0.72rem', bgcolor: 'rgba(139,92,246,0.12)', color: '#A78BFA' }}>
                {t}
              </Typography>
            ))}
        </Box>
      ),
    },
    {
      field: 'geometryStatus',
      headerName: 'Phân tích',
      width: 130,
      renderCell: (params) => {
        const s = params.row.geometryStatus
        const color = s === 'Ready' ? '#34c759' : s === 'Failed' ? '#ff453a' : '#f5a623'
        const label = s === 'Ready' ? 'Đã phân tích' : s === 'Failed' ? 'Lỗi' : 'Đang phân tích…'
        return <Chip size="small" label={label} sx={{ bgcolor: `${color}22`, color, fontWeight: 600 }} />
      },
    },
    {
      field: 'geometrySummary',
      headerName: 'Kích thước / thể tích',
      flex: 1.2,
      minWidth: 170,
      sortable: false,
      renderCell: (params) => {
        const m = params.row
        if (m.geometryStatus !== 'Ready') return <Typography variant="caption" color="text.disabled">—</Typography>
        const dims = [m.boundingWidthMm, m.boundingDepthMm, m.boundingHeightMm]
          .filter((v): v is number => v != null)
          .map((v) => v.toFixed(0))
          .join(' × ')
        const vol = m.volumeCm3 != null ? `${m.volumeCm3.toFixed(1)} cm³` : ''
        const mins = m.estimatedPrintMinutes != null ? `· ${m.estimatedPrintMinutes} phút` : ''
        return <Typography variant="caption" color="text.secondary">{dims ? `${dims} mm` : '—'}{vol && ` · ${vol}`}{mins}</Typography>
      },
    },
    {
      field: 'createdAt',
      headerName: 'Ngày tạo',
      width: 130,
      valueFormatter: (value: string) => new Date(value).toLocaleDateString('vi-VN'),
    },
    {
      field: 'actions',
      headerName: '',
      width: 130,
      sortable: false,
      filterable: false,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 0.25 }}>
          <Tooltip title="Xem chi tiết">
            <IconButton size="small" onClick={() => setDetail(params.row)} sx={{ color: 'text.primary' }}>
              <VisibilityRounded fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Sửa">
            <IconButton size="small" onClick={() => { setEditing(params.row); setFormError(null); setFormOpen(true) }} sx={{ color: 'text.primary' }}>
              <EditRounded fontSize="small" />
            </IconButton>
          </Tooltip>
          <Tooltip title="Xóa">
            <IconButton size="small" color="error" onClick={() => setDeleting(params.row)}>
              <DeleteOutlineRounded fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      ),
    },
  ]

  async function handleFormSubmit(values: ModelFormValues, file?: File) {
    setFormError(null)
    try {
      if (editing) {
        await updateMutation.mutateAsync({ id: editing.id, input: toModelInput(values) })
      } else if (file) {
        await uploadMutation.mutateAsync({
          name: values.name,
          description: values.description?.trim() || undefined,
          tags: (values.tags ?? '').split(',').map((t) => t.trim()).filter(Boolean),
          file,
        })
      } else {
        await createMutation.mutateAsync(toModelInput(values))
      }
      setFormOpen(false)
      setEditing(null)
    } catch {
      setFormError('Không thể lưu model. Vui lòng thử lại.')
    }
  }

  async function handleConfirmDelete() {
    if (!deleting) return
    try {
      await deleteMutation.mutateAsync(deleting.id)
      setDeleting(null)
      if (detail?.id === deleting.id) setDetail(null)
    } catch {
      setDeleting(null)
    }
  }

  const submitting = createMutation.isPending || updateMutation.isPending || uploadMutation.isPending

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Thư viện model 3D
            </Typography>
            <Typography color="text.secondary">Quản lý các model đã tải lên của bạn (đủ thao tác CRUD)</Typography>
          </Box>
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ justifyContent: { xs: 'space-between', sm: 'flex-end' } }}>
            <Button
              variant="outlined"
              color="error"
              startIcon={<LogoutRounded />}
              onClick={handleLogout}
              sx={{ borderColor: 'rgba(255,71,87,0.35)', '&:hover': { borderColor: 'error.main' } }}
            >
              Đăng xuất
            </Button>
            <Button variant="contained" color="primary" startIcon={<AddRounded />} onClick={() => { setEditing(null); setFormError(null); setFormOpen(true) }}>
              Thêm model
            </Button>
          </Stack>
        </Stack>

        <TextField
          placeholder="Tìm theo tên hoặc mô tả…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          size="small"
          sx={{ width: { xs: '100%', sm: 320 } }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchRounded fontSize="small" color="action" />
              </InputAdornment>
            ),
          }}
        />

        {isError ? (
          <Alert severity="error">Không tải được danh sách model.</Alert>
        ) : (
          <Box sx={{ border: '1px solid rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper' }}>
            <DataGrid
              rows={rows}
              columns={columns}
              loading={isLoading}
              autoHeight
              disableRowSelectionOnClick
              initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
              pageSizeOptions={[10, 25, 50]}
              slots={{ loadingOverlay: () => (
                <Box sx={{ display: 'grid', placeItems: 'center', height: '100%' }}>
                  <CircularProgress size={40} />
                </Box>
              ) }}
              sx={{
                '&.MuiDataGrid-root': { border: 'none' },
                '& .MuiDataGrid-columnHeaders': { bgcolor: 'rgba(255,255,255,0.03)' },
              }}
            />
          </Box>
        )}
      </Stack>

      <ModelFormDialog
        key={formOpen ? (editing?.id ?? 'new') : 'closed'}
        open={formOpen}
        onClose={() => { setFormOpen(false); setEditing(null) }}
        editing={editing}
        onSubmit={handleFormSubmit}
        submitting={submitting}
        error={formError}
      />

      <ModelDetailDrawer
        model={detail}
        onClose={() => setDetail(null)}
        onEdit={(m) => { setDetail(null); setEditing(m); setFormError(null); setFormOpen(true) }}
        onDelete={(m) => setDeleting(m)}
        onOrder={(m) => { setDetail(null); navigate(`/models/${m.id}/order`) }}
      />

      <DeleteConfirmDialog
        open={deleting !== null}
        modelName={deleting?.name ?? ''}
        deleting={deleteMutation.isPending}
        onCancel={() => setDeleting(null)}
        onConfirm={handleConfirmDelete}
      />
    </Box>
  )
}