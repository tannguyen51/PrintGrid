import { useState } from 'react'
import {
  Box,
  Button,
  Container,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, LogoutRounded } from '@mui/icons-material'

const NAV_ITEMS = [
  { label: 'Trang chủ', href: '/' },
  { label: 'Tính năng', href: '/#tinh-nang' },
  { label: 'Cách hoạt động', href: '/#cach-hoat-dong' },
  { label: 'Bắt đầu', href: '/#bat-dau' },
]

function scrollToHash(hash: string) {
  if (!hash) return
  const el = document.getElementById(hash)
  if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

interface HomeHeaderProps {
  onLogin: () => void
  onRegister: () => void
}

/**
 * Sticky navbar for the public homepage: brand left, anchor links center,
 * auth actions right. On mobile the links collapse into a drawer.
 * Auth actions open the AuthDialog modal instead of navigating.
 */
export function HomeHeader({ onLogin, onRegister }: HomeHeaderProps) {
  const { isAuthenticated, logout, user } = useAuth()
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)

  const handleLogout = () => {
    setDrawerOpen(false)
    logout()
    navigate('/', { replace: true })
  }

  const roles = user?.roles ?? []
  const isCustomer = roles.includes('Customer')
  const isLab = roles.some((r) => r === 'LabManager' || r === 'LabOperator')
  const isHub = roles.some((r) => r === 'HubQC' || r === 'HubFulfillment')
  const isOps = roles.some((r) => r === 'OpsManager' || r === 'Admin')

  const go = (href: string) => {
    setDrawerOpen(false)
    if (href.startsWith('/#')) {
      const hash = href.slice(2)
      if (window.location.pathname === '/') {
        scrollToHash(hash)
      } else {
        navigate('/')
        // Wait for the route to mount before scrolling.
        setTimeout(() => scrollToHash(hash), 80)
      }
    } else {
      navigate(href)
    }
  }

  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1200,
        bgcolor: 'rgba(5,5,5,0.82)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid',
        borderColor: 'rgba(255,255,255,0.08)',
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.5 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          {/* ── Brand ── */}
          <Button component={RouterLink} to="/" onClick={() => setDrawerOpen(false)} sx={{ p: 0, textTransform: 'none', minWidth: 0 }} disableRipple>
            <BrandGlyph size={34} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 700, color: 'common.white', lineHeight: 1.15 }}>
                PrintGrid
              </Typography>
              <Typography sx={{ fontSize: '0.55rem', color: 'rgba(255,255,255,0.5)', letterSpacing: '0.14em', textTransform: 'uppercase', lineHeight: 1 }}>
                Mạng lưới in 3D thông minh
              </Typography>
            </Box>
          </Button>

          {/* ── Desktop nav ── */}
          <Stack direction="row" spacing={1} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {NAV_ITEMS.map((item) => (
              <Button
                key={item.label}
                onClick={() => go(item.href)}
                color="inherit"
                sx={{
                  color: 'text.secondary',
                  fontWeight: 500,
                  fontSize: '0.9rem',
                  px: 1.5,
                  py: 0.75,
                  borderRadius: 2,
                  '&:hover': { color: 'text.primary', bgcolor: 'rgba(255,255,255,0.05)' },
                }}
              >
                {item.label}
              </Button>
            ))}
          </Stack>

          {/* ── Auth actions ── */}
          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {isAuthenticated ? (
              <>
                {isCustomer && (
                  <>
                    <Button onClick={() => go('/models')} color="inherit" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75 }}>
                      Thư viện model
                    </Button>
                    <Button onClick={() => go('/orders')} color="inherit" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75 }}>
                      Đơn hàng
                    </Button>
                  </>
                )}
                {isLab && (
                  <Button onClick={() => go('/lab/queue')} color="inherit" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75 }}>
                    Hàng đợi sản xuất
                  </Button>
                )}
                {isHub && (
                  <Button onClick={() => go('/hub/qc')} color="inherit" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75 }}>
                    Kiểm tra chất lượng
                  </Button>
                )}
                {isOps && (
                  <Button onClick={() => go('/scheduling')} color="inherit" sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75 }}>
                    Bảng điều phối
                  </Button>
                )}
                <Button variant="contained" color="primary" onClick={() => go(isCustomer ? '/models' : isOps ? '/scheduling' : '/lab/queue')} sx={{ px: 2.5, py: 0.9, fontSize: '0.9rem' }}>
                  {isCustomer ? 'Bắt đầu in' : isOps ? 'Điều phối' : 'Vào việc'}
                </Button>
                <Button
                  onClick={handleLogout}
                  color="inherit"
                  startIcon={<LogoutRounded fontSize="small" />}
                  sx={{ color: 'text.secondary', fontWeight: 500, fontSize: '0.9rem', px: 1.2, py: 0.75, ml: 0.5 }}
                >
                  Đăng xuất
                </Button>
              </>
            ) : (
              <>
                <Button
                  onClick={() => { setDrawerOpen(false); onLogin() }}
                  color="inherit"
                  sx={{ color: 'text.primary', px: 2, py: 0.9, fontSize: '0.9rem' }}
                >
                  Đăng nhập
                </Button>
                <Button
                  onClick={() => { setDrawerOpen(false); onRegister() }}
                  variant="contained"
                  color="primary"
                  sx={{ px: 2.5, py: 0.9, fontSize: '0.9rem' }}
                >
                  Tạo tài khoản
                </Button>
              </>
            )}
          </Stack>

          {/* ── Mobile hamburger ── */}
          <IconButton
            onClick={() => setDrawerOpen(true)}
            sx={{ display: { md: 'none' }, color: 'text.primary' }}
            aria-label="Mở menu"
          >
            <MenuRounded />
          </IconButton>
        </Stack>
      </Container>

      {/* ── Mobile drawer ── */}
      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} PaperProps={{ sx: { width: 280, bgcolor: '#0A0A0A' } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: 'text.primary' }} aria-label="Đóng menu">
            <CloseRounded />
          </IconButton>
        </Box>
        <List>
          {NAV_ITEMS.map((item) => (
            <ListItem key={item.label} disablePadding>
              <ListItemButton onClick={() => go(item.href)}>
                <ListItemText
                  primary={item.label}
                  primaryTypographyProps={{ color: 'text.primary', fontWeight: 600 }}
                />
              </ListItemButton>
            </ListItem>
          ))}
          <ListItem disablePadding sx={{ mt: 1 }}>
            <Box sx={{ px: 2, width: '100%' }}>
              {isAuthenticated ? (
                <Stack spacing={1.25}>
                  {isCustomer && (
                    <>
                      <Button fullWidth variant="contained" color="primary" onClick={() => go('/models')}>
                        Bắt đầu in
                      </Button>
                      <Button fullWidth variant="outlined" color="inherit" onClick={() => { setDrawerOpen(false); go('/orders') }} sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}>
                        Đơn hàng
                      </Button>
                    </>
                  )}
                  {isLab && (
                    <Button fullWidth variant="contained" color="primary" onClick={() => go('/lab/queue')}>
                      Hàng đợi sản xuất
                    </Button>
                  )}
                  {isHub && (
                    <Button fullWidth variant="contained" color="primary" onClick={() => go('/hub/qc')}>
                      Kiểm tra chất lượng
                    </Button>
                  )}
                  {isOps && (
                    <Button fullWidth variant="contained" color="primary" onClick={() => go('/scheduling')}>
                      Bảng điều phối
                    </Button>
                  )}
                  <Button fullWidth variant="outlined" color="error" onClick={handleLogout} startIcon={<LogoutRounded />}>
                    Đăng xuất
                  </Button>
                </Stack>
              ) : (
                <Stack spacing={1.25}>
                  <Button fullWidth variant="outlined" color="inherit" onClick={() => { setDrawerOpen(false); onLogin() }} sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}>
                    Đăng nhập
                  </Button>
                  <Button fullWidth variant="contained" color="primary" onClick={() => { setDrawerOpen(false); onRegister() }}>
                    Tạo tài khoản
                  </Button>
                </Stack>
              )}
            </Box>
          </ListItem>
        </List>
      </Drawer>
    </Box>
  )
}

/** Small grid glyph — same motif as the login brand mark. */
export function BrandGlyph({ size = 40 }: { size?: number }) {
  return (
    <Box
      sx={{
        width: size,
        height: size,
        borderRadius: 2.5,
        display: 'grid',
        placeItems: 'center',
        background: 'linear-gradient(140deg, #8B5CF6, #7C3AED)',
        boxShadow: '0 8px 22px rgba(139,92,246,0.4)',
        flexShrink: 0,
      }}
    >
      <Box sx={{ display: 'grid', gridTemplateColumns: `repeat(2, ${size * 0.25}px)`, gap: size * 0.065 }}>
        <Box sx={{ width: size * 0.25, height: size * 0.25, borderRadius: 1, bgcolor: 'rgba(255,255,255,0.4)' }} />
        <Box sx={{ width: size * 0.25, height: size * 0.25, borderRadius: 1, bgcolor: 'common.white' }} />
        <Box sx={{ width: size * 0.25, height: size * 0.25, borderRadius: 1, bgcolor: 'rgba(5,5,5,0.85)' }} />
        <Box sx={{ width: size * 0.25, height: size * 0.25, borderRadius: 1, bgcolor: 'rgba(255,255,255,0.4)' }} />
      </Box>
    </Box>
  )
}