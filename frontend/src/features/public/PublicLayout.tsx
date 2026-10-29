import { useEffect, useState, type ReactNode } from 'react'
import { Box } from '@mui/material'
import { Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { HomeFooter } from '../home/components/HomeFooter'
import { CustomerNavbar } from '../home/components/CustomerNavbar'
import { StaffNavbar } from '../home/components/StaffNavbar'
import { PublicNavbar } from '../home/landing/PublicNavbar'
import { landing } from '../../shared/theme/landing'

/**
 * Shell for the public screens behind the guest navbar (samples, pricing, network,
 * guides). Mirrors HomePage's role switch so a signed-in visitor keeps their own
 * navbar instead of being thrown back to the visitor one.
 */
export function PublicLayout() {
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)
  const { isAuthenticated, user } = useAuth()
  const { pathname } = useLocation()

  // Public pages are separate routes; without this a deep link opens mid-page.
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'auto' })
  }, [pathname])

  const isCustomer = user?.roles?.includes('Customer') ?? false
  const showCustomerNavbar = isAuthenticated && isCustomer
  const showStaffNavbar = isAuthenticated && !isCustomer

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: landing.pageBg, display: 'flex', flexDirection: 'column' }}>
      {showCustomerNavbar ? (
        <CustomerNavbar />
      ) : showStaffNavbar ? (
        <StaffNavbar />
      ) : (
        <PublicNavbar onLogin={() => setAuthMode('login')} onRegister={() => setAuthMode('register')} />
      )}

      <Box component="main" sx={{ flex: 1 }}>
        <Outlet />
      </Box>

      <HomeFooter onLogin={() => setAuthMode('login')} onRegister={() => setAuthMode('register')} />

      <AuthDialog
        open={authMode !== null}
        mode={authMode ?? 'login'}
        onClose={() => setAuthMode(null)}
        onSwitchMode={setAuthMode}
      />
    </Box>
  )
}

/** Page gutter shared by every public screen (matches the landing's 104px rhythm). */
export function PublicPage({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        px: { xs: '20px', md: '40px', lg: '104px' },
        py: { xs: '40px', lg: '72px' },
        maxWidth: 1440,
        mx: 'auto',
      }}
    >
      {children}
    </Box>
  )
}

export function PageHeading({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow: string
  title: string
  description?: string
  actions?: ReactNode
}) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: { xs: 'column', md: 'row' },
        alignItems: { xs: 'flex-start', md: 'flex-end' },
        justifyContent: 'space-between',
        gap: 3,
        pb: { xs: 4, md: 5 },
      }}
    >
      <Box sx={{ maxWidth: 760 }}>
        <Box sx={{ fontSize: 13, letterSpacing: '0.14em', textTransform: 'uppercase', color: landing.textMuted, mb: 1.5 }}>
          {eyebrow}
        </Box>
        <Box
          component="h1"
          sx={{
            m: 0,
            fontFamily: '"Domine", serif',
            fontSize: { xs: 'clamp(30px, 5vw, 40px)', lg: 52 },
            lineHeight: 1.15,
            fontWeight: 500,
            color: landing.text,
          }}
        >
          {title}
        </Box>
        {description && (
          <Box sx={{ mt: 2, fontSize: 17, lineHeight: 1.6, color: landing.textMuted, maxWidth: 680 }}>{description}</Box>
        )}
      </Box>
      {actions && <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap' }}>{actions}</Box>}
    </Box>
  )
}
