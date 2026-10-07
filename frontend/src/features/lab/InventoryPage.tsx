import { useState } from 'react'
import { Alert, Box, Button, Chip, Stack, TextField, Typography } from '@mui/material'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'

interface Stock {
  id: string; materialCode: string; colorCode: string; availableGrams: number
  reservedGrams: number; assignableGrams: number; reorderPointGrams: number; isLowStock: boolean
}

export default function InventoryPage() {
  const { labId = '' } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [materialCode, setMaterial] = useState('PLA')
  const [colorCode, setColor] = useState('BLACK')
  const [quantityGrams, setQuantity] = useState(0)
  const [reorderPointGrams, setReorder] = useState(100)
  const [message, setMessage] = useState<string | null>(null)
  const inventory = useQuery({
    queryKey: ['inventory', labId],
    queryFn: () => apiClient.get<Stock[]>(`/labs/${labId}/inventory`).then(r => r.data),
    enabled: !!labId,
  })
  const save = useMutation({
    mutationFn: () => apiClient.put(`/labs/${labId}/inventory`, { materialCode, colorCode, quantityGrams, reorderPointGrams }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['inventory', labId] }); setMessage('Đã ghi nhận giao dịch kho.') },
  })

  return <Box sx={{ minHeight: '100vh', p: { xs: 2, md: 4 }, maxWidth: 1000, mx: 'auto' }}>
    <Stack spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h4" fontWeight={800}>Kho vật liệu</Typography>
        <Button onClick={() => navigate('/lab/queue')}>Hàng đợi</Button>
      </Stack>
      {message && <Alert severity="success" onClose={() => setMessage(null)}>{message}</Alert>}
      {inventory.isError && <Alert severity="error">Không tải được tồn kho.</Alert>}
      {(inventory.data ?? []).some(x => x.isLowStock) && <Alert severity="warning">Có vật liệu đã chạm ngưỡng nhập thêm.</Alert>}
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} component="form" onSubmit={e => { e.preventDefault(); save.mutate() }}>
        <TextField label="Vật liệu" value={materialCode} onChange={e => setMaterial(e.target.value)} required />
        <TextField label="Màu" value={colorCode} onChange={e => setColor(e.target.value)} required />
        <TextField label="Tồn thực tế (g)" type="number" value={quantityGrams} onChange={e => setQuantity(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} required />
        <TextField label="Ngưỡng cảnh báo (g)" type="number" value={reorderPointGrams} onChange={e => setReorder(Number(e.target.value))} inputProps={{ min: 0, step: .01 }} required />
        <Button type="submit" variant="contained" disabled={save.isPending}>Lưu kho</Button>
      </Stack>
      <Stack spacing={1.5}>{(inventory.data ?? []).map(x => <Box key={x.id} sx={{ p: 2, border: '1px solid', borderColor: x.isLowStock ? 'warning.main' : 'divider', borderRadius: 2 }}>
        <Stack direction="row" justifyContent="space-between">
          <Typography fontWeight={700}>{x.materialCode} · {x.colorCode}</Typography>
          {x.isLowStock && <Chip color="warning" label="Gần hết" />}
        </Stack>
        <Typography>Tồn: {x.availableGrams} g · Đang giữ: {x.reservedGrams} g · Có thể gán: {x.assignableGrams} g · Ngưỡng: {x.reorderPointGrams} g</Typography>
      </Box>)}</Stack>
    </Stack>
  </Box>
}
