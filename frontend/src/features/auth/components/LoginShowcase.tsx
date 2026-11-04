import { Box, Stack, Typography } from '@mui/material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { useTheme } from '@mui/material/styles'
import { GeometricNetwork } from '../../home/landing/GeometricNetwork'
import { ambient, palette, shadows } from '../../../shared/theme/tokens'

/**
 * Right showcase panel: a soft lavender/gold studio with the metallic Rubik
 * scene, brand header and (when framed inside a dialog) a white metallic border.
 *
 * The scene is the same procedural R3F piece used by the public hero — no
 * external GLB fetch, no 2 MB download, and the lighting stays on-brand in both
 * placements.
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
        color: palette.text.primary,
        background: theme.custom.showcaseBg,
        borderLeft: { md: `1px solid ${palette.border.hairline}` },
        ...(frame
          ? {
              flex: '1 1 auto',
              m: { md: '6px 8px 6px 2px' },
              borderRadius: { md: 4 },
              border: `1px solid ${palette.border.hairline}`,
              boxShadow: shadows.cardHover,
              overflow: 'hidden',
            }
          : {}),
      }}
    >
      {/* Ambient purple and gold pools behind the model. */}
      <Box aria-hidden sx={{ position: 'absolute', inset: 0, background: ambient.purple, opacity: 0.7 }} />
      <Box aria-hidden sx={{ position: 'absolute', inset: 0, background: ambient.gold, opacity: 0.8 }} />

      {/* ── Brand header ── */}
      <Stack direction="row" alignItems="center" spacing={1.5} sx={{ position: 'absolute', top: 32, left: 40, zIndex: 2 }}>
        <BrandGlyph size={40} />
        <Box>
          <Typography sx={{ fontSize: '1.15rem', fontWeight: 800, lineHeight: 1, letterSpacing: '-0.02em', color: palette.text.primary }}>
            PrintGrid
          </Typography>
          <Typography
            sx={{
              fontSize: '0.66rem',
              color: palette.text.secondary,
              mt: 0.5,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
              fontWeight: 600,
            }}
          >
            In 3D · Quản lý thông minh
          </Typography>
        </Box>
      </Stack>

      {/* ── Metallic showcase scene ── */}
      <Box sx={{ position: 'absolute', inset: 0, zIndex: 1 }}>
        <GeometricNetwork cameraDistance={10.5} />
      </Box>

      {/* Stat chips with hardcoded figures removed (26/09): the product must not
          display numbers that do not come from the database. */}
    </Box>
  )
}
