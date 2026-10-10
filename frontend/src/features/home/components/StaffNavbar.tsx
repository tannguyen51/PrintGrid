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
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth, type Role } from '../../../app/AuthContext'
import { MenuRounded, CloseRounded, LogoutRounded, PersonOutlineRounded } from '@mui/icons-material'
import { BrandGlyph } from '../../../shared/components/BrandGlyph'
import { landing } from '../../../shared/theme/landing'

/**
 * Navbar for staff roles on the shared homepage (08/10). Same landing token system as
 * CustomerNavbar so every role sees one consistent site; only the links change.
 * Two primary links per role on the bar, the rest in the avatar menu — a console link
 * list long enough to wrap is worse than one extra click.
 */

interface NavLink {
  label: string
  href: string
}

const PRIMARY: Partial<Record<Role, NavLink[]>> = {
  LabManager: [{ label: 'Hàng đợi sản xuất', href: '/lab/queue' }],
  LabOperator: [{ label: 'Hàng đợi sản xuất', href: '/lab/queue' }],
  HubQC: [
    { label: 'Kiểm tra chất lượng', href: '/hub/qc' },
    { label: 'Lô giao hàng', href: '/hub/shipments' },
  ],
  HubFulfillment: [
    { label: 'Lô giao hàng', href: '/hub/shipments' },
    { label: 'Kiểm tra chất lượng', href: '/hub/qc' },
  ],
  OpsManager: [
    { label: 'Bảng điều phối', href: '/scheduling' },
    { label: 'Xưởng & máy', href: '/ops/labs' },
  ],
  Admin: [
    { label: 'Bảng điều phối', href: '/scheduling' },
    { label: 'Xưởng & máy', href: '/ops/labs' },
  ],
  OrderStaff: [
    { label: 'Duyệt báo giá', href: '/quote-reviews' },
    { label: 'Nghiệm thu QC', href: '/staff/qc-proofs' },
  ],
}

const MORE: Partial<Record<Role, NavLink[]>> = {
  OpsManager: [
    { label: 'Duyệt báo giá', href: '/quote-reviews' },
    { label: 'Nghiệm thu QC', href: '/staff/qc-proofs' },
    { label: 'Cảnh báo', href: '/ops/escalations' },
    { label: 'Nhật ký quyết định', href: '/ops/decisions' },
  ],
  Admin: [
    { label: 'Duyệt báo giá', href: '/quote-reviews' },
    { label: 'Nghiệm thu QC', href: '/staff/qc-proofs' },
    { label: 'Cảnh báo', href: '/ops/escalations' },
    { label: 'Nhật ký quyết định', href: '/ops/decisions' },
    { label: 'Người dùng & phân quyền', href: '/admin/users' },
  ],
}

const linkSx = {
  color: landing.textMuted,
  fontWeight: 500,
  fontSize: '0.9rem',
  px: 1.5,
  py: 0.75,
  borderRadius: `${landing.radius}px`,
  '&:hover': { color: landing.text, bgcolor: 'rgba(255,255,255,0.05)' },
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
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)

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
    logout()
    navigate('/', { replace: true })
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
        bgcolor: 'rgba(0,0,0,0.72)',
        backdropFilter: 'blur(10px)',
        borderBottom: `1px solid ${landing.hairline}`,
      }}
    >
      <Container maxWidth="lg" sx={{ py: 1.5 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
          <Button
            component={RouterLink}
            to="/"
            onClick={() => setDrawerOpen(false)}
            aria-label="PrintGrid — về trang chủ"
            sx={{ p: 0, textTransform: 'none', minWidth: 0, '&:hover': { bgcolor: 'transparent' } }}
            disableRipple
          >
            <BrandGlyph size={34} color={landing.text} mutedColor={landing.textMuted} />
            <Box textAlign="left" sx={{ ml: 1.25 }}>
              <Typography sx={{ fontSize: '1.05rem', fontWeight: 700, color: landing.text, lineHeight: 1.15 }}>
                PrintGrid
              </Typography>
              <Typography sx={{ fontSize: '0.55rem', color: 'rgba(255,255,255,0.5)', letterSpacing: '0.14em', textTransform: 'uppercase', lineHeight: 1 }}>
                {roles[0] ?? 'Staff'}
              </Typography>
            </Box>
          </Button>

          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            {primary.map((link) => (
              <Button key={link.href} onClick={() => go(link.href)} sx={{ ...linkSx, textTransform: 'none' }}>
                {link.label}
              </Button>
            ))}
          </Stack>

          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ display: { xs: 'none', md: 'flex' } }}>
            <IconButton onClick={(e) => setAnchorEl(e.currentTarget)} sx={{ p: 0.5 }} aria-label="Menu tài khoản">
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 15 }}>
                {user?.email?.charAt(0).toUpperCase() || 'S'}
              </Avatar>
            </IconButton>
            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={() => setAnchorEl(null)}
              PaperProps={{
                sx: {
                  mt: 1,
                  minWidth: 220,
                  bgcolor: landing.card,
                  border: `1px solid ${landing.hairline}`,
                  borderRadius: `${landing.radius}px`,
                },
              }}
              transformOrigin={{ horizontal: 'right', vertical: 'top' }}
              anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
            >
              <MenuItem disabled sx={{ opacity: '1 !important' }}>
                <PersonOutlineRounded fontSize="small" sx={{ mr: 1.25, color: landing.textMuted }} />
                <Typography sx={{ fontSize: '0.85rem', color: landing.textMuted }}>{user?.email}</Typography>
              </MenuItem>
              <Divider sx={{ borderColor: landing.hairlineSoft }} />
              {more.map((link) => (
                <MenuItem key={link.href} onClick={() => { setAnchorEl(null); go(link.href) }}>
                  {link.label}
                </MenuItem>
              ))}
              {more.length > 0 && <Divider sx={{ borderColor: landing.hairlineSoft }} />}
              <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
                <LogoutRounded fontSize="small" sx={{ mr: 1.25 }} />
                Đăng xuất
              </MenuItem>
            </Menu>
          </Stack>

          <IconButton onClick={() => setDrawerOpen(true)} sx={{ display: { md: 'none' }, color: landing.text }} aria-label="Mở menu">
            <MenuRounded />
          </IconButton>
        </Stack>
      </Container>

      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)} PaperProps={{ sx: { width: 280, bgcolor: landing.card } }}>
        <Box sx={{ p: 2, display: 'flex', justifyContent: 'flex-end' }}>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: landing.text }} aria-label="Đóng menu">
            <CloseRounded />
          </IconButton>
        </Box>
        <List>
          {[...primary, ...more].map((link) => (
            <ListItem key={link.href} disablePadding>
              <ListItemButton onClick={() => go(link.href)}>
                <ListItemText primary={link.label} sx={{ color: landing.text, '& .MuiListItemText-primary': { fontWeight: 500 } }} />
              </ListItemButton>
            </ListItem>
          ))}
          <ListItem disablePadding sx={{ mt: 2 }}>
            <Box sx={{ px: 2, width: '100%' }}>
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