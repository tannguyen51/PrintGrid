import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { StaffNavbar } from '../features/home/components/StaffNavbar'

/**
 * Shared shell for every staff console page (lab, hub, ops, admin): the site navbar
 * stays pinned above the page instead of each screen being its own standalone app.
 * The navbar owns the role links and the active highlight, so moving between consoles
 * no longer drops the user into a page with no navigation.
 */
export function StaffLayout() {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <StaffNavbar />
      <Box component="main">
        <Outlet />
      </Box>
    </Box>
  )
}
