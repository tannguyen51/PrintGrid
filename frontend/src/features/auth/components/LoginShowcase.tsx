import { Box, Stack, Typography } from '@mui/material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { ModelViewer } from '../../../shared/components/ModelViewer'
import { useTheme } from '@mui/material/styles'

/**
 * MODEL_URL
 * File lives at frontend/public/models/placeholder-cube_5.glb.
 * Vite serves `public/` at root, so the fetch URL is `/models/placeholder-cube_5.glb`.
 * The 3D canvas itself is the shared ModelViewer (07/10) — login, library and
 * sample pages all render through it so the look stays identical.
 */
const MODEL_URL = '/models/placeholder-cube_5.glb'

/**
 * Right showcase panel (on desktop, after the layout flip).
 * Full-height: contains the 3D glb model + brand header + stat chips.
 * On mobile (stacked) it appears below the login form.
 *
 * When `frame` is true (used inside the AuthDialog), the panel gets a rounded
 * border so the 3D area reads as its own framed panel next to the form.
 */
export function LoginShowcase({ frame = false }: { frame?: boolean }) {
  const theme = useTheme()

  return (
    <Box
      sx={{
        flex: { xs: '0 0 320px', md: '1 1 52%' },
        minWidth: 0,
        position: 'relative',
        overflow: 'hidden',
        color: 'common.white',
        background: theme.custom.showcaseBg, // #050505 — seamless with login panel
        ...(frame
          ? {
              flex: '1 1 auto',
              m: { md: '6px 8px 6px 2px' },
              borderRadius: { md: 5 },
              border: '1px solid rgba(255,255,255,0.12)',
              boxShadow: '0 24px 80px rgba(0,0,0,0.5)',
              overflow: 'hidden',
            }
          : {}),
      }}
    >
      {/* ── Brand header: top 32px, left 40px ── */}
      <Stack direction="row" alignItems="center" spacing={1.5} sx={{ position: 'absolute', top: 32, left: 40, zIndex: 2 }}>
        <BrandGlyph size={40} />
        <Box>
          <Typography sx={{ fontSize: '1.15rem', fontWeight: 700, lineHeight: 1, letterSpacing: '0.02em', color: 'common.white' }}>
            PrintGrid
          </Typography>
          <Typography
            sx={{
              fontSize: '0.66rem',
              color: 'rgba(255,255,255,0.55)',
              mt: 0.5,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
            }}
          >
            In 3D · Quản lý thông minh
          </Typography>
        </Box>
      </Stack>

      {/* ── 3D canvas — shared ModelViewer keeps its loading/error overlays ── */}
      <Box sx={{ position: 'absolute', inset: 0, zIndex: 1 }}>
        <ModelViewer url={MODEL_URL} height="100%" background="#000000" />
      </Box>

      {/* Stat chips with hardcoded figures removed (26/09): the product must not
          display numbers that do not come from the database. */}
    </Box>
  )
}

