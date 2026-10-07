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
  Avatar,
  Menu,
  MenuItem,
  Badge
} from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, NotificationsOutlined } from '@mui/icons-material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'

interface CustomerNavbarProps {
  onLogin?: () => void
  onRegister?: () => void
}

export function CustomerNavbar({ onLogin, onRegister }: CustomerNavbarProps = {}) {
  const { isAuthenticated, logout, user } = useAuth()
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
        bgcolor: 'rgba(5,5,5,0.82)',
        backdropFilter: 'blur(10px)',
        borderBottom: '1px solid',
        borderColor: 'rgba(255,255,255,0.08)',
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.5 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          {/* Brand */}
          <Button component={RouterLink} to="/" onClick={() => setDrawerOpen(false)} sx={{ p: 0, textTransform: 'none', minWidth: 0 }} disableRipple>
            <BrandGlyph size={34} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 700, color: 'common.white', lineHeight: 1.15 }}>
                PrintGrid
              </Typography>
            </Box>
          </Button>

          {/* Desktop nav */}
          <Stack direction="row" spacing={1} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            <Button onClick={() => go('/')} color="inherit" sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}>
              Trang chủ
            </Button>
            {isAuthenticated && (
              <>
                <Button onClick={() => go('/models')} color="inherit" sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}>Đặt in</Button>
                <Button onClick={() => go('/models')} color="inherit" sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}>Thư viện model</Button>
                <Button onClick={() => go('/orders')} color="inherit" sx={{ color: 'text.secondary', '&:hover': { color: 'text.primary' } }}>Đơn hàng</Button>
              </>
            )}
          </Stack>

          {/* Auth actions */}
          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {isAuthenticated ? (
              <>
                <IconButton color="inherit" sx={{ color: 'text.secondary' }}>
                  <Badge variant="dot" color="primary">
                    <NotificationsOutlined />
                  </Badge>
                </IconButton>
                <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ p: 0.5 }}>
                  <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
                    {user?.email?.charAt(0).toUpperCase() || 'U'}
                  </Avatar>
                </IconButton>
                <Menu
                  anchorEl={anchorEl}
                  open={Boolean(anchorEl)}
                  onClose={() => setAnchorEl(null)}
                  PaperProps={{ sx: { mt: 1, minWidth: 180 } }}
                  transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                  anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                >
                  <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>Đăng xuất</MenuItem>
                </Menu>
              </>
            ) : (
              <>
                <Button onClick={() => { setDrawerOpen(false); onLogin ? onLogin() : navigate('/login') }} color="inherit" sx={{ color: 'text.primary' }}>
                  Đăng nhập
                </Button>
                <Button onClick={() => { setDrawerOpen(false); onRegister ? onRegister() : navigate('/register') }} variant="contained" color="primary">
                  Đăng ký
                </Button>
              </>
            )}
          </Stack>

          {/* Mobile hamburger */}
          <IconButton onClick={() => setDrawerOpen(true)} sx={{ display: { md: 'none' }, color: 'text.primary' }} aria-label="Mở menu">
            <MenuRounded />
          </IconButton>
        </Stack>
      </Container>

      {/* Mobile drawer */}
      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} PaperProps={{ sx: { width: 280, bgcolor: '#0A0A0A' } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: 'text.primary' }}>
            <CloseRounded />
          </IconButton>
        </Box>
        <List>
          <ListItem disablePadding>
            <ListItemButton onClick={() => go('/')}><ListItemText primary="Trang chủ" /></ListItemButton>
          </ListItem>
          {isAuthenticated && (
            <>
              <ListItem disablePadding><ListItemButton onClick={() => go('/models')}><ListItemText primary="Đặt in" /></ListItemButton></ListItem>
              <ListItem disablePadding><ListItemButton onClick={() => go('/models')}><ListItemText primary="Thư viện model" /></ListItemButton></ListItem>
              <ListItem disablePadding><ListItemButton onClick={() => go('/orders')}><ListItemText primary="Đơn hàng" /></ListItemButton></ListItem>
            </>
          )}
          <ListItem disablePadding sx={{ mt: 2 }}>
            <Box sx={{ px: 2, width: '100%' }}>
              {isAuthenticated ? (
                <Button fullWidth variant="outlined" color="error" onClick={handleLogout}>Đăng xuất</Button>
              ) : (
                <Stack spacing={1.25}>
                  <Button fullWidth variant="outlined" color="inherit" onClick={() => { setDrawerOpen(false); onLogin ? onLogin() : navigate('/login') }}>Đăng nhập</Button>
                  <Button fullWidth variant="contained" color="primary" onClick={() => { setDrawerOpen(false); onRegister ? onRegister() : navigate('/register') }}>Đăng ký</Button>
                </Stack>
              )}
            </Box>
          </ListItem>
        </List>
      </Drawer>
    </Box>
  )
}
