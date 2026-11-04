import type { ReactNode } from 'react'
import { Box, Stack, Typography } from '@mui/material'
import { landing } from '../theme/landing'
import { gradients, palette, shadows } from '../theme/tokens'

/**
 * The app previously rendered "no data" a different way on every page.
 * EmptyState is deliberately an invitation, not a dead end: always pass an
 * `action` when there is a next step.
 */
export function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
}) {
  return (
    <Stack
      alignItems="center"
      spacing={1.5}
      sx={{
        textAlign: 'center',
        p: { xs: 5, md: 7 },
        bgcolor: landing.card,
        backgroundImage: gradients.metal,
        border: `1px dashed ${palette.silver.metallic}`,
        borderRadius: `${landing.radius}px`,
        boxShadow: shadows.card,
      }}
    >
      {icon && (
        <Box
          sx={{
            width: 60,
            height: 60,
            borderRadius: '50%',
            display: 'grid',
            placeItems: 'center',
            background: palette.purple.soft,
            border: `1px solid ${palette.border.hairline}`,
            color: palette.purple.main,
            mb: 0.5,
            boxShadow: shadows.gold,
          }}
        >
          {icon}
        </Box>
      )}
      <Typography sx={{ fontSize: '1.05rem', fontWeight: 700, color: landing.text }}>{title}</Typography>
      {description && (
        <Typography sx={{ color: landing.textMuted, maxWidth: '46ch' }}>{description}</Typography>
      )}
      {action && <Box sx={{ pt: 1 }}>{action}</Box>}
    </Stack>
  )
}
