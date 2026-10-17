import { useState } from 'react'
import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'

type DateChangeRequest = {
  token: string
  originalDeliveryDate: string
  proposedDeliveryDate: string
  reasonCode: 'CustomerRequested' | 'ForceMajeure' | 'ReprintDeadlineUnrecoverable'
  reason: string
  status: 'Proposed' | 'Approved' | 'Rejected' | 'Expired'
}

const reasonLabels: Record<DateChangeRequest['reasonCode'], string> = {
  CustomerRequested: 'Thay đổi theo yêu cầu của khách hàng',
  ForceMajeure: 'Sự kiện bất khả kháng',
  ReprintDeadlineUnrecoverable: 'Không thể giữ hạn sau ít nhất hai lần in lại',
}

const fmt = (value: string) => new Date(`${value}T00:00:00`).toLocaleDateString('vi-VN')

export default function DateChangeDecisionPage() {
  const { token = '' } = useParams()
  const [message, setMessage] = useState<string | null>(null)
  const request = useQuery({
    queryKey: ['date-change-request', token],
    queryFn: () => apiClient.get<DateChangeRequest>(`/date-change-requests/${token}`).then(r => r.data),
    enabled: Boolean(token),
  })
  const decide = useMutation({
    mutationFn: (approve: boolean) => apiClient
      .post<DateChangeRequest>(`/date-change-requests/${token}/decision`, { approve })
      .then(r => r.data),
    onSuccess: (data) => {
      request.refetch()
      setMessage(data.status === 'Approved'
        ? 'Đã đồng ý. Ngày giao mới đã được cập nhật và ghi audit.'
        : 'Đã từ chối. Ngày giao không thay đổi và yêu cầu hoàn tiền đã được đưa vào hàng chờ.')
    },
  })

  if (request.isLoading) return <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh' }}><CircularProgress /></Box>
  if (request.isError || !request.data) return <Container maxWidth="sm" sx={{ py: 8 }}><Alert severity="error">{getApiErrorMessage(request.error, 'Không thể mở đề nghị đổi ngày.')}</Alert></Container>

  const data = request.data
  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: { xs: 3, md: 5 }, borderRadius: 3 }}>
        <Stack spacing={3}>
          <Typography variant="h4" fontWeight={800}>Xác nhận thay đổi ngày giao</Typography>
          <Typography color="text.secondary">PrintGrid chỉ thay đổi ngày đã cam kết sau khi bạn đồng ý.</Typography>
          <Box>
            <Typography variant="body2" color="text.secondary">Ngày đã cam kết</Typography>
            <Typography variant="h6">{fmt(data.originalDeliveryDate)}</Typography>
          </Box>
          <Box>
            <Typography variant="body2" color="text.secondary">Ngày được đề xuất</Typography>
            <Typography variant="h6" color="warning.main">{fmt(data.proposedDeliveryDate)}</Typography>
          </Box>
          <Alert severity="info"><strong>{reasonLabels[data.reasonCode]}</strong><br />{data.reason}</Alert>
          {message && <Alert severity="success">{message}</Alert>}
          {decide.isError && <Alert severity="error">{getApiErrorMessage(decide.error)}</Alert>}
          {data.status === 'Proposed' ? (
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <Button variant="contained" disabled={decide.isPending} onClick={() => decide.mutate(true)}>Đồng ý ngày mới</Button>
              <Button variant="outlined" color="error" disabled={decide.isPending} onClick={() => decide.mutate(false)}>Từ chối và yêu cầu hoàn tiền</Button>
            </Stack>
          ) : <Alert severity={data.status === 'Approved' ? 'success' : 'warning'}>Đề nghị này đã được xử lý: {data.status}.</Alert>}
        </Stack>
      </Paper>
    </Container>
  )
}
