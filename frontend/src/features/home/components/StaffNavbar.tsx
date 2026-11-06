import { useState } from 'react'
import {
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
  Avatar,
  Menu,
  MenuItem,
} from '@mui/material'
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth, type Role } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, LogoutRounded, PersonOutlineRounded } from '@mui/icons-material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { NotificationBell } from '../../../shared/components/NotificationBell'
import { palette } from '../../../shared/theme/tokens'

/**
 * Navbar for staff roles. Same light metallic shell as CustomerNavbar so every
 * role sees one consistent site; only the links change. Two primary links per
 * role on the bar, the rest in the avatar menu.
 */

interface NavLink {
  label: string
  href: string
}

const PRIMARY: Partial<Record<Role, NavLink[]>> = {
  LabManager: [
    { label: 'Hàng đợi sản xuất', href: '/lab/queue' },
    { label: 'Máy in xưởng', href: '/lab/machines' },
    { label: 'Lịch máy & Điểm', href: '/lab/timeline' },
  ],
  LabOperator: [
    { label: 'Hàng đợi sản xuất', href: '/lab/queue' },
    { label: 'Lịch máy & Điểm', href: '/lab/timeline' },
  ],
  HubQC: [
    { label: 'Kiểm tra chất lượng', href: '/hub/qc' },
    { label: 'Hoàn tất & Gộp kiện', href: '/hub/shipments' },
  ],
  HubFulfillment: [
    { label: 'Hoàn tất & Gộp kiện', href: '/hub/shipments' },
    { label: 'Kiểm tra chất lượng', href: '/hub/qc' },
  ],
  OpsManager: [
    { label: 'Bảng điều phối', href: '/scheduling' },
    { label: 'Duyệt xưởng & máy', href: '/ops/labs' },
  ],
  Admin: [
    { label: 'Tổng quan quản trị', href: '/admin' },
    // Admin and Operations are separate consoles: the admin bar never links into /ops,
    // /scheduling, /quote-reviews or /model-reviews.
    { label: 'Duyệt xưởng (quản trị)', href: '/admin/labs' },
    { label: 'Profile & duyệt máy', href: '/admin/cura-profiles' },
  ],
}

const MORE: Partial<Record<Role, NavLink[]>> = {
  LabManager: [
    { label: 'Đăng ký / Hồ sơ xưởng', href: '/lab/register' },
  ],
  LabOperator: [
    { label: 'Máy in xưởng', href: '/lab/machines' },
    { label: 'Đăng ký / Hồ sơ xưởng', href: '/lab/register' },
  ],
  OpsManager: [
    { label: 'Duyệt báo giá', href: '/quote-reviews' },
    { label: 'Kiểm duyệt model', href: '/model-reviews' },
    { label: 'Nghiệm thu QC', href: '/staff/qc-proofs' },
    { label: 'Cảnh báo', href: '/ops/escalations' },
    { label: 'Nhật ký quyết định', href: '/ops/decisions' },
    { label: 'Báo cáo SLA', href: '/ops/sla' },
    { label: 'Thanh toán', href: '/ops/payments' },
    { label: 'Hoàn tiền', href: '/ops/refunds' },
    { label: 'Yêu cầu thiết kế', href: '/ops/design-requests' },
  ],
  Admin: [
    { label: 'Người dùng & phân quyền', href: '/admin/users' },
    { label: 'Kho tri thức QC', href: '/admin/qc-knowledge' },
    { label: 'Cấu hình thương mại', href: '/admin/commercial-configurations' },
    { label: 'Catalogue dùng chung', href: '/admin/catalogs' },
    { label: 'Trọng số chấm điểm', href: '/admin/scoring-configurations' },
    { label: 'Nhật ký kiểm toán', href: '/admin/audit-logs' },
  ],
}

const linkSx = {
  color: palette.text.secondary,
  fontWeight: 500,
  fontSize: '0.9rem',
  px: 1.5,
  py: 0.75,
  borderRadius: '10px',
  transition: 'background-color 160ms ease, color 160ms ease',
  '&:hover': { color: palette.text.primary, bgcolor: palette.purple.soft },
} as const

/**
 * The role's main console — used by the homepage hero so a logged-in staff member gets
 * an action instead of a signup button. Null when the role has no console link yet.
 */
export function staffPrimaryLink(roles: Role[]): NavLink | null {
  for (const role of roles) {
    const links = PRIMARY[role]
    if (links && links.length > 0) return links[0]
  }
  return null
}

export function StaffNavbar() {
  const { logout, user } = useAuth()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

  /** A link is current when the browser is on it or on one of its sub-pages. */
  const isActive = (href: string) =>
    href === '/admin'
      // The admin overview is a leaf: don't light it up on every /admin/* section.
      ? pathname === '/admin'
      : pathname === href || pathname.startsWith(`${href}/`)

  const roles = user?.roles ?? []
  const primary = roles.flatMap((role) => PRIMARY[role] ?? []).filter(
    (link, index, all) => all.findIndex((other) => other.href === link.href) === index,
  )
  const more = roles.flatMap((role) => MORE[role] ?? []).filter(
    (link, index, all) =>
      all.findIndex((other) => other.href === link.href) === index &&
      !primary.some((p) => p.href === link.href),
  )

  const handleLogout = () => {
    setAnchorEl(null)
    setDrawerOpen(false)
    // AuthContext.logout() clears the session and returns to the guest homepage.
    logout()
  }

  const go = (href: string) => {
    setDrawerOpen(false)
    navigate(href)
  }

  return (
    <Box
      component="header"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1200,
        bgcolor: 'rgba(255,255,255,0.84)',
        backdropFilter: 'blur(16px)',
        borderBottom: `1px solid ${palette.border.hairline}`,
        boxShadow: '0 1px 0 rgba(37,38,58,0.02)',
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.25 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          <Button
            component={RouterLink}
            to="/"
            onClick={() => setDrawerOpen(false)}
            aria-label="PrintGrid — về trang chủ"
            sx={{ p: 0, textTransform: 'none', minWidth: 0, '&:hover': { bgcolor: 'transparent', boxShadow: 'none' } }}
            disableRipple
          >
            <BrandGlyph size={32} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 800, color: palette.text.primary, lineHeight: 1.15, letterSpacing: '-0.02em' }}>
                PrintGrid
              </Typography>
              <Typography sx={{ fontSize: '0.56rem', color: 'text.muted', letterSpacing: '0.14em', textTransform: 'uppercase', lineHeight: 1.4, fontWeight: 600 }}>
                {roles[0] ?? 'Staff'}
              </Typography>
            </Box>
          </Button>

          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {primary.map((link) => (
              <Button
                key={link.href}
                onClick={() => go(link.href)}
                sx={{
                  ...linkSx,
                  textTransform: 'none',
                  color: isActive(link.href) ? palette.purple.deep : palette.text.secondary,
                  fontWeight: isActive(link.href) ? 700 : 500,
                  bgcolor: isActive(link.href) ? palette.purple.soft : 'transparent',
                  boxShadow: isActive(link.href) ? 'inset 0 0 0 1px rgba(111,85,255,0.18)' : 'none',
                  '&:hover': { bgcolor: palette.purple.soft, boxShadow: 'none' },
                }}
              >
                {link.label}
              </Button>
            ))}
          </Stack>

          <NotificationBell />

          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ p: 0.5 }} aria-label="Menu tài khoản">
              <Avatar sx={{ width: 34, height: 34, fontSize: 14 }}>
                {user?.email?.charAt(0).toUpperCase() || 'S'}
              </Avatar>
            </IconButton>
            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={() => setAnchorEl(null)}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
              slotProps={{ paper: { sx: { minWidth: 240, maxHeight: 420 } } }}
            >
              <MenuItem disabled sx={{ opacity: '1 !important' }}>
                <PersonOutlineRounded fontSize="small" sx={{ mr: 1.25 }} />
                <Typography sx={{ fontSize: '0.82rem', color: 'text.secondary' }}>{user?.email}</Typography>
              </MenuItem>
              <Divider sx={{ my: 0.5 }} />
              {more.map((link) => (
                <MenuItem
                  key={link.href}
                  onClick={() => { setAnchorEl(null); go(link.href) }}
                  selected={isActive(link.href)}
                  sx={{ fontWeight: isActive(link.href) ? 700 : 400 }}
                >
                  {link.label}
                </MenuItem>
              ))}
              {more.length > 0 && <Divider sx={{ my: 0.5 }} />}
              <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
                <LogoutRounded fontSize="small" sx={{ mr: 1.25 }} />
                Đăng xuất
              </MenuItem>
            </Menu>
          </Stack>

          <IconButton onClick={() => setDrawerOpen(true)} sx={{ display: { md: 'none' }, color: 'text.primary' }} aria-label="Mở menu">
            <MenuRounded />
          </IconButton>
        </Stack>
      </Container>

      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} slotProps={{ paper: { sx: { width: 300 } } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} aria-label="Đóng menu">
            <CloseRounded />
          </IconButton>
        </Box>
        <List sx={{ px: 1 }}>
          {[...primary, ...more].map((link) => (
            <ListItem key={link.href} disablePadding>
              <ListItemButton onClick={() => go(link.href)} selected={isActive(link.href)} sx={{ mb: 0.25 }}>
                <ListItemText
                  primary={link.label}
                  slotProps={{ primary: { fontWeight: isActive(link.href) ? 700 : 500, fontSize: '0.9rem' } }}
                />
              </ListItemButton>
            </ListItem>
          ))}
          <ListItem disablePadding sx={{ mt: 2 }}>
            <Box sx={{ px: 1, width: '100%' }}>
              <Button fullWidth variant="outlined" color="error" onClick={handleLogout}>
                Đăng xuất
              </Button>
            </Box>
          </ListItem>
        </List>
      </Drawer>
    </Box>
  )
}
