import { useNavigate } from 'react-router-dom'
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
  Divider,
  FormControl,
  FormControlLabel,
  FormLabel,
  IconButton,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'


import {
  CheckCircleRounded,
  CancelRounded,
  FactoryRounded,
  LogoutRounded,
  AddPhotoAlternateRounded,
  DeleteOutlineRounded,
  ReportProblemRounded,
  CheckRounded,
} from '@mui/icons-material'
import { useEffect, useRef, useState } from 'react'
import { JOB_LABELS } from '../jobs/jobTypes'
import type { Job } from '../jobs/jobTypes'
import { useJobs, useInspectJob, useUploadInspectionPhotos } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')

interface ChecklistItemState {
  name: string
  status: 'Pass' | 'Fail' | 'NotApplicable'
}

/** Một ảnh QC đã chọn từ thiết bị, kèm URL xem trước tạm thời (Object URL). */
interface PhotoItem {
  file: File
  previewUrl: string
}

const MAX_PHOTOS = 5
const MAX_PHOTO_BYTES = 5 * 1024 * 1024

const DEFAULT_CHECKLIST_TEMPLATE: string[] = [
  'Kích thước hình học & dung sai (±0.2mm)',
  'Độ nhẵn bề mặt & hoàn thiện',
  'Độ bám dính lớp in & không tách lớp',
  'Độ phẳng & không cong vênh đế (warping)',
]

/**
 * Hub quality control (FR-HUB-002 & FR-HUB-003):
 * - Load checklist per standard
 * - Photo evidence mandatory before concluding inspection (AC01)
 * - Every checklist item evaluated (Pass/Fail/NA) (AC02)
 * - Fault attribution required on fail (AC03)
 * - Lab/Hub fault triggers urgent reprint; Customer fault notifies customer without reprint.
 */
export default function HubQCPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const awaiting = useJobs('AwaitingInspection')
  const inspect = useInspectJob()
  const uploadPhotos = useUploadInspectionPhotos()

  const [activeJob, setActiveJob] = useState<Job | null>(null)
  const [checklist, setChecklist] = useState<ChecklistItemState[]>([])
  const [photos, setPhotos] = useState<PhotoItem[]>([])
  const [faultAttribution, setFaultAttribution] = useState<'Lab' | 'Hub' | 'Customer'>('Lab')
  const [note, setNote] = useState('')
  const [modalMode, setModalMode] = useState<'inspect' | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successNotice, setSuccessNotice] = useState<string | null>(null)

  const fileInputRef = useRef<HTMLInputElement>(null)
  // Keep the latest photos so we can revoke their Object URLs when the page unmounts.
  const photosRef = useRef<PhotoItem[]>([])
  useEffect(() => {
    photosRef.current = photos
  }, [photos])
  useEffect(
    () => () => {
      photosRef.current.forEach((p) => URL.revokeObjectURL(p.previewUrl))
    },
    []
  )

  const jobs = awaiting.data ?? []

  function openInspectionModal(job: Job) {
    setActiveJob(job)
    setChecklist(
      DEFAULT_CHECKLIST_TEMPLATE.map((name) => ({
        name,
        status: 'Pass',
      }))
    )
    setPhotos((prev) => {
      prev.forEach((p) => URL.revokeObjectURL(p.previewUrl))
      return []
    })
    setFaultAttribution('Lab')
    setNote('')
    setErrorMessage(null)
    setModalMode('inspect')
  }

  function handleChecklistStatusChange(index: number, status: 'Pass' | 'Fail' | 'NotApplicable') {
    setChecklist((prev) => {
      const copy = [...prev]
      copy[index] = { ...copy[index], status }
      return copy
    })
  }

  function handleFilesSelected(fileList: FileList | null) {
    if (!fileList || fileList.length === 0) return
    setErrorMessage(null)

    const next = [...photos]
    let rejectedTypeOrSize = false
    let rejectedTooMany = false

    for (const file of Array.from(fileList)) {
      if (next.length >= MAX_PHOTOS) {
        rejectedTooMany = true
        break
      }
      if (!file.type.startsWith('image/') || file.size > MAX_PHOTO_BYTES || file.size === 0) {
        rejectedTypeOrSize = true
        continue
      }
      const isDuplicate = next.some((p) => p.file.name === file.name && p.file.size === file.size)
      if (isDuplicate) continue
      next.push({ file, previewUrl: URL.createObjectURL(file) })
    }

    if (rejectedTypeOrSize) {
      setErrorMessage('Chỉ nhận tệp ảnh (image/*), mỗi ảnh không quá 5 MB — một số tệp đã bị bỏ qua.')
    } else if (rejectedTooMany) {
      setErrorMessage(`Chỉ được chọn tối đa ${MAX_PHOTOS} ảnh cho một lần kiểm định.`)
    }
    setPhotos(next)
    // Reset the input so the same file can be re-selected after removal.
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  function handleRemovePhoto(index: number) {
    setPhotos((prev) => {
      const copy = [...prev]
      const [removed] = copy.splice(index, 1)
      if (removed) URL.revokeObjectURL(removed.previewUrl)
      return copy
    })
  }

  const hasAnyFail = checklist.some((item) => item.status === 'Fail')
  const isSubmitting = uploadPhotos.isPending || inspect.isPending

  async function handleSubmitDecision(passed: boolean) {
    if (!activeJob) return
    setErrorMessage(null)

    // Yêu cầu bắt buộc phải có ít nhất 1 ảnh bằng chứng nghiệm thu (AC01)
    if (photos.length === 0) {
      setErrorMessage('Bắt buộc phải tải lên ít nhất 1 ảnh chụp nghiệm thu chi tiết trước khi lưu kết luận.')
      return
    }

    // Mọi mục kiểm tra trong checklist phải có đánh giá Đậu / Trượt / NA
    const allEvaluated = checklist.every((c) => ['Pass', 'Fail', 'NotApplicable'].includes(c.status))
    if (!allEvaluated) {
      setErrorMessage('Mọi mục trong danh mục kiểm tra phải được đánh giá (Đạt / Trượt / N/A).')
      return
    }

    // Trượt thì bắt buộc phải quy trách nhiệm
    if (!passed && !faultAttribution) {
      setErrorMessage('Khi đánh trượt, bắt buộc phải chọn nguyên nhân quy trách nhiệm (Xưởng / Hub / Khách hàng).')
      return
    }

    try {
      // 1) Tải ảnh lên MinIO để lấy object keys, rồi 2) gửi keys vào kết luận kiểm định.
      const photoKeys = await uploadPhotos.mutateAsync({
        id: activeJob.id,
        photos: photos.map((p) => p.file),
      })

      await inspect.mutateAsync({
        id: activeJob.id,
        passed,
        checklistResults: checklist.map((c) => ({ itemName: c.name, status: c.status })),
        photoUrls: photoKeys,
        faultAttribution: passed ? undefined : faultAttribution,
        note: note.trim() || (passed ? 'Đạt tiêu chuẩn QC Hub' : `Trượt QC (${faultAttribution})`),
      })

      if (passed) {
        setSuccessNotice(`Job ${activeJob.id.slice(0, 8)} đã ĐẠT kiểm định và chuyển sang khâu đóng gói hoàn tất.`)
      } else if (faultAttribution === 'Customer') {
        setSuccessNotice(`Job ${activeJob.id.slice(0, 8)} trượt do lỗi khách: Đã gửi email thông báo, KHÔNG tạo lệnh in lại miễn phí.`)
      } else {
        setSuccessNotice(`Job ${activeJob.id.slice(0, 8)} trượt do lỗi ${faultAttribution}: Đã tự động tạo lệnh in lại URGENT cho xưởng!`)
      }

      // Cleanup Object URLs cho các ảnh đã xử lý xong.
      photos.forEach((p) => URL.revokeObjectURL(p.previewUrl))
      setPhotos([])
      setModalMode(null)
      setActiveJob(null)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể lưu kết luận kiểm định.')
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1000, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Kiểm tra chất lượng — Hub
              </Typography>
              <Typography color="text.secondary">Kiểm định và đánh giá chất lượng sản phẩm in hoàn thiện (FR-HUB-002)</Typography>
            </Box>
          </Stack>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/', { replace: true }) }}>
            Đăng xuất
          </Button>
        </Stack>

        {successNotice && (
          <Alert severity="success" onClose={() => setSuccessNotice(null)}>
            {successNotice}
          </Alert>
        )}

        {awaiting.isError ? <Alert severity="error">Không tải được danh sách chờ kiểm tra.</Alert> : null}
        {awaiting.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!awaiting.isLoading && jobs.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có job nào chờ kiểm tra chất lượng.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {jobs.map((j) => (
              <Box key={j.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', md: 'center' }} spacing={2}>
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
                      <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{j.materialCode} · {j.colorCode}</Typography>
                      <Chip label={JOB_LABELS[j.status]} size="small" color="warning" />
                    </Stack>
                    <Typography variant="body2" color="text.secondary">
                      Ước tính {j.estimatedPrintMinutes} phút · Lớp {j.layerHeightMm}mm · Hạn nội bộ {fmtDate(j.internalDueDate)}
                    </Typography>
                  </Box>
                  <Stack direction="row" spacing={1.5} justifyContent={{ xs: 'flex-start', md: 'flex-end' }}>
                    <Button
                      variant="contained"
                      color="primary"
                      startIcon={<CheckCircleRounded />}
                      onClick={() => openInspectionModal(j)}
                    >
                      Bắt đầu kiểm định QC
                    </Button>
                  </Stack>
                </Stack>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>

      {/* QC Inspection Modal */}
      <Dialog
        open={modalMode === 'inspect'}
        onClose={() => {
          if (!isSubmitting) setModalMode(null)
        }}
        fullWidth
        maxWidth="md"
        PaperProps={{ sx: { bgcolor: '#121212', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.15)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 800, borderBottom: '1px solid rgba(255,255,255,0.08)' }}>
          Biên bản kiểm tra chất lượng sản phẩm
        </DialogTitle>
        <DialogContent sx={{ mt: 2 }}>
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2.5 }} onClose={() => setErrorMessage(null)}>
              {errorMessage}
            </Alert>
          )}

          {activeJob && (
            <Box sx={{ mb: 3, p: 2, bgcolor: 'rgba(255,255,255,0.03)', borderRadius: 2 }}>
              <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 0.5 }}>
                Thông tin sản phẩm:
              </Typography>
              <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>
                Job: {activeJob.id.slice(0, 8)} · Vật liệu: {activeJob.materialCode} ({activeJob.colorCode}) · Hạn: {fmtDate(activeJob.internalDueDate)}
              </Typography>
            </Box>
          )}

          {/* Section 1: Checklist */}
          <Typography variant="h6" sx={{ fontSize: '1.05rem', fontWeight: 700, color: 'text.primary', mb: 1.5 }}>
            1. Danh mục tiêu chí kiểm tra (Đánh giá Đậu / Trượt / Không áp dụng)
          </Typography>
          <Stack spacing={1.5} sx={{ mb: 3 }}>
            {checklist.map((item, idx) => (
              <Box
                key={idx}
                sx={{
                  p: 1.5,
                  borderRadius: 1.5,
                  bgcolor: 'rgba(255,255,255,0.02)',
                  border: '1px solid rgba(255,255,255,0.06)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  flexWrap: 'wrap',
                  gap: 1.5,
                }}
              >
                <Typography sx={{ color: 'text.primary', fontSize: '0.92rem', fontWeight: 500 }}>
                  {idx + 1}. {item.name}
                </Typography>
                <Stack direction="row" spacing={1}>
                  <Button
                    size="small"
                    variant={item.status === 'Pass' ? 'contained' : 'outlined'}
                    color="success"
                    onClick={() => handleChecklistStatusChange(idx, 'Pass')}
                  >
                    Đậu (Pass)
                  </Button>
                  <Button
                    size="small"
                    variant={item.status === 'Fail' ? 'contained' : 'outlined'}
                    color="error"
                    onClick={() => handleChecklistStatusChange(idx, 'Fail')}
                  >
                    Trượt (Fail)
                  </Button>
                  <Button
                    size="small"
                    variant={item.status === 'NotApplicable' ? 'contained' : 'outlined'}
                    color="inherit"
                    onClick={() => handleChecklistStatusChange(idx, 'NotApplicable')}
                  >
                    N/A
                  </Button>
                </Stack>
              </Box>
            ))}
          </Stack>

          <Divider sx={{ my: 2.5, borderColor: 'rgba(255,255,255,0.08)' }} />

          {/* Section 2: Photo Evidence */}
          <Typography variant="h6" sx={{ fontSize: '1.05rem', fontWeight: 700, color: 'text.primary', mb: 1 }}>
            2. Ảnh chụp nghiệm thu (Bắt buộc tối thiểu 1 ảnh)
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            Không có ảnh chụp bằng chứng =&gt; hệ thống không cho phép lưu kết luận nghiệm thu.
          </Typography>

          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            multiple
            hidden
            onChange={(e) => handleFilesSelected(e.target.files)}
          />
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 2 }} flexWrap="wrap">
            <Button
              variant="outlined"
              startIcon={<AddPhotoAlternateRounded />}
              onClick={() => fileInputRef.current?.click()}
              disabled={photos.length >= MAX_PHOTOS || uploadPhotos.isPending || inspect.isPending}
            >
              Chọn ảnh từ thiết bị
            </Button>
            <Typography variant="caption" color="text.secondary">
              Tối đa {MAX_PHOTOS} ảnh, mỗi ảnh không quá 5 MB · Đã chọn {photos.length}/{MAX_PHOTOS}
            </Typography>
            {uploadPhotos.isPending && <CircularProgress size={18} />}
          </Stack>

          {photos.length === 0 ? (
            <Alert severity="warning" sx={{ mb: 3 }}>
              Chưa có ảnh bằng chứng nào được tải lên! Vui lòng chọn ảnh chụp sản phẩm từ thiết bị (bấm "Xác nhận" lúc này hệ thống sẽ chặn và báo lỗi).
            </Alert>
          ) : (
            <Stack direction="row" spacing={1.5} sx={{ mb: 3, flexWrap: 'wrap', gap: 1.5 }}>
              {photos.map((photo, i) => (
                <Box key={photo.previewUrl} sx={{ position: 'relative', width: 108 }}>
                  <Box
                    component="img"
                    src={photo.previewUrl}
                    alt={`Ảnh nghiệm thu ${i + 1}`}
                    sx={{
                      width: 108,
                      height: 108,
                      objectFit: 'cover',
                      display: 'block',
                      borderRadius: 1.5,
                      border: '1px solid rgba(255,255,255,0.15)',
                    }}
                  />
                  <Tooltip title="Bỏ ảnh này">
                    <IconButton
                      size="small"
                      onClick={() => handleRemovePhoto(i)}
                      disabled={uploadPhotos.isPending || inspect.isPending}
                      sx={{
                        position: 'absolute',
                        top: 4,
                        right: 4,
                        bgcolor: 'rgba(0,0,0,0.65)',
                        color: '#fff',
                        '&:hover': { bgcolor: 'rgba(0,0,0,0.85)' },
                      }}
                    >
                      <DeleteOutlineRounded fontSize="small" />
                    </IconButton>
                  </Tooltip>
                  <Typography
                    variant="caption"
                    sx={{
                      display: 'block',
                      mt: 0.5,
                      color: 'text.secondary',
                      width: 108,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    Ảnh {i + 1}: {photo.file.name}
                  </Typography>
                </Box>
              ))}
            </Stack>
          )}

          <Divider sx={{ my: 2.5, borderColor: 'rgba(255,255,255,0.08)' }} />

          {/* Section 3: Fault Attribution & Decision */}
          <Typography variant="h6" sx={{ fontSize: '1.05rem', fontWeight: 700, color: 'text.primary', mb: 1.5 }}>
            3. Kết luận nghiệm thu &amp; Quy trách nhiệm (nếu trượt)
          </Typography>

          {hasAnyFail && (
            <Box sx={{ p: 2, mb: 2.5, bgcolor: 'rgba(239, 68, 68, 0.08)', borderRadius: 2, border: '1px solid rgba(239, 68, 68, 0.2)' }}>
              <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 1.5 }}>
                <ReportProblemRounded color="error" />
                <Typography sx={{ fontWeight: 700, color: '#ef4444' }}>
                  Phát hiện mục không đạt trong checklist! Bắt buộc quy trách nhiệm khi FAIL:
                </Typography>
              </Stack>

              <FormControl component="fieldset">
                <FormLabel component="legend" sx={{ color: 'text.secondary', fontSize: '0.88rem' }}>
                  Nguyên nhân &amp; Quy trách nhiệm lỗi:
                </FormLabel>
                <RadioGroup
                  row
                  value={faultAttribution}
                  onChange={(e) => setFaultAttribution(e.target.value as any)}
                >
                  <FormControlLabel
                    value="Lab"
                    control={<Radio color="error" />}
                    label="Lỗi Xưởng in (Lab fault → Tự động tạo job URGENT in lại)"
                  />
                  <FormControlLabel
                    value="Hub"
                    control={<Radio color="error" />}
                    label="Lỗi Hub vận chuyển / đóng gói (Hub fault → Tạo job URGENT in lại)"
                  />
                  <FormControlLabel
                    value="Customer"
                    control={<Radio color="error" />}
                    label="Lỗi file khách hàng (Customer fault → Gửi thông báo, 0 tạo job in)"
                  />
                </RadioGroup>
              </FormControl>
            </Box>
          )}

          <TextField
            label="Ghi chú kết luận kiểm định"
            value={note}
            onChange={(e) => setNote(e.target.value)}
            fullWidth
            multiline
            minRows={2}
            placeholder="Nhập ghi chú chi tiết nếu có..."
          />
        </DialogContent>

        <DialogActions sx={{ px: 3, pb: 2.5, borderTop: '1px solid rgba(255,255,255,0.08)', pt: 2 }}>
          <Button onClick={() => setModalMode(null)} color="inherit" disabled={isSubmitting} sx={{ color: 'text.secondary' }}>
            Đóng
          </Button>

          <Button
            variant="contained"
            color="error"
            startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <CancelRounded />}
            disabled={isSubmitting}
            onClick={() => handleSubmitDecision(false)}
          >
            {uploadPhotos.isPending ? 'Đang tải ảnh lên…' : isSubmitting ? 'Đang xử lý…' : 'Xác nhận TRƯỢT (FAIL)'}
          </Button>

          <Button
            variant="contained"
            color="success"
            startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <CheckRounded />}
            disabled={isSubmitting || hasAnyFail}
            onClick={() => handleSubmitDecision(true)}
          >
            {uploadPhotos.isPending ? 'Đang tải ảnh lên…' : isSubmitting ? 'Đang xử lý…' : 'Xác nhận ĐẠT (PASS)'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}