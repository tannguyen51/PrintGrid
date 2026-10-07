import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { PageBackButton } from '../../shared/components/PageBackButton'
import type { Quote } from './quoteTypes'

export default function QuoteReviewPage() {
  const client = useQueryClient()
  const [edits, setEdits] = useState<Record<string, { price: string; date: string; reason: string }>>({})
  const queue = useQuery({
    queryKey: ['quote-reviews'],
    queryFn: () => apiClient.get<Quote[]>('/quote-reviews').then(r => r.data),
  })
  const approve = useMutation({
    mutationFn: ({ id, ...body }: { id: string; totalPrice: number; promisedDeliveryDate: string; reason?: string }) =>
      apiClient.post(`/quote-reviews/${id}/approve`, body),
    onSuccess: () => client.invalidateQueries({ queryKey: ['quote-reviews'] }),
  })

  const value = (q: Quote) => edits[q.id] ?? {
    price: String(q.engineTotalAmount ?? q.totalPrice),
    date: q.enginePromisedDeliveryDate ?? q.promisedDeliveryDate,
    reason: '',
  }
  const update = (q: Quote, patch: Partial<ReturnType<typeof value>>) =>
    setEdits(current => ({ ...current, [q.id]: { ...value(q), ...patch } }))

  return (
    <Box sx={{ maxWidth: 960, mx: 'auto', p: { xs: 2, md: 4 } }}>
      <Stack direction="row" spacing={1.5} alignItems="flex-start" sx={{ mb: 3 }}>
        <PageBackButton />
        <Box>
          <Typography variant="h4" fontWeight={800} mb={1}>Duyệt báo giá</Typography>
          <Typography color="text.secondary">Engine draft chờ thẩm định. Giá chỉ được chỉnh trong biên độ cấu hình; mọi thay đổi phải có lý do.</Typography>
        </Box>
      </Stack>
      {queue.isLoading && <CircularProgress />}
      {queue.isError && <Alert severity="error">Không tải được hàng đợi báo giá.</Alert>}
      {queue.data?.length === 0 && <Alert severity="success">Không có draft đang chờ duyệt.</Alert>}
      <Stack spacing={2}>
        {queue.data?.map(q => {
          const edit = value(q)
          const changed = Number(edit.price) !== (q.engineTotalAmount ?? q.totalPrice) || edit.date !== (q.enginePromisedDeliveryDate ?? q.promisedDeliveryDate)
          return (
            <Paper key={q.id} variant="outlined" sx={{ p: 2.5 }}>
              <Stack spacing={2}>
                <Typography fontWeight={700}>#{q.id.slice(0, 8)} · {q.items[0]?.materialCode} · {q.items[0]?.quantity} sản phẩm</Typography>
                <Typography variant="body2" color="text.secondary">Cơ sở xếp lịch: {q.placementBasis}</Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                  <TextField label="Tổng giá" type="number" value={edit.price} onChange={e => update(q, { price: e.target.value })} fullWidth />
                  <TextField label="Ngày giao" type="date" value={edit.date} onChange={e => update(q, { date: e.target.value })} InputLabelProps={{ shrink: true }} fullWidth />
                </Stack>
                <TextField label={changed ? 'Lý do điều chỉnh (bắt buộc)' : 'Lý do / ghi chú'} value={edit.reason} onChange={e => update(q, { reason: e.target.value })} required={changed} fullWidth />
                <Button variant="contained" disabled={approve.isPending || !Number(edit.price) || (changed && !edit.reason.trim())}
                  onClick={() => approve.mutate({ id: q.id, totalPrice: Number(edit.price), promisedDeliveryDate: edit.date, reason: edit.reason.trim() || undefined })}>
                  Duyệt và phát hành
                </Button>
              </Stack>
            </Paper>
          )
        })}
      </Stack>
    </Box>
  )
}
