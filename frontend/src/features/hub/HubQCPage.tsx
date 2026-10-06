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
  Radio,
  RadioGroup,
  Stack,
  TextField,
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
import { useState } from 'react'
import { JOB_LABELS } from '../jobs/jobTypes'
import type { Job } from '../jobs/jobTypes'
import { useJobs, useInspectJob } from '../jobs/useJobs'
import { useAuth } from '../../app/AuthContext'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')

interface ChecklistItemState {
  name: string
  status: 'Pass' | 'Fail' | 'NotApplicable'
}

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

  const [activeJob, setActiveJob] = useState<Job | null>(null)
  const [checklist, setChecklist] = useState<ChecklistItemState[]>([])
  const [photoUrls, setPhotoUrls] = useState<string[]>([])
  const [newPhotoInput, setNewPhotoInput] = useState('')
  const [faultAttribution, setFaultAttribution] = useState<'Lab' | 'Hub' | 'Customer'>('Lab')
  const [note, setNote] = useState('')
  const [modalMode, setModalMode] = useState<'inspect' | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successNotice, setSuccessNotice] = useState<string | null>(null)

  const jobs = awaiting.data ?? []

  function openInspectionModal(job: Job) {
    setActiveJob(job)
    setChecklist(
      DEFAULT_CHECKLIST_TEMPLATE.map((name) => ({
        name,
        status: 'Pass',
      }))
    )
    setPhotoUrls([])
    setNewPhotoInput('')
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

  function handleAddPhoto() {
    if (!newPhotoInput.trim()) return
    setPhotoUrls((prev) => [...prev, newPhotoInput.trim()])
    setNewPhotoInput('')
  }

  function handleRemovePhoto(index: number) {
    setPhotoUrls((prev) => prev.filter((_, i) => i !== index))
  }

  const hasAnyFail = checklist.some((item) => item.status === 'Fail')

  async function handleSubmitDecision(passed: boolean) {
    if (!activeJob) return
    setErrorMessage(null)

    // Lấy danh sách ảnh gồm photoUrls hiện tại và cả URL vừa gõ dở nếu có
    const effectivePhotos = [...photoUrls]
    if (newPhotoInput.trim() && !effectivePhotos.includes(newPhotoInput.trim())) {
      effectivePhotos.push(newPhotoInput.trim())
      setPhotoUrls(effectivePhotos)
      setNewPhotoInput('')
    }

    // Yêu cầu bắt buộc phải có ít nhất 1 ảnh bằng chứng nghiệm thu
    if (effectivePhotos.length === 0) {
      setErrorMessage('Bắt buộc phải có ít nhất 1 ảnh chụp nghiệm thu chi tiết trước khi lưu kết luận.')
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
      await inspect.mutateAsync({
        id: activeJob.id,
        passed,
        checklistResults: checklist.map((c) => ({ itemName: c.name, status: c.status })),
        photoUrls: effectivePhotos,
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
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
              Kiểm tra chất lượng — Hub
            </Typography>
            <Typography color="text.secondary">Kiểm định và đánh giá chất lượng sản phẩm in hoàn thiện</Typography>
          </Box>
          <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/login', { replace: true }) }}>
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
        onClose={() => setModalMode(null)}
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

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mb: 2 }}>
            <TextField
              size="small"
              fullWidth
              placeholder="Nhập đường dẫn URL ảnh nghiệm thu..."
              value={newPhotoInput}
              onChange={(e) => setNewPhotoInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  handleAddPhoto()
                }
              }}
            />
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" startIcon={<AddPhotoAlternateRounded />} onClick={handleAddPhoto}>
                Thêm ảnh
              </Button>
              <Button
                variant="text"
                size="small"
                sx={{ color: 'text.secondary', whiteSpace: 'nowrap' }}
                onClick={() => {
                  if (!photoUrls.includes('https://cdn.printgrid.dev/qc/sample-evidence.jpg')) {
                    setPhotoUrls((prev) => [...prev, 'https://cdn.printgrid.dev/qc/sample-evidence.jpg'])
                  }
                }}
              >
                + Ảnh mẫu
              </Button>
            </Stack>
          </Stack>

          {photoUrls.length === 0 ? (
            <Alert severity="warning" sx={{ mb: 3 }}>
              Chưa có ảnh bằng chứng nào được tải lên! Vui lòng thêm URL ảnh chụp sản phẩm (bấm "Xác nhận" lúc này hệ thống sẽ chặn và báo lỗi).
            </Alert>
          ) : (
            <Stack direction="row" spacing={1.5} sx={{ mb: 3, flexWrap: 'wrap', gap: 1 }}>
              {photoUrls.map((url, i) => (
                <Chip
                  key={i}
                  label={`Ảnh ${i + 1}: ${url.length > 30 ? url.slice(0, 30) + '...' : url}`}
                  onDelete={() => handleRemovePhoto(i)}
                  deleteIcon={<DeleteOutlineRounded />}
                  color="info"
                  variant="outlined"
                />
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
          <Button onClick={() => setModalMode(null)} color="inherit" sx={{ color: 'text.secondary' }}>
            Đóng
          </Button>

          <Button
            variant="contained"
            color="error"
            startIcon={<CancelRounded />}
            disabled={inspect.isPending}
            onClick={() => handleSubmitDecision(false)}
          >
            {inspect.isPending ? 'Đang xử lý…' : 'Xác nhận TRƯỢT (FAIL)'}
          </Button>

          <Button
            variant="contained"
            color="success"
            startIcon={<CheckRounded />}
            disabled={inspect.isPending || hasAnyFail}
            onClick={() => handleSubmitDecision(true)}
          >
            {inspect.isPending ? 'Đang xử lý…' : 'Xác nhận ĐẠT (PASS)'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}