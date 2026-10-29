import { useState } from 'react'
import { Box, Button, Divider, IconButton, Menu, MenuItem } from '@mui/material'
import { KeyboardArrowDownRounded, MenuRounded } from '@mui/icons-material'
import { useLocation, useNavigate } from 'react-router-dom'
import { landing, PillButton } from './landingTheme'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import './landing.css'

/**
 * Public top bar (Figma #36:4 / #36:7). The five Figma labels pointed at on-page
 * anchors while the pages did not exist; they now resolve to real routes.
 * "Chi tiết mẫu" was dropped from the top level — a sample detail is always reached
 * from a card (`/samples/:slug`), never from the navbar.
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
        height: 60,
        px: { xs: '20px', md: '40px', lg: '104px' },
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        bgcolor: 'rgba(0,0,0,0.72)',
        backdropFilter: 'blur(10px)',
      }}
    >
      {/* ── Wordmark ── */}
      <Box
        onClick={goHome}
        sx={{ display: 'flex', alignItems: 'center', gap: 1.25, cursor: 'pointer', userSelect: 'none' }}
      >
        <BrandGlyph size={26} color={landing.text} mutedColor={landing.textMuted} />
        <Box component="span" sx={{ fontSize: 20, fontWeight: 600, letterSpacing: '-0.01em', color: landing.text }}>
          PrintGrid
        </Box>
      </Box>

      {/* ── Center links ── */}
      <Box sx={{ display: { xs: 'none', lg: 'flex' }, alignItems: 'center', gap: '30px' }}>
        {NAV_ITEMS.map((item) => (
          <Button
            key={item.to}
            onClick={() => go(item.to)}
            disableRipple
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: '5px',
              p: 0,
              minWidth: 0,
              color: isActive(item.to) ? landing.text : landing.textMuted,
              fontSize: 14,
              fontWeight: isActive(item.to) ? 600 : 400,
              textTransform: 'none',
              transition: 'color .15s ease',
              '&:hover': { color: landing.text, bgcolor: 'transparent' },
            }}
          >
            {item.label}
            <KeyboardArrowDownRounded sx={{ fontSize: 12, opacity: 0.8 }} />
          </Button>
        ))}
      </Box>

      {/* ── Account actions (Figma #36:28, gap 24) ── */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: '24px' }}>
        <Button
          onClick={onLogin}
          disableRipple
          sx={{
            display: { xs: 'none', sm: 'inline-flex' },
            p: 0,
            minWidth: 0,
            color: landing.text,
            fontSize: 14,
            fontWeight: 400,
            textTransform: 'none',
            '&:hover': { bgcolor: 'transparent', opacity: 0.75 },
          }}
        >
          Đăng nhập
        </Button>
        <PillButton onClick={onRegister} sx={{ display: { xs: 'none', sm: 'inline-flex' } }}>
          Bắt đầu
        </PillButton>
        <IconButton
          onClick={(event) => setMobileMenu(event.currentTarget)}
          aria-label="Mở menu"
          sx={{ display: { lg: 'none' }, color: landing.text }}
        >
          <MenuRounded />
        </IconButton>
      </Box>

      {/* ── Mobile menu ── */}
      <Menu
        anchorEl={mobileMenu}
        open={Boolean(mobileMenu)}
        onClose={() => setMobileMenu(null)}
        PaperProps={{
          sx: {
            mt: 1,
            minWidth: 240,
            bgcolor: landing.card,
            border: `1px solid ${landing.hairline}`,
            borderRadius: `${landing.radius}px`,
          },
        }}
      >
        {NAV_ITEMS.map((item) => (
          <MenuItem
            key={item.to}
            onClick={() => go(item.to)}
            sx={{ color: isActive(item.to) ? landing.text : landing.textMuted, fontWeight: isActive(item.to) ? 700 : 500 }}
          >
            {item.label}
          </MenuItem>
        ))}
        <Divider sx={{ borderColor: landing.hairlineSoft, my: 0.5 }} />
        <MenuItem onClick={() => { setMobileMenu(null); onLogin() }} sx={{ color: landing.text }}>
          Đăng nhập
        </MenuItem>
        <MenuItem onClick={() => { setMobileMenu(null); onRegister() }} sx={{ color: landing.text }}>
          Tạo tài khoản
        </MenuItem>
      </Menu>
    </Box>
  )
}
