import { useQuery } from '@tanstack/react-query'
import { Alert, Box, LinearProgress, Stack, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { JOB_LABELS } from '../jobs/jobTypes'

interface ProductionItem {
  id: string; quantity: number; completedQuantity: number
  batches: { id: string; quantity: number; status: string; reprintIndex: number; plannedEndUtc: string | null }[]
}
export function OrderProductionProgress({ orderId }: { orderId: string }) {
  const query = useQuery({ queryKey: ['order-production', orderId], refetchInterval: 30_000,
    queryFn: () => apiClient.get<ProductionItem[]>(`/orders/${orderId}/production`).then(r => r.data) })
  return <Stack spacing={2} sx={{ my: 3 }}><Typography variant="h6">Tiến độ từng phần</Typography>
    {query.isLoading && <LinearProgress aria-label="Đang tải tiến độ" />}
    {query.isError && <Alert severity="error">Không tải được tiến độ sản xuất.</Alert>}
    {query.data?.map((item, index) => <Box key={item.id}>
      <Typography fontWeight={700}>Mẫu {index + 1}: đã nghiệm thu {item.completedQuantity}/{item.quantity} sản phẩm</Typography>
      {!item.batches.length && <Typography variant="body2" color="text.secondary">Chờ xác nhận thanh toán và phân công sản xuất.</Typography>}
      {item.batches.map((batch, batchIndex) => <Box key={batch.id} sx={{ mt: 1 }}>
        <Typography variant="body2">Phần {batchIndex + 1} · {batch.quantity} sản phẩm · {(JOB_LABELS as Record<string, string>)[batch.status] ?? batch.status}{batch.reprintIndex > 0 ? ` · In lại lần ${batch.reprintIndex}` : ''}</Typography>
        {batch.plannedEndUtc && <Typography variant="body2" color="text.secondary">Dự kiến sản xuất xong: {new Date(batch.plannedEndUtc).toLocaleString('vi-VN')}</Typography>}
      </Box>)}
    </Box>)}
  </Stack>
}
