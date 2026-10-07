import { useState } from 'react'
import { Box } from '@mui/material'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { HomeHeader } from './components/HomeHeader'
import { Hero } from './components/Hero'
import { FeatureGrid } from './components/FeatureGrid'
import { HowItWorks } from './components/HowItWorks'
import { CTABand } from './components/CTABand'
import { HomeFooter } from './components/HomeFooter'
import { useAuth } from '../../app/AuthContext'
import { QuickActions } from './components/QuickActions'
import { CustomerNavbar } from './components/CustomerNavbar'
import { NetworkStatsSection } from './components/NetworkStatsSection'
import { HowItWorksSection } from './components/HowItWorksSection'
import { WhyPrintGridSection } from './components/WhyPrintGridSection'
import { MaterialsSection } from './components/MaterialsSection'
import { SampleModelsSection } from './components/SampleModelsSection'
import { DesignRequestBanner } from './components/DesignRequestBanner'
import { FaqSection } from './components/FaqSection'
import { PublicNavbar } from './landing/PublicNavbar'
import { PublicHero } from './landing/PublicHero'
import { PublicSections } from './landing/PublicSections'

export default function HomePage() {
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)
  const { isAuthenticated, user } = useAuth()

  const openLogin = () => setAuthMode('login')
  const openRegister = () => setAuthMode('register')

  // Chỉ hiện giao diện mới cho Customer đã đăng nhập
  const isCustomer = user?.roles?.includes('Customer') ?? false;
  const showCustomerDashboard = isAuthenticated && isCustomer;
  // Khách (chưa đăng nhập) xem landing redesign theo Figma "PrintGrid homepage"
  const showPublicLanding = !isAuthenticated;

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: showPublicLanding ? '#000000' : 'background.default' }}>
      {showPublicLanding ? (
        <PublicNavbar onLogin={openLogin} onRegister={openRegister} />
      ) : showCustomerDashboard ? (
        <CustomerNavbar onLogin={openLogin} onRegister={openRegister} />
      ) : (
        <HomeHeader onLogin={openLogin} onRegister={openRegister} />
      )}

      {showPublicLanding ? (
        <>
          <PublicHero onRegister={openRegister} />
          <PublicSections />
        </>
      ) : showCustomerDashboard ? (
        <>
          <QuickActions />
          <NetworkStatsSection />
          <HowItWorksSection />
          <WhyPrintGridSection />
          <MaterialsSection />
          <SampleModelsSection />
          <DesignRequestBanner />
          <FaqSection />
        </>
      ) : (
        <>
          <Hero onLogin={openLogin} onRegister={openRegister} />
          <FeatureGrid />
          <HowItWorks />
          <CTABand onLogin={openLogin} onRegister={openRegister} />
        </>
      )}

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