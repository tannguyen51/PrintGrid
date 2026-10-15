import { useMemo, useState } from 'react'
import { Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, IconButton, MenuItem, Paper, Stack, Switch, Tab, Tabs, TextField, Tooltip, Typography } from '@mui/material'
import { AddRounded, DeleteOutlineRounded, EditRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { StaffNavbar } from '../home/components/StaffNavbar'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { type CatalogItem, useCatalogItems, useDeleteCatalogItem, useSaveCatalogItem } from './useAdminConfiguration'

const types = [
  ['Material', 'Vật liệu'], ['Color', 'Màu sắc'], ['Technology', 'Công nghệ'],
  ['QualityGrade', 'Cấp chất lượng'], ['PostProcessing', 'Hậu xử lý'],
] as const

type Form = {
  code: string; name: string; description: string; unitRate: string
  isPremium: boolean; isDeprecated: boolean
  density: string; heatResistanceC: string; hex: string
  minLayerHeightMm: string; inspectionLevel: string; leadTimeHours: string
  baseProperties: Record<string, unknown>
}
const empty: Form = {
  code: '', name: '', description: '', unitRate: '', isPremium: false, isDeprecated: false,
  density: '', heatResistanceC: '', hex: '#808080', minLayerHeightMm: '',
  inspectionLevel: 'standard', leadTimeHours: '', baseProperties: {},
}

export default function AdminSharedCatalogPage() {
  const query = useCatalogItems(); const saveMutation = useSaveCatalogItem(); const deleteMutation = useDeleteCatalogItem()
  const [type, setType] = useState<string>('Material'); const [form, setForm] = useState<Form>(empty); const [editing, setEditing] = useState<CatalogItem | null>(null); const [open, setOpen] = useState(false); const [error, setError] = useState('')
  const items = useMemo(() => (query.data ?? []).filter(x => x.catalogType === type), [query.data, type])
  const showCreate = () => { setEditing(null); setForm({ ...empty, baseProperties: {} }); setError(''); setOpen(true) }
  const showEdit = (item: CatalogItem) => {
    const text = (key: string) => item.properties[key] == null ? '' : String(item.properties[key])
    setEditing(item)
    setForm({
      code: item.code, name: item.name, description: item.description ?? '', unitRate: item.unitRate?.toString() ?? '',
      isPremium: item.isPremium, isDeprecated: item.isDeprecated,
      density: text('density'), heatResistanceC: text('heatResistanceC'), hex: text('hex') || '#808080',
      minLayerHeightMm: text('minLayerHeightMm'), inspectionLevel: text('inspectionLevel') || 'standard',
      leadTimeHours: text('leadTimeHours'), baseProperties: { ...item.properties },
    })
    setError(''); setOpen(true)
  }
  const save = async () => {
    setError('')
    if (type === 'Material' && (Number(form.density) <= 0 || Number(form.unitRate) <= 0)) { setError('Vật liệu cần đơn giá và khối lượng riêng lớn hơn 0.'); return }
    if (type === 'Color' && !/^#[0-9A-F]{6}$/i.test(form.hex)) { setError('Mã màu phải có dạng #RRGGBB.'); return }
    if (type === 'Technology' && Number(form.minLayerHeightMm) <= 0) { setError('Chiều cao lớp nhỏ nhất phải lớn hơn 0.'); return }
    if (type === 'PostProcessing' && Number(form.leadTimeHours) < 0) { setError('Thời gian xử lý không được âm.'); return }
    const properties: Record<string, unknown> = { ...form.baseProperties }
    const replace = (key: string, value: string, numeric = true) => { if (value === '') delete properties[key]; else properties[key] = numeric ? Number(value) : value }
    if (type === 'Material') { replace('density', form.density); replace('heatResistanceC', form.heatResistanceC) }
    if (type === 'Color') replace('hex', form.hex, false)
    if (type === 'Technology') replace('minLayerHeightMm', form.minLayerHeightMm)
    if (type === 'QualityGrade') replace('inspectionLevel', form.inspectionLevel, false)
    if (type === 'PostProcessing') replace('leadTimeHours', form.leadTimeHours)
    try {
      await saveMutation.mutateAsync({ type, code: form.code, name: form.name, description: form.description || undefined, unitRate: form.unitRate === '' ? undefined : Number(form.unitRate), isPremium: form.isPremium, isDeprecated: form.isDeprecated, properties })
      setOpen(false)
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể lưu mục danh mục.')) }
  }
  const remove = async (item: CatalogItem) => { if (!confirm(`Xóa “${item.name}”? Mục đã được sử dụng sẽ bị chặn.`)) return; setError(''); try { await deleteMutation.mutateAsync({ type: item.catalogType, code: item.code }) } catch (e) { setError(getApiErrorMessage(e, 'Không thể xóa. Hãy ngừng sử dụng mục này thay vì xóa.')) } }

  return <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
    <StaffNavbar />
    <Box sx={{ maxWidth: 1180, mx: 'auto', px: 3, py: 4 }}>
    <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}><Stack direction="row" spacing={1.5} alignItems="center"><PageBackButton /><Box><Typography variant="h4" fontWeight={800}>Danh mục dùng chung</Typography><Typography color="text.secondary">Dữ liệu chuẩn dùng xuyên suốt báo giá, sản xuất và kiểm tra chất lượng.</Typography></Box></Stack><Button variant="contained" startIcon={<AddRounded />} onClick={showCreate}>Thêm mục</Button></Stack>
    {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
    <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}><Tabs value={type} onChange={(_, value) => setType(value)} variant="scrollable" scrollButtons="auto" sx={{ borderBottom: 1, borderColor: 'divider', px: 1 }}>{types.map(([value, label]) => <Tab key={value} value={value} label={label} />)}</Tabs>
      {query.isLoading ? <Box p={4} textAlign="center"><CircularProgress /></Box> : items.length === 0 ? <Box p={5} textAlign="center"><Typography color="text.secondary">Chưa có dữ liệu trong danh mục này.</Typography></Box> : <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>{items.map(item => <Stack key={item.id} direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} gap={2} px={2.5} py={2} sx={{ opacity: item.isDeprecated ? 0.6 : 1 }}><Box flex={1}><Stack direction="row" spacing={1} alignItems="center"><Typography fontWeight={750}>{item.name}</Typography><Chip size="small" label={item.code} variant="outlined" /><Chip size="small" label={`r${item.revision}`} />{item.isPremium && <Chip size="small" color="secondary" label="Cao cấp" />}{item.isDeprecated && <Chip size="small" color="warning" label="Ngừng sử dụng" />}</Stack>{item.description && <Typography variant="body2" color="text.secondary" mt={0.5}>{item.description}</Typography>}</Box><Stack direction="row" alignItems="center">{item.unitRate != null && <Typography fontWeight={700} mr={2}>{item.unitRate.toLocaleString('vi-VN')} đ</Typography>}<Tooltip title="Chỉnh sửa tạo revision mới"><IconButton onClick={() => showEdit(item)}><EditRounded /></IconButton></Tooltip><Tooltip title="Chỉ xóa được mục chưa từng sử dụng"><IconButton color="error" onClick={() => remove(item)}><DeleteOutlineRounded /></IconButton></Tooltip></Stack></Stack>)}</Stack>}
    </Paper>

    <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth><DialogTitle>{editing ? `Cập nhật ${editing.code}` : 'Thêm mục danh mục'}</DialogTitle><DialogContent><Stack spacing={2} mt={1}>
      {error && <Alert severity="error">{error}</Alert>}
      <TextField select label="Loại danh mục" value={type} onChange={e => { setType(e.target.value); setForm(f => ({ ...empty, code: f.code, name: f.name, description: f.description, baseProperties: {} })) }} disabled={!!editing}>{types.map(([value, label]) => <MenuItem key={value} value={value}>{label}</MenuItem>)}</TextField>
      <TextField label="Mã" value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value.toUpperCase() }))} disabled={!!editing} required helperText="Chỉ dùng chữ, số, gạch ngang hoặc gạch dưới" />
      <TextField label="Tên hiển thị" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} required />
      <TextField label="Mô tả" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} multiline minRows={2} />

      {type === 'Material' && <>
        <TextField type="number" label="Đơn giá mỗi gram (VNĐ)" value={form.unitRate} onChange={e => setForm(f => ({ ...f, unitRate: e.target.value }))} required helperText="Sau khi đổi giá, hãy tạo phiên bản cấu hình thương mại mới để áp dụng cho báo giá." />
        <TextField type="number" label="Khối lượng riêng (g/cm³)" value={form.density} onChange={e => setForm(f => ({ ...f, density: e.target.value }))} required inputProps={{ step: 0.01, min: 0.01 }} />
        <TextField type="number" label="Nhiệt độ chịu nhiệt (°C)" value={form.heatResistanceC} onChange={e => setForm(f => ({ ...f, heatResistanceC: e.target.value }))} inputProps={{ min: 0 }} />
      </>}
      {type === 'Color' && <Stack direction="row" spacing={2} alignItems="center"><TextField type="color" label="Chọn màu" value={form.hex} onChange={e => setForm(f => ({ ...f, hex: e.target.value.toUpperCase() }))} sx={{ width: 120 }} InputLabelProps={{ shrink: true }} /><TextField fullWidth label="Mã màu HEX" value={form.hex} onChange={e => setForm(f => ({ ...f, hex: e.target.value.toUpperCase() }))} placeholder="#2563EB" /></Stack>}
      {type === 'Technology' && <TextField type="number" label="Chiều cao lớp nhỏ nhất (mm)" value={form.minLayerHeightMm} onChange={e => setForm(f => ({ ...f, minLayerHeightMm: e.target.value }))} required inputProps={{ step: 0.01, min: 0.01 }} />}
      {type === 'QualityGrade' && <TextField select label="Mức kiểm tra" value={form.inspectionLevel} onChange={e => setForm(f => ({ ...f, inspectionLevel: e.target.value }))}><MenuItem value="basic">Cơ bản</MenuItem><MenuItem value="standard">Tiêu chuẩn</MenuItem><MenuItem value="strict">Nghiêm ngặt</MenuItem></TextField>}
      {type === 'PostProcessing' && <><TextField type="number" label="Giá dịch vụ (VNĐ)" value={form.unitRate} onChange={e => setForm(f => ({ ...f, unitRate: e.target.value }))} inputProps={{ min: 0 }} /><TextField type="number" label="Thời gian xử lý dự kiến (giờ)" value={form.leadTimeHours} onChange={e => setForm(f => ({ ...f, leadTimeHours: e.target.value }))} inputProps={{ min: 0 }} /></>}

      <FormControlLabel control={<Switch checked={form.isPremium} onChange={e => setForm(f => ({ ...f, isPremium: e.target.checked }))} />} label="Mục cao cấp" />
      <FormControlLabel control={<Switch checked={form.isDeprecated} onChange={e => setForm(f => ({ ...f, isDeprecated: e.target.checked }))} />} label="Ngừng sử dụng cho dữ liệu mới" />
    </Stack></DialogContent><DialogActions><Button onClick={() => setOpen(false)}>Hủy</Button><Button variant="contained" disabled={!form.code.trim() || !form.name.trim() || saveMutation.isPending} onClick={save}>{editing ? 'Tạo revision mới' : 'Thêm mục'}</Button></DialogActions></Dialog>
    </Box>
  </Box>
}
