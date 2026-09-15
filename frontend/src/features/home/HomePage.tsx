import { useState } from 'react'
import { Box } from '@mui/material'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { HomeHeader } from './components/HomeHeader'
import { Hero } from './components/Hero'
import { FeatureGrid } from './components/FeatureGrid'
import { HowItWorks } from './components/HowItWorks'
import { CTABand } from './components/CTABand'
import { HomeFooter } from './components/HomeFooter'

/**
 * Public landing page for PrintGrid.
 * Built on the same design system as the auth pages (dark #050505,
 * orange accent, premium-minimal). Auth actions open a large modal instead of
 * navigating away; login shows form + 3D showcase, register shows form only.
 */
export default function HomePage() {
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)

  const openLogin = () => setAuthMode('login')
  const openRegister = () => setAuthMode('register')

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <HomeHeader onLogin={openLogin} onRegister={openRegister} />
      <Hero onLogin={openLogin} onRegister={openRegister} />
      <FeatureGrid />
      <HowItWorks />
      <CTABand onLogin={openLogin} onRegister={openRegister} />
      <HomeFooter onLogin={openLogin} onRegister={openRegister} />

      <AuthDialog
        open={authMode !== null}
        mode={authMode ?? 'login'}
        onClose={() => setAuthMode(null)}
        onSwitchMode={setAuthMode}
      />
    </Box>
  )
}