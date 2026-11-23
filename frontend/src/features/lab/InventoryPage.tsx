import { useMemo, useState } from 'react'
import { Alert, Autocomplete, Avatar, Box, Button, Card, CardContent, Chip, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, Divider, FormControl, Grid, IconButton, InputAdornment, InputLabel, LinearProgress, MenuItem, Select, Stack, TablePagination, TextField, Tooltip, Typography } from '@mui/material'
import { AddRounded, CategoryOutlined, DeleteOutlineRounded, Inventory2Outlined, SaveRounded, SearchRounded, WarningAmberRounded } from '@mui/icons-material'
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
  const [search, setSearch] = useState('')
  const [materialFilter, setMaterialFilter] = useState('ALL')
  const [colorFilter, setColorFilter] = useState('ALL')
  const [stockFilter, setStockFilter] = useState<'ALL' | 'LOW' | 'AVAILABLE'>('ALL')
  const [page, setPage] = useState(0)
  const [rowsPerPage, setRowsPerPage] = useState(5)
  const [deleteTarget, setDeleteTarget] = useState<Stock | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const inventory = useQuery({ queryKey: ['inventory', labId], queryFn: () => apiClient.get<Stock[]>(`/labs/${labId}/inventory`).then(r => r.data), enabled: !!labId })
  const save = useMutation({
    mutationFn: async () => {
      const material = materialCode.trim().toUpperCase()
      const color = colorCode.trim().toUpperCase()
      const existing = inventory.data?.find(x => x.materialCode === material && x.colorCode === color)
      if (existing) {
        return apiClient.post(`/labs/${labId}/inventory/adjustments`, {
          materialCode: material, colorCode: color, deltaGrams: quantityGrams, reason: 'Nhập thêm vật liệu vào kho',
        })
      }
      return apiClient.put(`/labs/${labId}/inventory`, {
        materialCode: material, colorCode: color, quantityGrams, reorderPointGrams,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory', labId] })
      queryClient.invalidateQueries({ queryKey: ['labs'] })
      setMessage(`Đã cộng thêm ${fmt(quantityGrams)} g vào tồn kho.`)
      setQuantity(0)
    },
  })
  const deleteStock = useMutation({
    mutationFn: (stockId: string) => apiClient.delete(`/labs/${labId}/inventory/${stockId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inventory', labId] })
      queryClient.invalidateQueries({ queryKey: ['labs'] })
      setMessage('Đã xóa vật liệu khỏi kho.')
      setDeleteTarget(null)
    },
  })
  const totals = useMemo(() => (inventory.data ?? []).reduce((sum, item) => ({ available: sum.available + item.availableGrams, reserved: sum.reserved + item.reservedGrams, low: sum.low + Number(item.isLowStock) }), { available: 0, reserved: 0, low: 0 }), [inventory.data])
  const materialFilters = useMemo(() => [...new Set((inventory.data ?? []).map(item => item.materialCode))].sort(), [inventory.data])
  const colorFilters = useMemo(() => [...new Set((inventory.data ?? []).map(item => item.colorCode))].sort(), [inventory.data])
  const filteredInventory = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase('vi-VN')
    return (inventory.data ?? []).filter(item => {
      const matchesSearch = !keyword || [item.materialCode, materialName(item.materialCode), item.colorCode, colorInfo(item.colorCode)?.label ?? '']
        .some(value => value.toLocaleLowerCase('vi-VN').includes(keyword))
      const matchesMaterial = materialFilter === 'ALL' || item.materialCode === materialFilter
      const matchesColor = colorFilter === 'ALL' || item.colorCode === colorFilter
      const matchesStock = stockFilter === 'ALL' || (stockFilter === 'LOW' ? item.isLowStock : !item.isLowStock)
      return matchesSearch && matchesMaterial && matchesColor && matchesStock
    })
  }, [colorFilter, inventory.data, materialFilter, search, stockFilter])
  const hasActiveFilters = Boolean(search.trim()) || materialFilter !== 'ALL' || colorFilter !== 'ALL' || stockFilter !== 'ALL'
  const currentPage = Math.min(page, Math.max(0, Math.ceil(filteredInventory.length / rowsPerPage) - 1))
  const paginatedInventory = useMemo(() => filteredInventory.slice(currentPage * rowsPerPage, currentPage * rowsPerPage + rowsPerPage), [currentPage, filteredInventory, rowsPerPage])

  return <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: '#F8F7FC', py: { xs: 2.5, md: 4 }, px: { xs: 2, md: 3 } }}><Box sx={{ maxWidth: 1120, mx: 'auto' }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} gap={2}>
      <Stack direction="row" spacing={1.5} alignItems="center"><PageBackButton /><Box><Typography variant="h4" fontWeight={800} color="#17122B">Kho vật liệu</Typography><Typography color="text.secondary">Theo dõi vật liệu, màu sắc và ngưỡng cần nhập thêm</Typography></Box></Stack>
      <Button variant="outlined" onClick={() => navigate('/lab/queue')}>Xem hàng đợi sản xuất</Button>
    </Stack>
    {message && <Alert severity="success" onClose={() => setMessage(null)}>{message}</Alert>}
    {save.isError && <Alert severity="error" onClose={() => save.reset()}>{getApiErrorMessage(save.error, 'Không lưu được tồn kho. Vui lòng kiểm tra lại thông tin.')}</Alert>}
    {deleteStock.isError && <Alert severity="error" onClose={() => deleteStock.reset()}>{getApiErrorMessage(deleteStock.error, 'Không thể xóa vật liệu khỏi kho.')}</Alert>}
    {inventory.isError && <Alert severity="error">Không tải được dữ liệu tồn kho.</Alert>}

    <Grid container spacing={2}>{[
      { label: 'Tổng tồn thực tế', value: `${fmt(totals.available)} g`, color: '#6D5CE7' },
      { label: 'Đang giữ cho đơn', value: `${fmt(totals.reserved)} g`, color: '#3B82F6' },
      { label: 'Cần nhập thêm', value: `${totals.low} loại`, color: totals.low ? '#D97706' : '#16A34A' },
    ].map(item => <Grid key={item.label} size={{ xs: 12, sm: 4 }}><Card variant="outlined" sx={{ height: '100%', borderRadius: 3 }}><CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}><Avatar sx={{ bgcolor: `${item.color}14`, color: item.color }}><Inventory2Outlined /></Avatar><Box><Typography variant="body2" color="text.secondary">{item.label}</Typography><Typography variant="h6" fontWeight={800}>{item.value}</Typography></Box></CardContent></Card></Grid>)}</Grid>

    <Card variant="outlined" sx={{ borderRadius: 3, overflow: 'visible' }}><CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
      <Stack direction="row" spacing={1.2} alignItems="center" mb={.5}><AddRounded sx={{ color: 'primary.main' }} /><Typography variant="h6" fontWeight={800}>Cập nhật vật liệu trong kho</Typography></Stack>
      <Typography variant="body2" color="text.secondary" mb={3}>Chọn từ danh mục chuẩn; bấm × trước nếu cần nhập một tên mới.</Typography>
      <Box component="form" onSubmit={e => { e.preventDefault(); save.mutate() }}><Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 6 }}><Autocomplete freeSolo options={MATERIALS.map(x => x.code)} value={materialCode || null} inputValue={materialInput}
          onChange={(_, value) => { const code = value ?? ''; setMaterial(code); setMaterialInput(code ? materialInputLabel(code) : '') }}
          onInputChange={(_, value, reason) => {
            if (reason === 'clear') { setMaterial(''); setMaterialInput(''); return }
            if (reason === 'input' && materialInput !== materialInputLabel(materialCode)) { setMaterialInput(value); setMaterial(value.trim().toUpperCase()) }
          }}
          getOptionLabel={code => `${code} — ${MATERIALS.find(x => x.code === code)?.name ?? code}`}
          renderOption={(props, code) => { const item = MATERIALS.find(x => x.code === code); return <Box component="li" {...props} key={code}><Stack><Typography fontWeight={700}>{item?.name ?? code} <Typography component="span" variant="caption" color="text.secondary">({code})</Typography></Typography><Typography variant="caption" color="text.secondary">{item?.note}</Typography></Stack></Box> }}
          renderInput={params => <TextField {...params} required label="Vật liệu" placeholder="Tìm vật liệu" InputProps={{ ...params.InputProps, startAdornment: <><InputAdornment position="start"><CategoryOutlined fontSize="small" /></InputAdornment>{params.InputProps.startAdornment}</> }} />} /></Grid>
        <Grid size={{ xs: 12, md: 6 }}><Autocomplete freeSolo options={COLORS.map(x => x.code)} value={colorCode || null} inputValue={colorInput}
          onChange={(_, value) => { const code = value ?? ''; setColor(code); setColorInput(code ? colorInputLabel(code) : '') }}
          onInputChange={(_, value, reason) => {
            if (reason === 'clear') { setColor(''); setColorInput(''); return }
            if (reason === 'input' && colorInput !== colorInputLabel(colorCode)) { setColorInput(value); setColor(value.trim().toUpperCase()) }
          }}
          getOptionLabel={code => `${COLORS.find(x => x.code === code)?.label ?? code} — ${code}`}
          renderOption={(props, code) => { const item = colorInfo(code); return <Box component="li" {...props} key={code} sx={{ gap: 1.5 }}><ColorDot code={code} /><Box><Typography fontWeight={700}>{item?.label ?? code}</Typography><Typography variant="caption" color="text.secondary">{code}</Typography></Box></Box> }}
          renderInput={params => <TextField {...params} required label="Màu sắc" placeholder="Tìm màu" InputProps={{ ...params.InputProps, startAdornment: <><InputAdornment position="start"><ColorDot code={colorCode} size={20} /></InputAdornment>{params.InputProps.startAdornment}</> }} />} /></Grid>
        <Grid size={{ xs: 12 }}><Typography variant="caption" color="text.secondary" display="block" mb={1}>Chọn nhanh màu phổ biến</Typography><Stack direction="row" gap={1} flexWrap="wrap">{COLORS.map(color => <Chip key={color.code} clickable onClick={() => { setColor(color.code); setColorInput(colorInputLabel(color.code)) }} variant={colorCode === color.code ? 'filled' : 'outlined'} color={colorCode === color.code ? 'primary' : 'default'} avatar={<Avatar sx={{ bgcolor: 'transparent !important' }}><ColorDot code={color.code} size={18} /></Avatar>} label={color.label} />)}</Stack></Grid>
        <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required label="Khối lượng nhập thêm" type="number" value={quantityGrams} onChange={e => setQuantity(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} InputProps={{ endAdornment: <InputAdornment position="end">gram</InputAdornment> }} helperText="Số gram này sẽ được cộng vào lượng đang có" /></Grid>
        <Grid size={{ xs: 12, sm: 6 }}><TextField fullWidth required label="Ngưỡng cảnh báo" type="number" value={reorderPointGrams} onChange={e => setReorder(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} InputProps={{ endAdornment: <InputAdornment position="end">gram</InputAdornment> }} helperText="Cảnh báo khi tồn kho bằng hoặc thấp hơn mức này" /></Grid>
        <Grid size={{ xs: 12 }}><Stack direction="row" justifyContent="flex-end"><Button type="submit" size="large" variant="contained" startIcon={<SaveRounded />} disabled={save.isPending || !materialCode.trim() || !colorCode.trim() || quantityGrams <= 0 || reorderPointGrams < 0} sx={{ px: 3, minWidth: 190 }}>{save.isPending ? 'Đang lưu...' : 'Cộng vào kho'}</Button></Stack></Grid>
      </Grid></Box>
    </CardContent></Card>

    <Box><Stack direction="row" justifyContent="space-between" alignItems="center" mb={1.5}><Typography variant="h6" fontWeight={800}>Danh sách tồn kho</Typography><Chip size="small" label={hasActiveFilters ? `${filteredInventory.length}/${inventory.data?.length ?? 0} kết quả` : `${inventory.data?.length ?? 0} loại vật liệu`} /></Stack>
      <Card variant="outlined" sx={{ borderRadius: 3, mb: 2 }}><CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}><Grid container spacing={1.5}>
        <Grid size={{ xs: 12, md: 4 }}><TextField fullWidth size="small" label="Tìm trong kho" value={search} onChange={event => { setSearch(event.target.value); setPage(0) }} placeholder="Tên, mã vật liệu hoặc màu" InputProps={{ startAdornment: <InputAdornment position="start"><SearchRounded fontSize="small" /></InputAdornment> }} /></Grid>
        <Grid size={{ xs: 12, sm: 4, md: 2.5 }}><FormControl fullWidth size="small"><InputLabel id="material-filter-label">Vật liệu</InputLabel><Select labelId="material-filter-label" label="Vật liệu" value={materialFilter} onChange={event => { setMaterialFilter(event.target.value); setPage(0) }}><MenuItem value="ALL">Tất cả vật liệu</MenuItem>{materialFilters.map(code => <MenuItem key={code} value={code}>{materialName(code)}</MenuItem>)}</Select></FormControl></Grid>
        <Grid size={{ xs: 12, sm: 4, md: 2.5 }}><FormControl fullWidth size="small"><InputLabel id="color-filter-label">Màu sắc</InputLabel><Select labelId="color-filter-label" label="Màu sắc" value={colorFilter} onChange={event => { setColorFilter(event.target.value); setPage(0) }}><MenuItem value="ALL">Tất cả màu</MenuItem>{colorFilters.map(code => <MenuItem key={code} value={code}><Stack direction="row" spacing={1} alignItems="center"><ColorDot code={code} size={16} /><span>{colorInfo(code)?.label ?? code}</span></Stack></MenuItem>)}</Select></FormControl></Grid>
        <Grid size={{ xs: 12, sm: 4, md: 3 }}><FormControl fullWidth size="small"><InputLabel id="stock-filter-label">Trạng thái</InputLabel><Select labelId="stock-filter-label" label="Trạng thái" value={stockFilter} onChange={event => { setStockFilter(event.target.value as 'ALL' | 'LOW' | 'AVAILABLE'); setPage(0) }}><MenuItem value="ALL">Tất cả trạng thái</MenuItem><MenuItem value="LOW">Sắp hết</MenuItem><MenuItem value="AVAILABLE">Còn đủ</MenuItem></Select></FormControl></Grid>
        {hasActiveFilters && <Grid size={{ xs: 12 }}><Stack direction="row" justifyContent="flex-end"><Button size="small" onClick={() => { setSearch(''); setMaterialFilter('ALL'); setColorFilter('ALL'); setStockFilter('ALL'); setPage(0) }}>Xóa bộ lọc</Button></Stack></Grid>}
      </Grid></CardContent></Card>
      {inventory.isLoading && <LinearProgress sx={{ borderRadius: 2 }} />}
      {!inventory.isLoading && !(inventory.data?.length) && <Card variant="outlined" sx={{ borderRadius: 3, py: 6, textAlign: 'center' }}><Inventory2Outlined sx={{ fontSize: 44, color: 'text.disabled' }} /><Typography fontWeight={700} mt={1}>Kho chưa có vật liệu</Typography><Typography variant="body2" color="text.secondary">Hãy thêm loại vật liệu đầu tiên ở biểu mẫu phía trên.</Typography></Card>}
      {!inventory.isLoading && Boolean(inventory.data?.length) && filteredInventory.length === 0 && <Card variant="outlined" sx={{ borderRadius: 3, py: 5, textAlign: 'center' }}><SearchRounded sx={{ fontSize: 40, color: 'text.disabled' }} /><Typography fontWeight={700} mt={1}>Không tìm thấy vật liệu phù hợp</Typography><Typography variant="body2" color="text.secondary">Thử đổi từ khóa hoặc xóa bớt bộ lọc.</Typography></Card>}
      <Stack spacing={1.5}>{paginatedInventory.map(item => { const ratio = item.reorderPointGrams > 0 ? Math.min(100, item.availableGrams / item.reorderPointGrams * 100) : 100; return <Card key={item.id} variant="outlined" sx={{ borderRadius: 3, borderColor: item.isLowStock ? 'warning.light' : 'divider' }}><CardContent sx={{ p: 2.5, '&:last-child': { pb: 2.5 } }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Stack direction="row" spacing={1.5} alignItems="center"><Avatar sx={{ bgcolor: '#F1EFFE' }}><ColorDot code={item.colorCode} /></Avatar><Box><Stack direction="row" gap={1} alignItems="center" flexWrap="wrap"><Typography fontWeight={800}>{materialName(item.materialCode)}</Typography><Chip size="small" label={colorInfo(item.colorCode)?.label ?? item.colorCode} />{item.isLowStock && <Chip size="small" color="warning" icon={<WarningAmberRounded />} label="Sắp hết" />}</Stack><Typography variant="caption" color="text.secondary">{item.materialCode} · {item.colorCode}</Typography></Box></Stack><Stack direction="row" spacing={2} alignItems="center"><Box textAlign="right"><Typography variant="caption" color="text.secondary">Có thể gán</Typography><Typography fontWeight={800}>{fmt(item.assignableGrams)} g</Typography></Box><Box textAlign="right"><Typography variant="caption" color="text.secondary">Đang giữ</Typography><Typography fontWeight={700}>{fmt(item.reservedGrams)} g</Typography></Box><Tooltip title={item.reservedGrams > 0 ? 'Không thể xóa khi vật liệu đang được giữ cho đơn in' : 'Xóa vật liệu khỏi kho'}><span><IconButton aria-label={`Xóa ${materialName(item.materialCode)} màu ${colorInfo(item.colorCode)?.label ?? item.colorCode}`} color="error" disabled={item.reservedGrams > 0 || deleteStock.isPending} onClick={() => { deleteStock.reset(); setDeleteTarget(item) }}><DeleteOutlineRounded /></IconButton></span></Tooltip></Stack></Stack>
        <Divider sx={{ my: 2 }} /><Stack direction="row" justifyContent="space-between" mb={.75}><Typography variant="body2">Tồn thực tế: <b>{fmt(item.availableGrams)} g</b></Typography><Typography variant="body2" color="text.secondary">Ngưỡng: {fmt(item.reorderPointGrams)} g</Typography></Stack><LinearProgress variant="determinate" value={ratio} color={item.isLowStock ? 'warning' : 'primary'} sx={{ height: 7, borderRadius: 4, bgcolor: '#EEEAF8' }} />
      </CardContent></Card> })}</Stack>
      {filteredInventory.length > 0 && <TablePagination component="div" count={filteredInventory.length} page={currentPage} onPageChange={(_, nextPage) => setPage(nextPage)} rowsPerPage={rowsPerPage} onRowsPerPageChange={event => { setRowsPerPage(Number(event.target.value)); setPage(0) }} rowsPerPageOptions={[5, 10, 20]} labelRowsPerPage="Số mục mỗi trang" labelDisplayedRows={({ from, to, count }) => `${from}–${to} trong ${count}`} />}
    </Box>
    <Dialog open={Boolean(deleteTarget)} onClose={() => !deleteStock.isPending && setDeleteTarget(null)} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ fontWeight: 800 }}>Xóa vật liệu khỏi kho?</DialogTitle>
      <DialogContent><DialogContentText>{deleteTarget && <>Bạn sắp xóa <b>{materialName(deleteTarget.materialCode)} · {colorInfo(deleteTarget.colorCode)?.label ?? deleteTarget.colorCode}</b> khỏi danh sách tồn kho. Hành động này không thể hoàn tác.</>}</DialogContentText></DialogContent>
      <DialogActions><Button disabled={deleteStock.isPending} onClick={() => setDeleteTarget(null)}>Hủy</Button><Button color="error" variant="contained" disabled={!deleteTarget || deleteStock.isPending} onClick={() => deleteTarget && deleteStock.mutate(deleteTarget.id)}>{deleteStock.isPending ? 'Đang xóa...' : 'Xóa khỏi kho'}</Button></DialogActions>
    </Dialog>
  </Stack></Box></Box>
}
