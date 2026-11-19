import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, CircularProgress, Dialog, DialogActions, DialogContent, DialogTitle, Paper, Stack, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'

interface Proposal {
  token: string
  plan: { jobId: string; quantity: number; internalDueDate: string; expiresAtUtc: string
    batches: { labId: string; labName: string; machineId: string; machineName: string; quantity: number; plannedStartUtc: string; plannedEndUtc: string }[] }
}
const time = (value: string) => new Date(value).toLocaleString('vi-VN')

export function SplitDispatchDialog({ jobId, onClose }: { jobId: string | null; onClose: () => void }) {
  const client = useQueryClient()
  const proposal = useQuery({ queryKey: ['split-preview', jobId], enabled: !!jobId, retry: false, staleTime: 0,
    queryFn: () => apiClient.post<Proposal>(`/scheduling/jobs/${jobId}/split-preview`).then(r => r.data) })
  const confirm = useMutation({
    mutationFn: () => apiClient.post(`/scheduling/jobs/${jobId}/split-confirm`, { token: proposal.data!.token }),
    onSuccess: () => {
      void client.invalidateQueries({ queryKey: ['jobs'] })
      void client.invalidateQueries({ queryKey: ['ops'] })
      void client.invalidateQueries({ queryKey: ['ops-orders'] })
    },
  })
  const close = () => { confirm.reset(); onClose() }
  return <Dialog open={!!jobId} onClose={confirm.isPending ? undefined : close} fullWidth maxWidth="md">
    <DialogTitle>Chia số lượng cho nhiều xưởng</DialogTitle>
    <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
      <Typography>Kiểm tra số lượng, xưởng nhận và lịch dự kiến trước khi xác nhận. Giá đơn hàng giữ nguyên.</Typography>
      {proposal.isFetching && <CircularProgress aria-label="Đang tạo phương án" />}
      {proposal.isError && <Alert severity="warning">{getApiErrorMessage(proposal.error, 'Không tạo được phương án chia lab.')}</Alert>}
      {proposal.data && <>
        <Typography fontWeight={700}>{proposal.data.plan.quantity} sản phẩm · {proposal.data.plan.batches.length} xưởng</Typography>
        <Typography color="text.secondary">Phương án có hiệu lực đến {time(proposal.data.plan.expiresAtUtc)}.</Typography>
        {proposal.data.plan.batches.map(batch => <Paper key={batch.labId} variant="outlined" sx={{ p: 2 }}>
          <Typography fontWeight={700}>{batch.labName} · {batch.machineName}</Typography>
          <Typography>{batch.quantity} sản phẩm</Typography>
          <Typography>Bắt đầu: {time(batch.plannedStartUtc)}</Typography><Typography>Hoàn tất dự kiến: {time(batch.plannedEndUtc)}</Typography>
        </Paper>)}
        <Alert severity="info">Tổng số lượng: {proposal.data.plan.batches.reduce((sum, b) => sum + b.quantity, 0)}/{proposal.data.plan.quantity}. Đơn chỉ hoàn thành khi các phần đều đạt nghiệm thu.</Alert>
      </>}
      {confirm.isError && <Alert severity="error">{getApiErrorMessage(confirm.error, 'Không thể xác nhận phương án.')}</Alert>}
      {confirm.isSuccess && <Alert severity="success">Đã chia đủ số lượng và gán cho các xưởng.</Alert>}
    </Stack></DialogContent>
    <DialogActions><Button onClick={close} disabled={confirm.isPending}>Đóng</Button>
      {!confirm.isSuccess && <><Button onClick={() => { confirm.reset(); void proposal.refetch() }} disabled={proposal.isFetching || confirm.isPending}>Tạo lại phương án</Button>
        <Button variant="contained" disabled={!proposal.data || proposal.isFetching || proposal.isError || confirm.isPending || confirm.isError}
          onClick={() => confirm.mutate()}>{confirm.isPending ? 'Đang phân công…' : 'Xác nhận chia và phân công'}</Button></>}
    </DialogActions>
  </Dialog>
}
