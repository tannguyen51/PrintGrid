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
          // The dialog frame belongs to the mode: login keeps the wide split panel,
          // register hugs its own form so the login frame never appears around it.
          width: { md: mode === 'login' ? 1360 : 560 },
          maxWidth: { md: mode === 'login' ? '95vw' : '92vw' },
          height: { md: mode === 'login' ? 760 : 'auto' },
          maxHeight: { md: '92vh' },
          margin: { xs: 1.5, md: 0 },
          transition: 'width .22s ease, height .22s ease',
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
          // Short screens / tall register form scroll inside the rounded paper
          // instead of clipping the submit button.
          overflowY: 'auto',
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