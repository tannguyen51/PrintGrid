import { useMemo, useState } from 'react'
import { Alert, Autocomplete, Avatar, Box, Button, Card, CardContent, Chip, Divider, Grid, InputAdornment, LinearProgress, Stack, TextField, Typography } from '@mui/material'
import { AddRounded, CategoryOutlined, Inventory2Outlined, SaveRounded, WarningAmberRounded } from '@mui/icons-material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { PageBackButton } from '../../shared/components/PageBackButton'

interface Stock { id: string; materialCode: string; colorCode: string; availableGrams: number; reservedGrams: number; assignableGrams: number; reorderPointGrams: number; isLowStock: boolean }

const MATERIALS = [
  { code: 'PLA', name: 'PLA', note: 'Dễ in, đa dụng' }, { code: 'PETG', name: 'PETG', note: 'Bền, chịu ẩm tốt' },
  { code: 'ABS', name: 'ABS', note: 'Cứng, chịu nhiệt' }, { code: 'ASA', name: 'ASA', note: 'Chịu UV, dùng ngoài trời' },
  { code: 'TPU', name: 'TPU / Flexible', note: 'Dẻo, đàn hồi' }, { code: 'NYLON', name: 'Nylon (PA)', note: 'Dai, chịu mài mòn' },
  { code: 'PC', name: 'Polycarbonate', note: 'Rất bền, chịu nhiệt cao' }, { code: 'PP', name: 'Polypropylene', note: 'Nhẹ, chịu hóa chất' },
  { code: 'HIPS', name: 'HIPS', note: 'Nhẹ, dùng làm support' }, { code: 'PVA', name: 'PVA', note: 'Support tan trong nước' },
  { code: 'RESIN', name: 'Resin tiêu chuẩn', note: 'Chi tiết cao' }, { code: 'RESIN-TOUGH', name: 'Resin Tough', note: 'Resin chịu lực' },
] as const

const COLORS = [
  { code: 'BLACK', label: 'Đen', hex: '#1F2937' }, { code: 'WHITE', label: 'Trắng', hex: '#FFFFFF' },
  { code: 'GRAY', label: 'Xám', hex: '#9CA3AF' }, { code: 'SILVER', label: 'Bạc', hex: '#CBD5E1' },
  { code: 'RED', label: 'Đỏ', hex: '#EF4444' }, { code: 'ORANGE', label: 'Cam', hex: '#F97316' },
  { code: 'YELLOW', label: 'Vàng', hex: '#FACC15' }, { code: 'GREEN', label: 'Xanh lá', hex: '#22C55E' },
  { code: 'BLUE', label: 'Xanh dương', hex: '#3B82F6' }, { code: 'NAVY', label: 'Xanh navy', hex: '#1E3A8A' },
  { code: 'PURPLE', label: 'Tím', hex: '#8B5CF6' }, { code: 'PINK', label: 'Hồng', hex: '#EC4899' },
  { code: 'BROWN', label: 'Nâu', hex: '#92400E' }, { code: 'BEIGE', label: 'Be', hex: '#D6C6A5' },
  { code: 'GOLD', label: 'Vàng kim', hex: '#D4A72C' }, { code: 'COPPER', label: 'Đồng', hex: '#B87333' },
  { code: 'TRANSPARENT', label: 'Trong suốt', hex: '#E0F2FE' }, { code: 'NATURAL', label: 'Tự nhiên', hex: '#F1E7D0' },
] as const

const fmt = (value: number) => new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 }).format(value)
const materialName = (code: string) => MATERIALS.find(x => x.code === code)?.name ?? code
const colorInfo = (code: string) => COLORS.find(x => x.code === code)
const materialInputLabel = (code: string) => `${code} — ${materialName(code)}`
const colorInputLabel = (code: string) => `${colorInfo(code)?.label ?? code} — ${code}`

function ColorDot({ code, size = 24 }: { code: string; size?: number }) {
  const color = colorInfo(code)
  return <Box aria-hidden sx={{ width: size, height: size, flex: '0 0 auto', borderRadius: '50%', bgcolor: color?.hex ?? '#E5E7EB', border: '1px solid', borderColor: ['WHITE', 'TRANSPARENT'].includes(code) ? 'divider' : 'transparent', boxShadow: '0 1px 2px rgba(15,23,42,.12)' }} />
}

export default function InventoryPage() {
  const { labId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [materialCode, setMaterial] = useState('PLA')
  const [materialInput, setMaterialInput] = useState(materialInputLabel('PLA'))
  const [colorCode, setColor] = useState('BLACK')
  const [colorInput, setColorInput] = useState(colorInputLabel('BLACK'))
  const [quantityGrams, setQuantity] = useState(0)
  const [reorderPointGrams, setReorder] = useState(100)
  const [message, setMessage] = useState<string | null>(null)
  const inventory = useQuery({ queryKey: ['inventory', labId], queryFn: () => apiClient.get<Stock[]>(`/labs/${labId}/inventory`).then(r => r.data), enabled: !!labId })
  const save = useMutation({
    mutationFn: () => apiClient.put(`/labs/${labId}/inventory`, { materialCode: materialCode.trim().toUpperCase(), colorCode: colorCode.trim().toUpperCase(), quantityGrams, reorderPointGrams }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['inventory', labId] }); queryClient.invalidateQueries({ queryKey: ['labs'] }); setMessage('Đã cập nhật tồn kho thành công.') },
  })
  const totals = useMemo(() => (inventory.data ?? []).reduce((sum, item) => ({ available: sum.available + item.availableGrams, reserved: sum.reserved + item.reservedGrams, low: sum.low + Number(item.isLowStock) }), { available: 0, reserved: 0, low: 0 }), [inventory.data])

  return <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: '#F8F7FC', py: { xs: 2.5, md: 4 }, px: { xs: 2, md: 3 } }}><Box sx={{ maxWidth: 1120, mx: 'auto' }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} gap={2}>
      <Stack direction="row" spacing={1.5} alignItems="center"><PageBackButton /><Box><Typography variant="h4" fontWeight={800} color="#17122B">Kho vật liệu</Typography><Typography color="text.secondary">Theo dõi vật liệu, màu sắc và ngưỡng cần nhập thêm</Typography></Box></Stack>
      <Button variant="outlined" onClick={() => navigate('/lab/queue')}>Xem hàng đợi sản xuất</Button>
    </Stack>
    {message && <Alert severity="success" onClose={() => setMessage(null)}>{message}</Alert>}
    {save.isError && <Alert severity="error" onClose={() => save.reset()}>{getApiErrorMessage(save.error, 'Không lưu được tồn kho. Vui lòng kiểm tra lại thông tin.')}</Alert>}
    {inventory.isError && <Alert severity="error">Không tải được dữ liệu tồn kho.</Alert>}

    <Grid container spacing={2}>{[
      { label: 'Tổng tồn thực tế', value: `${fmt(totals.available)} g`, color: '#6D5CE7' },
      { label: 'Đang giữ cho đơn', value: `${fmt(totals.reserved)} g`, color: '#3B82F6' },
      { label: 'Cần nhập thêm', value: `${totals.low} loại`, color: totals.low ? '#D97706' : '#16A34A' },
    ].map(item => <Grid key={item.label} size={{ xs: 12, sm: 4 }}><Card variant="outlined" sx={{ height: '100%', borderRadius: 3 }}><CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}><Avatar sx={{ bgcolor: `${item.color}14`, color: item.color }}><Inventory2Outlined /></Avatar><Box><Typography variant="body2" color="text.secondary">{item.label}</Typography><Typography variant="h6" fontWeight={800}>{item.value}</Typography></Box></CardContent></Card></Grid>)}</Grid>

    <Card variant="outlined" sx={{ borderRadius: 3, overflow: 'visible' }}><CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
      <Stack direction="row" spacing={1.2} alignItems="center" mb={.5}><AddRounded sx={{ color: 'primary.main' }} /><Typography variant="h6" fontWeight={800}>Cập nhật vật liệu trong kho</Typography></Stack>
      <Typography variant="body2" color="text.secondary" mb={3}>Chọn từ danh sách hoặc nhập mã vật liệu, màu riêng của xưởng.</Typography>
      <Box component="form" onSubmit={e => { e.preventDefault(); save.mutate() }}><Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}><Autocomplete freeSolo options={MATERIALS.map(x => x.code)} value={materialCode} inputValue={materialInput}
          onChange={(_, value) => { const code = value ?? ''; setMaterial(code); setMaterialInput(code ? materialInputLabel(code) : '') }}
          onInputChange={(_, value, reason) => { if (reason === 'input') { setMaterialInput(value); setMaterial(value.trim().toUpperCase()) } else if (reason === 'clear') { setMaterialInput(''); setMaterial('') } }}
          getOptionLabel={code => `${code} — ${MATERIALS.find(x => x.code === code)?.name ?? code}`}
          renderOption={(props, code) => { const item = MATERIALS.find(x => x.code === code); return <Box component="li" {...props} key={code}><Stack><Typography fontWeight={700}>{item?.name ?? code} <Typography component="span" variant="caption" color="text.secondary">({code})</Typography></Typography><Typography variant="caption" color="text.secondary">{item?.note}</Typography></Stack></Box> }}
          renderInput={params => <TextField {...params} required label="Vật liệu" placeholder="Tìm hoặc nhập vật liệu" InputProps={{ ...params.InputProps, startAdornment: <><InputAdornment position="start"><CategoryOutlined fontSize="small" /></InputAdornment>{params.InputProps.startAdornment}</> }} />} /></Grid>
        <Grid size={{ xs: 12, md: 6 }}><Autocomplete freeSolo options={COLORS.map(x => x.code)} value={colorCode} inputValue={colorInput}
          onChange={(_, value) => { const code = value ?? ''; setColor(code); setColorInput(code ? colorInputLabel(code) : '') }}
          onInputChange={(_, value, reason) => { if (reason === 'input') { setColorInput(value); setColor(value.trim().toUpperCase()) } else if (reason === 'clear') { setColorInput(''); setColor('') } }}
          getOptionLabel={code => `${COLORS.find(x => x.code === code)?.label ?? code} — ${code}`}
          renderOption={(props, code) => { const item = colorInfo(code); return <Box component="li" {...props} key={code} sx={{ gap: 1.5 }}><ColorDot code={code} /><Box><Typography fontWeight={700}>{item?.label ?? code}</Typography><Typography variant="caption" color="text.secondary">{code}</Typography></Box></Box> }}
          renderInput={params => <TextField {...params} required label="Màu sắc" placeholder="Tìm hoặc nhập màu" InputProps={{ ...params.InputProps, startAdornment: <><InputAdornment position="start"><ColorDot code={colorCode} size={20} /></InputAdornment>{params.InputProps.startAdornment}</> }} />} /></Grid>
        <Grid size={{ xs: 12 }}><Typography variant="caption" color="text.secondary" display="block" mb={1}>Chọn nhanh màu phổ biến</Typography><Stack direction="row" gap={1} flexWrap="wrap">{COLORS.map(color => <Chip key={color.code} clickable onClick={() => { setColor(color.code); setColorInput(colorInputLabel(color.code)) }} variant={colorCode === color.code ? 'filled' : 'outlined'} color={colorCode === color.code ? 'primary' : 'default'} avatar={<Avatar sx={{ bgcolor: 'transparent !important' }}><ColorDot code={color.code} size={18} /></Avatar>} label={color.label} />)}</Stack></Grid>
        <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required label="Tồn thực tế" type="number" value={quantityGrams} onChange={e => setQuantity(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} InputProps={{ endAdornment: <InputAdornment position="end">gram</InputAdornment> }} helperText="Khối lượng hiện có trong kho" /></Grid>
        <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required label="Ngưỡng cảnh báo" type="number" value={reorderPointGrams} onChange={e => setReorder(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} InputProps={{ endAdornment: <InputAdornment position="end">gram</InputAdornment> }} helperText="Cảnh báo khi tồn kho bằng hoặc thấp hơn mức này" /></Grid>
        <Grid size={{ xs: 12 }}><Stack direction="row" justifyContent="flex-end"><Button type="submit" size="large" variant="contained" startIcon={<SaveRounded />} disabled={save.isPending || !materialCode.trim() || !colorCode.trim() || quantityGrams < 0 || reorderPointGrams < 0} sx={{ px: 3, minWidth: 170 }}>{save.isPending ? 'Đang lưu...' : 'Lưu vào kho'}</Button></Stack></Grid>
      </Grid></Box>
    </CardContent></Card>

    <Box><Stack direction="row" justifyContent="space-between" alignItems="center" mb={1.5}><Typography variant="h6" fontWeight={800}>Danh sách tồn kho</Typography><Chip size="small" label={`${inventory.data?.length ?? 0} loại vật liệu`} /></Stack>
      {inventory.isLoading && <LinearProgress sx={{ borderRadius: 2 }} />}
      {!inventory.isLoading && !(inventory.data?.length) && <Card variant="outlined" sx={{ borderRadius: 3, py: 6, textAlign: 'center' }}><Inventory2Outlined sx={{ fontSize: 44, color: 'text.disabled' }} /><Typography fontWeight={700} mt={1}>Kho chưa có vật liệu</Typography><Typography variant="body2" color="text.secondary">Hãy thêm loại vật liệu đầu tiên ở biểu mẫu phía trên.</Typography></Card>}
      <Stack spacing={1.5}>{(inventory.data ?? []).map(item => { const ratio = item.reorderPointGrams > 0 ? Math.min(100, item.availableGrams / item.reorderPointGrams * 100) : 100; return <Card key={item.id} variant="outlined" sx={{ borderRadius: 3, borderColor: item.isLowStock ? 'warning.light' : 'divider' }}><CardContent sx={{ p: 2.5, '&:last-child': { pb: 2.5 } }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Stack direction="row" spacing={1.5} alignItems="center"><Avatar sx={{ bgcolor: '#F1EFFE' }}><ColorDot code={item.colorCode} /></Avatar><Box><Stack direction="row" gap={1} alignItems="center" flexWrap="wrap"><Typography fontWeight={800}>{materialName(item.materialCode)}</Typography><Chip size="small" label={colorInfo(item.colorCode)?.label ?? item.colorCode} />{item.isLowStock && <Chip size="small" color="warning" icon={<WarningAmberRounded />} label="Sắp hết" />}</Stack><Typography variant="caption" color="text.secondary">{item.materialCode} · {item.colorCode}</Typography></Box></Stack><Stack direction="row" spacing={3}><Box textAlign="right"><Typography variant="caption" color="text.secondary">Có thể gán</Typography><Typography fontWeight={800}>{fmt(item.assignableGrams)} g</Typography></Box><Box textAlign="right"><Typography variant="caption" color="text.secondary">Đang giữ</Typography><Typography fontWeight={700}>{fmt(item.reservedGrams)} g</Typography></Box></Stack></Stack>
        <Divider sx={{ my: 2 }} /><Stack direction="row" justifyContent="space-between" mb={.75}><Typography variant="body2">Tồn thực tế: <b>{fmt(item.availableGrams)} g</b></Typography><Typography variant="body2" color="text.secondary">Ngưỡng: {fmt(item.reorderPointGrams)} g</Typography></Stack><LinearProgress variant="determinate" value={ratio} color={item.isLowStock ? 'warning' : 'primary'} sx={{ height: 7, borderRadius: 4, bgcolor: '#EEEAF8' }} />
      </CardContent></Card> })}</Stack>
    </Box>
  </Stack></Box></Box>
}
