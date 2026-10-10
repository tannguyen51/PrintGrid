import { useState } from 'react'
import {
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
  Avatar,
  Menu,
  MenuItem,
} from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, NotificationsOutlined, PersonOutlineRounded } from '@mui/icons-material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { landing } from '../../../shared/theme/landing'

/**
 * Navbar for a logged-in customer (07/10, trimmed 08/10).
 *   Logo (→ homepage) · Đặt in · Thư viện model · Đơn hàng · 🔔 · avatar ▾
 *
 * No "Trang chủ" item — the logo is the way home. No login/register buttons
 * either: visitors get PublicNavbar, so this component is logged-in only.
 * The bell is inert until the notifications REST contract lands (plan §A4).
 */

interface NavItem {
  label: string
  href: string
}

const CUSTOMER_NAV: NavItem[] = [
  { label: 'Đặt in', href: '/order/new' },
  { label: 'Thư viện model', href: '/models' },
  { label: 'Đơn hàng', href: '/orders' },
]

const linkSx = {
  color: landing.textMuted,
  fontWeight: 500,
  fontSize: '0.9rem',
  px: 1.5,
  py: 0.75,
  borderRadius: `${landing.radius}px`,
  '&:hover': { color: landing.text, bgcolor: 'rgba(255,255,255,0.05)' },
} as const

export function CustomerNavbar() {
  const { logout, user } = useAuth()
  const navigate = useNavigate()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

  const handleLogout = () => {
    setAnchorEl(null)
    setDrawerOpen(false)
    logout()
    navigate('/', { replace: true })
  }

  const go = (href: string) => {
    setDrawerOpen(false)
    navigate(href)
  }

  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1200,
        bgcolor: 'rgba(0,0,0,0.72)',
        backdropFilter: 'blur(10px)',
        borderBottom: `1px solid ${landing.hairline}`,
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.5 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          {/* Brand — the logo is the only route back to the homepage */}
          <Button
            component={RouterLink}
            to="/"
            onClick={() => setDrawerOpen(false)}
            aria-label="PrintGrid — về trang chủ"
            sx={{ p: 0, textTransform: 'none', minWidth: 0, '&:hover': { bgcolor: 'transparent' } }}
            disableRipple
          >
            <BrandGlyph size={34} color={landing.text} mutedColor={landing.textMuted} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 700, color: landing.text, lineHeight: 1.15 }}>
                PrintGrid
              </Typography>
            </Box>
          </Button>

          {/* Desktop nav */}
          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {CUSTOMER_NAV.map((item) => (
              <Button key={item.href} onClick={() => go(item.href)} sx={{ ...linkSx, textTransform: 'none' }}>
                {item.label}
              </Button>
            ))}
          </Stack>

          {/* Account actions */}
          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            <IconButton
              sx={{ color: landing.textMuted, '&:hover': { color: landing.text } }}
              aria-label="Thông báo (sắp có)"
              onClick={() => go('/account')}
            >
              <NotificationsOutlined />
            </IconButton>
            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ p: 0.5 }} aria-label="Menu tài khoản">
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 15 }}>
                {user?.email?.charAt(0).toUpperCase() || 'U'}
              </Avatar>
            </IconButton>
            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={() => setAnchorEl(null)}
              PaperProps={{
                sx: {
                  mt: 1,
                  minWidth: 200,
                  bgcolor: landing.card,
                  border: `1px solid ${landing.hairline}`,
                  borderRadius: `${landing.radius}px`,
                },
              }}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
            >
              <MenuItem onClick={() => { setAnchorEl(null); go('/account') }}>
                <PersonOutlineRounded fontSize="small" sx={{ mr: 1.25, color: landing.textMuted }} />
                Hồ sơ &amp; địa chỉ
              </MenuItem>
              <Divider sx={{ borderColor: landing.hairlineSoft }} />
              <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
                Đăng xuất
              </MenuItem>
            </Menu>
          </Stack>

          {/* Mobile hamburger */}
          <IconButton onClick={() => setDrawerOpen(true)} sx={{ display: { md: 'none' }, color: landing.text }} aria-label="Mở menu">
            <MenuRounded />
          </IconButton>
        </Stack>
      </Container>

      {/* Mobile drawer */}
      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} PaperProps={{ sx: { width: 280, bgcolor: landing.card } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: landing.text }} aria-label="Đóng menu">
            <CloseRounded />
          </IconButton>
        </Box>
        <List>
          {CUSTOMER_NAV.map((item) => (
            <ListItem key={item.href} disablePadding>
              <ListItemButton onClick={() => go(item.href)}>
                <ListItemText primary={item.label} sx={{ color: landing.text, '& .MuiListItemText-primary': { fontWeight: 500 } }} />
              </ListItemButton>
            </ListItem>
          ))}
          <ListItem disablePadding>
            <ListItemButton onClick={() => go('/account')}>
              <ListItemText primary="Hồ sơ & địa chỉ" sx={{ color: landing.text, '& .MuiListItemText-primary': { fontWeight: 500 } }} />
            </ListItemButton>
          </ListItem>
          <ListItem disablePadding sx={{ mt: 2 }}>
            <Box sx={{ px: 2, width: '100%' }}>
              <Button fullWidth variant="outlined" color="error" onClick={handleLogout}>
                Đăng xuất
              </Button>
            </Box>
          </ListItem>
        </List>
      </Drawer>
    </Box>
  )
}