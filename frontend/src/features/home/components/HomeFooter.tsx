import { Box, Container, Divider, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { BrandGlyph } from './HomeHeader'

interface HomeFooterProps {
  onLogin: () => void
  onRegister: () => void
}

export function HomeFooter({ onLogin, onRegister }: HomeFooterProps) {
  return (
    <Box component="footer" sx={{ borderTop: '1px solid rgba(255,255,255,0.08)', py: 5, bgcolor: 'background.default' }}>
      <Container maxWidth="lg">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} alignItems={{ xs: 'flex-start', sm: 'center' }} justifyContent="space-between">
          <Stack direction="row" alignItems="center" spacing={1.5}>
            <BrandGlyph size={36} />
            <Box>
              <Typography sx={{ fontWeight: 700, color: 'common.white', fontSize: '1rem' }}>PrintGrid</Typography>
              <Typography sx={{ fontSize: '0.72rem', color: 'rgba(255,255,255,0.45)' }}>
                Distributed 3D printing fulfillment & scheduling platform
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={3}>
            <FooterLink label="Đăng nhập" onClick={onLogin} />
            <FooterLink label="Tạo tài khoản" onClick={onRegister} />
            <Typography
              component={RouterLink}
              to="/orders"
              sx={{ color: 'text.secondary', fontSize: '0.88rem', textDecoration: 'none', '&:hover': { color: 'text.primary' } }}
            >
              Đơn hàng
            </Typography>
          </Stack>
        </Stack>

        <Divider sx={{ my: 3.5, borderColor: 'rgba(255,255,255,0.08)' }} />
        <Typography sx={{ fontSize: '0.78rem', color: 'rgba(255,255,255,0.4)', textAlign: 'center' }}>
          © {new Date().getFullYear()} PrintGrid · Capstone Project · FPT University
        </Typography>
      </Container>
    </Box>
  )
}

function FooterLink({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <Typography
      component="button"
      type="button"
      onClick={onClick}
      sx={{
        p: 0,
        border: 'none',
        background: 'none',
        cursor: 'pointer',
        color: 'text.secondary',
        fontSize: '0.88rem',
        fontFamily: 'inherit',
        '&:hover': { color: 'text.primary' },
      }}
    >
      {label}
    </Typography>
  )
}