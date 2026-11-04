import { useEffect, useRef, useState } from 'react'
import { Button, type ButtonProps } from '@mui/material'
import { gradients, palette, radii, shadows } from './tokens'

/**
 * Public-surface design tokens.
 *
 * History: these were the Figma homepage's white-on-black tokens. The brand
 * moved to a light metallic + purple + gold system, so the same token names now
 * carry the light values — every existing consumer (public pages, customer
 * navbars, landing sections) inherits the new look without a rename.
 */
export const landing = {
  pageBg: palette.background.main,
  heroGradient: gradients.page,
  text: palette.text.primary,
  textMuted: palette.text.secondary,
  surface: palette.background.secondary,
  card: palette.background.card,
  hairline: palette.border.hairline,
  hairlineSoft: palette.border.soft,
  radius: radii.lg,
} as const

/**
 * Primary call-to-action: the purple → violet → copper-gold gradient pill.
 * Kept as `PillButton` because every public page already imports it.
 */
export function PillButton({ children, sx, ...rest }: ButtonProps) {
  return (
    <Button
      variant="contained"
      color="primary"
      disableElevation
      sx={{
        height: 48,
        px: '24px',
        borderRadius: `${radii.md}px`,
        background: gradients.primary,
        color: palette.text.inverse,
        fontSize: 15,
        fontWeight: 600,
        letterSpacing: '0.01em',
        textTransform: 'none',
        boxShadow: shadows.button,
        transition: 'background 200ms ease, box-shadow 200ms ease, transform 140ms ease',
        '&:hover': {
          background: gradients.primaryHover,
          boxShadow: shadows.buttonHover,
        },
        '&:active': { transform: 'translateY(1px)' },
        ...sx,
      }}
      {...rest}
    >
      {children}
    </Button>
  )
}

/** Secondary CTA: white surface, metallic hairline, purple hover. */
export function GhostButton({ children, sx, ...rest }: ButtonProps) {
  return (
    <Button
      variant="outlined"
      disableElevation
      sx={{
        height: 48,
        px: '22px',
        borderRadius: `${radii.md}px`,
        backgroundColor: palette.background.card,
        border: `1px solid ${palette.silver.metallic}`,
        color: palette.text.primary,
        fontSize: 15,
        fontWeight: 600,
        textTransform: 'none',
        boxShadow: shadows.xs,
        transition: 'border-color 180ms ease, background-color 180ms ease, box-shadow 180ms ease',
        '&:hover': {
          borderColor: palette.purple.main,
          backgroundColor: palette.purple.soft,
          color: palette.purple.deep,
          boxShadow: shadows.card,
        },
        ...sx,
      }}
      {...rest}
    >
      {children}
    </Button>
  )
}

/** Smooth-scroll to a landing section by id. */
export function scrollToSection(id: string) {
  const el = document.getElementById(id)
  if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

/** Fade the block in when it enters the viewport (IntersectionObserver). */
export function useReveal<T extends HTMLElement>() {
  const ref = useRef<T | null>(null)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const node = ref.current
    if (!node) return
    // Safety net: never leave content hidden if the observer is unavailable,
    // disabled, or the section is already in view before observation starts.
    const fallback = window.setTimeout(() => setVisible(true), 1200)
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setVisible(true)
            observer.disconnect()
          }
        })
      },
      { threshold: 0.18 },
    )
    observer.observe(node)
    return () => {
      window.clearTimeout(fallback)
      observer.disconnect()
    }
  }, [])

  return { ref, className: `pg-reveal${visible ? ' pg-visible' : ''}` }
}
