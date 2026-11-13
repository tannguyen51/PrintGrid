import { Box } from '@mui/material'
import { Outlet, useLocation } from 'react-router-dom'
import { StaffNavbar } from '../features/home/components/StaffNavbar'
import { pageWash } from '../shared/theme/tokens'

/**
 * Shared shell for every staff console page (lab, hub, ops, admin): the site navbar
 * stays pinned above the page instead of each screen being its own standalone app.
 * The navbar owns the role links and the active highlight, so moving between consoles
 * no longer drops the user into a page with no navigation.
 */
export function StaffLayout() {
  const { pathname } = useLocation()
  // Administration owns its responsive navigation and full-height workspace.
  if (pathname === '/admin' || pathname.startsWith('/admin/')) return <Outlet />

  return (
    <Box sx={{ minHeight: '100vh', background: pageWash }}>
      <StaffNavbar />
      <Box component="main">
        <Outlet />
      </Box>
    </Box>
  )
}
