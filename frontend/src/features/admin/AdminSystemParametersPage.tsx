import { useState } from 'react'
import { Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { SaveRounded } from '@mui/icons-material'

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const renderNumberField = (label: string, value: number, onChange: (v: number) => void) => (
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  <TextField fullWidth type="number" label={label} value={value}
    onChange={(e: any) => onChange(Number(e.target.value))} />
)

export default function AdminSystemParametersPage() {
  const [pinSurchargeDefault, setPinSurchargeDefault] = useState(1.15)
  const [minMachinesPerModel, setMinMachinesPerModel] = useState(3)
  const [minLabsAcrossMachines, setMinLabsAcrossMachines] = useState(2)
  const [customerResponseDeadlineHours, setCustomerResponseDeadlineHours] = useState(24)

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Box sx={{ maxWidth: 800, mx: 'auto', px: 3, py: 4 }}>
        <Typography variant="h4" fontWeight={800} mb={3}>Thông số hệ thống — Chỉ định model</Typography>

        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack spacing={3}>
            {renderNumberField('Hệ số phụ phí mặc định', pinSurchargeDefault, setPinSurchargeDefault)}
            {renderNumberField('Số máy tối thiểu mỗi model', minMachinesPerModel, setMinMachinesPerModel)}
            {renderNumberField('Số lab tối thiểu', minLabsAcrossMachines, setMinLabsAcrossMachines)}
            {renderNumberField('Thời hạn phản hồi (giờ)', customerResponseDeadlineHours, setCustomerResponseDeadlineHours)}
            <Button variant="contained" startIcon={<SaveRounded />}>Lưu thông số</Button>
          </Stack>
        </Paper>
      </Box>
    </Box>
  )
}
