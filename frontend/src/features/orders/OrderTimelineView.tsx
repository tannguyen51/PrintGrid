import { Box, Chip, CircularProgress, Divider, Stack, Step, StepContent, StepLabel, Stepper, Typography } from '@mui/material'
import { WarningRounded } from '@mui/icons-material'
import { useOrderTimeline } from './useOrderTimeline'

const fmtDate = (d: string) => new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })

export function OrderTimelineView({ orderId }: { orderId: string }) {
  const { data: timeline, isLoading, isError } = useOrderTimeline(orderId)

  if (isLoading) return <Box sx={{ display: 'grid', placeItems: 'center', p: 4 }}><CircularProgress /></Box>
  if (isError || !timeline) return <Typography color="error" sx={{ p: 4 }}>Không tải được thông tin đơn hàng</Typography>

  const activeStep = timeline.stages.findIndex(s => s.isCurrent)

  return (
    <Stack spacing={3}>
      <Stack direction="row" alignItems="center" spacing={1}>
        <Typography sx={{ fontSize: '1.25rem', fontWeight: 800, color: 'text.primary' }}>
          {timeline.orderNumber}
        </Typography>
        {timeline.isDelayed && (
          <Chip
            size="small"
            color="error"
            icon={<WarningRounded fontSize="small" />}
            label="Giao trễ"
          />
        )}
      </Stack>

      <Typography variant="body2" color="text.secondary">
        Dự kiến giao: {fmtDate(timeline.promisedDeliveryDate)}
      </Typography>

      <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />

      <Stepper activeStep={activeStep === -1 ? timeline.stages.length : activeStep} orientation="vertical">
        {timeline.stages.map((stage) => (
          <Step key={stage.stageName}>
            <StepLabel>
              <Typography sx={{ fontWeight: stage.isCurrent ? 700 : 400, color: stage.isCurrent ? 'primary.main' : 'text.primary' }}>
                {stage.stageName}
              </Typography>
            </StepLabel>
            <StepContent>
              {stage.completedAt ? (
                <Typography variant="caption" color="text.secondary">
                  Hoàn thành: {new Date(stage.completedAt).toLocaleString('vi-VN')}
                </Typography>
              ) : stage.isCurrent ? (
                <Typography variant="caption" color="info.main">
                  Đang tiến hành...
                </Typography>
              ) : null}
            </StepContent>
          </Step>
        ))}
      </Stepper>

      <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />

      <Typography variant="body2" sx={{ fontWeight: 700, color: 'text.primary' }}>Chi tiết từng món ({timeline.items.length})</Typography>
      <Stack spacing={1.5}>
        {timeline.items.map(item => (
          <Box key={item.id} sx={{ borderRadius: 2, border: '1px solid rgba(255,255,255,0.08)', p: 1.5 }}>
            <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
              <Box>
                <Typography sx={{ fontSize: '0.9rem', fontWeight: 600, color: 'text.primary' }}>
                  {item.modelName}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Số lượng: {item.quantity}
                </Typography>
              </Box>
              <Chip size="small" label={item.status} color={item.status === 'Failed' ? 'error' : item.status === 'Completed' ? 'success' : 'info'} />
            </Stack>
          </Box>
        ))}
      </Stack>
    </Stack>
  )
}
