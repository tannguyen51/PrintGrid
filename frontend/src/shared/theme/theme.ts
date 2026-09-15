import { createTheme } from '@mui/material/styles'

declare module '@mui/material/styles' {
  interface Theme {
    custom: {
      showcaseBg: string
      loginCardShadow: string
      loginInputBg: string
    }
  }
  interface ThemeOptions {
    custom?: {
      showcaseBg?: string
      loginCardShadow?: string
      loginInputBg?: string
    }
  }
}

export const printGridTheme = createTheme({
  palette: {
    mode: 'dark',
    // Unified premium-tech dark palette (PrintGrid brand)
    primary: { main: '#8B5CF6', light: '#A78BFA', dark: '#7C3AED' },
    secondary: { main: '#8B5CF6' },
    success: { main: '#34c759' },
    warning: { main: '#f5a623' },
    error: { main: '#ff453a' },
    info: { main: '#0a84ff' },
    background: {
      default: '#050505',
      paper: '#0A0A0A',
    },
    divider: 'rgba(255,255,255,0.12)',
    text: {
      primary: '#FFFFFF',
      secondary: '#8A8A8A',
    },
    common: { black: '#000000', white: '#FFFFFF' },
    grey: {
      200: '#262626',
      800: '#141414',
      900: '#0A0A0A',
    },
  },
  shape: { borderRadius: 14 },
  typography: {
    fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif',
    h1: { fontSize: '1.9rem', fontWeight: 650 },
    h2: { fontSize: '1.5rem', fontWeight: 620 },
    h4: { fontWeight: 700, letterSpacing: '0.01em' },
    subtitle1: { fontWeight: 600 },
    body1: { lineHeight: 1.6 },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  components: {
    MuiPaper: {
      defaultProps: { elevation: 0 },
      styleOverrides: {
        root: { border: '1px solid rgba(255,255,255,0.12)', backgroundImage: 'none' },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 12, boxShadow: 'none', '&:hover': { boxShadow: 'none' } },
      },
    },
    MuiTextField: {
      defaultProps: { variant: 'outlined' },
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 12,
            '& fieldset': { borderColor: 'rgba(255,255,255,0.16)' },
            '&:hover fieldset': { borderColor: 'rgba(255,255,255,0.3)' },
            '&.Mui-focused fieldset': { borderColor: '#8B5CF6', borderWidth: 1 },
          },
          '& .MuiInputLabel-root': { color: '#8A8A8A' },
        },
      },
    },
    MuiCheckbox: {
      styleOverrides: {
        root: { color: '#8A8A8A', '&.Mui-checked': { color: '#8B5CF6' } },
      },
    },
  },
  custom: {
    // Uniform pure black — matches background.default (#050505) for a seamless dark login.
    showcaseBg: '#050505',
    loginCardShadow: '0 24px 80px rgba(0,0,0,0.55)',
    loginInputBg: '#050505',
  },
})
