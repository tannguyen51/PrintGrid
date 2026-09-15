import { Box, Button, Chip, Divider, Drawer, Stack, Typography } from '@mui/material'
import { CloseRounded, DeleteOutlineRounded, DescriptionRounded, EditRounded } from '@mui/icons-material'
import type { ThreeDModel } from '../modelTypes'

interface ModelDetailDrawerProps {
  model: ThreeDModel | null
  onClose: () => void
  onEdit: (model: ThreeDModel) => void
  onDelete: (model: ThreeDModel) => void
}

function formatSize(bytes: number): string {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${bytes} B`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function ModelDetailDrawer({ model, onClose, onEdit, onDelete }: ModelDetailDrawerProps) {
  return (
    <Drawer
      anchor="right"
      open={model !== null}
      onClose={onClose}
      PaperProps={{ sx: { width: { xs: 320, sm: 400 }, bgcolor: '#0A0A0A', backgroundImage: 'none' } }}
    >
      {model && (
        <Stack sx={{ height: '100%' }}>
          <Box sx={{ p: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <Box>
              <Typography sx={{ fontSize: '1.3rem', fontWeight: 800, color: 'text.primary' }}>{model.name}</Typography>
              <Typography variant="caption" color="text.secondary">
                {model.fileFormat} · {formatSize(model.sizeBytes)}
              </Typography>
            </Box>
            <Button onClick={onClose} color="inherit" size="small" sx={{ color: 'text.secondary', minWidth: 0 }} aria-label="Đóng">
              <CloseRounded />
            </Button>
          </Box>

          <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />

          <Box sx={{ p: 3, flex: 1, overflowY: 'auto' }}>
            <Stack spacing={3}>
              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'rgba(255,255,255,0.5)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                  File
                </Typography>
                <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 0.75 }}>
                  <DescriptionRounded fontSize="small" color="action" />
                  <Typography color="text.primary">{model.fileName}</Typography>
                </Stack>
              </Box>

              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'rgba(255,255,255,0.5)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                  Mô tả
                </Typography>
                <Typography color="text.secondary" sx={{ mt: 0.75, whiteSpace: 'pre-wrap' }}>
                  {model.description?.trim() || '—'}
                </Typography>
              </Box>

              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'rgba(255,255,255,0.5)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                  Tags
                </Typography>
                {model.tags.length > 0 ? (
                  <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap sx={{ mt: 0.75 }}>
                    {model.tags.map((tag) => (
                      <Chip key={tag} label={tag} size="small" sx={{ bgcolor: 'rgba(139,92,246,0.12)', color: '#A78BFA' }} />
                    ))}
                  </Stack>
                ) : (
                  <Typography color="text.secondary" sx={{ mt: 0.75 }}>—</Typography>
                )}
              </Box>

              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: 'rgba(255,255,255,0.5)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                  Thông tin khác
                </Typography>
                <Typography color="text.secondary" sx={{ mt: 0.75 }}>
                  Tạo lúc: {formatDate(model.createdAt)} · Cập nhật: {formatDate(model.updatedAt)}
                </Typography>
              </Box>
            </Stack>
          </Box>

          <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />
          <Stack direction="row" spacing={1.5} sx={{ p: 3 }}>
            <Button variant="outlined" color="inherit" fullWidth startIcon={<EditRounded />} onClick={() => onEdit(model)} sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}>
              Sửa
            </Button>
            <Button variant="contained" color="error" fullWidth startIcon={<DeleteOutlineRounded />} onClick={() => onDelete(model)}>
              Xóa
            </Button>
          </Stack>
        </Stack>
      )}
    </Drawer>
  )
}