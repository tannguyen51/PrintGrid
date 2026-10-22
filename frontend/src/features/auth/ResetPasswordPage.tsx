import { useMemo, useState } from 'react'
import { Alert, Box, Button, Link, Stack, TextField, Typography } from '@mui/material'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'

export default function ResetPasswordPage() {
  const [params] = useSearchParams()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''
  const validLink = useMemo(() => Boolean(email && token), [email, token])
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [completed, setCompleted] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (password.length < 8) return setError('Mật khẩu phải có ít nhất 8 ký tự.')
    if (password !== confirmPassword) return setError('Mật khẩu xác nhận không khớp.')
    setSubmitting(true)
    setError(null)
    try {
      await apiClient.post('/auth/reset-password', { email, token, newPassword: password })
      setCompleted(true)
    } catch (reason) {
      setError(getApiErrorMessage(reason, 'Liên kết không hợp lệ hoặc đã hết hạn.'))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 3, bgcolor: 'background.default' }}>
      <Box component="form" onSubmit={submit} sx={{ width: '100%', maxWidth: 460, p: { xs: 3, sm: 5 }, border: '1px solid rgba(255,255,255,.12)', borderRadius: 4, bgcolor: 'background.paper' }}>
        <Stack spacing={2.5}>
          <Typography variant="h4" fontWeight={700}>Đặt lại mật khẩu</Typography>
          {!validLink && <Alert severity="error">Liên kết đặt lại mật khẩu không hợp lệ.</Alert>}
          {completed && <Alert severity="success">Mật khẩu đã được cập nhật. Bạn có thể đăng nhập ngay.</Alert>}
          {error && <Alert severity="error">{error}</Alert>}
          {validLink && !completed && (
            <>
              <Typography color="text.secondary">Tài khoản: {email}</Typography>
              <TextField required type="password" label="Mật khẩu mới" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} helperText="Ít nhất 8 ký tự" />
              <TextField required type="password" label="Xác nhận mật khẩu" autoComplete="new-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} />
              <Button type="submit" variant="contained" size="large" disabled={submitting}>{submitting ? 'ĐANG CẬP NHẬT…' : 'CẬP NHẬT MẬT KHẨU'}</Button>
            </>
          )}
          <Link component={RouterLink} to="/login" underline="hover">Đi đến đăng nhập</Link>
        </Stack>
      </Box>
    </Box>
  )
}
