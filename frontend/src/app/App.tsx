import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { AuthProvider } from './AuthContext'
import { queryClient } from './queryClient'
import { AppRoutes } from '../routes/AppRoutes'
import { printGridTheme } from '../shared/theme/theme'

export function App() {
  return (
    <ThemeProvider theme={printGridTheme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          {/* Inside the router so AuthContext.logout() can send the visitor to the guest homepage. */}
          <AuthProvider>
            <AppRoutes />
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
