import { Stack, Typography } from '@mui/material'

export default function LabQueuePage() {
  return (
    <Stack spacing={2} sx={{ p: 3 }}>
      <Typography variant="h1">Hàng đợi sản xuất</Typography>
      <Typography color="text.secondary">
        Danh sách job được phân về lab sẽ hiển thị tại đây (Sprint 5).
      </Typography>
    </Stack>
  )
}
