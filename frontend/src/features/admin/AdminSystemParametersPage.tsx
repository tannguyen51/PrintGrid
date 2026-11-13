import { Alert, Box, Button, Grid, Paper, Skeleton, Stack, Typography } from '@mui/material'
import { ArrowForwardRounded, SettingsOutlined } from '@mui/icons-material'
import { Link } from 'react-router-dom'
import { useCommercialConfigurations } from './useAdminConfiguration'

/** These values are versioned by the commercial configuration API. */
export default function AdminSystemParametersPage() {
  const query = useCommercialConfigurations()
  const active = query.data?.find((configuration) => configuration.status === 'Active')
  const parameters = active?.parameters
  const fields = parameters ? [
    ['Kích thước lô tối đa', parameters.maxBatchSize, 'sản phẩm'],
    ['Ngân sách lập lịch', parameters.schedulingTimeBudgetSeconds, 'giây'],
    ['Ngưỡng rủi ro SLA', parameters.slaRiskThresholdHours, 'giờ'],
    ['Hiệu lực báo giá', parameters.quoteValidityHours, 'giờ'],
    ['Ngưỡng tự duyệt', parameters.autoLaneThreshold, ''],
    ['Phụ phí ưu tiên', parameters.prioritySurchargePercent, '%'],
  ] as const : []

  return <Stack spacing={3}>
    <Box><Typography variant="h1">Thông số hệ thống</Typography><Typography color="text.secondary">Các giới hạn vận hành từ phiên bản cấu hình thương mại đang áp dụng.</Typography></Box>
    {query.isError && <Alert severity="error" action={<Button onClick={() => { void query.refetch() }}>Thử lại</Button>}>Không tải được thông số hệ thống.</Alert>}
    <Paper variant="outlined" sx={{ p: { xs: 2.5, md: 3 } }}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="space-between" alignItems={{ sm: 'center' }}>
        <Box><Typography variant="h6">{active?.name ?? 'Cấu hình vận hành'}</Typography><Typography variant="body2" color="text.secondary">{active ? `Phiên bản ${active.versionNumber} · Đang áp dụng` : 'Thông số được quản lý theo từng phiên bản.'}</Typography></Box>
        <Button component={Link} to="/admin/commercial-configurations" variant="contained" endIcon={<ArrowForwardRounded />}>Quản lý phiên bản</Button>
      </Stack>
      {query.isLoading ? <Skeleton height={200} /> : parameters ? <Grid container spacing={2} sx={{ mt: 2 }}>
        {fields.map(([label, value, unit]) => <Grid size={{ xs: 12, sm: 6, lg: 4 }} key={label}>
          <Box sx={{ bgcolor: '#F8F9FC', border: '1px solid', borderColor: 'divider', borderRadius: 2, p: 2.5, height: '100%' }}>
            <Typography variant="body2" color="text.secondary">{label}</Typography>
            <Typography sx={{ fontSize: 23, fontWeight: 700, mt: 1 }}>{value.toLocaleString('vi-VN')} <Typography component="span" sx={{ fontSize: 12, color: 'text.secondary' }}>{unit}</Typography></Typography>
          </Box>
        </Grid>)}
      </Grid> : !query.isError ? <Stack spacing={1} alignItems="center" sx={{ py: 5, textAlign: 'center' }}><SettingsOutlined color="disabled" /><Typography>Chưa có phiên bản cấu hình đang áp dụng.</Typography><Typography variant="body2" color="text.secondary">Mở quản lý phiên bản để tạo và kích hoạt cấu hình.</Typography></Stack> : null}
    </Paper>
    <Alert severity="info">Để thay đổi thông số, tạo một phiên bản cấu hình thương mại mới và kích hoạt sau khi kiểm tra. Các báo giá đã tạo vẫn giữ phiên bản cấu hình ban đầu.</Alert>
  </Stack>
}
