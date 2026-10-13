import { useMemo, useState } from 'react'
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Grid, Paper, Stack, TextField, Typography } from '@mui/material'
import { AddRounded, CheckCircleRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { type CommercialParameters, useActivateCommercialConfiguration, useCatalogItems, useCommercialConfigurations, useCreateCommercialConfiguration } from './useAdminConfiguration'

const defaults: CommercialParameters = {
  materialRates: { PLA: 470, PETG: 575, ABS: 520, TPU: 680, RESIN: 1550 },
  materialDensities: { PLA: 1.24, PETG: 1.27, ABS: 1.04, TPU: 1.21, RESIN: 1.1 },
  pricePerPrintHour: 94500, hubHandlingFee: 25000, platformMarginPercent: 12,
  prioritySurchargePercent: 20, autoLaneThreshold: 300000, quoteValidityHours: 48,
  assignmentWeights: { time: 0.3, load: 0.2, quality: 0.2, cost: 0.15, logistics: 0.15 },
  maxBatchSize: 100, schedulingTimeBudgetSeconds: 30, slaRiskThresholdHours: 24,
}

export default function AdminCommercialConfigurationPage() {
  const query = useCommercialConfigurations(); const create = useCreateCommercialConfiguration(); const activate = useActivateCommercialConfiguration()
  const catalog = useCatalogItems()
  const [open, setOpen] = useState(false); const [name, setName] = useState(''); const [summary, setSummary] = useState('')
  const active = query.data?.find(x => x.status === 'Active')
  const [params, setParams] = useState<CommercialParameters>(defaults)
  const [error, setError] = useState('')
  const materials = useMemo(() => Object.keys(params.materialRates), [params.materialRates])
  const startCreate = () => {
    const next = active ? structuredClone(active.parameters) : structuredClone(defaults)
    for (const material of (catalog.data ?? []).filter(x => x.catalogType === 'Material' && !x.isDeprecated)) {
      next.materialRates[material.code] = material.unitRate ?? next.materialRates[material.code] ?? 1
      const density = typeof material.properties.density === 'number' ? material.properties.density : undefined
      next.materialDensities[material.code] = density ?? next.materialDensities[material.code] ?? 1
    }
    const activeCodes = new Set((catalog.data ?? []).filter(x => x.catalogType === 'Material' && !x.isDeprecated).map(x => x.code))
    if (activeCodes.size) { for (const code of Object.keys(next.materialRates)) if (!activeCodes.has(code)) { delete next.materialRates[code]; delete next.materialDensities[code] } }
    setName(''); setSummary(''); setParams(next); setError(''); setOpen(true)
  }
  const number = (key: keyof CommercialParameters, value: string) => setParams(p => ({ ...p, [key]: Number(value) }))
  const save = async () => { try { await create.mutateAsync({ name, changeSummary: summary, parameters: params }); setOpen(false) } catch (e) { setError(getApiErrorMessage(e, 'Không thể tạo phiên bản.')) } }
  const doActivate = async (id: string) => { setError(''); try { await activate.mutateAsync(id) } catch (e) { setError(getApiErrorMessage(e, 'Không thể kích hoạt phiên bản.')) } }

  return <Box sx={{ maxWidth: 1180, mx: 'auto', px: 3, py: 4 }}>
    <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
      <Stack direction="row" spacing={1.5} alignItems="center"><PageBackButton /><Box><Typography variant="h4" fontWeight={800}>Cấu hình thương mại</Typography><Typography color="text.secondary">Mỗi lần thay đổi tạo một phiên bản mới; chỉ phiên bản đang hoạt động áp dụng cho báo giá mới.</Typography></Box></Stack>
      <Button variant="contained" startIcon={<AddRounded />} onClick={startCreate}>Tạo phiên bản</Button>
    </Stack>
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    {query.isLoading ? <CircularProgress /> : <Stack spacing={2}>{query.data?.map(item => <Paper key={item.id} variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}>
        <Box><Stack direction="row" spacing={1} alignItems="center"><Typography variant="h6" fontWeight={750}>v{item.versionNumber} · {item.name}</Typography><Chip size="small" color={item.status === 'Active' ? 'success' : item.status === 'Draft' ? 'warning' : 'default'} label={item.status === 'Active' ? 'Đang hoạt động' : item.status === 'Draft' ? 'Bản nháp' : 'Đã lưu trữ'} /></Stack><Typography color="text.secondary" mt={0.5}>{item.changeSummary}</Typography><Typography variant="caption" color="text.secondary">Tạo lúc {new Date(item.createdAtUtc).toLocaleString('vi-VN')}</Typography></Box>
        {item.status === 'Draft' && <Button variant="outlined" startIcon={<CheckCircleRounded />} disabled={activate.isPending} onClick={() => doActivate(item.id)}>Kích hoạt</Button>}
      </Stack>
      <Grid container spacing={2} mt={1}>{[['Giá giờ máy', item.parameters.pricePerPrintHour], ['Phí xử lý hub', item.parameters.hubHandlingFee], ['Biên nền tảng', `${item.parameters.platformMarginPercent}%`], ['Hiệu lực báo giá', `${item.parameters.quoteValidityHours} giờ`], ['Kích thước lô tối đa', item.parameters.maxBatchSize], ['Ngưỡng rủi ro SLA', `${item.parameters.slaRiskThresholdHours} giờ`]].map(([label, value]) => <Grid key={String(label)} size={{ xs: 6, md: 2 }}><Typography variant="caption" color="text.secondary">{label}</Typography><Typography fontWeight={700}>{typeof value === 'number' ? value.toLocaleString('vi-VN') : value}</Typography></Grid>)}</Grid>
    </Paper>)}</Stack>}

    <Dialog open={open} onClose={() => setOpen(false)} maxWidth="md" fullWidth><DialogTitle>Tạo phiên bản cấu hình mới</DialogTitle><DialogContent><Stack spacing={2} mt={1}>
      {error && <Alert severity="error">{error}</Alert>}<TextField label="Tên phiên bản" value={name} onChange={e => setName(e.target.value)} required /><TextField label="Mô tả thay đổi" value={summary} onChange={e => setSummary(e.target.value)} multiline minRows={2} required />
      <Typography fontWeight={750}>Thông số giá</Typography><Grid container spacing={2}><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Giá mỗi giờ máy" value={params.pricePerPrintHour} onChange={e => number('pricePerPrintHour', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Phí xử lý hub" value={params.hubHandlingFee} onChange={e => number('hubHandlingFee', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Biên nền tảng (%)" value={params.platformMarginPercent} onChange={e => number('platformMarginPercent', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Phụ phí ưu tiên (%)" value={params.prioritySurchargePercent} onChange={e => number('prioritySurchargePercent', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Ngưỡng tự duyệt" value={params.autoLaneThreshold} onChange={e => number('autoLaneThreshold', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Hiệu lực báo giá (giờ)" value={params.quoteValidityHours} onChange={e => number('quoteValidityHours', e.target.value)} /></Grid></Grid>
      <Typography fontWeight={750}>Đơn giá vật liệu / gram</Typography><Grid container spacing={2}>{materials.map(code => <Grid key={code} size={{ xs: 6, md: 4 }}><TextField fullWidth type="number" label={code} value={params.materialRates[code]} onChange={e => setParams(p => ({ ...p, materialRates: { ...p.materialRates, [code]: Number(e.target.value) } }))} /></Grid>)}</Grid>
      <Typography fontWeight={750}>Giới hạn vận hành</Typography><Grid container spacing={2}><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Kích thước lô tối đa" value={params.maxBatchSize} onChange={e => number('maxBatchSize', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Ngân sách lập lịch (giây)" value={params.schedulingTimeBudgetSeconds} onChange={e => number('schedulingTimeBudgetSeconds', e.target.value)} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth type="number" label="Ngưỡng rủi ro SLA (giờ)" value={params.slaRiskThresholdHours} onChange={e => number('slaRiskThresholdHours', e.target.value)} /></Grid></Grid>
    </Stack></DialogContent><DialogActions><Button onClick={() => setOpen(false)}>Hủy</Button><Button variant="contained" disabled={!name.trim() || !summary.trim() || create.isPending} onClick={save}>Lưu bản nháp</Button></DialogActions></Dialog>
  </Box>
}
