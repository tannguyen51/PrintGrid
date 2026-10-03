import { Box, Button, Container, Stack, Typography } from '@mui/material'
import { ArrowForward } from '@mui/icons-material'

interface CTABandProps {
  onLogin: () => void
  onRegister: () => void
}

export function CTABand({ onLogin, onRegister }: CTABandProps) {
  return (
    <Box component="section" id="bat-dau" sx={{ py: { xs: 7, md: 11 }, scrollMarginTop: 90 }}>
      <Container maxWidth="lg">
        <Box
          sx={{
            position: 'relative',
            overflow: 'hidden',
            borderRadius: 4,
            p: { xs: 5, md: 8 },
            textAlign: 'center',
            border: '1px solid rgba(139,92,246,0.3)',
            background:
              'radial-gradient(800px 400px at 50% 0%, rgba(139,92,246,0.18), transparent 60%), #0A0A0A',
          }}
        >
          <Typography sx={{ fontSize: { xs: '1.6rem', md: '2.2rem' }, fontWeight: 800, color: 'common.white' }}>
            Sẵn sàng in ý tưởng đầu tiên của bạn chưa?
          </Typography>
          <Typography sx={{ mt: 1.5, mb: 4, color: 'text.secondary', fontSize: { xs: '1rem', md: '1.1rem' } }}>
            Tạo tài khoản miễn phí và nhận báo giá ngay trong vài phút.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="center">
            <Button
              onClick={onRegister}
              variant="contained"
              color="primary"
              size="large"
              endIcon={<ArrowForward />}
              sx={{ px: 4.5, py: 1.6, fontSize: '1rem', fontWeight: 700 }}
            >
              Tạo tài khoản miễn phí
            </Button>
            <Button
              onClick={onLogin}
              variant="outlined"
              color="inherit"
              size="large"
              sx={{ px: 4.5, py: 1.6, borderColor: 'rgba(255,255,255,0.25)', color: 'text.primary' }}
            >
              Đăng nhập
            </Button>
          </Stack>
        </Box>
      </Container>
    </Box>
  )
}