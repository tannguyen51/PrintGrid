import { useEffect, useRef, useState } from 'react'
import { Button, type ButtonProps } from '@mui/material'

/**
 * Design tokens extracted from Figma "PrintGrid homepage" (file ZrutUZ4MuMQwrY92DtBgcc).
 * White-on-black system: #F1F1F1 text, #909096 muted, #131315 surfaces, #232326 hairlines.
 */
export const landing = {
  pageBg: '#000000',
  heroGradient: 'linear-gradient(221deg, rgba(22,22,22,1) 0%, rgba(5,5,5,1) 52%, rgba(0,0,0,1) 80%)',
  text: '#F1F1F1',
  textMuted: '#909096',
  surface: '#131315',
  card: '#0B0B0D',
  hairline: '#232326',
  hairlineSoft: '#1B1B1E',
  radius: 14,
} as const

/** Pill button per Figma #36:30 / #36:42 — h48, px22, bg #131315, 1px #232326, r14. */
export function PillButton({ children, sx, ...rest }: ButtonProps) {
  return (
    <Button
      disableRipple
      sx={{
        height: 48,
        px: '22px',
        borderRadius: `${landing.radius}px`,
        bgcolor: landing.surface,
        border: `1px solid ${landing.hairline}`,
        color: landing.text,
        fontSize: 15,
        fontWeight: 500,
        textTransform: 'none',
        boxShadow: 'none',
        transition: 'background-color .18s ease, border-color .18s ease',
        '&:hover': {
          bgcolor: '#1B1B1F',
          borderColor: '#303036',
          boxShadow: 'none',
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
    return () => observer.disconnect()
  }, [])

  return { ref, className: `pg-reveal${visible ? ' pg-visible' : ''}` }
}
