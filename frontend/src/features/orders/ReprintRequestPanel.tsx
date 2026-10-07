import { useState } from 'react'
import { Alert, Box, Button, Chip, CircularProgress, FormControl, InputLabel, MenuItem, Select, Stack, TextField, Typography } from '@mui/material'
import { AddPhotoAlternateRounded, ReplayRounded } from '@mui/icons-material'
import axios from 'axios'
import type { Order } from '../../shared/types/order'
import { useCreateReprintRequest, useReprintRequests } from './useReprintRequests'

const reasons = [
  ['dimensional_inaccuracy', 'Sai kích thước'],
  ['surface_defect', 'Lỗi bề mặt / chất lượng in'],
  ['wrong_material_or_color', 'Sai vật liệu hoặc màu'],
  ['damaged_in_transit', 'Hư hỏng khi vận chuyển'],
  ['missing_parts', 'Thiếu chi tiết'],
  ['other', 'Vấn đề khác'],
] as const

const statusLabels: Record<string, string> = {
  under_review: 'Đang xem xét', approved: 'Đã duyệt', denied: 'Từ chối', stale: 'Hết hạn phản hồi', closed: 'Đã đóng',
}

const fileToDataUrl = (file: File) => new Promise<string>((resolve, reject) => {
  const reader = new FileReader()
  reader.onload = () => resolve(String(reader.result))
  reader.onerror = reject
  reader.readAsDataURL(file)
})

export function ReprintRequestPanel({ order }: { order: Order }) {
  const { data = [], isLoading } = useReprintRequests(order.id)
  const create = useCreateReprintRequest(order.id)
  const [open, setOpen] = useState(false)
  const [reason, setReason] = useState('')
  const [description, setDescription] = useState('')
  const [photos, setPhotos] = useState<string[]>([])
  const [fileError, setFileError] = useState('')

  const deliveredAt = order.deliveredAt ? new Date(order.deliveredAt) : null
  const guaranteeEnds = deliveredAt ? new Date(deliveredAt.getTime() + 30 * 86400000) : null
  const eligible = order.status === 'Delivered' && !!guaranteeEnds && guaranteeEnds >= new Date()
  const hasActive = data.some(x => x.status === 'under_review' || x.status === 'approved')

  const selectPhotos = async (files: FileList | null) => {
    setFileError('')
    if (!files) return
    const selected = Array.from(files)
    if (selected.length + photos.length > 5) return setFileError('Chỉ được tải tối đa 5 ảnh.')
    if (selected.some(f => !['image/jpeg', 'image/png', 'image/webp'].includes(f.type) || f.size > 5 * 1024 * 1024))
      return setFileError('Chỉ nhận JPEG, PNG, WebP; mỗi ảnh tối đa 5 MB.')
    setPhotos([...photos, ...await Promise.all(selected.map(fileToDataUrl))])
  }

  const submit = async () => {
    await create.mutateAsync({ reason, description, photos })
    setOpen(false); setReason(''); setDescription(''); setPhotos([])
  }

  const errorMessage = axios.isAxiosError(create.error)
    ? (create.error.response?.data as { error?: { message?: string } })?.error?.message
    : undefined

  return (
    <Stack spacing={2}>
      <Typography sx={{ fontWeight: 700 }}>Xin in lại / khiếu nại</Typography>
      {isLoading ? <CircularProgress size={24} /> : data.map(request => (
        <Box key={request.requestId} sx={{ p: 1.5, border: '1px solid rgba(255,255,255,.1)', borderRadius: 2 }}>
          <Stack direction="row" justifyContent="space-between" gap={1}>
            <Typography variant="body2">{reasons.find(x => x[0] === request.reason)?.[1] ?? request.reason}</Typography>
            <Chip size="small" label={statusLabels[request.status] ?? request.status} color={request.status === 'denied' ? 'error' : request.status === 'approved' ? 'success' : 'warning'} />
          </Stack>
          <Typography variant="caption" color="text.secondary">Gửi lúc {new Date(request.createdAt).toLocaleString('vi-VN')} · {request.photos.length} ảnh</Typography>
          {request.resolutionNote && <Alert severity="info" sx={{ mt: 1 }}>{request.resolutionNote}</Alert>}
        </Box>
      ))}

      {!open && (
        <Button variant="outlined" startIcon={<ReplayRounded />} disabled={!eligible || hasActive} onClick={() => setOpen(true)}>
          {hasActive ? 'Yêu cầu đang được xem xét' : 'Gửi yêu cầu in lại'}
        </Button>
      )}
      {!eligible && order.status === 'Delivered' && <Alert severity="info">Đơn hàng đã ngoài thời hạn bảo hành 30 ngày.</Alert>}

      {open && <Stack spacing={2}>
        <FormControl fullWidth>
          <InputLabel>Lý do</InputLabel>
          <Select value={reason} label="Lý do" onChange={e => setReason(e.target.value)}>
            {reasons.map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}
          </Select>
        </FormControl>
        <TextField label="Mô tả vấn đề" multiline minRows={3} value={description} onChange={e => setDescription(e.target.value)} inputProps={{ maxLength: 2000 }} />
        <Button component="label" variant="outlined" startIcon={<AddPhotoAlternateRounded />}>
          Chọn ảnh ({photos.length}/5)
          <input hidden multiple type="file" accept="image/jpeg,image/png,image/webp" onChange={e => void selectPhotos(e.target.files)} />
        </Button>
        {photos.length > 0 && <Stack direction="row" gap={1} flexWrap="wrap">{photos.map((src, i) => <Box key={i} component="img" src={src} alt={`Ảnh lỗi ${i + 1}`} sx={{ width: 64, height: 64, objectFit: 'cover', borderRadius: 1 }} />)}</Stack>}
        {fileError && <Alert severity="error">{fileError}</Alert>}
        {create.isError && <Alert severity="error">{errorMessage ?? 'Không gửi được yêu cầu.'}</Alert>}
        <Stack direction="row" spacing={1}>
          <Button onClick={() => setOpen(false)} color="inherit">Hủy</Button>
          <Button variant="contained" disabled={!reason || !description.trim() || photos.length === 0 || create.isPending} onClick={() => void submit()}>
            {create.isPending ? 'Đang gửi…' : 'Gửi yêu cầu'}
          </Button>
        </Stack>
      </Stack>}
    </Stack>
  )
}
