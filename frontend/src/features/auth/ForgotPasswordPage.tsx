import { useState } from 'react'
import { Alert, Box, Button, Link, Stack, TextField, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [sent, setSent] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await apiClient.post('/auth/forgot-password', { email: email.trim() })
      setSent(true)
    } catch (reason) {
      setError(getApiErrorMessage(reason, 'Không thể gửi email. Vui lòng thử lại.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 3, bgcolor: 'background.default' }}>
      <Box component="form" onSubmit={submit} sx={{ width: '100%', maxWidth: 460, p: { xs: 3, sm: 5 }, border: '1px solid rgba(255,255,255,.12)', borderRadius: 4, bgcolor: 'background.paper' }}>
        <Stack spacing={2.5}>
          <Typography variant="h4" fontWeight={700}>Quên mật khẩu</Typography>
          <Typography color="text.secondary">Nhập email tài khoản. PrintGrid sẽ gửi liên kết đặt lại mật khẩu có hiệu lực trong 30 phút.</Typography>
          {sent && <Alert severity="success">Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi. Hãy kiểm tra cả thư rác.</Alert>}
          {error && <Alert severity="error">{error}</Alert>}
          {!sent && (
            <>
              <TextField required type="email" label="Email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
              <Button type="submit" variant="contained" size="large" disabled={submitting || !email.trim()}>{submitting ? 'ĐANG GỬI…' : 'GỬI LIÊN KẾT'}</Button>
            </>
          )}
          <Link component={RouterLink} to="/login" underline="hover">Quay lại đăng nhập</Link>
        </Stack>
      </Box>
    </Box>
  )
}
