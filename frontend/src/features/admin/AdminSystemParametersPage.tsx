import { useState, useCallback } from 'react'
import { Alert, Box, Button, Paper, Stack, TextField, Typography, Grid, type ChangeEvent } from '@mui/material'
import { SaveRounded } from '@mui/icons-material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'

interface SystemParameters {
  pinSurchargeDefault: number
  minMachinesPerModel: number
  minLabsAcrossMachines: number
  customerResponseDeadlineHours: number
}

const defaults: SystemParameters = {
  pinSurchargeDefault: 1.15,
  minMachinesPerModel: 3,
  minLabsAcrossMachines: 2,
  customerResponseDeadlineHours: 24,
}

export default function AdminSystemParametersPage() {
  const [form, setForm] = useState<SystemParameters>(defaults)
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const updateField = useCallback((key: keyof SystemParameters, value: any) => {
    setForm(f => ({ ...f, [key]: typeof value === 'string' ? Number(value) : value }))
  }, [])

  const save = async () => {
    try {
      setLoading(true)
      await apiClient.put('/api/v1/admin/system-parameters', form)
      setMessage('Đã cập nhật thông số hệ thống.')
      setTimeout(() => setMessage(''), 3000)
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không thể cập nhật thông số.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Box sx={{ maxWidth: 800, mx: 'auto', px: 3, py: 4 }}>
        <Typography variant="h4" fontWeight={800} mb={3}>Thông số hệ thống — Chỉ định model</Typography>

        {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
        {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}

        <Paper variant="outlined" sx={{ p: 3 }}>
          <Stack spacing={3}>
            <Typography variant="h6">Hệ số phụ phí chỉ định model (h_chỉ_định)</Typography>
            {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
            <TextField fullWidth type="number" label="Hệ số mặc định" step="0.01" min={1}
              value={form.pinSurchargeDefault} onChange={(e: ChangeEvent<HTMLInputElement>) => updateField('pinSurchargeDefault', e.target.value)}
              helperText="Nhân giá cuối khi khách chọn model cụ thể. Mặc định 1.15" />

            <Typography variant="h6" mt={2}>Ngưỡng hiển thị model trong danh sách chỉ định (BR-04)</Typography>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 6 }}>
                {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                <TextField fullWidth type="number" label="Số máy tối thiểu mỗi model"
                  value={form.minMachinesPerModel} onChange={(e: ChangeEvent<HTMLInputElement>) => updateField('minMachinesPerModel', e.target.value)}
                  helperText="Model chỉ hiện nếu có ≥ N máy đã duyệt" />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
                <TextField fullWidth type="number" label="Số lab tối thiểu"
                  value={form.minLabsAcrossMachines} onChange={(e: ChangeEvent<HTMLInputElement>) => updateField('minLabsAcrossMachines', e.target.value)}
                  helperText="Các máy phải thuộc ≥ M lab khác nhau" />
              </Grid>
            </Grid>

            {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
            <TextField fullWidth type="number" label="Thời hạn khách phản hồi (giờ)"
              value={form.customerResponseDeadlineHours} onChange={(e: ChangeEvent<HTMLInputElement>) => updateField('customerResponseDeadlineHours', e.target.value)}
              helperText="Giờ trước khi chuyển đơn sang trạng thái chờ khi hết máy cùng model" />

            <Button variant="contained" startIcon={<SaveRounded />} onClick={save} disabled={loading}>
              Lưu thông số
            </Button>
          </Stack>
        </Paper>
      </Box>
    </Box>
  )
}
