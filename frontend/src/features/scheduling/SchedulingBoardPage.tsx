import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Divider, Paper, Stack, TextField, Typography } from '@mui/material'
import { AssignmentTurnedInRounded, FactoryRounded, HistoryRounded, RefreshRounded, WarningAmberRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { useOpsMonitor, useOpsReassign, useOpsReassignPreview, type OpsRiskJob } from '../ops/useOps'

const fmtDate = (value?: string | null) => value ? new Date(value.includes('T') ? value : `${value}T00:00:00`).toLocaleString('vi-VN') : '—'

export default function SchedulingBoardPage() {
  const navigate = useNavigate()
  const monitor = useOpsMonitor()
  const reassign = useOpsReassign()
  const [target, setTarget] = useState<OpsRiskJob | null>(null)
  const [reason, setReason] = useState('')
  const [step, setStep] = useState<1 | 2>(1)
  const preview = useOpsReassignPreview(target?.jobId ?? null)
  const close = () => { setTarget(null); setReason(''); setStep(1); reassign.reset() }
  const open = (risk: OpsRiskJob) => { setTarget(risk); setReason(''); setStep(1) }
  async function confirm() {
    if (!target) return
    const impact = Boolean(preview.data?.requiresCustomerApproval)
    if (impact && step === 1) { setStep(2); return }
    await reassign.mutateAsync({ jobId: target.jobId, reason: reason.trim(), confirmCustomerDateImpact: impact && step === 2 })
  }

  const data = monitor.data
  return <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', p: { xs: 2.5, md: 4 } }}>
    <Stack spacing={3} sx={{ maxWidth: 1180, mx: 'auto' }}>
      <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}>
        <Stack direction="row" spacing={1.5}><PageBackButton /><Box><Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Bảng vận hành & can thiệp</Typography><Typography color="text.secondary">Đơn mở · nguy cơ trễ · tải xưởng · cập nhật mỗi 30 giây</Typography></Box></Stack>
        <Button startIcon={<RefreshRounded />} onClick={() => monitor.refetch()} disabled={monitor.isFetching}>Làm mới</Button>
      </Stack>
      {monitor.isError && <Alert severity="error">Không tải được dữ liệu vận hành.</Alert>}
      {monitor.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
      {data && <>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
          {[['Đơn/job đang mở', data.openJobs, 'primary'], ['Job nguy hiểm trễ', data.atRiskJobs, 'error'], ['Dự báo đúng hạn', `${data.onTimeForecastPercent}%`, 'success']].map(([label, value, color]) => <Paper key={String(label)} variant="outlined" sx={{ p: 2.5, flex: 1 }}><Typography color="text.secondary">{label}</Typography><Typography color={`${color}.main`} sx={{ fontSize: '2rem', fontWeight: 800 }}>{value}</Typography></Paper>)}
        </Stack>
        <Paper variant="outlined" sx={{ overflow: 'hidden' }}><Box sx={{ p: 2.5 }}><Typography variant="h6" fontWeight={750}>Nguy cơ trễ — slack &lt; {data.atRiskSlackHours} giờ</Typography><Typography variant="body2" color="text.secondary">Sắp xếp từ rủi ro cao nhất; phát hiện trong chu kỳ monitor tối đa 30 giây.</Typography></Box><Divider />
          {data.risks.length === 0 ? <Box sx={{ p: 5, textAlign: 'center' }}><Typography color="success.main">Không có job nguy hiểm trễ.</Typography></Box> : <Stack divider={<Divider />}>{data.risks.map(risk => <Stack key={risk.jobId} direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} gap={2} sx={{ p: 2.5, bgcolor: risk.slackHours < 0 ? 'rgba(244,67,54,.08)' : undefined }}><Box><Stack direction="row" spacing={1} alignItems="center"><WarningAmberRounded color="error" /><Typography fontFamily="monospace" fontWeight={700}>{risk.jobId.slice(0, 8).toUpperCase()}</Typography><Chip size="small" color="error" label={`${risk.slackHours}h slack`} /><Chip size="small" label={risk.status} /></Stack><Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>{risk.riskReason} · hạn {fmtDate(risk.internalDueDate)}</Typography></Box><Stack direction="row" spacing={1}><Button size="small" startIcon={<HistoryRounded />} onClick={() => navigate(`/ops/decisions?jobId=${risk.jobId}`)}>Audit</Button>{['Assigned', 'Accepted'].includes(risk.status) && <Button variant="contained" color="warning" startIcon={<AssignmentTurnedInRounded />} onClick={() => open(risk)}>Gán lại</Button>}</Stack></Stack>)}</Stack>}
        </Paper>
        <Paper variant="outlined" sx={{ p: 2.5 }}><Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}><FactoryRounded /><Typography variant="h6" fontWeight={750}>Tải xưởng</Typography></Stack><Stack spacing={1.25}>{data.labLoads.map(lab => <Stack key={lab.labId} direction="row" justifyContent="space-between" alignItems="center" sx={{ p: 1.5, borderRadius: 2, bgcolor: 'action.hover' }}><Box><Typography fontWeight={650}>{lab.labName}</Typography><Typography variant="body2" color="text.secondary">{lab.activeJobs} job / {lab.machines} máy</Typography></Box><Stack direction="row" spacing={1}><Chip label={`${lab.jobsPerMachine} job/máy`} color={lab.jobsPerMachine >= 2 ? 'warning' : 'default'} /><Chip label={lab.isActive && lab.isInGoodStanding ? 'Sẵn sàng' : 'Tạm ngưng'} color={lab.isActive && lab.isInGoodStanding ? 'success' : 'error'} /></Stack></Stack>)}</Stack></Paper>
      </>}
    </Stack>
    <Dialog open={Boolean(target)} onClose={close} fullWidth maxWidth="sm"><DialogTitle>{step === 1 ? 'Preview gán lại qua capability filter' : 'Bước 2 — xác nhận ảnh hưởng ngày khách'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ mt: 1 }}>
      {preview.isLoading && <CircularProgress />}{preview.isError && <Alert severity="error">Không thể tạo preview cho job này.</Alert>}
      {preview.data && <Alert severity={preview.data.requiresCustomerApproval ? 'warning' : 'success'}>{preview.data.message}{preview.data.proposedCustomerDate ? ` Ngày đề xuất: ${fmtDate(preview.data.proposedCustomerDate)}.` : ''}</Alert>}
      {step === 2 && <Alert severity="warning">Xác nhận này chỉ tạo yêu cầu đổi ngày cho khách. Hệ thống giữ nguyên lịch hiện tại cho đến khi khách duyệt.</Alert>}
      <TextField label="Lý do can thiệp" value={reason} onChange={e => setReason(e.target.value)} multiline minRows={3} required helperText="Tối thiểu 10 ký tự; lý do được ghi bất biến vào audit log." />
      {reassign.isError && <Alert severity="error">{(reassign.error as any)?.response?.data?.error?.message ?? 'Không thể gán lại job.'}</Alert>}
      {reassign.isSuccess && <Alert severity="success">{reassign.data.message}</Alert>}
    </Stack></DialogContent><DialogActions><Button onClick={close}>Đóng</Button>{!reassign.isSuccess && <Button variant="contained" color={preview.data?.requiresCustomerApproval ? 'warning' : 'primary'} disabled={reason.trim().length < 10 || preview.isLoading || reassign.isPending} onClick={confirm}>{reassign.isPending ? 'Đang xử lý…' : preview.data?.requiresCustomerApproval && step === 1 ? 'Tiếp tục bước 2' : 'Xác nhận can thiệp'}</Button>}</DialogActions></Dialog>
  </Box>
}
