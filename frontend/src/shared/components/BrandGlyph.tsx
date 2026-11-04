import { Box } from '@mui/material'
import { gradients, palette } from '../theme/tokens'

/**
 * PrintGrid brand mark — the 2×2 checkerboard from the original identity:
 * two filled cells on the diagonal, two outlined cells between them.
 * Single source of truth for every logo placement: navbars, auth screens,
 * showcase panel, footer and the favicon.
 *
 * Defaults use the purple→gold brand gradient so the mark reads as premium on
 * the light surfaces; callers can still override both tones.
 */
export function BrandGlyph({
  size = 40,
  color = gradients.brand,
  mutedColor = palette.silver.metallic,
}: {
  size?: number
  /** Fill of the two solid cells (accepts a colour or a gradient). */
  color?: string
  /** Stroke of the two outlined cells. */
  mutedColor?: string
}) {
  const radius = Math.max(2, Math.round(size * 0.115))
  const border = Math.max(1, Math.round(size * 0.04))

  return (
    <Box
      sx={{
        width: size,
        height: size,
        display: 'grid',
        gridTemplateColumns: 'repeat(2, 1fr)',
        gap: `${radius}px`,
        flexShrink: 0,
      }}
    >
      <Box sx={{ borderRadius: `${radius}px`, background: color }} />
      <Box sx={{ borderRadius: `${radius}px`, border: `${border}px solid ${mutedColor}` }} />
      <Box sx={{ borderRadius: `${radius}px`, border: `${border}px solid ${mutedColor}` }} />
      <Box sx={{ borderRadius: `${radius}px`, background: color }} />
    </Box>
  )
}
