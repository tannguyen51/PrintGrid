import { useMemo, useState } from 'react'
import {
  Alert, Box, Button, Chip, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControlLabel, IconButton, MenuItem, Paper, Stack, Switch, Tab,
  Tabs, TextField, Tooltip, Typography,
} from '@mui/material'
import { AddRounded, DeleteOutlineRounded, EditRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { StaffNavbar } from '../home/components/StaffNavbar'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  type DefectTaxonomyItem, type InspectionChecklistItem,
  useCreateInspectionChecklist, useDefectTaxonomy, useDeleteDefectTaxonomyItem,
  useInspectionChecklists, useSaveDefectTaxonomyItem,
} from './useQcKnowledge'

const grades = [{ code: 'STANDARD', label: 'Tiêu chuẩn' }, { code: 'FINE', label: 'Chi tiết cao' }]
const blankItem = (order: number): InspectionChecklistItem => ({ code: '', name: '', passCriteria: '', isMandatory: true, displayOrder: order })

export default function AdminQcKnowledgePage() {
  const [tab, setTab] = useState<'checklists' | 'defects'>('checklists')
  const [grade, setGrade] = useState('STANDARD')
  const checklists = useInspectionChecklists(); const defects = useDefectTaxonomy()
  const createChecklist = useCreateInspectionChecklist(); const saveDefect = useSaveDefectTaxonomyItem(); const deleteDefect = useDeleteDefectTaxonomyItem()
  const versions = useMemo(() => (checklists.data ?? []).filter(x => x.qualityGradeCode === grade), [checklists.data, grade])
  const active = versions.find(x => x.isActive)
  const [items, setItems] = useState<InspectionChecklistItem[]>([])
  const [checklistOpen, setChecklistOpen] = useState(false)
  const [defectOpen, setDefectOpen] = useState(false)
  const [editingDefect, setEditingDefect] = useState<DefectTaxonomyItem | null>(null)
  const [defectForm, setDefectForm] = useState({ code: '', name: '', description: '', defaultFaultAttribution: '', isDeprecated: false })
  const [error, setError] = useState('')

  const openChecklist = () => {
    setItems((active?.items ?? [blankItem(1)]).map(x => ({ ...x })))
    setError(''); setChecklistOpen(true)
  }
  const updateItem = (index: number, patch: Partial<InspectionChecklistItem>) => setItems(old => old.map((x, i) => i === index ? { ...x, ...patch } : x))
  const saveChecklist = async () => {
    setError('')
    if (!items.length || items.some(x => !x.code.trim() || !x.name.trim() || !x.passCriteria.trim())) { setError('Mỗi tiêu chí cần đủ mã, tên và điều kiện đạt.'); return }
    try {
      await createChecklist.mutateAsync({ grade, items: items.map((x, i) => ({ ...x, code: x.code.toUpperCase(), displayOrder: i + 1 })) })
      setChecklistOpen(false)
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể lưu phiên bản bộ kiểm.')) }
  }
  const openDefect = (item?: DefectTaxonomyItem) => {
    setEditingDefect(item ?? null); setError('')
    setDefectForm(item ? { code: item.code, name: item.name, description: item.description ?? '', defaultFaultAttribution: item.defaultFaultAttribution ?? '', isDeprecated: item.isDeprecated } : { code: '', name: '', description: '', defaultFaultAttribution: '', isDeprecated: false })
    setDefectOpen(true)
  }
  const persistDefect = async () => {
    setError('')
    try {
      await saveDefect.mutateAsync({ code: defectForm.code, name: defectForm.name, description: defectForm.description || null, defaultFaultAttribution: (defectForm.defaultFaultAttribution || null) as DefectTaxonomyItem['defaultFaultAttribution'], isDeprecated: defectForm.isDeprecated })
      setDefectOpen(false)
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể lưu mã lỗi.')) }
  }
  const removeDefect = async (item: DefectTaxonomyItem) => {
    if (!confirm(`Xóa mã lỗi “${item.code}”? Mã đã được dùng trong kiểm định sẽ bị chặn.`)) return
    try { await deleteDefect.mutateAsync(item.code) } catch (e) { setError(getApiErrorMessage(e, 'Không thể xóa. Hãy chuyển sang ngừng sử dụng.')) }
  }

  return <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
    <StaffNavbar />
    <Box sx={{ maxWidth: 1180, mx: 'auto', px: 3, py: 4 }}>
    <Stack direction="row" spacing={1.5} alignItems="center" mb={3}><PageBackButton /><Box><Typography variant="h4" fontWeight={800}>Bộ kiểm &amp; danh mục lỗi</Typography><Typography color="text.secondary">Quản lý tiêu chuẩn nghiệm thu theo cấp chất lượng và các loại lỗi dùng tại Hub.</Typography></Box></Stack>
    {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>{error}</Alert>}
    <Paper variant="outlined" sx={{ borderRadius: 3, overflow: 'hidden' }}>
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ borderBottom: 1, borderColor: 'divider', px: 1 }}><Tab value="checklists" label="Bộ kiểm chất lượng" /><Tab value="defects" label="Danh mục lỗi" /></Tabs>
      {tab === 'checklists' ? <Box p={3}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2} mb={3}><TextField select size="small" label="Cấp chất lượng" value={grade} onChange={e => setGrade(e.target.value)} sx={{ minWidth: 220 }}>{grades.map(x => <MenuItem key={x.code} value={x.code}>{x.label}</MenuItem>)}</TextField><Button variant="contained" startIcon={<AddRounded />} onClick={openChecklist}>{active ? 'Tạo phiên bản mới' : 'Tạo bộ kiểm'}</Button></Stack>
        {checklists.isLoading ? <CircularProgress /> : versions.length === 0 ? <Typography color="text.secondary">Chưa có bộ kiểm cho cấp chất lượng này.</Typography> : <Stack spacing={2}>{versions.map(v => <Paper key={v.id} variant="outlined" sx={{ p: 2.5, opacity: v.isActive ? 1 : .65 }}><Stack direction="row" justifyContent="space-between" alignItems="center"><Box><Stack direction="row" spacing={1} alignItems="center"><Typography fontWeight={750}>Phiên bản {v.versionNumber}</Typography><Chip size="small" color={v.isActive ? 'success' : 'default'} label={v.isActive ? 'Đang hoạt động' : 'Lịch sử'} /></Stack><Typography variant="body2" color="text.secondary" mt={.5}>{v.items.length} tiêu chí · tạo {new Date(v.createdAtUtc).toLocaleString('vi-VN')}</Typography></Box>{v.isActive && <Tooltip title="Chỉnh sửa sẽ tạo phiên bản mới"><IconButton onClick={openChecklist}><EditRounded /></IconButton></Tooltip>}</Stack><Stack mt={2} spacing={1}>{v.items.map(i => <Box key={i.code}><Typography fontWeight={650}>{i.displayOrder}. {i.name} {i.isMandatory && <Chip size="small" label="Bắt buộc" sx={{ ml: 1 }} />}</Typography><Typography variant="body2" color="text.secondary">Điều kiện đạt: {i.passCriteria}</Typography></Box>)}</Stack></Paper>)}</Stack>}
      </Box> : <Box p={3}>
        <Stack direction="row" justifyContent="flex-end" mb={2}><Button variant="contained" startIcon={<AddRounded />} onClick={() => openDefect()}>Thêm mã lỗi</Button></Stack>
        {defects.isLoading ? <CircularProgress /> : <Stack divider={<Box sx={{ borderTop: 1, borderColor: 'divider' }} />}>{(defects.data ?? []).map(d => <Stack key={d.id} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} py={2} sx={{ opacity: d.isDeprecated ? .6 : 1 }}><Box><Stack direction="row" spacing={1} alignItems="center"><Typography fontWeight={750}>{d.name}</Typography><Chip size="small" variant="outlined" label={d.code} />{d.defaultFaultAttribution && <Chip size="small" label={`Mặc định: ${d.defaultFaultAttribution}`} />}{d.isDeprecated && <Chip size="small" color="warning" label="Ngừng sử dụng" />}</Stack>{d.description && <Typography variant="body2" color="text.secondary" mt={.5}>{d.description}</Typography>}</Box><Stack direction="row"><Tooltip title="Chỉnh sửa"><IconButton onClick={() => openDefect(d)}><EditRounded /></IconButton></Tooltip><Tooltip title="Chỉ xóa được mã chưa từng sử dụng"><IconButton color="error" onClick={() => removeDefect(d)}><DeleteOutlineRounded /></IconButton></Tooltip></Stack></Stack>)}</Stack>}
      </Box>}
    </Paper>

    <Dialog open={checklistOpen} onClose={() => setChecklistOpen(false)} fullWidth maxWidth="md"><DialogTitle>Tạo phiên bản bộ kiểm mới · {grade}</DialogTitle><DialogContent><Stack spacing={2} mt={1}>{error && <Alert severity="error">{error}</Alert>}{items.map((item, index) => <Paper key={index} variant="outlined" sx={{ p: 2 }}><Stack direction="row" justifyContent="space-between"><Typography fontWeight={700}>Tiêu chí {index + 1}</Typography><IconButton color="error" disabled={items.length === 1} onClick={() => setItems(old => old.filter((_, i) => i !== index))}><DeleteOutlineRounded /></IconButton></Stack><Stack spacing={1.5} mt={1}><TextField label="Mã tiêu chí" value={item.code} onChange={e => updateItem(index, { code: e.target.value.toUpperCase() })} required /><TextField label="Tên tiêu chí" value={item.name} onChange={e => updateItem(index, { name: e.target.value })} required /><TextField label="Điều kiện đạt" value={item.passCriteria} onChange={e => updateItem(index, { passCriteria: e.target.value })} multiline minRows={2} required /><FormControlLabel control={<Switch checked={item.isMandatory} onChange={e => updateItem(index, { isMandatory: e.target.checked })} />} label="Bắt buộc đánh giá" /></Stack></Paper>)}<Button variant="outlined" startIcon={<AddRounded />} onClick={() => setItems(old => [...old, blankItem(old.length + 1)])}>Thêm tiêu chí</Button></Stack></DialogContent><DialogActions><Button onClick={() => setChecklistOpen(false)}>Hủy</Button><Button variant="contained" onClick={saveChecklist} disabled={createChecklist.isPending}>Lưu thành phiên bản mới</Button></DialogActions></Dialog>

    <Dialog open={defectOpen} onClose={() => setDefectOpen(false)} fullWidth maxWidth="sm"><DialogTitle>{editingDefect ? `Cập nhật ${editingDefect.code}` : 'Thêm mã lỗi'}</DialogTitle><DialogContent><Stack spacing={2} mt={1}>{error && <Alert severity="error">{error}</Alert>}<TextField label="Mã lỗi" value={defectForm.code} disabled={!!editingDefect} onChange={e => setDefectForm(x => ({ ...x, code: e.target.value.toUpperCase() }))} required helperText="Mã ổn định, không đổi sau khi tạo" /><TextField label="Tên lỗi" value={defectForm.name} onChange={e => setDefectForm(x => ({ ...x, name: e.target.value }))} required /><TextField label="Mô tả" value={defectForm.description} onChange={e => setDefectForm(x => ({ ...x, description: e.target.value }))} multiline minRows={2} /><TextField select label="Quy trách nhiệm mặc định" value={defectForm.defaultFaultAttribution} onChange={e => setDefectForm(x => ({ ...x, defaultFaultAttribution: e.target.value }))}><MenuItem value="">Không đặt mặc định</MenuItem><MenuItem value="Lab">Xưởng in</MenuItem><MenuItem value="Hub">Hub</MenuItem><MenuItem value="Customer">Khách hàng / file</MenuItem></TextField><FormControlLabel control={<Switch checked={defectForm.isDeprecated} onChange={e => setDefectForm(x => ({ ...x, isDeprecated: e.target.checked }))} />} label="Ngừng sử dụng cho kiểm định mới" /></Stack></DialogContent><DialogActions><Button onClick={() => setDefectOpen(false)}>Hủy</Button><Button variant="contained" disabled={!defectForm.code.trim() || !defectForm.name.trim() || saveDefect.isPending} onClick={persistDefect}>Lưu</Button></DialogActions></Dialog>
    </Box>
  </Box>
}
