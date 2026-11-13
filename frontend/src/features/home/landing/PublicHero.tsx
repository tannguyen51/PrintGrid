import { Box } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { PrinterScene } from './PrinterScene'
import { PillButton, GhostButton, landing } from './landingTheme'
import { palette, ambient, gradients } from '../../../shared/theme/tokens'
import './landing.css'

interface PublicHeroProps {
  onRegister?: () => void
  /**
   * Renders as the secondary CTA next to the primary "Tải model & báo giá".
   * `null` hides the secondary button.
   */
  cta?: { label: string; onClick: () => void } | null
}

const HERO_STATS = [
  { value: '±0.1mm', label: 'Độ chính xác' },
  { value: '24-48h', label: 'Hoàn thành' },
  { value: '5+', label: 'Vật liệu' },
  { value: '4', label: 'Định dạng file' },
]

/**
 * Hero — bright lavender-to-gold wash, a soft purple/gold ambient glow, and the
 * animated 3D printer on the right. The copy column keeps the same hierarchy:
 * eyebrow, oversized display headline, supporting line, dual CTAs and a stats grid.
 */
export function PublicHero({ onRegister, cta }: PublicHeroProps) {
  const navigate = useNavigate()
  const secondary = cta === undefined ? { label: 'Bắt đầu ngay', onClick: () => onRegister?.() } : cta

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
      <Box sx={{ width: { xs: '100%', lg: '50%' }, maxWidth: 620, flexShrink: 0, position: 'relative', zIndex: 1 }}>
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
              fontSize: { xs: 'clamp(38px, 8vw, 52px)', lg: 'clamp(48px, 4.6vw, 72px)' },
              lineHeight: 1.05,
              letterSpacing: '-0.04em',
              color: landing.text,
            }}
          >
            In 3D theo mạng lưới xưởng —{' '}
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
              một giá, ngày giao cam kết
            </Box>
          </Box>
          <Box
            component="p"
            sx={{
              m: 0,
              maxWidth: 540,
              fontSize: 18,
              lineHeight: 1.6,
              color: landing.textMuted,
            }}
          >
            Tải file STL/OBJ/3MF/GLB, nhận báo giá tức thì. Độ chính xác ±0.1mm, giao hàng 24-48h.
          </Box>

          {/* Hero actions */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
            <PillButton onClick={() => navigate('/models')}>Tải model &amp; báo giá</PillButton>
            {secondary && <GhostButton onClick={secondary.onClick}>{secondary.label}</GhostButton>}
          </Box>

          {/* Stats grid */}
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(4, 1fr)' },
              gap: { xs: 1.5, md: 2 },
              pt: { xs: 2, md: 3 },
            }}
          >
            {HERO_STATS.map((stat) => (
              <Box
                key={stat.label}
                sx={{
                  p: { xs: 1.5, md: 2 },
                  borderRadius: '14px',
                  bgcolor: 'rgba(255,255,255,0.64)',
                  border: `1px solid ${palette.border.hairline}`,
                  backdropFilter: 'blur(8px)',
                  textAlign: 'center',
                }}
              >
                <Box
                  sx={{
                    fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
                    fontSize: { xs: 20, md: 24 },
                    fontWeight: 800,
                    background: gradients.primary,
                    WebkitBackgroundClip: 'text',
                    backgroundClip: 'text',
                    WebkitTextFillColor: 'transparent',
                    color: 'transparent',
                  }}
                >
                  {stat.value}
                </Box>
                <Box sx={{ fontSize: 13, color: 'text.secondary', mt: 0.5 }}>{stat.label}</Box>
              </Box>
            ))}
          </Box>
        </Box>
      </Box>

      {/* SPARKX i7 Nano reconstruction with animated print head and bed. */}
      <Box
        component="figure"
        sx={{
          position: 'relative',
          width: '100%',
          maxWidth: { xs: 480, lg: 560 },
          minWidth: 0,
          height: { xs: 320, sm: 460, lg: 560 },
          transform: { lg: 'translateX(200px)' },
          flex: { lg: 1 },
          m: 0,
          zIndex: 1,
        }}
      >
        <Box role="group" aria-label="Mô hình Creality SPARKX i7 Nano chuyển động. Kéo để xoay góc nhìn." sx={{ height: 'calc(100% - 28px)' }}>
          <PrinterScene />
        </Box>
        <Box
          component="figcaption"
          sx={{
            mt: 1,
            textAlign: 'center',
            fontSize: 12,
            color: landing.textMuted,
          }}
        >
          SPARKX i7 Nano — máy in 3D cỡ bàn nhỏ phổ biến trong mạng lưới PrintGrid
        </Box>
      </Box>
    </Box>
  )
}
