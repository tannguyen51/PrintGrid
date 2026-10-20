import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { CustomerNavbar } from '../features/home/components/CustomerNavbar'

/**
 * Shared shell for signed-in customer pages: the customer navbar stays pinned above
 * model library, orders and order configuration instead of those being standalone pages.
 */
export function CustomerLayout() {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <CustomerNavbar />
      <Box component="main">
        <Outlet />
      </Box>
    </Box>
  )
}
