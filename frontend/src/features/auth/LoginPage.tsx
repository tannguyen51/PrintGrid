import { Box } from '@mui/material'
import { LoginShowcase } from './components/LoginShowcase'
import { LoginForm } from './components/LoginForm'

/**
 * Split-panel login page — unified dark theme.
 * Left: login card. Right: 3D statue showcase + PrintGrid brand.
 * Below md breakpoint: stack vertically, showcase compressed to a band.
 */
export default function LoginPage() {
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