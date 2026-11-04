import { useState } from 'react'
import { Box, Button, Divider, IconButton, Menu, MenuItem } from '@mui/material'
import { KeyboardArrowDownRounded, MenuRounded } from '@mui/icons-material'
import { useLocation, useNavigate } from 'react-router-dom'
import { landing, PillButton } from './landingTheme'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { palette } from '../../../shared/theme/tokens'
import './landing.css'

/**
 * Public top bar. Light translucent shell: white glass over the lavender wash,
 * silver hairline, purple active state, gradient CTA.
 */
const NAV_ITEMS: { label: string; to: string }[] = [
  { label: 'Kho mẫu 3D', to: '/samples' },
  { label: 'Bảng giá & vật liệu', to: '/pricing' },
  { label: 'Mạng lưới xưởng', to: '/network' },
  { label: 'Hướng dẫn / FAQ', to: '/guides' },
]

interface PublicNavbarProps {
  onLogin: () => void
  onRegister: () => void
}

export function PublicNavbar({ onLogin, onRegister }: PublicNavbarProps) {
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [mobileMenu, setMobileMenu] = useState<null | HTMLElement>(null)

  const isActive = (to: string) => pathname === to || pathname.startsWith(`${to}/`)

  const goHome = () => {
    if (pathname === '/') window.scrollTo({ top: 0, behavior: 'smooth' })
    else navigate('/')
  }

  const go = (to: string) => {
    setMobileMenu(null)
    navigate(to)
  }

  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1200,
        height: 68,
        px: { xs: '20px', md: '40px', lg: '104px' },
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        bgcolor: 'rgba(255,255,255,0.82)',
        backdropFilter: 'blur(16px)',
        borderBottom: `1px solid ${landing.hairline}`,
        boxShadow: '0 1px 0 rgba(37,38,58,0.02)',
      }}
    >
      {/* ── Wordmark ── */}
      <Box
        onClick={goHome}
        sx={{ display: 'flex', alignItems: 'center', gap: 1.25, cursor: 'pointer', userSelect: 'none' }}
      >
        <BrandGlyph size={28} />
        <Box component="span" sx={{ fontSize: 20, fontWeight: 800, letterSpacing: '-0.025em', color: landing.text }}>
          PrintGrid
        </Box>
      </Box>

      {/* ── Center links ── */}
      <Box sx={{ display: { xs: 'none', lg: 'flex' }, alignItems: 'center', gap: '6px' }}>
        {NAV_ITEMS.map((item) => (
          <Button
            key={item.to}
            onClick={() => go(item.to)}
            disableRipple
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
              px: 1.5,
              py: 0.75,
              minWidth: 0,
              borderRadius: '10px',
              color: isActive(item.to) ? palette.purple.deep : landing.textMuted,
              fontSize: 14,
              fontWeight: isActive(item.to) ? 700 : 500,
              textTransform: 'none',
              bgcolor: isActive(item.to) ? palette.purple.soft : 'transparent',
              transition: 'color .15s ease, background-color .15s ease',
              '&:hover': { color: palette.purple.deep, bgcolor: palette.purple.soft, boxShadow: 'none' },
            }}
          >
            {item.label}
            <KeyboardArrowDownRounded sx={{ fontSize: 13, opacity: 0.65 }} />
          </Button>
        ))}
      </Box>

      {/* ── Account actions ── */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        <Button
          onClick={onLogin}
          disableRipple
          sx={{
            display: { xs: 'none', sm: 'inline-flex' },
            px: 1.5,
            minWidth: 0,
            color: landing.text,
            fontSize: 14,
            fontWeight: 600,
            textTransform: 'none',
            borderRadius: '10px',
            '&:hover': { bgcolor: palette.purple.soft, color: palette.purple.deep, boxShadow: 'none' },
          }}
        >
          Đăng nhập
        </Button>
        <PillButton onClick={onRegister} sx={{ display: { xs: 'none', sm: 'inline-flex' }, height: 42, px: '20px' }}>
          Bắt đầu
        </PillButton>
        <IconButton
          onClick={(event) => setMobileMenu(event.currentTarget)}
          aria-label="Mở menu"
          sx={{ display: { lg: 'none' } }}
        >
          <MenuRounded />
        </IconButton>
      </Box>

      {/* ── Mobile menu ── */}
      <Menu
        anchorEl={mobileMenu}
        open={Boolean(mobileMenu)}
        onClose={() => setMobileMenu(null)}
        slotProps={{ paper: { sx: { mt: 1, minWidth: 240 } } }}
      >
        {NAV_ITEMS.map((item) => (
          <MenuItem
            key={item.to}
            onClick={() => go(item.to)}
            selected={isActive(item.to)}
          >
            {item.label}
          </MenuItem>
        ))}
        <Divider sx={{ my: 0.5 }} />
        <MenuItem onClick={() => { setMobileMenu(null); onLogin() }}>Đăng nhập</MenuItem>
        <MenuItem onClick={() => { setMobileMenu(null); onRegister() }}>Tạo tài khoản</MenuItem>
      </Menu>
    </Box>
  )
}
