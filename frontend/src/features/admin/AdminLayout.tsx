import {
  Box,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Typography,
} from '@mui/material'
import {
  CategoryRounded,
  DashboardRounded,
  FactCheckRounded,
  HistoryRounded,
  InsightsRounded,
  PeopleRounded,
  StorefrontRounded,
  TuneRounded,
  ViewInArRounded,
} from '@mui/icons-material'
import type { ReactNode } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'

interface AdminSection {
  label: string
  href: string
  icon: ReactNode
}

/** Every console the Admin role owns (mirrors the backend `RequireAdmin` controllers). */
const SECTIONS: AdminSection[] = [
  { label: 'Tổng quan', href: '/admin', icon: <DashboardRounded fontSize="small" /> },
  { label: 'Người dùng & phân quyền', href: '/admin/users', icon: <PeopleRounded fontSize="small" /> },
  { label: 'Duyệt xưởng', href: '/admin/labs', icon: <StorefrontRounded fontSize="small" /> },
  { label: 'Kho tri thức QC', href: '/admin/qc-knowledge', icon: <FactCheckRounded fontSize="small" /> },
  { label: 'Cấu hình thương mại', href: '/admin/commercial-configurations', icon: <TuneRounded fontSize="small" /> },
  { label: 'Catalogue dùng chung', href: '/admin/catalogs', icon: <CategoryRounded fontSize="small" /> },
  { label: 'Kho mẫu 3D', href: '/admin/samples', icon: <ViewInArRounded fontSize="small" /> },
  { label: 'Trọng số chấm điểm', href: '/admin/scoring-configurations', icon: <InsightsRounded fontSize="small" /> },
  { label: 'Nhật ký kiểm toán', href: '/admin/audit-logs', icon: <HistoryRounded fontSize="small" /> },
]

function isActive(pathname: string, href: string): boolean {
  if (href === '/admin') return pathname === '/admin'
  return pathname === href || pathname.startsWith(`${href}/`)
}

/**
 * Admin console shell: the staff navbar stays on top and a section sidebar keeps every
 * configuration surface one click away. The overview page is the landing screen; each
 * section is the existing page for that capability.
 */
export function AdminLayout() {
  const navigate = useNavigate()
  const { pathname } = useLocation()

  return (
    <Box
      sx={{
        maxWidth: 1400,
        mx: 'auto',
        p: { xs: 2, md: 3 },
        display: 'flex',
        gap: 3,
        alignItems: 'flex-start',
      }}
    >
      <Paper
        component="nav"
        variant="outlined"
        sx={{
          width: 250,
          flexShrink: 0,
          p: 1,
          position: 'sticky',
          top: 84,
          display: { xs: 'none', md: 'block' },
        }}
      >
        <Typography
          sx={{
            px: 1.5,
            py: 1,
            fontSize: '0.72rem',
            fontWeight: 700,
            letterSpacing: '0.08em',
            textTransform: 'uppercase',
            color: 'text.secondary',
          }}
        >
          Quản trị hệ thống
        </Typography>
        <List dense disablePadding>
          {SECTIONS.map((section) => {
            const active = isActive(pathname, section.href)
            return (
              <ListItemButton
                key={section.href}
                selected={active}
                onClick={() => navigate(section.href)}
                sx={{ borderRadius: 2, mb: 0.25 }}
              >
                <ListItemIcon sx={{ minWidth: 34, color: active ? 'primary.main' : 'text.secondary' }}>
                  {section.icon}
                </ListItemIcon>
                <ListItemText
                  primary={section.label}
                  primaryTypographyProps={{
                    fontSize: '0.86rem',
                    fontWeight: active ? 700 : 500,
                    color: active ? 'primary.main' : 'text.primary',
                  }}
                />
              </ListItemButton>
            )
          })}
        </List>
      </Paper>

      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Outlet />
      </Box>
    </Box>
  )
}
