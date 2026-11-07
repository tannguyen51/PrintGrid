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
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, PersonOutlineRounded } from '@mui/icons-material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { NotificationBell } from '../../../shared/components/NotificationBell'
import { palette } from '../../../shared/theme/tokens'

/**
 * Navbar for a logged-in customer.
 *   Logo (→ homepage) · Đặt in · Thư viện model · Đơn hàng · 🔔 · avatar ▾
 *
 * Light metallic shell: translucent white bar, silver hairline, purple active
 * pill. No "Trang chủ" item — the logo is the way home.
 */

interface NavItem {
  label: string
  href: string
}

const CUSTOMER_NAV: NavItem[] = [
  { label: 'Đặt in', href: '/order/new' },
  { label: 'Thư viện model', href: '/models' },
  { label: 'Đơn hàng', href: '/orders' },
  { label: 'Yêu cầu thiết kế', href: '/design-request' },
]

const linkSx = {
  color: palette.text.secondary,
  fontWeight: 500,
  fontSize: '0.9rem',
  px: 1.5,
  py: 0.75,
  borderRadius: '10px',
  transition: 'background-color 160ms ease, color 160ms ease',
  '&:hover': { color: palette.text.primary, bgcolor: palette.purple.soft },
} as const

export function CustomerNavbar() {
  const { logout, user } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()

  /** Current link: exact match or a sub-page of it ("/models/…" highlights "Thư viện model"). */
  const isActive = (href: string) =>
    pathname === href || (href !== '/order/new' && pathname.startsWith(`${href}/`))
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

  const handleLogout = () => {
    setAnchorEl(null)
    setDrawerOpen(false)
    // AuthContext.logout() clears the session and returns to the guest homepage.
    logout()
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
        bgcolor: 'rgba(255,255,255,0.84)',
        backdropFilter: 'blur(16px)',
        borderBottom: `1px solid ${palette.border.hairline}`,
        boxShadow: '0 1px 0 rgba(37,38,58,0.02)',
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.25 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          {/* Brand — the logo is the only route back to the homepage */}
          <Button
            component={RouterLink}
            to="/"
            onClick={() => setDrawerOpen(false)}
            aria-label="PrintGrid — về trang chủ"
            sx={{ p: 0, textTransform: 'none', minWidth: 0, '&:hover': { bgcolor: 'transparent', boxShadow: 'none' } }}
            disableRipple
          >
            <BrandGlyph size={32} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 800, color: palette.text.primary, lineHeight: 1.15, letterSpacing: '-0.02em' }}>
                PrintGrid
              </Typography>
            </Box>
          </Button>

          {/* Desktop nav */}
          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {CUSTOMER_NAV.map((item) => (
              <Button
                key={item.href}
                onClick={() => go(item.href)}
                sx={{
                  ...linkSx,
                  textTransform: 'none',
                  color: isActive(item.href) ? palette.purple.deep : palette.text.secondary,
                  fontWeight: isActive(item.href) ? 700 : 500,
                  bgcolor: isActive(item.href) ? palette.purple.soft : 'transparent',
                  boxShadow: isActive(item.href) ? 'inset 0 0 0 1px rgba(111,85,255,0.18)' : 'none',
                  '&:hover': { bgcolor: palette.purple.soft, boxShadow: 'none' },
                }}
              >
                {item.label}
              </Button>
            ))}
          </Stack>

          {/* Notifications and account controls form one action cluster. */}
          <Stack direction="row" spacing={1} alignItems="center" sx={{ flexShrink: 0 }}>
            <NotificationBell />
            <IconButton
              onClick={(e) => setAnchorEl(e.currentTarget)}
              sx={{ p: 0.5, display: { xs: 'none', md: 'inline-flex' } }}
              aria-label="Menu tài khoản"
            >
              <Avatar sx={{ width: 34, height: 34, fontSize: 14 }}>
                {user?.email?.charAt(0).toUpperCase() || 'U'}
              </Avatar>
            </IconButton>
            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={() => setAnchorEl(null)}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
            >
              <MenuItem onClick={() => { setAnchorEl(null); go('/account') }}>
                <PersonOutlineRounded fontSize="small" sx={{ mr: 1.25, color: 'text.secondary' }} />
                Hồ sơ &amp; địa chỉ
              </MenuItem>
              <Divider sx={{ my: 0.5 }} />
              <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
                Đăng xuất
              </MenuItem>
            </Menu>
            {/* Mobile hamburger */}
            <IconButton onClick={() => setDrawerOpen(true)} sx={{ display: { md: 'none' }, color: 'text.primary' }} aria-label="Mở menu">
              <MenuRounded />
            </IconButton>
          </Stack>
        </Stack>
      </Container>

      {/* Mobile drawer */}
      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} slotProps={{ paper: { sx: { width: 290 } } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} aria-label="Đóng menu">
            <CloseRounded />
          </IconButton>
        </Box>
        <List sx={{ px: 1 }}>
          {CUSTOMER_NAV.map((item) => (
            <ListItem key={item.href} disablePadding>
              <ListItemButton onClick={() => go(item.href)} selected={isActive(item.href)} sx={{ mb: 0.25 }}>
                <ListItemText
                  primary={item.label}
                  slotProps={{ primary: { fontWeight: isActive(item.href) ? 700 : 500, fontSize: '0.92rem' } }}
                />
              </ListItemButton>
            </ListItem>
          ))}
          <ListItem disablePadding>
            <ListItemButton onClick={() => go('/account')} sx={{ mb: 0.25 }}>
              <ListItemText primary="Hồ sơ & địa chỉ" slotProps={{ primary: { fontWeight: 500, fontSize: '0.92rem' } }} />
            </ListItemButton>
          </ListItem>
          <ListItem disablePadding sx={{ mt: 2 }}>
            <Box sx={{ px: 1, width: '100%' }}>
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
