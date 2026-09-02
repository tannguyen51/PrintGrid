import { Stack, Typography } from '@mui/material'

export default function SchedulingBoardPage() {
  return (
    <Stack spacing={2} sx={{ p: 3 }}>
      <Typography variant="h1">Bảng điều phối</Typography>
      <Typography color="text.secondary">
        Timeline máy in và điểm phân công sẽ hiển thị tại đây (Sprint 3-4).
      </Typography>
    </Stack>
  )
}
