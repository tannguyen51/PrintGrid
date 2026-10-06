import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Box } from '@mui/material'
import { LoginShowcase } from './components/LoginShowcase'
import { LoginForm } from './components/LoginForm'
import { useAuth } from '../../app/AuthContext'

/**
 * Split-panel login page — unified dark theme.
 * Left: login card. Right: 3D statue showcase + PrintGrid brand.
 * Below md breakpoint: stack vertically, showcase compressed to a band.
 */
export default function LoginPage() {
  const { isAuthenticated, user } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    if (isAuthenticated && user) {
      const roles = user.roles ?? []
      if (roles.includes('HubQC') || roles.includes('HubFulfillment')) {
        navigate('/hub/qc', { replace: true })
      } else if (roles.includes('LabManager') || roles.includes('LabOperator')) {
        navigate('/lab/queue', { replace: true })
      } else if (roles.includes('OpsManager') || roles.includes('Admin')) {
        navigate('/scheduling', { replace: true })
      } else {
        navigate('/models', { replace: true })
      }
    }
  }, [isAuthenticated, user, navigate])
  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        flexDirection: { xs: 'column', md: 'row' },
        bgcolor: 'background.default', // #050505 everywhere
      }}
    >
      <LoginForm />
      <LoginShowcase />
    </Box>
  )
}