import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Typography } from '@mui/material'

/**
 * 403 landing for ProtectedRoute role mismatches (the old target of a
 * non-existent /forbidden route). Public so an unauthenticated visit
 * cannot bounce into another blank page.
 */
export default function ForbiddenPage() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 3 }}>
      <Box sx={{ textAlign: 'center', maxWidth: 480 }}>
        <Typography variant="h1" sx={{ fontSize: '3rem', fontWeight: 800, color: 'text.primary', lineHeight: 1.1 }}>
          403
        </Typography>
        <Typography variant="h5" fontWeight={700} sx={{ mt: 1, mb: 1 }}>
          Không có quyền truy cập
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          Tài khoản của bạn không có quyền xem trang này. Hãy quay về trang chủ hoặc đăng nhập bằng tài khoản phù hợp.
        </Typography>
        <Button component={RouterLink} to="/" variant="contained" size="large">
          Về trang chủ
        </Button>
      </Box>
    </Box>
  )
}
