import { useState } from 'react'
import {
  Autocomplete, Avatar, Box, Breadcrumbs, Button, Divider, Drawer, IconButton,
  InputAdornment, List, ListItemButton, ListItemIcon, ListItemText, Menu,
  MenuItem, Stack, TextField, ThemeProvider, Typography,
} from '@mui/material'
import {
  ArrowBackRounded, ArrowOutwardRounded, CategoryRounded, ChevronRightRounded,
  CloseRounded, DashboardRounded, FactCheckRounded, HistoryRounded, InsightsRounded,
  LogoutRounded, MenuRounded, PeopleRounded, SearchRounded, SettingsRounded,
  ShieldOutlined, StorefrontRounded, TuneRounded, ViewInArRounded,
} from '@mui/icons-material'
import type { ReactNode } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { BrandGlyph } from '../../shared/components/BrandGlyph'
import { NotificationBell } from '../../shared/components/NotificationBell'
import { adminTheme } from './adminTheme'
import './admin.css'

interface AdminSection { label: string; href: string; icon: ReactNode }
const GROUPS: { label: string; items: AdminSection[] }[] = [
  { label: 'Không gian làm việc', items: [
    { label: 'Tổng quan', href: '/admin', icon: <DashboardRounded /> },
    { label: 'Người dùng & phân quyền', href: '/admin/users', icon: <PeopleRounded /> },
    { label: 'Duyệt xưởng', href: '/admin/labs', icon: <StorefrontRounded /> },
    { label: 'Profile & duyệt máy', href: '/admin/cura-profiles', icon: <TuneRounded /> },
  ] },
  { label: 'Dữ liệu & chất lượng', items: [
    { label: 'Danh mục dùng chung', href: '/admin/catalogs', icon: <CategoryRounded /> },
    { label: 'Kho mẫu 3D', href: '/admin/samples', icon: <ViewInArRounded /> },
    { label: 'Kho tri thức QC', href: '/admin/qc-knowledge', icon: <FactCheckRounded /> },
  ] },
  { label: 'Cấu hình hệ thống', items: [
    { label: 'Cấu hình thương mại', href: '/admin/commercial-configurations', icon: <TuneRounded /> },
    { label: 'Trọng số chấm điểm', href: '/admin/scoring-configurations', icon: <InsightsRounded /> },
    { label: 'Nhật ký kiểm toán', href: '/admin/audit-logs', icon: <HistoryRounded /> },
    { label: 'Thông số hệ thống', href: '/admin/system-parameters', icon: <SettingsRounded /> },
  ] },
]
const SECTIONS = GROUPS.flatMap((group) => group.items)
const isActive = (path: string, href: string) => href === '/admin' ? path === href : path === href || path.startsWith(`${href}/`)

export function AdminLayout() {
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const [mobileOpen, setMobileOpen] = useState(false)
  const [accountAnchor, setAccountAnchor] = useState<HTMLElement | null>(null)
  const [search, setSearch] = useState('')
  const current = SECTIONS.find((section) => isActive(pathname, section.href))
  const name = user?.fullName || user?.email || 'Quản trị viên'
  const initials = name.split(/\s+/).filter(Boolean).slice(-2).map((part) => part[0]).join('').toUpperCase()

  const sidebar = (
    <Box className="admin-sidebar-inner">
      <Box className="admin-brand">
        <Link to="/admin" onClick={() => setMobileOpen(false)} aria-label="PrintGrid — Tổng quan quản trị">
          <BrandGlyph size={31} color="#B1A0FF" mutedColor="#6C688B" />
          <span>PrintGrid<span className="admin-brand-caption">ADMIN WORKSPACE</span></span>
        </Link>
        <IconButton onClick={() => setMobileOpen(false)} aria-label="Đóng menu quản trị" sx={{ display: { md: 'none' }, color: '#CCD0DF' }}><CloseRounded /></IconButton>
      </Box>
      <Box component="nav" aria-label="Điều hướng quản trị" className="admin-navigation">
        {GROUPS.map((group) => <Box key={group.label} sx={{ mb: 2.5 }}>
          <Typography className="admin-nav-heading">{group.label}</Typography>
          <List disablePadding>
            {group.items.map((section) => <ListItemButton
              key={section.href} component={Link} to={section.href}
              selected={isActive(pathname, section.href)}
              aria-current={isActive(pathname, section.href) ? 'page' : undefined}
              onClick={() => setMobileOpen(false)} className="admin-nav-link"
            >
              <ListItemIcon>{section.icon}</ListItemIcon>
              <ListItemText primary={section.label} />
            </ListItemButton>)}
          </List>
        </Box>)}
      </Box>
      <Box className="admin-sidebar-footer">
        <Stack direction="row" spacing={1.25} alignItems="center" sx={{ mb: 2 }}>
          <ShieldOutlined sx={{ fontSize: 20, color: '#B1A0FF' }} />
          <Box><Typography sx={{ fontSize: 12, fontWeight: 600, color: '#E6E7EF' }}>Không gian quản trị</Typography>
            <Typography sx={{ fontSize: 11, color: '#A0A7BF' }}>Dành cho quản trị viên hệ thống</Typography></Box>
        </Stack>
        <Button component={Link} to="/" startIcon={<ArrowBackRounded />} endIcon={<ArrowOutwardRounded />} fullWidth
          sx={{ justifyContent: 'space-between', fontSize: 12, color: '#CCD0DF', border: '1px solid #393D52', '&:hover': { bgcolor: '#292D43' } }}>Về trang PrintGrid</Button>
      </Box>
    </Box>
  )

  return <ThemeProvider theme={adminTheme}>
    <Box className="admin-workspace">
      <a className="admin-skip-link" href="#admin-content">Đến nội dung chính</a>
      <Box component="aside" className="admin-sidebar">{sidebar}</Box>
      <Drawer open={mobileOpen} onClose={() => setMobileOpen(false)}
        slotProps={{ paper: { sx: { width: 264, bgcolor: '#1C2033', border: 0, borderRadius: 0, color: 'white' } } }}>{sidebar}</Drawer>
      <Box className="admin-main">
        <Box component="header" className="admin-topbar">
          <Stack direction="row" alignItems="center" spacing={1.5} sx={{ minWidth: 0 }}>
            <IconButton onClick={() => setMobileOpen(true)} aria-label="Mở menu quản trị" sx={{ display: { md: 'none' } }}><MenuRounded /></IconButton>
            <Breadcrumbs separator={<ChevronRightRounded sx={{ fontSize: 15 }} />} aria-label="Đường dẫn trang" sx={{ '& .MuiBreadcrumbs-ol': { flexWrap: 'nowrap' } }}>
              <Link className="admin-breadcrumb-root" to="/admin">Quản trị</Link>
              <Typography sx={{ fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap' }}>{current?.label ?? 'Tổng quan'}</Typography>
            </Breadcrumbs>
          </Stack>
          <Stack direction="row" alignItems="center" spacing={{ xs: 0.5, sm: 1.5 }}>
            <Autocomplete<AdminSection> size="small" options={SECTIONS} value={null} inputValue={search}
              onInputChange={(_, value) => setSearch(value)} getOptionLabel={(option) => option.label}
              onChange={(_, section) => { if (section) { navigate(section.href); setSearch('') } }}
              noOptionsText="Không tìm thấy trang phù hợp" clearText="Xóa tìm kiếm" openText="Mở danh sách" closeText="Đóng danh sách"
              sx={{ width: 250, display: { xs: 'none', lg: 'block' } }}
              renderInput={(params) => <TextField {...params} placeholder="Tìm trang quản trị…" inputProps={{ ...params.inputProps, 'aria-label': 'Tìm trang quản trị' }}
                InputProps={{ ...params.InputProps, startAdornment: <InputAdornment position="start"><SearchRounded sx={{ fontSize: 18 }} /></InputAdornment> }} />} />
            <NotificationBell />
            <Divider orientation="vertical" flexItem sx={{ my: 0.5, display: { xs: 'none', sm: 'block' } }} />
            <Button onClick={(event) => setAccountAnchor(event.currentTarget)} aria-label="Menu tài khoản quản trị"
              aria-haspopup="menu" aria-expanded={Boolean(accountAnchor)} sx={{ minWidth: 0, gap: 1, px: 0.5, color: 'text.primary' }}>
              <Avatar sx={{ width: 34, height: 34, bgcolor: '#EEEAFB', color: '#6550CE', fontSize: 12, fontWeight: 700 }}>{initials}</Avatar>
              <Box sx={{ textAlign: 'left', display: { xs: 'none', sm: 'block' }, maxWidth: 140 }}>
                <Typography noWrap sx={{ fontSize: 12, fontWeight: 650 }}>{name}</Typography>
                <Typography sx={{ fontSize: 10, color: 'text.secondary' }}>Quản trị viên</Typography>
              </Box>
            </Button>
            <Menu anchorEl={accountAnchor} open={Boolean(accountAnchor)} onClose={() => setAccountAnchor(null)}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
              <MenuItem disabled sx={{ opacity: '1 !important', fontSize: 12 }}>{user?.email}</MenuItem>
              <Divider />
              <MenuItem onClick={() => { setAccountAnchor(null); logout() }}><LogoutRounded sx={{ fontSize: 18, mr: 1 }} />Đăng xuất</MenuItem>
            </Menu>
          </Stack>
        </Box>
        <Box component="main" id="admin-content" tabIndex={-1} className="admin-content"><Outlet /></Box>
        <Box component="footer" className="admin-footer"><span>PrintGrid · Hệ thống quản trị</span><span>Mạng lưới in 3D phân tán</span></Box>
      </Box>
    </Box>
  </ThemeProvider>
}
