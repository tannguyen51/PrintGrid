/**
 * PrintGrid design tokens — the single source of truth for the light,
 * metallic silver + purple + copper-gold visual language.
 *
 * Every page, layout and shared component should read colours, gradients,
 * shadows and radii from here (or from the MUI theme built on top of it) so a
 * future brand tweak is one edit, not a search across 100 files.
 */

/** Brand palette. Names mirror the design brief. */
export const palette = {
  silver: {
    metallic: '#C0C6D4',
    light: '#E8EBF2',
    surface: '#F1F3F8',
    dark: '#8992A5',
  },
  purple: {
    main: '#6F55FF',
    light: '#A78BFA',
    soft: '#F0ECFF',
    deep: '#4933B8',
  },
  gold: {
    main: '#FFB347',
    light: '#FFD58A',
    soft: '#FFF2D9',
    deep: '#D99528',
  },
  text: {
    primary: '#25263A',
    secondary: '#68708A',
    muted: '#9299AA',
    inverse: '#FFFFFF',
  },
  background: {
    main: '#F8F9FC',
    secondary: '#F1F3F8',
    card: '#FFFFFF',
    elevated: '#FFFFFF',
    purple: '#F5F2FF',
    gold: '#FFF8EB',
  },
  border: {
    hairline: '#E8EBF2',
    soft: '#EFF1F6',
    input: '#DCE0EA',
  },
  status: {
    success: '#1F9D6B',
    successSoft: '#E7F7EF',
    warning: '#C9821B',
    warningSoft: '#FFF4E0',
    error: '#D64545',
    errorSoft: '#FDECEC',
    info: '#3D6FEA',
    infoSoft: '#EAF1FF',
    purple: '#6F55FF',
    purpleSoft: '#EFEBFF',
    neutral: '#68708A',
    neutralSoft: '#F1F3F8',
  },
} as const

/** Gradients used across buttons, hero backgrounds, and premium accents. */
export const gradients = {
  /** Primary action: purple → violet → copper gold. */
  primary: 'linear-gradient(100deg, #6F55FF 0%, #A16BEE 50%, #FFB347 100%)',
  primaryHover: 'linear-gradient(100deg, #7C63FF 0%, #AF7BF4 50%, #FFBE5C 100%)',
  /** Page wash: white → lavender → warm gold. */
  page: 'linear-gradient(135deg, #FFFFFF 0%, #F4F2FF 55%, #FFF8EB 100%)',
  /** Soft purple wash for feature/side panels. */
  lavender: 'linear-gradient(145deg, #F7F4FF 0%, #F0ECFF 55%, #FFF8EB 100%)',
  /** Metallic silver surface, top-lit. */
  metal: 'linear-gradient(145deg, #FFFFFF 0%, #F5F7FB 45%, #E9EDF5 100%)',
  /** Thin metal rule used under navbars and above footers. */
  hairline: 'linear-gradient(90deg, transparent, #DCE0EA 18%, #DCE0EA 82%, transparent)',
  /** Gold highlight for premium badges and stat numbers. */
  gold: 'linear-gradient(120deg, #FFB347 0%, #FFD58A 60%, #D99528 100%)',
  /** Brand mark gradient. */
  brand: 'linear-gradient(135deg, #6F55FF 0%, #A78BFA 55%, #FFB347 100%)',
} as const

/** Layered, low-contrast shadows — elevation without the heavy dark-theme blur. */
export const shadows = {
  xs: '0 1px 2px rgba(37, 38, 58, 0.05)',
  card: '0 1px 2px rgba(37, 38, 58, 0.04), 0 10px 28px rgba(37, 38, 58, 0.05)',
  cardHover: '0 2px 6px rgba(111, 85, 255, 0.08), 0 20px 48px rgba(111, 85, 255, 0.14)',
  button: '0 6px 16px rgba(111, 85, 255, 0.24), 0 2px 4px rgba(73, 51, 184, 0.16)',
  buttonHover: '0 10px 24px rgba(111, 85, 255, 0.32), 0 3px 8px rgba(217, 149, 40, 0.2)',
  gold: '0 6px 18px rgba(255, 179, 71, 0.28)',
  inset: 'inset 0 1px 0 rgba(255, 255, 255, 0.9)',
  popover: '0 16px 48px rgba(37, 38, 58, 0.16), 0 2px 6px rgba(37, 38, 58, 0.06)',
} as const

/** Corner radii — the brief calls for 12–16px controls, 16–20px cards. */
export const radii = {
  xs: 8,
  sm: 10,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 28,
  pill: 999,
} as const

/** Consistent motion curves and durations. */
export const motion = {
  fast: '140ms cubic-bezier(0.4, 0, 0.2, 1)',
  base: '200ms cubic-bezier(0.4, 0, 0.2, 1)',
  slow: '320ms cubic-bezier(0.4, 0, 0.2, 1)',
} as const

/** Typography stack. Plus Jakarta Sans for display, Inter for body/UI. */
export const fonts = {
  display: '"Plus Jakarta Sans", "Inter", "Segoe UI", system-ui, sans-serif',
  body: '"Inter", "Segoe UI", system-ui, sans-serif',
  mono: '"JetBrains Mono", "SFMono-Regular", Consolas, monospace',
} as const

/** Ambient page decoration (soft radial light pools). */
export const ambient = {
  purple: 'radial-gradient(ellipse 55% 45% at 18% 0%, rgba(111, 85, 255, 0.12), transparent 70%)',
  gold: 'radial-gradient(ellipse 50% 40% at 88% 12%, rgba(255, 179, 71, 0.14), transparent 72%)',
  silver: 'radial-gradient(ellipse 60% 50% at 50% 100%, rgba(192, 198, 212, 0.22), transparent 72%)',
} as const

/** Full-page wash: the white→lavender→gold gradient plus two soft light pools. */
export const pageWash = `${ambient.purple}, ${ambient.gold}, ${gradients.page}`

/** Highest-contrast tone pairs for the semantic status chips. */export const statusTones = {
  PaymentPending: { fg: palette.status.warning, bg: palette.status.warningSoft, border: '#F3D8AE' },
  Confirmed: { fg: palette.status.info, bg: palette.status.infoSoft, border: '#C7D8FB' },
  InProduction: { fg: palette.purple.main, bg: palette.purple.soft, border: '#D8CDFF' },
  QualityCheck: { fg: palette.status.warning, bg: palette.status.warningSoft, border: '#F3D8AE' },
  Shipping: { fg: palette.status.info, bg: palette.status.infoSoft, border: '#C7D8FB' },
  Delivered: { fg: palette.status.success, bg: palette.status.successSoft, border: '#B7E7D0' },
  Cancelled: { fg: palette.status.error, bg: palette.status.errorSoft, border: '#F5C6C6' },
  Pending: { fg: palette.status.warning, bg: palette.status.warningSoft, border: '#F3D8AE' },
  Ready: { fg: palette.status.success, bg: palette.status.successSoft, border: '#B7E7D0' },
  Failed: { fg: palette.status.error, bg: palette.status.errorSoft, border: '#F5C6C6' },
  Expired: { fg: palette.status.neutral, bg: palette.status.neutralSoft, border: palette.border.hairline },
  Converted: { fg: palette.status.info, bg: palette.status.infoSoft, border: '#C7D8FB' },
} as const

/** Add an alpha channel to a #RRGGBB token. */
export function alpha(hex: string, opacity: number): string {
  const value = hex.replace('#', '')
  const r = parseInt(value.slice(0, 2), 16)
  const g = parseInt(value.slice(2, 4), 16)
  const b = parseInt(value.slice(4, 6), 16)
  return `rgba(${r}, ${g}, ${b}, ${opacity})`
}
