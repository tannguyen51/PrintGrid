import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Typography } from '@mui/material'

/**
 * Catch-all 404 for unknown paths. Public, same reasoning as ForbiddenPage.
 */
export default function NotFoundPage() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 3 }}>
      <Box sx={{ textAlign: 'center', maxWidth: 480 }}>
        <Typography variant="h1" sx={{ fontSize: '3rem', fontWeight: 800, color: 'text.primary', lineHeight: 1.1 }}>
          404
        </Typography>
        <Typography variant="h5" fontWeight={700} sx={{ mt: 1, mb: 1 }}>
          Không tìm thấy trang
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 3 }}>
          Đường dẫn không tồn tại hoặc đã thay đổi. Kiểm tra lại URL hoặc quay về trang chủ để tiếp tục.
        </Typography>
        <Button component={RouterLink} to="/" variant="contained" size="large">
          Về trang chủ
        </Button>
      </Box>
    </Box>
  )
}
