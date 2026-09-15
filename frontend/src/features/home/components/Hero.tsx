import { Box, Button, Container, Stack, Typography } from '@mui/material'
import { ArrowForward } from '@mui/icons-material'

const stats = [
  { value: '50+', label: 'Lab liên kết' },
  { value: '95%', label: 'Giao đúng hẹn' },
  { value: '1000+', label: 'Đơn hàng / tháng' },
  { value: '<5%', label: 'Tỉ lệ lỗi' },
]

interface HeroProps {
  onLogin: () => void
  onRegister: () => void
}

/**
 * Home hero: headline + CTA + key metrics, over a dark premium backdrop.
 */
export function Hero({ onLogin, onRegister }: HeroProps) {
  return (
    <Box sx={{ position: 'relative', overflow: 'hidden' }} component="section">
      {/* backdrop sheen */}
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
          background:
            'radial-gradient(1100px 620px at 75% -10%, rgba(139,92,246,0.16), transparent 55%), radial-gradient(900px 520px at 10% 115%, rgba(10,132,255,0.12), transparent 55%)',
        }}
      />

      <Container maxWidth="lg" sx={{ pt: { xs: 9, md: 14 }, pb: { xs: 7, md: 10 }, position: 'relative' }}>
        <Stack spacing={4} alignItems="center" textAlign="center">
          <Typography
            component="span"
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 1,
              px: 2,
              py: 0.75,
              borderRadius: 999,
              border: '1px solid rgba(139,92,246,0.35)',
              bgcolor: 'rgba(139,92,246,0.08)',
              color: '#A78BFA',
              fontSize: '0.82rem',
              fontWeight: 600,
              letterSpacing: '0.03em',
            }}
          >
            <Box sx={{ width: 7, height: 7, borderRadius: 999, bgcolor: '#8B5CF6', boxShadow: '0 0 10px #8B5CF6' }} />
            Nền tảng in 3D phân tán — một điểm đến, mạng lưới lab toàn quốc
          </Typography>

          <Typography
            variant="h1"
            component="h1"
            sx={{
              fontSize: { xs: '2.3rem', sm: '3.2rem', md: '4rem' },
              fontWeight: 800,
              lineHeight: 1.12,
              letterSpacing: '-0.02em',
              maxWidth: 860,
              color: 'common.white',
            }}
          >
            Đặt in 3D{' '}
            <Box component="span" sx={{ color: '#8B5CF6' }}>
              thông minh
            </Box>
            , giao đúng hẹn theo{' '}
            <Box component="span" sx={{ color: '#8B5CF6' }}>
              năng lực thực tế
            </Box>
          </Typography>

          <Typography
            variant="h2"
            component="p"
            sx={{ fontSize: { xs: '1.05rem', md: '1.2rem' }, fontWeight: 400, color: 'text.secondary', maxWidth: 640, lineHeight: 1.7 }}
          >
            Tải model, nhận báo giá tức thì với ngày giao chính xác, và theo dõi đơn hàng theo thời gian
            thực — trong khi mạng lưới lab độc lập của chúng tôi chia sẻ năng lực in làm một.
          </Typography>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems="center">
            <Button
              onClick={onRegister}
              variant="contained"
              color="primary"
              size="large"
              endIcon={<ArrowForward />}
              sx={{ px: 4, py: 1.6, fontSize: '1rem', fontWeight: 700 }}
            >
              Bắt đầu in ngay
            </Button>
            <Button
              onClick={onLogin}
              variant="outlined"
              color="inherit"
              size="large"
              sx={{ px: 4, py: 1.6, fontSize: '1rem', borderColor: 'rgba(255,255,255,0.25)', color: 'text.primary' }}
            >
              Đăng nhập
            </Button>
          </Stack>

          {/* metric strip */}
          <Stack
            direction="row"
            spacing={{ xs: 3, md: 6 }}
            justifyContent="center"
            flexWrap="wrap"
            useFlexGap
            sx={{ mt: { xs: 2, md: 4 }, pt: { xs: 2, md: 3 }, borderTop: '1px solid rgba(255,255,255,0.08)', width: '100%' }}
          >
            {stats.map((s) => (
              <Stack key={s.label} alignItems="center" spacing={0.25}>
                <Typography sx={{ fontSize: { xs: '1.6rem', md: '2rem' }, fontWeight: 800, color: 'common.white' }}>
                  {s.value}
                </Typography>
                <Typography sx={{ fontSize: '0.82rem', color: 'rgba(255,255,255,0.55)' }}>{s.label}</Typography>
              </Stack>
            ))}
          </Stack>
        </Stack>
      </Container>
    </Box>
  )
}