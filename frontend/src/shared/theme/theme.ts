import { createTheme } from '@mui/material/styles'

export const printGridTheme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1f6feb' },
    secondary: { main: '#f97316' },
    success: { main: '#16a34a' },
    warning: { main: '#d97706' },
    error: { main: '#dc2626' },
    background: { default: '#f6f7f9' },
  },
  shape: { borderRadius: 10 },
  typography: {
    fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif',
    h1: { fontSize: '1.9rem', fontWeight: 650 },
    h2: { fontSize: '1.5rem', fontWeight: 620 },
    button: { textTransform: 'none', fontWeight: 560 },
  },
  components: {
    MuiPaper: { defaultProps: { elevation: 0 }, styleOverrides: { root: { border: '1px solid #e4e7ec' } } },
  },
})
