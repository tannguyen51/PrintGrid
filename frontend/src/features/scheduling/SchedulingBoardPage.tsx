import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  InputAdornment,
  Skeleton,
  ToggleButton,
  ToggleButtonGroup,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  AssignmentTurnedInRounded,
  ArrowForwardRounded,
  CheckCircleOutlineRounded,
  ChevronRightRounded,
  FactCheckOutlined,
  InsightsOutlined,
  ReceiptLongOutlined,
  ScheduleRounded,
  SearchRounded,
  ViewInArOutlined,
  FactoryRounded,
  RefreshRounded,
  RocketLaunchRounded,
  WarningAmberRounded,
} from '@mui/icons-material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { OpsEmptyState, OpsPageHeader, OpsStatCard } from '../ops/OpsComponents'
import { apiClient } from '../../shared/api/apiClient'
import { JOB_LABELS } from '../jobs/jobTypes'
import { useJobs } from '../jobs/useJobs'
import { useOpsMonitor, useOpsReassign, useOpsReassignPreview, type DateChangeReason, type OpsRiskJob } from '../ops/useOps'

const fmtDate = (value?: string | null) => value ? new Date(value.includes('T') ? value : `${value}T00:00:00`).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }) : '—'

/**
 * Ops risk-monitoring / reassignment console (FR-ANALYTICS-001, FR-SCHED-009) plus the
 * pending-job board that drives the assignment engine (FR-SCHED-004/006).
 * Monitoring refreshes every 30 seconds; manual assign/repair-risk remain available here.
 */
export default function SchedulingBoardPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  // Risk-monitoring / reassignment console (incoming).
  const monitor = useOpsMonitor()
  const reassign = useOpsReassign()
  const [target, setTarget] = useState<OpsRiskJob | null>(null)
  const [reason, setReason] = useState('')
  const [dateChangeReason, setDateChangeReason] = useState<DateChangeReason | ''>('')
  const [step, setStep] = useState<1 | 2>(1)
  const preview = useOpsReassignPreview(target?.jobId ?? null)
  const close = () => { setTarget(null); setReason(''); setDateChangeReason(''); setStep(1); reassign.reset() }
  const open = (risk: OpsRiskJob) => { setTarget(risk); setReason(''); setDateChangeReason(''); setStep(1) }
  async function confirm() {
    if (!target) return
    const impact = Boolean(preview.data?.requiresCustomerApproval)
    if (impact && step === 1) { setStep(2); return }
    let reasonCode: DateChangeReason | undefined
    if (impact && step === 2) {
      // B16 requires one of the three approved conditions before a date-change request exists.
      if (!dateChangeReason) return
      reasonCode = dateChangeReason
    }
    try {
      await reassign.mutateAsync({
        jobId: target.jobId,
        reason: reason.trim(),
        confirmCustomerDateImpact: impact && step === 2,
        dateChangeReason: reasonCode,
      })
    } catch {
      // The mutation error is displayed inside the intervention dialog.
    }
  }

  // Pending-job board — assign / repair-risk (HEAD).
  const pending = useJobs('Pending')
  const [notice, setNotice] = useState<string | null>(null)

  const assign = useMutation({
    mutationFn: (jobId: string) => apiClient.post(`/scheduling/jobs/${jobId}/assign`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['jobs'] }),
  })
  const repair = useMutation({
    mutationFn: (jobId: string) => apiClient.post<{ message: string }>(`/scheduling/jobs/${jobId}/repair-risk`).then(r => r.data),
    onSuccess: data => { setNotice(data.message); queryClient.invalidateQueries({ queryKey: ['jobs'] }) },
  })

  const jobs = pending.data ?? []
  const data = monitor.data
  const [riskFilter, setRiskFilter] = useState('all')
  const [jobSearch, setJobSearch] = useState('')
  const risks = [...(data?.risks ?? [])].sort((a, b) => a.slackHours - b.slackHours)
  const visibleRisks = risks.filter((risk) => riskFilter === 'all' || (riskFilter === 'overdue' ? risk.slackHours < 0 : risk.slackHours >= 0))
  const term = jobSearch.trim().toLocaleLowerCase('vi-VN')
  const visibleJobs = jobs.filter((job) => [job.id, job.orderNumber, job.modelFileName, job.materialCode, job.colorCode].join(' ').toLocaleLowerCase('vi-VN').includes(term))
  const refresh = () => { void Promise.all([monitor.refetch(), pending.refetch()]) }
  const fetching = monitor.isFetching || pending.isFetching

  return <Box className="ops-page">
    <Stack spacing={3}>
      <OpsPageHeader eyebrow="Điều hành mạng lưới" title="Trung tâm điều phối" description="Theo dõi tiến độ, nhận diện rủi ro và phân bổ công việc trong mạng lưới."
        actions={<Button variant="outlined" startIcon={<RefreshRounded />} onClick={refresh} disabled={fetching}>{fetching ? 'Đang cập nhật…' : 'Làm mới dữ liệu'}</Button>} />
      <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1} flexWrap="wrap">
        <Stack direction="row" spacing={0.75} alignItems="center"><ScheduleRounded sx={{ fontSize: 14, color: 'text.secondary' }} /><Typography sx={{ fontSize: 11, color: 'text.secondary' }}>Giám sát tự động mỗi 30 giây</Typography></Stack>
        {data && <Typography component="time" dateTime={data.refreshedAtUtc} sx={{ fontSize: 11, color: 'text.secondary' }}>Cập nhật lúc {new Date(data.refreshedAtUtc).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' })}</Typography>}
      </Stack>
      {monitor.isError && <Alert severity="error" action={<Button onClick={refresh}>Thử lại</Button>}>Không tải được dữ liệu giám sát. Các hàng đợi khác vẫn có thể truy cập từ menu vận hành.</Alert>}
      <Box className="ops-overview-grid">
        <OpsStatCard label="Công việc đang mở" value={data?.openJobs ?? '—'} hint="Toàn bộ mạng lưới sản xuất" icon={<FactoryRounded />} loading={monitor.isLoading} />
        <OpsStatCard label="Có nguy cơ trễ hạn" value={data?.atRiskJobs ?? '—'} hint={data ? 'Dư thời gian dưới ' + data.atRiskSlackHours + ' giờ' : 'Chưa có dữ liệu giám sát'} icon={<WarningAmberRounded />} tone={data?.atRiskJobs ? 'error' : 'primary'} loading={monitor.isLoading} />
        <OpsStatCard label="Công việc chờ gán" value={pending.data ? pending.data.length : '—'} hint="Cần phân bổ xưởng và máy in" icon={<AssignmentTurnedInRounded />} tone="warning" loading={pending.isLoading} />
        <OpsStatCard label="Dự báo đúng hạn" value={data ? data.onTimeForecastPercent.toLocaleString('vi-VN', { maximumFractionDigits: 1 }) + '%' : '—'} hint="Theo kết quả giám sát hiện tại" icon={<InsightsOutlined />} tone="success" loading={monitor.isLoading} />
      </Box>
      <Box className="ops-board-grid">
        <Stack spacing={2.75} sx={{ minWidth: 0 }}>
          <Paper variant="outlined" className="ops-panel">
            <Box className="ops-panel-heading"><Box><div className="ops-panel-title"><WarningAmberRounded sx={{ fontSize: 18, color: '#B67B2C' }} /><h2>Ưu tiên xử lý</h2><Chip label={data ? risks.length : '—'} size="small" color={risks.length ? 'warning' : 'default'} /></div><p>Công việc được sắp theo mức độ rủi ro, cao nhất trước</p></Box><Button component={Link} to="/ops/escalations" size="small" endIcon={<ArrowForwardRounded />}>Cảnh báo</Button></Box>
            <Box className="ops-filter-bar"><ToggleButtonGroup size="small" exclusive value={riskFilter} onChange={(_, value) => { if (value) setRiskFilter(value) }} aria-label="Lọc mức độ rủi ro">
              <ToggleButton value="all">Tất cả</ToggleButton><ToggleButton value="overdue">Đã quá hạn</ToggleButton><ToggleButton value="upcoming">Sắp đến hạn</ToggleButton>
            </ToggleButtonGroup><Typography role="status" sx={{ fontSize: 11, color: 'text.secondary' }}>{data ? visibleRisks.length + ' công việc' : 'Chưa có dữ liệu'}</Typography></Box>
            {monitor.isLoading ? <Box sx={{ p: 3 }}><Skeleton height={60} /><Skeleton height={60} /><Skeleton height={60} /></Box>
              : !data ? <OpsEmptyState icon={<FactoryRounded />} title="Chưa tải được dữ liệu giám sát" description="Chọn làm mới để kiểm tra tiến độ và các công việc cần can thiệp." />
              : visibleRisks.length === 0 ? <OpsEmptyState icon={<CheckCircleOutlineRounded />} title={risks.length ? 'Không có công việc phù hợp' : 'Tiến độ đang trong tầm kiểm soát'} description={risks.length ? 'Chọn một mức độ rủi ro khác để xem danh sách.' : 'Chưa có công việc nào được ghi nhận có nguy cơ trễ hạn.'} />
              : <Box className="ops-risk-list">{visibleRisks.map((risk) => <Box key={risk.jobId} className="ops-risk-row">
                <span className={'ops-risk-indicator' + (risk.slackHours < 0 ? ' overdue' : '')} />
                <Box className="ops-risk-content"><Stack direction="row" alignItems="center" gap={0.75} flexWrap="wrap"><Typography sx={{ fontFamily: 'monospace', fontSize: 12, fontWeight: 700 }}>#{risk.jobId.slice(0, 8).toUpperCase()}</Typography><Chip size="small" color={risk.slackHours < 0 ? 'error' : 'warning'} label={risk.slackHours < 0 ? 'Quá hạn ' + Math.abs(risk.slackHours).toLocaleString('vi-VN') + ' giờ' : 'Còn ' + risk.slackHours.toLocaleString('vi-VN') + ' giờ'} /><Chip size="small" variant="outlined" label={(JOB_LABELS as Record<string, string>)[risk.status] ?? risk.status} /></Stack>
                  <Box className="ops-risk-details">{risk.riskReason}<br />Hạn nội bộ: {fmtDate(risk.internalDueDate)}</Box></Box>
                <Box className="ops-risk-actions"><Button size="small" onClick={() => navigate('/ops/decisions?jobId=' + risk.jobId)} aria-label={'Xem nhật ký ' + risk.jobId.slice(0, 8)}>Nhật ký</Button>{['Assigned', 'Accepted'].includes(risk.status) && <Button variant="outlined" size="small" onClick={() => open(risk)}>Gán lại</Button>}</Box>
              </Box>)}</Box>}
          </Paper>
          <Paper variant="outlined" className="ops-panel">
            <Box className="ops-panel-heading"><Box><div className="ops-panel-title"><AssignmentTurnedInRounded sx={{ fontSize: 18, color: '#8979AD' }} /><h2>Hàng đợi phân công</h2><Chip size="small" label={pending.data ? jobs.length : '—'} /></div><p>Phân bổ đơn in cho xưởng và máy có năng lực phù hợp</p></Box></Box>
            <Box className="ops-filter-bar"><TextField size="small" fullWidth placeholder="Tìm mã đơn, tên file hoặc vật liệu…" value={jobSearch} onChange={(event) => setJobSearch(event.target.value)} inputProps={{ 'aria-label': 'Tìm công việc chờ gán' }} InputProps={{ startAdornment: <InputAdornment position="start"><SearchRounded sx={{ fontSize: 18 }} /></InputAdornment> }} /></Box>
            {notice && <Alert severity="success" sx={{ m: 2 }} onClose={() => setNotice(null)}>{notice}</Alert>}
            {pending.isError && <Alert severity="error" sx={{ m: 2 }}>Không tải được danh sách công việc chờ gán.</Alert>}
            {assign.isError && <Alert severity="error" sx={{ m: 2 }}>{getApiErrorMessage(assign.error, 'Không thể gán công việc. Vui lòng thử lại.')}</Alert>}
            {repair.isError && <Alert severity="error" sx={{ m: 2 }}>{getApiErrorMessage(repair.error, 'Không thể xử lý nguy cơ trễ.')}</Alert>}
            {pending.isLoading ? <Box sx={{ p: 3 }}><Skeleton height={70} /><Skeleton height={70} /></Box>
              : pending.isError && !pending.data ? null
              : visibleJobs.length === 0 ? <OpsEmptyState icon={<AssignmentTurnedInRounded />} title={jobSearch ? 'Không tìm thấy công việc phù hợp' : 'Đã phân công hết công việc'} description={jobSearch ? 'Thử tìm bằng mã đơn, tên file hoặc loại vật liệu khác.' : 'Công việc từ đơn hàng mới sẽ xuất hiện tại đây khi cần phân công.'} />
              : visibleJobs.map((job) => <Box key={job.id} className="ops-pending-job">
                <Stack direction="row" justifyContent="space-between" alignItems="center" gap={1} flexWrap="wrap"><Typography sx={{ fontSize: 13, fontWeight: 700 }}>{job.orderNumber || '#' + job.id.slice(0, 8).toUpperCase()}</Typography><Chip size="small" color="warning" label={JOB_LABELS[job.status]} /></Stack>
                <Typography sx={{ mt: 1, fontSize: 12, fontWeight: 550, overflowWrap: 'anywhere' }}>{job.modelFileName || 'Mô hình in 3D'} · {job.materialCode} / {job.colorCode}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>{job.quantity} sản phẩm · {job.estimatedPrintMinutes.toLocaleString('vi-VN')} phút dự kiến · Lớp {job.layerHeightMm} mm</Typography>
                <Typography variant="body2" color="text.secondary">{job.boundingWidthMm} × {job.boundingDepthMm} × {job.boundingHeightMm} mm · {job.technology} · Dung sai ≤ {job.toleranceMm} mm</Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} gap={1.5} sx={{ mt: 2 }}>
                  <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>Hạn nội bộ: {fmtDate(job.internalDueDate)}</Typography>
                  <Stack direction="row" gap={1} flexWrap="wrap">{job.quantity > 1 && <Button size="small" variant="outlined" disabled={repair.isPending} onClick={() => repair.mutate(job.id)}>Xử lý nguy cơ trễ</Button>}
                    <Button size="small" variant="contained" startIcon={assign.isPending ? <CircularProgress size={14} color="inherit" /> : <RocketLaunchRounded sx={{ fontSize: 16 }} />} disabled={assign.isPending} onClick={() => assign.mutate(job.id)}>Gán cho xưởng</Button></Stack>
                </Stack>
              </Box>)}
          </Paper>
        </Stack>
        <Stack spacing={2.75} sx={{ minWidth: 0 }}>
          <Paper variant="outlined" className="ops-panel"><Box className="ops-panel-heading"><Box><div className="ops-panel-title"><FactoryRounded sx={{ fontSize: 18, color: '#8979AD' }} /><h2>Phân bổ tải xưởng</h2></div><p>Công việc đang mở trên mỗi máy in</p></Box><Button component={Link} to="/ops/labs" size="small">Quản lý</Button></Box>
            {monitor.isLoading ? <Box sx={{ p: 3 }}><Skeleton height={65} /><Skeleton height={65} /></Box> : !data ? <OpsEmptyState icon={<FactoryRounded />} title="Chưa có dữ liệu tải xưởng" description="Thử làm mới dữ liệu giám sát." /> : !data.labLoads.length ? <OpsEmptyState icon={<FactoryRounded />} title="Chưa có xưởng trong báo cáo" description="Kiểm tra danh sách xưởng và trạng thái tham gia mạng lưới." /> : data.labLoads.map((lab) => <Box key={lab.labId} className="ops-lab-load">
              <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1}><Typography sx={{ fontSize: 12, fontWeight: 650, minWidth: 0, overflowWrap: 'anywhere' }}>{lab.labName}</Typography><Chip size="small" label={lab.isActive && lab.isInGoodStanding ? 'Sẵn sàng' : 'Tạm ngưng'} color={lab.isActive && lab.isInGoodStanding ? 'success' : 'default'} /></Stack>
              <Stack direction="row" alignItems="center" justifyContent="space-between" gap={1} sx={{ mt: 1.5 }}><Typography sx={{ fontSize: 11, color: 'text.secondary' }}>{lab.activeJobs} công việc / {lab.machines} máy</Typography><Typography sx={{ fontSize: 12, fontWeight: 650, color: lab.jobsPerMachine >= 2 ? 'warning.main' : 'text.secondary' }}>{lab.jobsPerMachine.toLocaleString('vi-VN')} việc/máy</Typography></Stack>
            </Box>)}
          </Paper>
          <Paper variant="outlined" className="ops-panel"><Box className="ops-panel-heading"><Box><h2>Hàng đợi nghiệp vụ</h2><p>Tiếp tục các tác vụ vận hành thường dùng</p></Box></Box>
            <Link className="ops-shortcut" to="/quote-reviews"><ReceiptLongOutlined />Duyệt & phát hành báo giá<ChevronRightRounded /></Link>
            <Link className="ops-shortcut" to="/model-reviews"><ViewInArOutlined />Kiểm duyệt mô hình 3D<ChevronRightRounded /></Link>
            <Link className="ops-shortcut" to="/staff/qc-proofs"><FactCheckOutlined />Kiểm tra bằng chứng QC<ChevronRightRounded /></Link>
            <Link className="ops-shortcut" to="/ops/sla"><InsightsOutlined />Báo cáo hiệu suất & SLA<ChevronRightRounded /></Link>
          </Paper>
        </Stack>
      </Box>
    </Stack>
    <Dialog open={Boolean(target)} onClose={close} fullWidth maxWidth="sm"><DialogTitle>{step === 1 ? 'Xem phương án gán lại' : 'Xác nhận điều chỉnh ngày giao'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ mt: 1 }}>
      {preview.isLoading && <CircularProgress />}{preview.isError && <Alert severity="error">Không thể tạo preview cho job này.</Alert>}
      {preview.data && <Alert severity={preview.data.requiresCustomerApproval ? 'warning' : 'success'}>{preview.data.message}{preview.data.proposedCustomerDate ? ` Ngày đề xuất: ${fmtDate(preview.data.proposedCustomerDate)}.` : ''}</Alert>}
      {step === 2 && <Alert severity="warning">Xác nhận này chỉ tạo yêu cầu đổi ngày cho khách. Hệ thống giữ nguyên lịch hiện tại cho đến khi khách duyệt.</Alert>}
      <TextField label="Lý do can thiệp" value={reason} onChange={e => setReason(e.target.value)} multiline minRows={3} required helperText="Tối thiểu 10 ký tự. Lý do được lưu vào nhật ký quyết định." />
      {step === 2 && <TextField
        select
        label="Lý do điều chỉnh ngày giao"
        value={dateChangeReason}
        onChange={e => setDateChangeReason(e.target.value as DateChangeReason)}
        required
        helperText="Chọn lý do phù hợp để gửi yêu cầu điều chỉnh ngày giao đến khách hàng."
      >
        <MenuItem value="CustomerRequested">Khách xin dời</MenuItem>
        <MenuItem value="ForceMajeure">Bất khả kháng</MenuItem>
        <MenuItem value="ReprintDeadlineUnrecoverable">In lại ≥ 2 lần không kịp lịch</MenuItem>
      </TextField>}
      {reassign.isError && <Alert severity="error">{getApiErrorMessage(reassign.error, 'Không thể gán lại công việc.')}</Alert>}
      {reassign.isSuccess && <Alert severity="success">{reassign.data.message}</Alert>}
    </Stack></DialogContent><DialogActions><Button onClick={close}>Đóng</Button>{!reassign.isSuccess && <Button variant="contained" color={preview.data?.requiresCustomerApproval ? 'warning' : 'primary'} disabled={reason.trim().length < 10 || !preview.data || preview.isLoading || reassign.isPending || (step === 2 && !dateChangeReason)} onClick={confirm}>{reassign.isPending ? 'Đang xử lý…' : preview.data?.requiresCustomerApproval && step === 1 ? 'Tiếp tục bước 2' : 'Xác nhận can thiệp'}</Button>}</DialogActions></Dialog>
  </Box>
}
