import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Alert, Box, Button, Checkbox, Chip, CircularProgress, Divider, Paper, Stack,
  Table, TableBody, TableCell, TableHead, TableRow, Typography,
} from '@mui/material'
import { Inventory2Rounded, LocalShippingRounded, LockRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { usePreviewShipment, useSealShipment, useShipmentPlanner } from './useShipmentBatches'

const money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })
const date = (value: string) => new Date(`${value}T00:00:00`).toLocaleDateString('vi-VN')
const weightClassLabel: Record<string, string> = {
  UP_TO_1_KG: 'Đến 1 kg', UP_TO_5_KG: 'Trên 1–5 kg', OVER_5_KG: 'Trên 5 kg',
}

export default function ShipmentBatchPlannerPage() {
  const { labId = '' } = useParams()
  const planner = useShipmentPlanner(labId)
  const preview = usePreviewShipment()
  const seal = useSealShipment(labId)
  const [selected, setSelected] = useState<string[]>([])
  const [confirmed, setConfirmed] = useState(false)
  const jobs = planner.data?.eligibleJobs ?? []
  const selectionKey = useMemo(() => [...selected].sort().join(','), [selected])

  useEffect(() => {
    setConfirmed(false)
    if (selected.length === 0) { preview.reset(); return }
    preview.mutate({ labId, jobIds: selected })
    // selectionKey is the stable trigger; mutation functions are intentionally excluded.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [labId, selectionKey])

  const toggle = (id: string) => setSelected(current => current.includes(id) ? current.filter(x => x !== id) : [...current, id])

  async function handleSeal() {
    await seal.mutateAsync(selected)
    setSelected([])
    setConfirmed(false)
    preview.reset()
  }

  if (planner.isLoading) return <Box sx={{ minHeight: '70vh', display: 'grid', placeItems: 'center' }}><CircularProgress /></Box>

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', p: { xs: 2, md: 4 } }}>
      <Stack spacing={3} sx={{ maxWidth: 1120, mx: 'auto' }}>
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <PageBackButton />
          <Box>
            <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800 }}>Lô chuyển xưởng → hub</Typography>
            <Typography color="text.secondary">Gom job đã duyệt chứng tích theo lịch xe, xem phí zone rồi niêm phong.</Typography>
          </Box>
        </Stack>

        {planner.isError && <Alert severity="error">Không tải được kế hoạch chuyển hàng.</Alert>}
        {seal.isError && <Alert severity="error">Không thể niêm phong lô. Danh sách job có thể vừa thay đổi, vui lòng tải lại.</Alert>}
        {seal.isSuccess && <Alert severity="success">Đã niêm phong lô {seal.data.waybillNumber}; hub đã được ghi nhận chờ nhận hàng.</Alert>}

        {planner.data && <>
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}>
              <Box><Typography fontWeight={700}>{planner.data.labName}</Typography><Typography color="text.secondary">Lịch xe: {planner.data.transferDaysOfWeek}</Typography></Box>
              <Stack direction="row" spacing={1}><Chip label={`Zone ${planner.data.transportZone}`} /><Chip color="primary" icon={<LocalShippingRounded />} label={`Chuyến kế: ${date(planner.data.nextTransferDate)}`} /></Stack>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ overflow: 'hidden' }}>
            <Box sx={{ p: 2.5 }}><Typography variant="h6" fontWeight={750}>Job đủ điều kiện ({jobs.length})</Typography><Typography variant="body2" color="text.secondary">Chỉ gồm job hoàn tất và được Order Staff duyệt chứng tích QC.</Typography></Box>
            <Divider />
            {jobs.length === 0 ? <Box sx={{ p: 5, textAlign: 'center' }}><Inventory2Rounded color="disabled" sx={{ fontSize: 44 }} /><Typography color="text.secondary">Chưa có job sẵn sàng cho chuyến kế tiếp.</Typography></Box> :
              <Table><TableHead><TableRow><TableCell padding="checkbox"><Checkbox checked={selected.length === jobs.length} indeterminate={selected.length > 0 && selected.length < jobs.length} onChange={() => setSelected(selected.length === jobs.length ? [] : jobs.map(j => j.id))} /></TableCell><TableCell>Job</TableCell><TableCell>Vật liệu</TableCell><TableCell>Hạn nội bộ</TableCell><TableCell align="right">Khối lượng</TableCell></TableRow></TableHead>
                <TableBody>{jobs.map(job => <TableRow key={job.id} hover selected={selected.includes(job.id)}><TableCell padding="checkbox"><Checkbox checked={selected.includes(job.id)} onChange={() => toggle(job.id)} /></TableCell><TableCell><Typography fontFamily="monospace">{job.id.slice(0, 8).toUpperCase()}</Typography><Typography variant="caption" color="text.secondary">SL {job.quantity}</Typography></TableCell><TableCell>{job.materialCode}</TableCell><TableCell>{date(job.internalDueDate)}</TableCell><TableCell align="right">{job.weightGrams.toLocaleString('vi-VN')} g</TableCell></TableRow>)}</TableBody>
              </Table>}
          </Paper>

          {selected.length > 0 && <Paper variant="outlined" sx={{ p: 2.5, borderColor: 'primary.main' }}>
            <Typography variant="h6" fontWeight={750}>Preview trước khi niêm phong</Typography>
            {preview.isPending && <CircularProgress size={22} sx={{ mt: 2 }} />}
            {preview.data && <Stack spacing={1.25} sx={{ mt: 2 }}>
              <Stack direction="row" justifyContent="space-between"><Typography color="text.secondary">Tổng {selected.length} job</Typography><Typography fontWeight={700}>{preview.data.totalWeightGrams.toLocaleString('vi-VN')} g</Typography></Stack>
              <Stack direction="row" justifyContent="space-between"><Typography color="text.secondary">Zone / hạng cân</Typography><Typography>{preview.data.zone} · {weightClassLabel[preview.data.weightClass] ?? preview.data.weightClass}</Typography></Stack>
              <Stack direction="row" justifyContent="space-between"><Typography color="text.secondary">Công thức allowance</Typography><Typography>{money.format(preview.data.zoneRate)} × {preview.data.weightMultiplier}</Typography></Stack>
              <Divider /><Stack direction="row" justifyContent="space-between"><Typography fontWeight={700}>Phí hỗ trợ vận chuyển</Typography><Typography color="primary.main" fontWeight={800} fontSize="1.25rem">{money.format(preview.data.allowance)}</Typography></Stack>
              <Alert severity="info">Lô sẽ chạy ngày {date(planner.data.nextTransferDate)}. Sau khi seal, các job chuyển sang trạng thái đang vận chuyển và không thể thêm/bớt.</Alert>
              <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="flex-end" alignItems="center" gap={1.5}><Checkbox checked={confirmed} onChange={e => setConfirmed(e.target.checked)} /><Typography variant="body2">Tôi đã đối chiếu đủ hàng và khối lượng</Typography><Button variant="contained" startIcon={<LockRounded />} disabled={!confirmed || seal.isPending} onClick={handleSeal}>{seal.isPending ? 'Đang niêm phong…' : 'Seal lô & tạo waybill'}</Button></Stack>
            </Stack>}
          </Paper>}

          {planner.data.recentBatches.length > 0 && <Paper variant="outlined" sx={{ p: 2.5 }}><Typography variant="h6" fontWeight={750} sx={{ mb: 1.5 }}>Lô gần đây</Typography>{planner.data.recentBatches.map(batch => <Stack key={batch.id} direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" sx={{ py: 1.25, borderTop: '1px solid', borderColor: 'divider' }}><Box><Typography fontWeight={650}>{batch.waybillNumber}</Typography><Typography variant="body2" color="text.secondary">Chuyến {date(batch.scheduledTransferDate)} · {batch.totalWeightGrams.toLocaleString('vi-VN')} g</Typography></Box><Chip color="success" label={`${batch.status} · ${money.format(batch.transportAllowance)}`} /></Stack>)}</Paper>}
        </>}
      </Stack>
    </Box>
  )
}
