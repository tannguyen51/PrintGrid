import { useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Box } from '@mui/material'
import { LoginShowcase } from './components/LoginShowcase'
import { LoginForm } from './components/LoginForm'
import { useAuth } from '../../app/AuthContext'
import { resolvePostLoginPath } from './postLoginRedirect'

/**
 * Split-panel login page — light premium theme.
 * Left: login card on the page wash. Right: 3D showcase with lavender lighting.
 * Below md breakpoint: stack vertically, showcase compressed to a band.
 */
export default function LoginPage() {
  const { isAuthenticated, user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  useEffect(() => {
    if (isAuthenticated && user) {
      // Same deep-link rule as the form: an already-signed-in visitor who lands on
      // /login still returns to the page they originally requested.
      navigate(resolvePostLoginPath(location.state, user.roles), { replace: true })
    }
  }, [isAuthenticated, user, navigate, location.state])
  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        flexDirection: { xs: 'column', md: 'row' },
        background: 'transparent',
      }}
    >
      <LoginForm />
      <LoginShowcase />
    </Box>
  )
}
