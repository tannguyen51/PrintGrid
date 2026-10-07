import { Box } from '@mui/material'
import { PillButton, landing } from './landingTheme'
import { GeometricNetwork } from './GeometricNetwork'
import './landing.css'

interface PublicHeroProps {
  onRegister: () => void
}

/**
 * Hero — Figma #36:32: 1028px tall gradient panel (#161616 → #050505 → #000),
 * 560px copy column on the left, 600×650 artwork area pinned top-right (x780, y150)
 * with 60px left / 70px top edge fades.
 *
 * The artwork slot holds the Geometric Network 3D scene (Lab/Printer/Job nodes,
 * xoay → tách → kết nối → converge) in place of the Figma raster cube crop.
 */
export function PublicHero({ onRegister }: PublicHeroProps) {
  return (
    <Box
      component="section"
      sx={{
        position: 'relative',
        overflow: 'hidden',
        background: landing.heroGradient,
        pl: { xs: '20px', md: '48px', lg: '168px' },
        pr: { xs: '20px', lg: '104px' },
        pt: { xs: '72px', lg: '150px' },
        pb: { xs: '88px', lg: '200px' },
        display: 'flex',
        alignItems: 'center',
        gap: { xs: 6, lg: 0 },
        flexDirection: { xs: 'column', lg: 'row' },
        minHeight: { lg: 1028 },
      }}
    >
      {/* ── Copy (Figma #36:38) ── */}
      <Box sx={{ width: { xs: '100%', lg: 560 }, flexShrink: 0, position: 'relative', zIndex: 1 }}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
          <Box
            component="h1"
            sx={{
              m: 0,
              fontFamily: '"Domine", serif',
              fontWeight: 400,
              fontSize: { xs: 'clamp(40px, 9vw, 58px)', lg: 'clamp(58px, 6.7vw, 96px)' },
              lineHeight: 1,
              color: landing.text,
            }}
          >
            3D printing, simplified
          </Box>
          <Box
            component="p"
            sx={{
              m: 0,
              maxWidth: 530,
              fontSize: 18,
              lineHeight: 1.55,
              color: landing.textMuted,
            }}
          >
            Upload your model. Get a price and delivery date. We handle the rest.
          </Box>
          {/* Hero actions — Figma #36:41 */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '36px' }}>
            <PillButton onClick={onRegister}>Get started</PillButton>
          </Box>
        </Box>
      </Box>

      {/* ── Rubik artwork — transparent canvas, blends straight into the gradient ── */}
      <Box
        sx={{
          position: { xs: 'relative', lg: 'absolute' },
          left: { lg: 'auto' },
          right: { lg: 60 },
          top: { lg: 150 },
          width: { xs: '100%', lg: 600 },
          height: { xs: 380, lg: 650 },
          flexShrink: 0,
        }}
      >
        <Box sx={{ position: 'absolute', inset: 0 }}>
          <GeometricNetwork />
        </Box>
      </Box>
    </Box>
  )
}
