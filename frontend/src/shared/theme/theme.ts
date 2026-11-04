import { createTheme } from '@mui/material/styles'
import type {} from '@mui/x-data-grid/themeAugmentation'
import { alpha, gradients, pageWash, palette, radii, shadows, fonts } from './tokens'

/**
 * PrintGrid light theme.
 *
 * Bright, premium SaaS: white cards on a soft lavender/gold wash, metallic
 * silver hairlines, purple as the interaction colour and copper gold as the
 * premium accent. All values come from `tokens.ts`; nothing here is a one-off.
 */

declare module '@mui/material/styles' {
  interface Theme {
    custom: {
      showcaseBg: string
      loginCardShadow: string
      loginInputBg: string
      gradients: typeof gradients
      shadows: typeof shadows
      radii: typeof radii
      brand: typeof palette
      navHeight: number
    }
  }
  interface ThemeOptions {
    custom?: {
      showcaseBg?: string
      loginCardShadow?: string
      loginInputBg?: string
      gradients?: typeof gradients
      shadows?: typeof shadows
      radii?: typeof radii
      brand?: typeof palette
      navHeight?: number
    }
  }
}

const focusRing = `0 0 0 4px ${alpha(palette.purple.main, 0.13)}`
const hairline = palette.border.hairline
const motionDuration = '180ms cubic-bezier(0.4, 0, 0.2, 1)'

export const printGridTheme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: palette.purple.main,
      light: palette.purple.light,
      dark: palette.purple.deep,
      contrastText: palette.text.inverse,
    },
    secondary: {
      main: palette.gold.main,
      light: palette.gold.light,
      dark: palette.gold.deep,
      contrastText: '#4A2B00',
    },
    success: { main: palette.status.success, light: palette.status.successSoft, contrastText: '#FFFFFF' },
    warning: { main: palette.status.warning, light: palette.status.warningSoft, contrastText: '#FFFFFF' },
    error: { main: palette.status.error, light: palette.status.errorSoft, contrastText: '#FFFFFF' },
    info: { main: palette.status.info, light: palette.status.infoSoft, contrastText: '#FFFFFF' },
    background: {
      default: palette.background.main,
      paper: palette.background.card,
    },
    divider: hairline,
    text: {
      primary: palette.text.primary,
      secondary: palette.text.secondary,
      disabled: palette.text.muted,
    },
    action: {
      active: palette.text.secondary,
      hover: alpha(palette.purple.main, 0.06),
      selected: alpha(palette.purple.main, 0.1),
      focus: alpha(palette.purple.main, 0.12),
      disabled: palette.text.muted,
      disabledBackground: palette.background.secondary,
    },
    common: { black: '#000000', white: '#FFFFFF' },
    grey: {
      50: palette.background.main,
      100: palette.background.secondary,
      200: palette.border.hairline,
      300: palette.silver.metallic,
      500: palette.text.muted,
      700: palette.text.secondary,
      900: palette.text.primary,
    },
  },
  shape: { borderRadius: radii.md },
  typography: {
    fontFamily: fonts.body,
    h1: {
      fontFamily: fonts.display,
      fontSize: '2rem',
      fontWeight: 700,
      letterSpacing: '-0.025em',
      lineHeight: 1.2,
    },
    h2: {
      fontFamily: fonts.display,
      fontSize: '1.55rem',
      fontWeight: 700,
      letterSpacing: '-0.02em',
      lineHeight: 1.25,
    },
    h3: { fontFamily: fonts.display, fontWeight: 700, letterSpacing: '-0.015em' },
    h4: { fontFamily: fonts.display, fontWeight: 700, letterSpacing: '-0.01em' },
    h5: { fontFamily: fonts.display, fontWeight: 700 },
    h6: { fontFamily: fonts.display, fontWeight: 700 },
    subtitle1: { fontWeight: 600 },
    subtitle2: { fontWeight: 600, letterSpacing: '0.01em' },
    body1: { lineHeight: 1.6 },
    body2: { lineHeight: 1.55 },
    button: { textTransform: 'none', fontWeight: 600, letterSpacing: '0.01em' },
    caption: { letterSpacing: '0.01em' },
    overline: { letterSpacing: '0.12em', fontWeight: 700 },
  },
  shadows: [
    'none',
    shadows.xs,
    '0 2px 6px rgba(37, 38, 58, 0.06)',
    shadows.card,
    shadows.card,
    shadows.card,
    shadows.cardHover,
    shadows.cardHover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
    shadows.popover,
  ],
  components: {
    MuiCssBaseline: {
      styleOverrides: {
        body: {
          color: palette.text.primary,
          backgroundColor: palette.background.main,
          backgroundImage: pageWash,
          backgroundAttachment: 'fixed',
        },
        '::selection': {
          backgroundColor: alpha(palette.purple.main, 0.2),
          color: palette.text.primary,
        },
        '*::-webkit-scrollbar': { width: 10, height: 10 },
        '*::-webkit-scrollbar-track': { background: 'transparent' },
        '*::-webkit-scrollbar-thumb': {
          background: alpha(palette.silver.dark, 0.45),
          borderRadius: radii.pill,
          border: '2px solid transparent',
          backgroundClip: 'content-box',
        },
        '*::-webkit-scrollbar-thumb:hover': {
          background: alpha(palette.purple.main, 0.45),
          backgroundClip: 'content-box',
        },
      },
    },
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          border: `1px solid ${hairline}`,
          borderRadius: radii.lg,
        },
        outlined: { border: `1px solid ${hairline}` },
      },
    },
    MuiCard: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: palette.background.card,
          border: `1px solid ${hairline}`,
          borderRadius: radii.xl,
          boxShadow: shadows.card,
          transition: `box-shadow 200ms ease, border-color 200ms ease, transform 200ms ease`,
        },
      },
    },
    MuiAppBar: {
      defaultProps: { elevation: 0, color: 'transparent' },
      styleOverrides: {
        root: {
          backgroundColor: alpha('#FFFFFF', 0.82),
          backdropFilter: 'blur(16px)',
          borderBottom: `1px solid ${hairline}`,
          boxShadow: 'none',
          color: palette.text.primary,
        },
      },
    },
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: {
          borderRadius: radii.md,
          textTransform: 'none',
          fontWeight: 600,
          minHeight: 40,
          paddingInline: 18,
          boxShadow: 'none',
          transition: `background ${motionDuration}, box-shadow 200ms ease, transform 140ms ease, border-color 200ms ease`,
          '&:active': { transform: 'translateY(1px)' },
        },
        sizeSmall: { minHeight: 32, paddingInline: 14, fontSize: '0.8rem' },
        sizeLarge: { minHeight: 48, paddingInline: 24, fontSize: '0.95rem' },
        contained: { '&:hover': { boxShadow: 'none' } },
        containedPrimary: {
          background: gradients.primary,
          color: palette.text.inverse,
          boxShadow: shadows.button,
          '&:hover': {
            background: gradients.primaryHover,
            boxShadow: shadows.buttonHover,
          },
          '&.Mui-disabled': {
            background: palette.silver.light,
            color: palette.text.muted,
            boxShadow: 'none',
          },
        },
        containedSecondary: {
          background: gradients.gold,
          color: '#4A2B00',
          boxShadow: shadows.gold,
          '&:hover': { background: gradients.gold, boxShadow: shadows.gold, filter: 'brightness(1.04)' },
        },
        outlined: {
          borderColor: palette.silver.metallic,
          color: palette.text.primary,
          backgroundColor: palette.background.card,
          '&:hover': {
            borderColor: palette.purple.main,
            color: palette.purple.deep,
            backgroundColor: palette.purple.soft,
          },
        },
        outlinedError: {
          borderColor: '#F0B4B4',
          color: palette.status.error,
          '&:hover': { borderColor: palette.status.error, backgroundColor: palette.status.errorSoft },
        },
        text: {
          color: palette.purple.main,
          '&:hover': { backgroundColor: alpha(palette.purple.main, 0.07) },
        },
        textPrimary: { color: palette.purple.main },
        textSecondary: { color: palette.text.secondary },
      },
    },
    MuiIconButton: {
      styleOverrides: {
        root: {
          borderRadius: radii.sm,
          color: palette.text.secondary,
          transition: 'background-color 160ms ease, color 160ms ease',
          '&:hover': { color: palette.purple.deep, backgroundColor: palette.purple.soft },
        },
        colorPrimary: { color: palette.purple.main },
      },
    },
    MuiToggleButton: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          borderRadius: radii.sm,
          borderColor: hairline,
          color: palette.text.secondary,
          '&.Mui-selected': {
            color: palette.purple.deep,
            backgroundColor: palette.purple.soft,
            borderColor: alpha(palette.purple.main, 0.4),
          },
        },
      },
    },
    MuiTextField: {
      defaultProps: { variant: 'outlined' },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          backgroundColor: palette.background.card,
          borderRadius: radii.md,
          transition: 'box-shadow 180ms ease, background-color 180ms ease',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: palette.border.input,
            transition: 'border-color 180ms ease',
          },
          '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: palette.silver.dark },
          '&.Mui-focused': { boxShadow: focusRing },
          '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
            borderColor: palette.purple.main,
            borderWidth: 1.5,
          },
          '&.Mui-error.Mui-focused': { boxShadow: `0 0 0 4px ${alpha(palette.status.error, 0.12)}` },
          '&.Mui-disabled': { backgroundColor: palette.background.secondary },
        },
        input: { paddingBlock: 12.5 },
        inputSizeSmall: { paddingBlock: 9 },
      },
    },
    MuiInputLabel: {
      styleOverrides: {
        root: {
          color: palette.text.secondary,
          '&.Mui-focused': { color: palette.purple.main },
        },
      },
    },
    MuiFormHelperText: {
      styleOverrides: { root: { marginLeft: 2 } },
    },
    MuiSelect: {
      styleOverrides: { root: { borderRadius: radii.md } },
    },
    MuiMenu: {
      styleOverrides: {
        paper: {
          marginTop: 6,
          borderRadius: radii.lg,
          border: `1px solid ${hairline}`,
          boxShadow: shadows.popover,
          backgroundColor: palette.background.card,
          padding: 6,
        },
        list: { padding: 0 },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          borderRadius: radii.sm,
          minHeight: 38,
          fontSize: '0.875rem',
          color: palette.text.primary,
          '&:hover': { backgroundColor: palette.purple.soft },
          '&.Mui-selected': {
            backgroundColor: palette.purple.soft,
            color: palette.purple.deep,
            fontWeight: 600,
            '&:hover': { backgroundColor: palette.purple.soft },
          },
        },
      },
    },
    MuiPopover: {
      styleOverrides: {
        paper: { borderRadius: radii.lg, boxShadow: shadows.popover, border: `1px solid ${hairline}` },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          borderRadius: radii.xl,
          border: `1px solid ${hairline}`,
          boxShadow: shadows.popover,
          backgroundImage: 'none',
          backgroundColor: palette.background.card,
        },
      },
    },
    MuiDialogTitle: {
      styleOverrides: {
        root: { fontFamily: fonts.display, fontWeight: 700, fontSize: '1.2rem', letterSpacing: '-0.01em' },
      },
    },
    MuiDialogActions: { styleOverrides: { root: { padding: '16px 24px 22px' } } },
    MuiBackdrop: {
      styleOverrides: {
        root: { backgroundColor: alpha('#1B1C2E', 0.42), backdropFilter: 'blur(4px)' },
        invisible: { backgroundColor: 'transparent' },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          backgroundColor: palette.background.card,
          backgroundImage: 'none',
          borderLeft: `1px solid ${hairline}`,
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderColor: palette.border.soft,
          color: palette.text.primary,
          paddingTop: 12,
          paddingBottom: 12,
        },
        head: {
          backgroundColor: palette.background.secondary,
          color: palette.text.secondary,
          fontWeight: 700,
          fontSize: '0.72rem',
          letterSpacing: '0.055em',
          textTransform: 'uppercase',
          borderBottom: `1px solid ${hairline}`,
          whiteSpace: 'nowrap',
        },
      },
    },
    MuiTableRow: {
      styleOverrides: {
        root: {
          transition: 'background-color 140ms ease',
          '&:hover': { backgroundColor: alpha(palette.purple.main, 0.035) },
          '&.Mui-selected': {
            backgroundColor: palette.purple.soft,
            '&:hover': { backgroundColor: alpha(palette.purple.main, 0.12) },
          },
        },
      },
    },
    MuiTableContainer: {
      styleOverrides: {
        root: {
          border: `1px solid ${hairline}`,
          borderRadius: radii.lg,
          backgroundColor: palette.background.card,
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: radii.pill,
          fontWeight: 600,
          fontSize: '0.78rem',
          backgroundColor: palette.background.secondary,
          color: palette.text.secondary,
          border: `1px solid ${hairline}`,
        },
        filledPrimary: { backgroundColor: palette.purple.soft, color: palette.purple.deep, borderColor: '#D8CDFF' },
        filledSecondary: { backgroundColor: palette.gold.soft, color: '#8A5A12', borderColor: '#F5DDB0' },
        colorPrimary: { backgroundColor: palette.purple.soft, color: palette.purple.deep, borderColor: '#D8CDFF' },
        colorSecondary: { backgroundColor: palette.gold.soft, color: '#8A5A12', borderColor: '#F5DDB0' },
        colorSuccess: { backgroundColor: palette.status.successSoft, color: palette.status.success, borderColor: '#B7E7D0' },
        colorWarning: { backgroundColor: palette.status.warningSoft, color: palette.status.warning, borderColor: '#F3D8AE' },
        colorError: { backgroundColor: palette.status.errorSoft, color: palette.status.error, borderColor: '#F5C6C6' },
        colorInfo: { backgroundColor: palette.status.infoSoft, color: palette.status.info, borderColor: '#C7D8FB' },
        outlined: { backgroundColor: 'transparent' },
        deleteIcon: { color: 'inherit' },
      },
    },
    MuiTooltip: {
      defaultProps: { arrow: true },
      styleOverrides: {
        tooltip: {
          backgroundColor: '#2B2C42',
          color: palette.text.inverse,
          borderRadius: radii.xs,
          padding: '7px 11px',
          fontSize: '0.76rem',
          boxShadow: shadows.popover,
        },
        arrow: { color: '#2B2C42' },
      },
    },
    MuiTabs: {
      styleOverrides: {
        root: { minHeight: 42 },
        indicator: { height: 3, borderRadius: 3, background: gradients.primary },
      },
    },
    MuiTab: {
      styleOverrides: {
        root: {
          textTransform: 'none',
          fontWeight: 600,
          minHeight: 42,
          color: palette.text.secondary,
          '&.Mui-selected': { color: palette.purple.deep },
        },
      },
    },
    MuiAlert: {
      styleOverrides: {
        root: { borderRadius: radii.md, border: '1px solid', alignItems: 'center' },
        standardSuccess: { backgroundColor: palette.status.successSoft, color: '#146B4A', borderColor: '#B7E7D0' },
        standardWarning: { backgroundColor: palette.status.warningSoft, color: '#8A5A12', borderColor: '#F3D8AE' },
        standardError: { backgroundColor: palette.status.errorSoft, color: '#A52E2E', borderColor: '#F5C6C6' },
        standardInfo: { backgroundColor: palette.status.infoSoft, color: '#2C55B8', borderColor: '#C7D8FB' },
      },
    },
    MuiDivider: {
      styleOverrides: { root: { borderColor: hairline } },
    },
    MuiSkeleton: {
      defaultProps: { animation: 'wave' },
      styleOverrides: {
        root: { backgroundColor: alpha(palette.silver.metallic, 0.28), borderRadius: radii.sm },
        wave: {
          '&::after': {
            background: `linear-gradient(90deg, transparent, ${alpha('#FFFFFF', 0.65)}, transparent)`,
          },
        },
      },
    },
    MuiCheckbox: {
      styleOverrides: {
        root: {
          color: palette.silver.dark,
          '&.Mui-checked': { color: palette.purple.main },
        },
      },
    },
    MuiRadio: {
      styleOverrides: {
        root: {
          color: palette.silver.dark,
          '&.Mui-checked': { color: palette.purple.main },
        },
      },
    },
    MuiSwitch: {
      styleOverrides: {
        root: { width: 42, height: 26, padding: 0 },
        switchBase: {
          padding: 3,
          '&.Mui-checked': {
            transform: 'translateX(16px)',
            color: '#FFFFFF',
            '& + .MuiSwitch-track': { backgroundColor: palette.purple.main, opacity: 1 },
          },
        },
        thumb: { width: 20, height: 20, boxShadow: shadows.xs },
        track: { borderRadius: radii.pill, backgroundColor: palette.silver.light, opacity: 1 },
      },
    },
    MuiAvatar: {
      styleOverrides: {
        root: { background: gradients.brand, color: palette.text.inverse, fontWeight: 700 },
      },
    },
    MuiListItemButton: {
      styleOverrides: {
        root: {
          borderRadius: radii.md,
          color: palette.text.primary,
          '&.Mui-selected': {
            backgroundColor: palette.purple.soft,
            color: palette.purple.deep,
            '&:hover': { backgroundColor: alpha(palette.purple.main, 0.14) },
          },
        },
      },
    },
    MuiListItemIcon: {
      styleOverrides: { root: { color: 'inherit', minWidth: 36 } },
    },
    MuiLink: {
      defaultProps: { underline: 'hover' },
      styleOverrides: {
        root: { color: palette.purple.main, fontWeight: 500, '&:hover': { color: palette.purple.deep } },
      },
    },
    MuiLinearProgress: {
      styleOverrides: {
        root: { height: 7, borderRadius: radii.pill, backgroundColor: palette.silver.light },
        bar: { borderRadius: radii.pill, background: gradients.primary },
      },
    },
    MuiCircularProgress: {
      styleOverrides: { circle: { strokeLinecap: 'round' } },
    },
    MuiAccordion: {
      defaultProps: { elevation: 0, disableGutters: true },
      styleOverrides: {
        root: {
          border: `1px solid ${hairline}`,
          borderRadius: `${radii.md}px !important`,
          backgroundColor: palette.background.card,
          backgroundImage: 'none',
          boxShadow: 'none',
          '&:before': { display: 'none' },
          '&.Mui-expanded': { margin: 0 },
        },
      },
    },
    MuiBadge: {
      styleOverrides: {
        badge: { fontWeight: 700, fontSize: '0.66rem' },
      },
    },
    MuiDataGrid: {
      styleOverrides: {
        root: {
          border: 'none',
          backgroundColor: palette.background.card,
          color: palette.text.primary,
          fontFamily: fonts.body,
          fontSize: '0.875rem',
          '--DataGrid-rowBorderColor': palette.border.soft,
        },
        columnHeaders: {
          backgroundColor: palette.background.secondary,
          borderBottom: `1px solid ${hairline}`,
        },
        columnHeader: {
          color: palette.text.secondary,
          fontWeight: 700,
          fontSize: '0.72rem',
          letterSpacing: '0.05em',
          textTransform: 'uppercase',
          '&:focus, &:focus-within': { outline: 'none' },
        },
        columnSeparator: { color: hairline },
        cell: {
          borderColor: palette.border.soft,
          color: palette.text.primary,
          '&:focus, &:focus-within': { outline: 'none' },
        },
        row: {
          '&:hover': { backgroundColor: alpha(palette.purple.main, 0.035) },
          '&.Mui-selected': {
            backgroundColor: palette.purple.soft,
            '&:hover': { backgroundColor: alpha(palette.purple.main, 0.12) },
          },
        },
        footerContainer: {
          borderTop: `1px solid ${hairline}`,
          backgroundColor: palette.background.card,
        },
        toolbarContainer: { borderBottom: `1px solid ${hairline}` },
        menu: { borderRadius: radii.md },
        paper: { borderRadius: radii.md, boxShadow: shadows.popover },
        overlay: { backgroundColor: alpha('#FFFFFF', 0.7) },
        withBorderColor: { borderColor: hairline },
      },
    },
  },
  custom: {
    showcaseBg: gradients.lavender,
    loginCardShadow: shadows.cardHover,
    loginInputBg: '#FFFFFF',
    gradients,
    shadows,
    radii,
    brand: palette,
    navHeight: 68,
  },
})
