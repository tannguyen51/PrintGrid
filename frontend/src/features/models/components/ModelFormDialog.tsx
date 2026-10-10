import { useEffect, useState } from 'react'
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
import { SaveRounded, UploadFileRounded } from '@mui/icons-material'
import type { ThreeDModel } from '../modelTypes'
import { modelSchema, type ModelFormValues } from '../modelSchema'

interface ModelFormDialogProps {
  open: boolean
  onClose: () => void
  editing: ThreeDModel | null
  onSubmit: (values: ModelFormValues, file?: File) => Promise<void>
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
 * Create / edit dialog for a 3D model.
 * - Create mode: pick a real STL/OBJ/3MF/GLB file to upload (name auto-fills from the file).
 * - Edit mode: edit metadata only.
 */
export function ModelFormDialog({ open, onClose, editing, onSubmit, submitting, error }: ModelFormDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const isCreate = !editing

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<ModelFormValues>({
    resolver: zodResolver(modelSchema),
    defaultValues: defaultsFor(editing),
  })

  useEffect(() => {
    if (open) {
      reset(defaultsFor(editing))
    }
    // Note: file state resets via the dialog `key` prop in ModelLibraryPage on each open.
  }, [open, editing, reset])

  function handleFileChange(next: File | null) {
    setFileError(null)
    if (next) {
      const extension = next.name.split('.').pop()?.toLowerCase()
      if (!extension || !['stl', 'obj', '3mf', 'glb'].includes(extension)) {
        setFile(null)
        setFileError('Chỉ chấp nhận file STL, OBJ, 3MF hoặc GLB.')
        return
      }
      if (next.size > 50 * 1024 * 1024) {
        setFile(null)
        setFileError('Kích thước file vượt giới hạn 50 MiB.')
        return
      }
    }
    setFile(next)
    if (next && isCreate) {
      // Auto-fill name + fileName + format from the chosen file.
      const base = next.name.replace(/\.(stl|obj|3mf|glb)$/i, '')
      setValue('name', base, { shouldValidate: true })
      setValue('fileName', next.name, { shouldValidate: true })
      setValue(
        'fileFormat',
        (next.name.split('.').pop() ?? 'STL').toUpperCase(),
        { shouldValidate: true },
      )
      setValue('sizeBytes', next.size, { shouldValidate: true })
    }
  }

  const submit = handleSubmit(async (values: ModelFormValues) => {
    await onSubmit(values, file ?? undefined)
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

            {/* ── File picker (create mode) ── */}
            {isCreate && (
              <Box>
                <input
                  id="model-file-input"
                  type="file"
                  accept=".stl,.obj,.3mf,.glb"
                  hidden
                  onChange={(e) => handleFileChange(e.target.files?.[0] ?? null)}
                />
                <Button
                  component="label"
                  htmlFor="model-file-input"
                  variant="outlined"
                  color="inherit"
                  fullWidth
                  startIcon={<UploadFileRounded />}
                  sx={{ py: 1.5, color: file ? 'text.primary' : 'text.secondary', borderColor: file ? 'rgba(255,120,80,0.5)' : 'rgba(255,255,255,0.2)', borderStyle: 'dashed' }}
                >
                  {file ? `Đã chọn: ${file.name} (${(file.size / 1024).toFixed(1)} KB)` : 'Chọn file STL / OBJ / 3MF / GLB để tải lên'}
                </Button>
                {fileError && <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.75 }}>{fileError}</Typography>}
              </Box>
            )}

            <TextField
              label="Tên model"
              placeholder={isCreate ? 'Tự điền khi chọn file (có thể sửa)' : 'VD: Bình hoa PLA'}
              fullWidth
              autoFocus={!isCreate}
              {...register('name')}
              error={Boolean(errors.name)}
              helperText={errors.name?.message}
            />

            {/* ── Metadata fields shown for file-less create OR edit ── */}
            {(!isCreate || !file) && (
              <>
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

                {!isCreate && (
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
                      placeholder="STL / OBJ / 3MF / GLB"
                      sx={{ width: { xs: '100%', sm: 140 } }}
                      {...register('fileFormat')}
                      error={Boolean(errors.fileFormat)}
                      helperText={errors.fileFormat?.message}
                    />
                  </Stack>
                )}

                {!isCreate && (
                  <TextField
                    label="Kích thước (bytes)"
                    type="number"
                    inputProps={{ min: 0 }}
                    fullWidth
                    {...register('sizeBytes', { valueAsNumber: true })}
                    error={Boolean(errors.sizeBytes)}
                    helperText={errors.sizeBytes?.message}
                  />
                )}
              </>
            )}

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

            {isCreate && !file && (
              <Typography variant="caption" color={fileError ? 'error' : 'text.secondary'}>
                Hãy chọn một file model hợp lệ để tiếp tục.
              </Typography>
            )}
          </Stack>
        </Box>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={onClose} disabled={submitting} color="inherit" sx={{ color: 'text.secondary' }}>
          Hủy
        </Button>
        <Button type="submit" form="model-form" variant="contained" color="primary" disabled={submitting || (isCreate && !file)} startIcon={<SaveRounded />}>
          {submitting ? 'Đang lưu…' : isCreate ? 'Tải lên' : 'Lưu thay đổi'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
