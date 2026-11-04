import { Box, Container, Divider, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { palette } from '../../../shared/theme/tokens'

interface HomeFooterProps {
  onLogin: () => void
  onRegister: () => void
}

import { useAuth } from '../../../app/AuthContext'

export function HomeFooter({ onLogin, onRegister }: HomeFooterProps) {
  const { isAuthenticated } = useAuth()
  return (
    <Box
      component="footer"
      sx={{
        borderTop: `1px solid ${palette.border.hairline}`,
        py: 5,
        bgcolor: 'rgba(248,249,252,0.72)',
        backdropFilter: 'blur(8px)',
      }}
    >
      <Container maxWidth="lg">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} alignItems={{ xs: 'flex-start', sm: 'center' }} justifyContent="space-between">
          <Stack direction="row" alignItems="center" spacing={1.5}>
            <BrandGlyph size={36} />
            <Box>
              <Typography sx={{ fontWeight: 800, color: 'text.primary', fontSize: '1rem', letterSpacing: '-0.02em' }}>PrintGrid</Typography>
              <Typography sx={{ fontSize: '0.72rem', color: 'text.secondary' }}>
                Distributed 3D printing fulfillment & scheduling platform
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={3} sx={{ flexWrap: 'wrap', rowGap: 1 }}>
            <FooterRoute label="Kho mẫu 3D" to="/samples" />
            <FooterRoute label="Bảng giá" to="/pricing" />
            <FooterRoute label="Mạng lưới" to="/network" />
            <FooterRoute label="Hướng dẫn" to="/guides" />
            {isAuthenticated ? (
              <FooterRoute label="Đơn hàng" to="/orders" />
            ) : (
              <>
                <FooterLink label="Đăng nhập" onClick={onLogin} />
                <FooterLink label="Tạo tài khoản" onClick={onRegister} />
              </>
            )}
          </Stack>
        </Stack>

        <Divider sx={{ my: 3.5 }} />
        <Typography sx={{ fontSize: '0.78rem', color: 'text.disabled', textAlign: 'center' }}>
          © {new Date().getFullYear()} PrintGrid · Capstone Project · FPT University
        </Typography>
      </Container>
    </Box>
  )
}

function FooterRoute({ label, to }: { label: string; to: string }) {
  return (
    <Typography
      component={RouterLink}
      to={to}
      sx={{ color: 'text.secondary', fontSize: '0.88rem', textDecoration: 'none', '&:hover': { color: 'primary.main' } }}
    >
      {label}
    </Typography>
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
        '&:hover': { color: 'primary.main' },
      }}
    >
      {label}
    </Typography>
  )
}
