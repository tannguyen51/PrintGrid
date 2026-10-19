import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { SendRounded, UploadFileRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useCreateDesignRequest, useMyDesignRequests, useUploadDesignImages } from './useDesignRequests'

const statusColor = (status: string): 'warning' | 'info' | 'success' | 'error' | 'default' =>
  status === 'Pending' ? 'warning'
    : status === 'InReview' ? 'info'
      : status === 'Accepted' ? 'success'
        : status === 'Rejected' ? 'error'
          : 'default'

/**
 * FR-CUST-013 / BR-IP-004 — design intake. The customer describes the part and attaches 1–5
 * reference images; ops then triages the request from the design queue.
 */
export default function DesignRequestPage() {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const { data, isLoading } = useMyDesignRequests()
  const uploadImages = useUploadDesignImages()
  const createRequest = useCreateDesignRequest()
  const isPending = uploadImages.isPending || createRequest.isPending

  function pickFiles(list: FileList | null) {
    const picked = Array.from(list ?? [])
    if (picked.length > 5) {
      setError('Chỉ được đính kèm tối đa 5 ảnh tham chiếu.')
      setFiles(picked.slice(0, 5))
      return
    }
    setError(null)
    setFiles(picked)
  }

  async function submit() {
    setError(null)
    setSuccess(null)
    if (!title.trim() || !description.trim()) {
      setError('Vui lòng nhập tiêu đề và mô tả thiết kế.')
      return
    }
    if (files.length < 1 || files.length > 5) {
      setError('Vui lòng đính kèm từ 1 đến 5 ảnh tham chiếu.')
      return
    }

    try {
      const referenceImageKeys = await uploadImages.mutateAsync(files)
      await createRequest.mutateAsync({
        title: title.trim(),
        description: description.trim(),
        referenceImageKeys,
      })
      setTitle('')
      setDescription('')
      setFiles([])
      setSuccess('Đã gửi yêu cầu thiết kế. Bộ phận vận hành sẽ phản hồi sớm nhất.')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Không gửi được yêu cầu thiết kế.')
    }
  }

  const requests = data ?? []

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 900, mx: 'auto' }} spacing={3}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <PageBackButton />
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Yêu cầu thiết kế
            </Typography>
            <Typography color="text.secondary">
              Mô tả chi tiết chi tiết bạn cần in và đính kèm ảnh tham chiếu (1–5 ảnh). Đội ngũ PrintGrid sẽ báo giá sau khi xem xét.
            </Typography>
          </Box>
        </Stack>

        <Box sx={{ p: 3, borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
          <Stack spacing={2}>
            <TextField
              label="Tiêu đề"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              inputProps={{ maxLength: 200 }}
              required
            />
            <TextField
              label="Mô tả"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              multiline
              minRows={5}
              inputProps={{ maxLength: 4000 }}
              placeholder="Kích thước, vật liệu, màu sắc, yêu cầu lắp ghép…"
              required
            />
            <Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap">
              <Button component="label" variant="outlined" startIcon={<UploadFileRounded />} disabled={isPending}>
                Chọn ảnh tham chiếu
                <input
                  hidden
                  type="file"
                  accept="image/*"
                  multiple
                  onChange={(e) => pickFiles(e.target.files)}
                />
              </Button>
              <Typography variant="body2" color="text.secondary">
                {files.length === 0 ? 'Chưa chọn ảnh (tối đa 5 ảnh, mỗi ảnh ≤ 5 MB)' : `${files.length} ảnh đã chọn`}
              </Typography>
            </Stack>

            {error && <Alert severity="error">{error}</Alert>}
            {success && <Alert severity="success">{success}</Alert>}

            <Box>
              <Button
                variant="contained"
                startIcon={isPending ? <CircularProgress size={16} color="inherit" /> : <SendRounded />}
                disabled={isPending}
                onClick={submit}
              >
                {isPending ? 'Đang gửi…' : 'Gửi yêu cầu'}
              </Button>
            </Box>
          </Stack>
        </Box>

        <Box>
          <Typography sx={{ fontWeight: 800, color: 'text.primary', mb: 1.5 }}>
            Yêu cầu của tôi
          </Typography>
          {isLoading ? (
            <CircularProgress size={24} />
          ) : requests.length === 0 ? (
            <Typography color="text.disabled">Bạn chưa gửi yêu cầu thiết kế nào.</Typography>
          ) : (
            <Stack spacing={1.25}>
              {requests.map((row) => (
                <Box key={row.id} sx={{ p: 2, borderRadius: 2, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={2}>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{row.title}</Typography>
                      <Typography variant="body2" color="text.secondary" noWrap>{row.description}</Typography>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center">
                      <Typography variant="caption" color="text.disabled">
                        {new Date(row.createdAtUtc).toLocaleDateString('vi-VN')}
                      </Typography>
                      <Chip size="small" color={statusColor(row.status)} label={row.status} />
                    </Stack>
                  </Stack>
                </Box>
              ))}
            </Stack>
          )}
        </Box>
      </Stack>
    </Box>
  )
}
