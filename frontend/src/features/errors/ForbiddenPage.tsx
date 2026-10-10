import { Link as RouterLink, useLocation } from 'react-router-dom'
import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { useAuth } from '../../app/AuthContext'

interface ForbiddenState {
  from?: string
  requiredRoles?: string[]
  userRoles?: string[]
}

/**
 * 403 landing for ProtectedRoute role mismatches (the old target of a
 * non-existent /forbidden route). Public so an unauthenticated visit
 * cannot bounce into another blank page.
 *
 * ProtectedRoute passes the mismatch context through navigation state, and we
 * print it here — which page was blocked, which roles it needs, which roles the
 * account actually has — so "không có quyền truy cập" is diagnosable, not a dead end.
 */
export default function ForbiddenPage() {
  const location = useLocation()
  const state = (location.state ?? {}) as ForbiddenState
  const { user, logout } = useAuth()

  // Khi mở thẳng /forbidden (không có state), tự suy từ user hiện tại.
  const userRoles = state.userRoles ?? user?.roles ?? []
  const attempted = state.from ?? (document.referrer ? new URL(document.referrer).pathname : undefined)

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 3 }}>
      <Box sx={{ textAlign: 'center', maxWidth: 560, width: '100%' }}>
        <Typography variant="h1" sx={{ fontSize: '3rem', fontWeight: 800, color: 'text.primary', lineHeight: 1.1 }}>
          403
        </Typography>
        <Typography variant="h5" fontWeight={700} sx={{ mt: 1, mb: 1 }}>
          Không có quyền truy cập
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          Tài khoản của bạn không có quyền xem trang này. Chi tiết bên dưới cho biết vì sao bị chặn.
        </Typography>

        <Alert severity="warning" sx={{ mb: 3, textAlign: 'left' }}>
          <Stack spacing={1}>
            {attempted && (
              <Typography variant="body2" color="text.primary">
                Trang bị chặn: <strong>{attempted}</strong>
              </Typography>
            )}
            <Typography variant="body2">
              Trang cần role:{' '}
              {state.requiredRoles?.length ? (
                state.requiredRoles.map((r) => <Chip key={r} label={r} size="small" sx={{ ml: 0.5 }} />)
              ) : (
                <em>không rõ (mở trực tiếp trang 403)</em>
              )}
            </Typography>
            <Typography variant="body2">
              Tài khoản {user ? user.email : '(chưa đăng nhập)'} có role:{' '}
              {userRoles.length > 0 ? (
                userRoles.map((r) => (
                  <Chip key={r} label={r} size="small" color={r === 'Customer' ? 'info' : 'primary'} sx={{ ml: 0.5 }} />
                ))
              ) : (
                <em>rỗng</em>
              )}
            </Typography>
            {user && state.requiredRoles?.length && (
              <Typography variant="body2" color="text.secondary">
                Nếu role này là sai so với dự kiến: tài khoản có thể được cấp role sau khi đăng nhập — token
                cũ vẫn mang role cũ. Đăng xuất rồi đăng nhập lại để lấy role mới.
              </Typography>
            )}
          </Stack>
        </Alert>

        <Stack direction="row" spacing={2} justifyContent="center">
          <Button component={RouterLink} to="/" variant="contained" size="large">
            Về trang chủ
          </Button>
          {user && (
            <Button
              component={RouterLink}
              to="/"
              variant="outlined"
              color="inherit"
              size="large"
              onClick={logout}
              sx={{ borderColor: 'rgba(255,255,255,0.2)' }}
            >
              Đăng nhập lại
            </Button>
          )}
        </Stack>
      </Box>
    </Box>
  )
}
