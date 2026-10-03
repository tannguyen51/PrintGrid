import { useEffect, useState, useRef } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { Box, Typography, CircularProgress, Button, Alert } from '@mui/material'
import { apiClient } from '../../shared/api/apiClient'
import { CheckCircleOutline, ErrorOutline } from '@mui/icons-material'

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = searchParams.get('email')
  const token = searchParams.get('token')

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  
  const hasFetched = useRef(false)

  useEffect(() => {
    if (!email || !token) {
      setStatus('error')
      setErrorMessage('Liên kết xác thực không hợp lệ (thiếu email hoặc token).')
      return
    }

    if (hasFetched.current) return
    hasFetched.current = true

    apiClient
      .post('/auth/verify-email', { email, token })
      .then(() => {
        setStatus('success')
      })
      .catch((err) => {
        setStatus('error')
        setErrorMessage(err.response?.data?.error?.message || 'Xác thực thất bại. Token có thể đã hết hạn hoặc không hợp lệ.')
      })
  }, [email, token])

  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
      }}
    >
      <Box
        sx={{
          width: '100%',
          maxWidth: 420,
          bgcolor: 'background.paper',
          border: '1px solid',
          borderColor: 'rgba(255,255,255,0.12)',
          borderRadius: 4,
          p: 5,
          textAlign: 'center',
          boxShadow: (t) => t.custom.loginCardShadow,
        }}
      >
        {status === 'loading' && (
          <>
            <CircularProgress size={48} sx={{ mb: 3 }} />
            <Typography variant="h6" fontWeight={600}>
              Đang xác thực email...
            </Typography>
          </>
        )}

        {status === 'success' && (
          <>
            <CheckCircleOutline color="success" sx={{ fontSize: 64, mb: 2 }} />
            <Typography variant="h5" fontWeight={700} mb={1}>
              Xác thực thành công!
            </Typography>
            <Typography color="text.secondary" mb={4}>
              Email của bạn đã được xác thực thành công. Bạn đã có thể đặt đơn hàng.
            </Typography>
            <Button variant="contained" fullWidth size="large" onClick={() => navigate('/login')}>
              Đăng nhập ngay
            </Button>
          </>
        )}

        {status === 'error' && (
          <>
            <ErrorOutline color="error" sx={{ fontSize: 64, mb: 2 }} />
            <Typography variant="h5" fontWeight={700} mb={1}>
              Xác thực thất bại
            </Typography>
            <Alert severity="error" sx={{ mb: 4, textAlign: 'left' }}>
              {errorMessage}
            </Alert>
            <Button variant="outlined" fullWidth size="large" onClick={() => navigate('/')}>
              Về trang chủ
            </Button>
          </>
        )}
      </Box>
    </Box>
  )
}
