import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { SaveRounded } from '@mui/icons-material'
import type { ThreeDModel } from '../modelTypes'
import { modelSchema, type ModelFormValues } from '../modelSchema'

interface ModelFormDialogProps {
  open: boolean
  onClose: () => void
  editing: ThreeDModel | null
  onSubmit: (values: ModelFormValues) => Promise<void>
  submitting: boolean
  error?: string | null
}

function defaultsFor(editing: ThreeDModel | null): ModelFormValues {
  if (editing) {
    return {
      name: editing.name,
      description: editing.description ?? '',
      fileName: editing.fileName,
      fileFormat: editing.fileFormat,
      sizeBytes: editing.sizeBytes,
      tags: editing.tags.join(', '),
    }
  }
  return { name: '', description: '', fileName: '', fileFormat: 'STL', sizeBytes: 0, tags: '' }
}

/**
 * Create / edit dialog for a 3D model. Uses one form for both modes.
 */
export function ModelFormDialog({ open, onClose, editing, onSubmit, submitting, error }: ModelFormDialogProps) {
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ModelFormValues>({
    resolver: zodResolver(modelSchema),
    defaultValues: defaultsFor(editing),
  })

  // Sync form when switching between create-mode and edit-mode.
  useEffect(() => {
    if (open) reset(defaultsFor(editing))
  }, [open, editing, reset])

  const submit = handleSubmit(async (values: ModelFormValues) => {
    await onSubmit(values) // parse + call mutation; dialog stays open on error
  })

  return (
    <Dialog open={open} onClose={submitting ? undefined : onClose} fullWidth maxWidth="sm"
      PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
      <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>
        {editing ? 'Sửa model' : 'Thêm model mới'}
      </DialogTitle>
      <DialogContent>
        <Box component="form" id="model-form" onSubmit={submit} noValidate>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}

            <TextField
              label="Tên model"
              placeholder="VD: Bình hoa PLA"
              fullWidth
              autoFocus
              {...register('name')}
              error={Boolean(errors.name)}
              helperText={errors.name?.message}
            />
            <TextField
              label="Mô tả"
              placeholder="Mô tả ngắn về model..."
              fullWidth
              multiline
              minRows={2}
              {...register('description')}
              error={Boolean(errors.description)}
              helperText={errors.description?.message}
            />

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField
                label="Tên file"
                placeholder="vase.stl"
                fullWidth
                {...register('fileName')}
                error={Boolean(errors.fileName)}
                helperText={errors.fileName?.message}
              />
              <TextField
                label="Định dạng"
                placeholder="STL / OBJ / 3MF"
                sx={{ width: { xs: '100%', sm: 140 } }}
                {...register('fileFormat')}
                error={Boolean(errors.fileFormat)}
                helperText={errors.fileFormat?.message}
              />
            </Stack>

            <TextField
              label="Kích thước (bytes)"
              type="number"
              inputProps={{ min: 0 }}
              fullWidth
              {...register('sizeBytes', { valueAsNumber: true })}
              error={Boolean(errors.sizeBytes)}
              helperText={errors.sizeBytes?.message}
            />

            <Box>
              <TextField
                label="Tags"
                placeholder="trang-tri, pla, gift — cách nhau bằng dấu phẩy"
                fullWidth
                {...register('tags')}
                error={Boolean(errors.tags)}
                helperText={errors.tags?.message ?? 'Gõ các tag cách nhau bằng dấu phẩy'}
              />
            </Box>

            <Typography variant="caption" color="text.secondary">
              File vật lý (STL/OBJ) không được tải lên ở bản demo này — chỉ lưu thông tin mô tả model.
            </Typography>
          </Stack>
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={onClose} disabled={submitting} color="inherit" sx={{ color: 'text.secondary' }}>
          Hủy
        </Button>
        <Button type="submit" form="model-form" variant="contained" color="primary" disabled={submitting} startIcon={<SaveRounded />}>
          {submitting ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm model'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}