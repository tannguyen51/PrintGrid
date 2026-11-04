import { Box } from '@mui/material'
import { PillButton, landing } from './landingTheme'
import { GeometricNetwork } from './GeometricNetwork'
import { palette, ambient, gradients } from '../../../shared/theme/tokens'
import './landing.css'

interface PublicHeroProps {
  onRegister?: () => void
  /**
   * Overrides the hero CTA. Logged-in customers see the same landing as visitors
   * but their action is to order, not to sign up; `null` removes the button
   * for a role that has nowhere useful to send the visitor.
   */
  cta?: { label: string; onClick: () => void } | null
}

/**
 * Hero — bright lavender-to-gold wash, a soft purple/gold ambient glow, and the
 * metallic Rubik scene on the right. The copy column keeps the same hierarchy:
 * eyebrow, oversized display headline, one supporting line, one gradient CTA.
 */
export function PublicHero({ onRegister, cta }: PublicHeroProps) {
  const action = cta === undefined ? { label: 'Bắt đầu ngay', onClick: () => onRegister?.() } : cta
  return (
    <Box
      component="section"
      sx={{
        position: 'relative',
        overflow: 'hidden',
        background: landing.heroGradient,
        borderBottom: `1px solid ${landing.hairline}`,
        pl: { xs: '20px', md: '48px', lg: '120px' },
        pr: { xs: '20px', lg: '104px' },
        pt: { xs: '64px', lg: '108px' },
        pb: { xs: '72px', lg: '128px' },
        display: 'flex',
        alignItems: 'center',
        gap: { xs: 6, lg: 2 },
        flexDirection: { xs: 'column', lg: 'row' },
        minHeight: { lg: 660 },
      }}
    >
      {/* Ambient light pools: purple top-left, gold top-right, silver at the base. */}
      <Box aria-hidden sx={{ position: 'absolute', inset: 0, pointerEvents: 'none', background: ambient.purple, opacity: 0.9 }} />
      <Box aria-hidden sx={{ position: 'absolute', inset: 0, pointerEvents: 'none', background: ambient.gold }} />
      <Box aria-hidden sx={{ position: 'absolute', inset: 0, pointerEvents: 'none', background: ambient.silver, opacity: 0.6 }} />
      {/* Fine metallic grid, very low contrast. */}
      <Box
        aria-hidden
        sx={{
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
          opacity: 0.35,
          backgroundImage:
            'linear-gradient(rgba(111,85,255,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(111,85,255,0.05) 1px, transparent 1px)',
          backgroundSize: '56px 56px',
          maskImage: 'radial-gradient(ellipse 70% 60% at 40% 40%, #000 30%, transparent 75%)',
          WebkitMaskImage: 'radial-gradient(ellipse 70% 60% at 40% 40%, #000 30%, transparent 75%)',
        }}
      />

      {/* ── Copy ── */}
      <Box sx={{ width: { xs: '100%', lg: 600 }, flexShrink: 0, position: 'relative', zIndex: 1 }}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: '26px' }}>
          <Box
            sx={{
              display: 'inline-flex',
              alignSelf: 'flex-start',
              alignItems: 'center',
              gap: 1,
              px: 1.75,
              py: 0.75,
              borderRadius: '999px',
              bgcolor: 'rgba(255,255,255,0.72)',
              border: `1px solid ${palette.border.hairline}`,
              boxShadow: '0 1px 2px rgba(37,38,58,0.04)',
              fontSize: 12,
              fontWeight: 700,
              letterSpacing: '0.1em',
              textTransform: 'uppercase',
              color: palette.purple.deep,
              backdropFilter: 'blur(8px)',
            }}
          >
            <Box sx={{ width: 7, height: 7, borderRadius: '50%', background: gradients.primary }} />
            Nền tảng in 3D phân tán
          </Box>

          <Box
            component="h1"
            sx={{
              m: 0,
              fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
              fontWeight: 800,
              fontSize: { xs: 'clamp(38px, 8vw, 52px)', lg: 'clamp(52px, 5.4vw, 78px)' },
              lineHeight: 1.02,
              letterSpacing: '-0.04em',
              color: landing.text,
            }}
          >
            3D printing,{' '}
            <Box
              component="span"
              sx={{
                background: gradients.primary,
                WebkitBackgroundClip: 'text',
                backgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                color: 'transparent',
              }}
            >
              simplified
            </Box>
          </Box>
          <Box
            component="p"
            sx={{
              m: 0,
              maxWidth: 520,
              fontSize: 18,
              lineHeight: 1.6,
              color: landing.textMuted,
            }}
          >
            Upload your model. Get a price and delivery date. We handle the rest.
          </Box>
          {/* Hero actions */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '20px', flexWrap: 'wrap' }}>
            {action && <PillButton onClick={action.onClick}>{action.label}</PillButton>}
          </Box>
        </Box>
      </Box>

      {/* ── Metallic artwork — transparent canvas over the light wash ── */}
      <Box
        sx={{
          position: { xs: 'relative', lg: 'absolute' },
          left: { lg: 'auto' },
          right: { lg: 60 },
          top: { lg: 40 },
          width: { xs: '100%', lg: 560 },
          height: { xs: 360, lg: 560 },
          flexShrink: 0,
          zIndex: 1,
        }}
      >
        <Box sx={{ position: 'absolute', inset: 0 }}>
          <GeometricNetwork />
        </Box>
      </Box>
    </Box>
  )
}
