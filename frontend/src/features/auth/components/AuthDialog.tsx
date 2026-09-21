import { Dialog, Box, useMediaQuery, useTheme } from '@mui/material'
import { LoginForm } from './LoginForm'
import { RegisterForm } from './RegisterForm'
import { LoginShowcase } from './LoginShowcase'

export type AuthMode = 'login' | 'register'

interface AuthDialogProps {
  open: boolean
  mode: AuthMode
  onClose: () => void
  onSwitchMode: (mode: AuthMode) => void
}

/**
 * Large modal auth screen, opened from the homepage instead of navigating away.
 * - login:    form on the left + 3D showcase on the right (matches the /login page)
 * - register: form only — no 3D showcase (full-bleed form)
 */
export function AuthDialog({ open, mode, onClose, onSwitchMode }: AuthDialogProps) {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="xl"
      PaperProps={{
        sx: {
          overflow: 'hidden',
          borderRadius: { xs: 3, md: 5 },
          bgcolor: 'background.default',
          backgroundImage: 'none',
          width: { md: 1360 },
          maxWidth: { md: '95vw' },
          height: { md: 760 },
          maxHeight: { md: '92vh' },
          margin: { xs: 1.5, md: 0 },
        },
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', md: 'row' },
          height: '100%',
          width: '100%',
          overflow: 'hidden',
        }}
      >
        {mode === 'login' ? (
          <>
            <LoginForm onSwitchToRegister={() => onSwitchMode('register')} onSuccess={onClose} />
            {isDesktop && (
              // Critical: this wrapper must stretch (flex) so the showcase's
              // absolutely-positioned canvas has a real height to fill.
              <Box sx={{ flex: { md: '1 1 52%' }, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
                <LoginShowcase frame />
              </Box>
            )}
          </>
        ) : (
          <RegisterForm fullBleed onSwitchToLogin={() => onSwitchMode('login')} onSuccess={onClose} />
        )}
      </Box>
    </Dialog>
  )
}