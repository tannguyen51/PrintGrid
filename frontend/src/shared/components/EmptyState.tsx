import type { ReactNode } from 'react'
import { Box, Stack, Typography } from '@mui/material'
import { landing } from '../theme/landing'

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
        border: `1px dashed ${landing.hairline}`,
        borderRadius: `${landing.radius}px`,
      }}
    >
      {icon && (
        <Box
          sx={{
            width: 56,
            height: 56,
            borderRadius: '50%',
            display: 'grid',
            placeItems: 'center',
            bgcolor: landing.surface,
            border: `1px solid ${landing.hairline}`,
            color: landing.textMuted,
            mb: 0.5,
          }}
        >
          {icon}
        </Box>
      )}
      <Typography sx={{ fontSize: '1.05rem', fontWeight: 600, color: landing.text }}>{title}</Typography>
      {description && (
        <Typography sx={{ color: landing.textMuted, maxWidth: '46ch' }}>{description}</Typography>
      )}
      {action && <Box sx={{ pt: 1 }}>{action}</Box>}
    </Stack>
  )
}
