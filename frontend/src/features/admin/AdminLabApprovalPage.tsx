import { Alert, Box, Button, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { CheckCircleRounded, FactoryRounded } from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useApproveLab, useLabs } from '../ops/useLabs'

export default function AdminLabApprovalPage() {
  const labs = useLabs(true)
  const approve = useApproveLab()
  const items = labs.data ?? []
  const pendingCount = items.filter((lab) => lab.status === 'Pending').length

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ maxWidth: 1180, mx: 'auto', px: 3, py: 4 }} spacing={3}>
        <Stack direction="row" spacing={1.5} alignItems="center">
          <PageBackButton />
          <Box>
            <Typography variant="h4" fontWeight={800}>Duyệt kích hoạt xưởng</Typography>
            <Typography color="text.secondary">Kiểm tra hồ sơ, máy in và vật liệu trước khi cho phép xưởng nhận job từ hệ thống.</Typography>
          </Box>
        </Stack>

        <Stack direction="row" spacing={1} alignItems="center">
          <Chip color={pendingCount > 0 ? 'warning' : 'default'} label={`${pendingCount} xưởng chờ duyệt`} />
          <Typography variant="body2" color="text.secondary">Admin chỉ phê duyệt kích hoạt; vận hành mạng lưới thuộc OpsManager.</Typography>
        </Stack>

        {labs.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
        {labs.isError && <Alert severity="error">Không tải được danh sách xưởng.</Alert>}
        {approve.isError && <Alert severity="error">{getApiErrorMessage(approve.error, 'Không thể duyệt xưởng.')}</Alert>}

        {!labs.isLoading && items.length === 0 && (
          <Paper variant="outlined" sx={{ p: 6, textAlign: 'center', borderStyle: 'dashed' }}>
            <FactoryRounded sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có xưởng nào gửi hồ sơ đăng ký.</Typography>
          </Paper>
        )}

        <Stack spacing={2}>
          {items.map((lab) => (
            (() => {
              const missing = [
                lab.machines.length === 0 ? 'máy' : null,
                lab.materialStockCount === 0 ? 'vật liệu' : null,
                !lab.operatingHours ? 'lịch hoạt động' : null,
                lab.transitDaysToHub < 0 ? 'thời gian trung chuyển' : null,
              ].filter(Boolean)
              const statusLabel = lab.status === 'Active'
                ? 'Đã kích hoạt'
                : lab.status === 'Rejected'
                  ? 'Đã từ chối'
                  : lab.status === 'Inactive'
                    ? 'Đang tạm dừng'
                    : 'Chờ duyệt'
              return (
            <Paper key={lab.id} variant="outlined" sx={{ p: 2.5, borderRadius: 3, opacity: lab.isActive ? 0.7 : 1 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'center' }} gap={2}>
                <Box>
                  <Stack direction="row" spacing={1} alignItems="center" mb={0.5}>
                    <Typography variant="h6" fontWeight={750}>{lab.name}</Typography>
                    <Chip
                      size="small"
                      color={lab.status === 'Active' ? 'success' : lab.status === 'Rejected' ? 'error' : lab.status === 'Pending' ? 'warning' : 'default'}
                      label={statusLabel}
                    />
                  </Stack>
                  <Typography color="text.secondary">{lab.city} · {lab.machines.length} máy · {lab.materialStockCount} dòng vật liệu · vận chuyển về hub {lab.transitDaysToHub} ngày</Typography>
                  <Typography variant="body2" color="text.secondary">Lịch: {lab.operatingHours || 'Chưa khai'}</Typography>
                  {lab.machines.length > 0 && <Typography variant="body2" color="text.secondary" mt={0.75}>{lab.machines.map((machine) => `${machine.name} (${machine.model})`).join(' · ')}</Typography>}
                  {lab.status === 'Rejected' && lab.rejectionReason && <Alert severity="error" sx={{ mt: 1 }}>{lab.rejectionReason}</Alert>}
                  {missing.length > 0 && lab.status === 'Pending' && <Alert severity="warning" sx={{ mt: 1 }}>Hồ sơ còn thiếu: {missing.join(', ')}.</Alert>}
                </Box>
                {lab.status === 'Pending' && (
                  <Button variant="contained" startIcon={<CheckCircleRounded />} disabled={approve.isPending || missing.length > 0} onClick={() => approve.mutate(lab.id)}>
                    {missing.length > 0 ? 'Hồ sơ chưa đầy đủ' : 'Duyệt kích hoạt'}
                  </Button>
                )}
              </Stack>
            </Paper>
              )
            })()
          ))}
        </Stack>
      </Stack>
    </Box>
  )
}
