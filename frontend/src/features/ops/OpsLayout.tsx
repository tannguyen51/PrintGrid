import { useState, type ReactNode } from 'react'
import {
  Autocomplete, Avatar, Box, Breadcrumbs, Button, Divider, Drawer, IconButton,
  InputAdornment, List, ListItemButton, ListItemIcon, ListItemText, Menu,
  MenuItem, Stack, TextField, ThemeProvider, Typography,
} from '@mui/material'
import {
  ArrowBackRounded, ArrowOutwardRounded, AssignmentOutlined, ChevronRightRounded,
  CloseRounded, DashboardRounded, DesignServicesOutlined, FactCheckOutlined,
  FactoryOutlined, HistoryRounded, InsightsOutlined, LogoutRounded, MenuRounded,
  PaymentsOutlined, ReceiptLongOutlined, SearchRounded, ViewInArOutlined,
  WarningAmberRounded, WorkspacesOutlined,
} from '@mui/icons-material'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { BrandGlyph } from '../../shared/components/BrandGlyph'
import { NotificationBell } from '../../shared/components/NotificationBell'
import { OPS_GROUPS, OPS_SECTIONS } from './opsNavigation'
import { opsTheme } from './opsTheme'
import './ops.css'

const icons: Record<string, ReactNode> = {
  dashboard: <DashboardRounded />, warning: <WarningAmberRounded />, factory: <FactoryOutlined />,
  quote: <ReceiptLongOutlined />, model: <ViewInArOutlined />, qc: <FactCheckOutlined />,
  design: <DesignServicesOutlined />, payment: <PaymentsOutlined />, refund: <AssignmentOutlined />,
  chart: <InsightsOutlined />, history: <HistoryRounded />,
}

export default function OpsLayout() {
  const { pathname } = useLocation()
  const navigate = useNavigate()
  const { user, logout } = useAuth()
  const [mobileOpen, setMobileOpen] = useState(false)
  const [accountAnchor, setAccountAnchor] = useState<HTMLElement | null>(null)
  const [search, setSearch] = useState('')
  const active = (href: string) => pathname === href || pathname.startsWith(`${href}/`)
  const section = OPS_SECTIONS.find((item) => active(item.href))
  const name = user?.fullName || user?.email || 'Điều phối viên'
  const initials = name.split(/\s+/).filter(Boolean).slice(-2).map((part) => part[0]).join('').toUpperCase()

  const sidebar = <Box className="ops-sidebar-inner">
    <Box className="ops-brand">
      <Link to="/scheduling" onClick={() => setMobileOpen(false)} aria-label="PrintGrid — Trung tâm điều phối">
        <BrandGlyph size={32} color="#B9AAFF" mutedColor="#646984" />
        <span>PrintGrid<span className="ops-brand-caption">OPERATIONS WORKSPACE</span></span>
      </Link>
      <IconButton aria-label="Đóng menu vận hành" onClick={() => setMobileOpen(false)} sx={{ display: { md: 'none' }, color: '#CBD0E0' }}><CloseRounded /></IconButton>
    </Box>
    <Box className="ops-workspace-label"><WorkspacesOutlined sx={{ fontSize: 16 }} /><span>Quản lý vận hành</span><span className="ops-role-badge">OPS</span></Box>
    <Box component="nav" className="ops-navigation" aria-label="Điều hướng OpsManager">
      {OPS_GROUPS.map((group) => <Box key={group.label} sx={{ mb: 2.25 }}>
        <Typography className="ops-nav-heading">{group.label}</Typography>
        <List disablePadding>{group.items.map((item) => <ListItemButton key={item.href} component={Link} to={item.href}
          selected={active(item.href)} aria-current={active(item.href) ? 'page' : undefined}
          onClick={() => setMobileOpen(false)} className="ops-nav-link">
          <ListItemIcon>{icons[item.icon]}</ListItemIcon><ListItemText primary={item.label} />
        </ListItemButton>)}</List>
      </Box>)}
    </Box>
    <Box className="ops-sidebar-footer">
      <Typography sx={{ color: '#D4D8E8', fontSize: 12, fontWeight: 650, mb: 0.5 }}>Mạng lưới in 3D phân tán</Typography>
      <Typography sx={{ color: '#A0A9C0', fontSize: 11, mb: 2 }}>Điều phối · Chất lượng · Đối soát</Typography>
      <Button component={Link} to="/" fullWidth startIcon={<ArrowBackRounded />} endIcon={<ArrowOutwardRounded />}
        sx={{ justifyContent: 'space-between', fontSize: 12, color: '#CDD3E3', border: '1px solid #363E55', '&:hover': { bgcolor: '#293148' } }}>Về trang PrintGrid</Button>
    </Box>
  </Box>

  return <ThemeProvider theme={opsTheme}>
    <Box className="ops-workspace">
      <a className="ops-skip-link" href="#ops-content">Đến nội dung chính</a>
      <Box component="aside" className="ops-sidebar">{sidebar}</Box>
      <Drawer open={mobileOpen} onClose={() => setMobileOpen(false)} slotProps={{ paper: { sx: { width: 262, bgcolor: '#1B2337', border: 0, borderRadius: 0 } } }}>{sidebar}</Drawer>
      <Box className="ops-main">
        <Box component="header" className="ops-topbar">
          <Stack direction="row" alignItems="center" spacing={1.25} sx={{ minWidth: 0 }}>
            <IconButton onClick={() => setMobileOpen(true)} aria-label="Mở menu vận hành" sx={{ display: { md: 'none' } }}><MenuRounded /></IconButton>
            <Breadcrumbs aria-label="Đường dẫn trang" separator={<ChevronRightRounded sx={{ fontSize: 15 }} />}>
              <Link to="/scheduling" className="ops-breadcrumb-root">Vận hành</Link>
              <Typography sx={{ fontSize: 12, fontWeight: 600 }}>{section?.label ?? 'Điều phối'}</Typography>
            </Breadcrumbs>
          </Stack>
          <Stack direction="row" spacing={{ xs: 0.5, sm: 1.5 }} alignItems="center">
            <Autocomplete size="small" options={OPS_SECTIONS} value={null} inputValue={search}
              onInputChange={(_, value) => setSearch(value)} getOptionLabel={(option) => option.label}
              onChange={(_, item) => { if (item) { navigate(item.href); setSearch('') } }}
              noOptionsText="Không tìm thấy trang phù hợp" clearText="Xóa tìm kiếm" openText="Mở danh sách" closeText="Đóng danh sách"
              sx={{ width: 240, display: { xs: 'none', lg: 'block' } }}
              renderInput={(params) => <TextField {...params} placeholder="Tìm trong không gian Ops…"
                inputProps={{ ...params.inputProps, 'aria-label': 'Tìm trang vận hành' }}
                InputProps={{ ...params.InputProps, startAdornment: <InputAdornment position="start"><SearchRounded sx={{ fontSize: 18 }} /></InputAdornment> }} />} />
            <NotificationBell />
            <Divider orientation="vertical" flexItem sx={{ my: 0.5, display: { xs: 'none', sm: 'block' } }} />
            <Button aria-label="Menu tài khoản vận hành" aria-haspopup="menu" aria-expanded={Boolean(accountAnchor)}
              onClick={(event) => setAccountAnchor(event.currentTarget)} sx={{ minWidth: 0, p: 0.5, gap: 1, color: 'text.primary' }}>
              <Avatar sx={{ width: 34, height: 34, fontSize: 12 }}>{initials}</Avatar>
              <Box sx={{ display: { xs: 'none', sm: 'block' }, textAlign: 'left', maxWidth: 140 }}>
                <Typography noWrap sx={{ fontSize: 12, fontWeight: 650 }}>{name}</Typography>
                <Typography sx={{ fontSize: 10, color: 'text.secondary' }}>OpsManager</Typography>
              </Box>
            </Button>
            <Menu anchorEl={accountAnchor} open={Boolean(accountAnchor)} onClose={() => setAccountAnchor(null)}
              anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }} transformOrigin={{ vertical: 'top', horizontal: 'right' }}>
              <MenuItem disabled sx={{ opacity: '1 !important', fontSize: 12 }}>{user?.email}</MenuItem><Divider />
              <MenuItem onClick={() => { setAccountAnchor(null); logout() }}><LogoutRounded sx={{ fontSize: 18, mr: 1 }} />Đăng xuất</MenuItem>
            </Menu>
          </Stack>
        </Box>
        <Box component="main" id="ops-content" className="ops-content" tabIndex={-1}><Outlet /></Box>
        <Box component="footer" className="ops-footer"><span>PrintGrid · Không gian vận hành</span><span>Kết nối xưởng in, quản lý chất lượng</span></Box>
      </Box>
    </Box>
  </ThemeProvider>
}
