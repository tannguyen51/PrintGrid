import type { ReactNode } from 'react'
import { Box } from '@mui/material'
import { useReveal } from '../../../../shared/theme/landing'

interface SectionShellProps {
  id: string
  label: string
  heading: string
  body: string
  children?: ReactNode
  centered?: boolean
}

/**
 * Reusable wrapper for homepage sections.
 * Provides the standard eyebrow, display heading, intro body and reveal animation.
 */
export function SectionShell({ id, label, heading, body, children, centered = false }: SectionShellProps) {
  const { ref, className } = useReveal<HTMLDivElement>()

  return (
    <Box
      component="section"
      id={id}
      ref={ref}
      className={className}
      sx={{
        px: { xs: '20px', md: '40px', lg: '104px' },
        py: { xs: '64px', lg: '104px' },
        display: 'flex',
        flexDirection: 'column',
        gap: '32px',
        borderBottom: (theme) => `1px solid ${theme.palette.divider}`,
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: '14px',
          textAlign: centered ? 'center' : 'left',
          alignItems: centered ? 'center' : 'flex-start',
        }}
      >
        <Box
          sx={{
            fontSize: 12,
            fontWeight: 700,
            letterSpacing: '0.16em',
            textTransform: 'uppercase',
            color: 'primary.main',
          }}
        >
          {label}
        </Box>
        <Box
          component="h2"
          sx={{
            m: 0,
            fontFamily: '"Plus Jakarta Sans", "Inter", sans-serif',
            fontWeight: 800,
            fontSize: { xs: 'clamp(30px, 5vw, 38px)', lg: 50 },
            lineHeight: 1.12,
            letterSpacing: '-0.03em',
            color: 'text.primary',
          }}
        >
          {heading}
        </Box>
        <Box
          component="p"
          sx={{
            m: 0,
            fontSize: 17,
            lineHeight: 1.65,
            color: 'text.secondary',
            maxWidth: 720,
          }}
        >
          {body}
        </Box>
      </Box>
      {children}
    </Box>
  )
}
