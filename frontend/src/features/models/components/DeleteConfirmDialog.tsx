import { Box, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle } from '@mui/material'
import { DeleteForeverRounded } from '@mui/icons-material'

interface DeleteConfirmDialogProps {
  open: boolean
  modelName: string
  deleting: boolean
  onCancel: () => void
  onConfirm: () => void
}

export function DeleteConfirmDialog({ open, modelName, deleting, onCancel, onConfirm }: DeleteConfirmDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={deleting ? undefined : onCancel}
      fullWidth
      maxWidth="xs"
      PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}
    >
      <DialogTitle sx={{ color: 'text.primary', fontWeight: 700 }}>Xóa model</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ color: 'text.secondary' }}>
          Bạn có chắc muốn xóa <Box component="strong" sx={{ color: 'text.primary' }}>{modelName}</Box>?
          Hành động này không thể hoàn tác.
        </DialogContentText>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        <Button onClick={onCancel} disabled={deleting} color="inherit" sx={{ color: 'text.secondary' }}>
          Hủy
        </Button>
        <Button onClick={onConfirm} disabled={deleting} variant="contained" color="error" startIcon={<DeleteForeverRounded />}>
          {deleting ? 'Đang xóa…' : 'Xóa'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}