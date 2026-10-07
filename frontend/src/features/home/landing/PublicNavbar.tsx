import { Box, Button } from '@mui/material'
import { KeyboardArrowDownRounded } from '@mui/icons-material'
import { landing, PillButton, scrollToSection } from './landingTheme'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import './landing.css'

/** Navigation items from Figma #36:7 — Vietnamese labels, chevron affordance. */
const NAV_ITEMS: { label: string; target?: string }[] = [
  { label: 'Kho mẫu 3D', target: 'sec-01' },
  { label: 'Chi tiết mẫu', target: 'sec-01' },
  { label: 'Bảng giá & vật liệu', target: 'sec-02' },
  { label: 'Mạng lưới xưởng', target: 'sec-03' },
  { label: 'Hướng dẫn / FAQ' },
]

interface PublicNavbarProps {
  onLogin: () => void
  onRegister: () => void
}

/**
 * Public (logged-out) top bar — Figma #36:4: 60px tall, 104px side padding,
 * wordmark left, 5 muted links with chevrons in the middle, auth actions right.
 */
export function PublicNavbar({ onLogin, onRegister }: PublicNavbarProps) {
  const handleNav = (item: (typeof NAV_ITEMS)[number]) => {
    if (item.target) {
      scrollToSection(item.target)
      return
    }
    // No matching landing block yet (FAQ section is not in the approved design) —
    // scroll to the page bottom where the footer lives.
    window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' })
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
      {/* ── Wordmark (Figma #36:5 is a 175×48 raster crop — recreated in text) ── */}
      <Box
        onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })}
        sx={{ display: 'flex', alignItems: 'center', gap: 1.25, cursor: 'pointer', userSelect: 'none' }}
      >
        <BrandGlyph size={26} color={landing.text} mutedColor={landing.textMuted} />
        <Box
          component="span"
          sx={{ fontSize: 20, fontWeight: 600, letterSpacing: '-0.01em', color: landing.text }}
        >
          PrintGrid
        </Box>
      </Box>

      {/* ── Center links ── */}
      <Box
        sx={{
          display: { xs: 'none', lg: 'flex' },
          alignItems: 'center',
          gap: '30px',
        }}
      >
        {NAV_ITEMS.map((item) => (
          <Button
            key={item.label}
            onClick={() => handleNav(item)}
            disableRipple
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: '5px',
              p: 0,
              minWidth: 0,
              color: landing.textMuted,
              fontSize: 14,
              fontWeight: 400,
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
            p: 0,
            minWidth: 0,
            color: landing.text,
            fontSize: 14,
            fontWeight: 400,
            textTransform: 'none',
            '&:hover': { bgcolor: 'transparent', opacity: 0.75 },
          }}
        >
          Log in
        </Button>
        <PillButton onClick={onRegister}>Get started</PillButton>
      </Box>
    </Box>
  )
}
