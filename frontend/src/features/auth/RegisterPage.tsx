import { Box } from '@mui/material'
import { RegisterForm } from './components/RegisterForm'
import { LoginShowcase } from './components/LoginShowcase'

/**
 * Split-panel register page — same design system as login.
 * Left: register card. Right: 3D showcase + PrintGrid brand.
 * Below md breakpoint: stack vertically, showcase compressed to a band.
 */
export default function RegisterPage() {
  return (
    <Box
      sx={{
        display: 'flex',
        minHeight: '100vh',
        flexDirection: { xs: 'column', md: 'row' },
        bgcolor: 'background.default', // #050505 everywhere
      }}
    >
      <RegisterForm />
      <LoginShowcase />
    </Box>
  )
}