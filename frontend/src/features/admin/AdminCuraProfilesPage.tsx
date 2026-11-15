import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, Chip, CircularProgress, Grid, Paper, Stack, TextField, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useLabs } from '../ops/useLabs'
import { PROFILE_STATUS_LABELS } from '../ops/labTypes'
import { useCuraProfiles } from '../lab/PrinterConfigurationFields'

const empty = { name: '', manufacturer: '', machineModel: '', materialCode: 'PLA', version: '1', engineVersion: '5.11.0', extruderCount: 1, settingsJson: '' }

export default function AdminCuraProfilesPage() {
  const profiles = useCuraProfiles()
  const labs = useLabs(true)
  const client = useQueryClient()
  const [form, setForm] = useState(empty)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [reasons, setReasons] = useState<Record<string, string>>({})
  const refresh = () => { client.invalidateQueries({ queryKey: ['cura-profiles'] }); client.invalidateQueries({ queryKey: ['labs'] }) }
  const action = useMutation({
    mutationFn: async ({ id, kind, approve }: { id: string; kind: 'match' | 'review'; approve?: boolean }) => {
      await apiClient.post(`/cura-profiles/machines/${id}/${kind}`, kind === 'review' ? { approve, reason: reasons[id] || null } : {})
    },
    onSuccess: () => { setError(''); setMessage('Đã cập nhật hồ sơ máy.'); refresh() },
    onError: e => setError(getApiErrorMessage(e, 'Không cập nhật được hồ sơ máy.')),
  })
  const save = useMutation({
    mutationFn: () => apiClient.post('/cura-profiles', form),
    onSuccess: () => { setError(''); setMessage('Đã lưu profile và tự tìm lại cho các máy thiếu profile hoặc chờ duyệt.'); setForm(empty); refresh() },
    onError: e => setError(getApiErrorMessage(e, 'Không nhập được profile.')),
  })
  async function readFile(file?: File) {
    if (!file) return
    if (file.size > 2 * 1024 * 1024) { setError('File profile tối đa 2 MB.'); return }
    try { const json = await file.text(); JSON.parse(json); setForm(f => ({ ...f, settingsJson: json })); setError('') }
    catch { setError('File JSON không hợp lệ.') }
  }
  async function copyProfile(id: string) {
    try {
      const { data } = await apiClient.get(`/cura-profiles/${id}`)
      setForm({ name: data.name, manufacturer: data.manufacturer, machineModel: data.machineModel,
        materialCode: data.materialCode, version: `${data.version}-new`, engineVersion: data.engineVersion,
        extruderCount: data.extruderCount, settingsJson: data.settingsJson })
      setMessage('Đã mở cấu hình. Sửa thông số và lưu thành một phiên bản mới.'); setError('')
    } catch (e) { setError(getApiErrorMessage(e, 'Không tải được profile.')) }
  }
  return <Stack spacing={3}>
    <Box><Typography variant="h1">Profile CuraEngine & duyệt máy</Typography>
      <Typography color="text.secondary">Quản lý thư viện cấu hình in và cho phép máy tham gia báo giá.</Typography></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Stack spacing={2}>
        <Typography variant="h6">Tạo / nhập profile</Typography>
        <Typography variant="body2" color="text.secondary">Mỗi profile dành cho một model máy, vật liệu và cấu hình đầu phun. Nhập resolved JSON gồm global và extruder.0, extruder.1… Các giới hạn khổ in, đường kính, số tool màu và nhiệt độ được đọc từ file. Mỗi lần lưu tạo một phiên bản độc lập.</Typography>
        <Grid container spacing={2}>
          {([['name', 'Tên profile'], ['manufacturer', 'Hãng sản xuất'], ['machineModel', 'Model máy'],
            ['materialCode', 'Mã vật liệu'], ['version', 'Phiên bản profile'], ['engineVersion', 'Phiên bản CuraEngine']] as const)
            .map(([key, label]) => <Grid key={key} size={{ xs: 12, sm: 6 }}><TextField fullWidth required label={label}
              value={form[key]} onChange={e => setForm(f => ({ ...f, [key]: e.target.value }))} /></Grid>)}
          <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required type="number" label="Số đầu đùn vật lý" value={form.extruderCount}
            onChange={e => setForm(f => ({ ...f, extruderCount: Number(e.target.value) }))} /></Grid>
        </Grid>
        <Button component="label" variant="outlined">Chọn file resolved JSON<input hidden type="file" accept=".json,application/json"
          onChange={e => { void readFile(e.target.files?.[0]); e.target.value = '' }} /></Button>
        <TextField multiline minRows={5} maxRows={14} label="Nội dung profile JSON" value={form.settingsJson}
          onChange={e => setForm(f => ({ ...f, settingsJson: e.target.value }))} />
        <Button variant="contained" disabled={save.isPending || !form.settingsJson || !form.name || !form.manufacturer || !form.machineModel}
          onClick={() => save.mutate()}>{save.isPending ? 'Đang nhập…' : 'Lưu vào thư viện & tìm profile cho máy'}</Button>
      </Stack>
    </Paper>
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Typography variant="h6" mb={2}>Thư viện profile</Typography>
      {profiles.isLoading && <CircularProgress />}
      {profiles.isError && <Alert severity="error">Không tải được thư viện profile.</Alert>}
      {!profiles.isLoading && !profiles.isError && !profiles.data?.length && <Typography color="text.secondary">Chưa có profile. Nhập profile để máy đăng ký mới được tự đối chiếu.</Typography>}
      <Stack spacing={1}>{profiles.data?.map(p => <Box key={p.id} sx={{ borderBottom: 1, borderColor: 'divider', pb: 1 }}>
        <Typography fontWeight={700}>{p.name} · v{p.version}</Typography>
        <Typography variant="body2">{p.manufacturer} · {p.machineModel} · {p.materialCode} · Đầu phun {p.nozzleDiameterMm} mm · {p.extruderCount} đầu đùn · {p.maximumColorSlots} màu · {p.widthMm}×{p.depthMm}×{p.heightMm} mm</Typography>
        <Button size="small" onClick={() => void copyProfile(p.id)}>Xem / tạo phiên bản từ profile này</Button>
      </Box>)}</Stack>
    </Paper>
    <Typography variant="h6">Hồ sơ máy của các xưởng</Typography>
    {labs.isLoading && <CircularProgress />}
    {labs.isError && <Alert severity="error">Không tải được hồ sơ máy.</Alert>}
    {labs.data?.flatMap(lab => lab.machines.map(m => <Paper variant="outlined" key={m.id} sx={{ p: 2.5 }}>
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap"><Typography fontWeight={700}>{lab.name} · {m.name}</Typography>
          <Chip size="small" color={m.profileStatus === 'Approved' ? 'success' : 'warning'} label={PROFILE_STATUS_LABELS[m.profileStatus] ?? 'Chưa có profile'} /></Stack>
        <Typography variant="body2">{m.printerConfiguration?.manufacturer ?? 'Chưa khai hãng'} · {m.model} · {m.buildWidthMm}×{m.buildDepthMm}×{m.buildHeightMm} mm · {m.supportedMaterials.join(', ')}</Typography>
        {m.printerConfiguration && <Typography variant="body2">Đầu phun {m.printerConfiguration.nozzleDiameterMm} mm · Sợi {m.printerConfiguration.filamentDiameterMm} mm · {m.printerConfiguration.extruderCount} đầu đùn · {m.printerConfiguration.maximumColorSlots} màu · Giới hạn nhiệt đầu phun/bàn: {m.printerConfiguration.maxNozzleTemperatureC}/{m.printerConfiguration.maxBedTemperatureC} °C · {m.printerConfiguration.pricePerHour.toLocaleString('vi-VN')} đ/giờ · Lỗi {Number((m.printerConfiguration.failureRate * 100).toFixed(2))}%</Typography>}
        <Typography variant="body2">Profile đã gắn: {m.curaProfileIds?.map(id => profiles.data?.find(p => p.id === id)?.name ?? id).join(', ') || 'Chưa có'}</Typography>
        {m.profileReviewReason && <Alert severity="warning">{m.profileReviewReason}</Alert>}
        {!m.printerConfiguration && <Alert severity="info">Lab cần mở form sửa máy và bổ sung cấu hình đầu in, hãng và đơn giá.</Alert>}
        <TextField size="small" label="Lý do từ chối" value={reasons[m.id] ?? ''} onChange={e => setReasons(r => ({ ...r, [m.id]: e.target.value }))} />
        <Stack direction="row" spacing={1} flexWrap="wrap">
          <Button disabled={action.isPending || !m.printerConfiguration} onClick={() => action.mutate({ id: m.id, kind: 'match' })}>Tìm lại profile</Button>
          <Button variant="contained" disabled={action.isPending || m.profileStatus !== 'PendingReview'} onClick={() => action.mutate({ id: m.id, kind: 'review', approve: true })}>Duyệt báo giá</Button>
          <Button color="error" disabled={action.isPending || !reasons[m.id]?.trim()} onClick={() => action.mutate({ id: m.id, kind: 'review', approve: false })}>Từ chối / thu hồi duyệt</Button>
        </Stack>
      </Stack>
    </Paper>))}
  </Stack>
}
