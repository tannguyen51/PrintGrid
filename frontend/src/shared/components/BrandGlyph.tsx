import { Box } from '@mui/material'

/**
 * PrintGrid brand mark — the 2×2 checkerboard from the Figma design (#36:5):
 * two filled cells on the diagonal, two outlined cells between them.
 * Single source of truth for every logo placement: landing navbar, auth screens,
 * showcase panel, footer and the favicon.
 */
export function BrandGlyph({
  size = 40,
  color = '#F1F1F1',
  mutedColor = '#909096',
}: {
  size?: number
  /** Fill of the two solid cells. */
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
      <Box sx={{ borderRadius: `${radius}px`, bgcolor: color }} />
      <Box sx={{ borderRadius: `${radius}px`, border: `${border}px solid ${mutedColor}` }} />
      <Box sx={{ borderRadius: `${radius}px`, border: `${border}px solid ${mutedColor}` }} />
      <Box sx={{ borderRadius: `${radius}px`, bgcolor: color }} />
    </Box>
  )
}
