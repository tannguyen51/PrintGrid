import { useState } from 'react'
import { Box } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { AuthDialog, type AuthMode } from '../auth/components/AuthDialog'
import { HomeFooter } from './components/HomeFooter'
import { useAuth } from '../../app/AuthContext'
import { CustomerNavbar } from './components/CustomerNavbar'
import { StaffNavbar, staffPrimaryLink } from './components/StaffNavbar'
import { PublicNavbar } from './landing/PublicNavbar'
import { PublicHero } from './landing/PublicHero'
import { PublicSections } from './landing/PublicSections'
import { pageWash } from '../../shared/theme/tokens'
import { CustomerHomeDashboard } from './CustomerHomeDashboard'

/** Public visitors and staff keep the marketing homepage; signed-in customers
 * receive a focused workspace with their orders, models and primary actions. */
export default function HomePage() {
  const [authMode, setAuthMode] = useState<AuthMode | null>(null)
  const { isAuthenticated, user } = useAuth()
  const navigate = useNavigate()

  const openLogin = () => setAuthMode('login')
  const openRegister = () => setAuthMode('register')

  const isCustomer = user?.roles?.includes('Customer') ?? false
  const showCustomerNavbar = isAuthenticated && isCustomer
  const showStaffNavbar = isAuthenticated && !isCustomer
  // Each signed-in role gets an action, never the signup button: customers order,
  // staff jump to their console. Null hides the button when there is no console yet.
  const staffLink = showStaffNavbar ? staffPrimaryLink(user?.roles ?? []) : null

  if (showCustomerNavbar) {
    return (
      <Box sx={{ minHeight: '100vh', background: pageWash }}>
        <CustomerNavbar />
        <CustomerHomeDashboard />
      </Box>
    )
  }

  return (
    <Box sx={{ minHeight: '100vh', background: pageWash }}>
      {showStaffNavbar ? (
        <StaffNavbar />
      ) : (
        <PublicNavbar onLogin={openLogin} onRegister={openRegister} />
      )}

      <PublicHero
        onRegister={openRegister}
        cta={
          showStaffNavbar
              ? staffLink
                ? { label: staffLink.label, onClick: () => navigate(staffLink.href) }
                : null
              : undefined
        }
      />
      <PublicSections />

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
