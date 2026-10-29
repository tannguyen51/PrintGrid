import { useState } from 'react'
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
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { AddRounded, DeleteOutlineRounded, EditRounded, VisibilityOffRounded, VisibilityRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { landing } from '../../shared/theme/landing'
import { SAMPLE_CATEGORIES, type AdminSample } from '../samples/sampleTypes'
import { useAdminSamples, useDeleteSample, useSaveSample, useSetSamplePublished, type SampleFormInput } from './useAdminSamples'

const emptyForm: SampleFormInput = {
  name: '',
  slug: '',
  description: '',
  category: 'Mô hình',
  tags: '',
  coverImageUrl: '',
  sortOrder: 0,
  isPublished: true,
  file: null,
}

function formatSize(bytes: number): string {
  if (bytes < 1024 * 1024) return `${Math.max(1, Math.round(bytes / 1024))} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export default function AdminSamplesPage() {
  const query = useAdminSamples()
  const saveMutation = useSaveSample()
  const publishMutation = useSetSamplePublished()
  const deleteMutation = useDeleteSample()

  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<AdminSample | null>(null)
  const [form, setForm] = useState<SampleFormInput>(emptyForm)
  const [error, setError] = useState('')

  const showCreate = () => {
    setEditing(null)
    setForm({ ...emptyForm })
    setError('')
    setOpen(true)
  }

  const showEdit = (sample: AdminSample) => {
    setEditing(sample)
    setForm({
      id: sample.id,
      name: sample.name,
      slug: sample.slug,
      description: sample.description ?? '',
      category: sample.category,
      tags: sample.tags.join(', '),
      coverImageUrl: sample.coverImageUrl ?? '',
      sortOrder: sample.sortOrder,
      isPublished: sample.isPublished,
      file: null,
    })
    setError('')
    setOpen(true)
  }

  const submit = async () => {
    setError('')
    if (!form.name.trim()) {
      setError('Tên mẫu là bắt buộc.')
      return
    }
    if (!editing && !form.file) {
      setError('Hãy chọn file mô hình (STL, OBJ, 3MF hoặc GLB).')
      return
    }

    try {
      await saveMutation.mutateAsync(form)
      setOpen(false)
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, 'Không lưu được mẫu.'))
    }
  }

  const togglePublished = async (sample: AdminSample) => {
    setError('')
    try {
      await publishMutation.mutateAsync({ id: sample.id, published: !sample.isPublished })
    } catch (publishError) {
      setError(getApiErrorMessage(publishError, 'Không đổi được trạng thái mẫu.'))
    }
  }

  const remove = async (sample: AdminSample) => {
    if (!confirm(`Xóa mẫu “${sample.name}”? Bản sao khách đã tạo vẫn được giữ nguyên.`)) return
    setError('')
    try {
      await deleteMutation.mutateAsync(sample.id)
    } catch (deleteError) {
      setError(getApiErrorMessage(deleteError, 'Không xóa được mẫu.'))
    }
  }

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Box sx={{ maxWidth: 1180, mx: 'auto', px: 3, py: 4 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3} gap={2}>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <PageBackButton />
            <Box>
              <Typography variant="h4" fontWeight={800}>Kho mẫu 3D</Typography>
              <Typography color="text.secondary">
                Mẫu công khai cho khách vãng lai xem và đặt in. Ngừng công khai thay vì xóa nếu mẫu đã được dùng.
              </Typography>
            </Box>
          </Stack>
          <Button variant="contained" startIcon={<AddRounded />} onClick={showCreate}>
            Thêm mẫu
          </Button>
        </Stack>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

        <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
          {query.isLoading ? (
            <Box p={5} textAlign="center"><CircularProgress /></Box>
          ) : (query.data ?? []).length === 0 ? (
            <Box p={5} textAlign="center">
              <Typography color="text.secondary">Chưa có mẫu nào. Bấm “Thêm mẫu” để tải mẫu đầu tiên.</Typography>
            </Box>
          ) : (
            <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>
              {(query.data ?? []).map((sample) => (
                <Stack
                  key={sample.id}
                  direction={{ xs: 'column', md: 'row' }}
                  alignItems={{ md: 'center' }}
                  justifyContent="space-between"
                  gap={2}
                  px={2.5}
                  py={2}
                  sx={{ opacity: sample.isPublished ? 1 : 0.62 }}
                >
                  <Stack direction="row" spacing={2} alignItems="center" flex={1} minWidth={0}>
                    <Box
                      sx={{
                        width: 64,
                        height: 48,
                        flexShrink: 0,
                        borderRadius: 2,
                        overflow: 'hidden',
                        bgcolor: '#08080A',
                        border: `1px solid ${landing.hairline}`,
                        display: 'grid',
                        placeItems: 'center',
                        color: landing.textMuted,
                        fontSize: 11,
                      }}
                    >
                      {sample.coverImageUrl ? (
                        <Box component="img" src={sample.coverImageUrl} alt="" sx={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                      ) : (
                        sample.fileFormat
                      )}
                    </Box>
                    <Box minWidth={0}>
                      <Stack direction="row" spacing={1} alignItems="center" sx={{ flexWrap: 'wrap' }}>
                        <Typography fontWeight={750} noWrap>{sample.name}</Typography>
                        <Chip size="small" label={sample.slug} variant="outlined" />
                        <Chip size="small" label={sample.category} />
                        {sample.isPublished ? (
                          <Chip size="small" color="success" label="Công khai" />
                        ) : (
                          <Chip size="small" color="warning" label="Đang ẩn" />
                        )}
                      </Stack>
                      <Typography variant="body2" color="text.secondary" mt={0.5} noWrap>
                        {sample.fileFormat} · {formatSize(sample.sizeBytes)} · thứ tự {sample.sortOrder}
                        {sample.volumeCm3 != null ? ` · ${sample.volumeCm3.toFixed(1)} cm³` : ''}
                        {sample.estimatedPrintMinutes != null ? ` · ${sample.estimatedPrintMinutes} phút` : ''}
                      </Typography>
                    </Box>
                  </Stack>

                  <Stack direction="row" alignItems="center" flexShrink={0}>
                    <Tooltip title={sample.isPublished ? 'Ngừng công khai' : 'Công khai mẫu'}>
                      <IconButton
                        onClick={() => togglePublished(sample)}
                        disabled={publishMutation.isPending}
                      >
                        {sample.isPublished ? <VisibilityOffRounded /> : <VisibilityRounded />}
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Sửa thông tin / thay file">
                      <IconButton onClick={() => showEdit(sample)}>
                        <EditRounded />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Xóa mẫu">
                      <IconButton color="error" onClick={() => remove(sample)} disabled={deleteMutation.isPending}>
                        <DeleteOutlineRounded />
                      </IconButton>
                    </Tooltip>
                  </Stack>
                </Stack>
              ))}
            </Stack>
          )}
        </Paper>

        <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
          <DialogTitle>{editing ? `Sửa mẫu: ${editing.name}` : 'Thêm mẫu mới'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} mt={1}>
              {error && <Alert severity="error">{error}</Alert>}

              <Button variant="outlined" component="label" sx={{ justifyContent: 'flex-start', textTransform: 'none' }}>
                {form.file ? `${form.file.name} · ${formatSize(form.file.size)}` : editing ? 'Thay file mô hình (tùy chọn)' : 'Chọn file mô hình *'}
                <input
                  hidden
                  type="file"
                  accept=".stl,.obj,.3mf,.glb"
                  onChange={(event) => setForm((current) => ({ ...current, file: event.target.files?.[0] ?? null }))}
                />
              </Button>
              <Typography variant="caption" color="text.secondary">
                STL, OBJ, 3MF hoặc GLB · tối đa 50 MB. Hình học được phân tích ngay khi lưu.
              </Typography>

              <TextField
                label="Tên hiển thị"
                value={form.name}
                onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
                required
              />
              <TextField
                label="Slug (để trống sẽ tự tạo)"
                value={form.slug}
                onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value.toLowerCase() }))}
                helperText="Đường dẫn công khai: /samples/<slug>"
              />
              <TextField
                select
                label="Danh mục"
                value={form.category}
                onChange={(event) => setForm((current) => ({ ...current, category: event.target.value }))}
              >
                {SAMPLE_CATEGORIES.map((category) => (
                  <MenuItem key={category} value={category}>{category}</MenuItem>
                ))}
              </TextField>
              <TextField
                label="Mô tả"
                value={form.description}
                onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                multiline
                minRows={2}
              />
              <TextField
                label="Thẻ (cách nhau bằng dấu phẩy)"
                value={form.tags}
                onChange={(event) => setForm((current) => ({ ...current, tags: event.target.value }))}
                placeholder="trang trí, để bàn"
              />
              <TextField
                label="URL ảnh bìa (tùy chọn)"
                value={form.coverImageUrl}
                onChange={(event) => setForm((current) => ({ ...current, coverImageUrl: event.target.value }))}
                placeholder="https://…"
                helperText="Không có ảnh bìa, thẻ mẫu dùng hình khối mặc định."
              />
              <TextField
                type="number"
                label="Thứ tự hiển thị"
                value={form.sortOrder}
                onChange={(event) => setForm((current) => ({ ...current, sortOrder: Number(event.target.value) }))}
                helperText="Số nhỏ hiện trước."
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={form.isPublished}
                    onChange={(event) => setForm((current) => ({ ...current, isPublished: event.target.checked }))}
                  />
                }
                label="Công khai ngay trong kho mẫu"
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Hủy</Button>
            <Button variant="contained" onClick={submit} disabled={saveMutation.isPending}>
              {editing ? 'Lưu thay đổi' : 'Thêm mẫu'}
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </Box>
  )
}
