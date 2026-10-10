import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { BlockRounded, LogoutRounded, ManageAccountsRounded, PersonAddRounded } from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  ROLES,
  ROLE_LABELS,
  useAdminUsers,
  useAssignRole,
  useCreateUser,
  useDeactivateUser,
  type AdminUser,
  type Role,
} from './useAdminUsers'

/**
 * Admin console: every account, with the role it carries (FR-ADMIN-001).
 * This screen is the supported way to grant a staff role — before it existed the only
 * route was editing the customers table by hand.
 */

const emptyForm = { email: '', password: '', fullName: '', phoneNumber: '', role: 'Customer' as Role }

const fmtDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '—'

export default function AdminUsersPage() {
  const navigate = useNavigate()
  const { logout, user: currentUser } = useAuth()

  const { data, isLoading, isError } = useAdminUsers()
  const rows = useMemo(() => data ?? [], [data])

  const createUser = useCreateUser()
  const assignRole = useAssignRole()
  const deactivate = useDeactivateUser()

  const [formOpen, setFormOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [formError, setFormError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const currentEmail = currentUser?.email

  async function submit() {
    setFormError(null)
    try {
      await createUser.mutateAsync({
        email: form.email.trim(),
        password: form.password,
        fullName: form.fullName.trim(),
        phoneNumber: form.phoneNumber.trim() || undefined,
        role: form.role,
      })
      setNotice(`Đã tạo tài khoản ${form.email} với role ${ROLE_LABELS[form.role]}.`)
      setFormOpen(false)
      setForm(emptyForm)
    } catch (error) {
      setFormError(getApiErrorMessage(error, 'Không tạo được tài khoản.'))
    }
  }

  const columns: GridColDef<AdminUser>[] = [
    { field: 'email', headerName: 'Email', flex: 1.4, minWidth: 200 },
    { field: 'fullName', headerName: 'Họ tên', flex: 1, minWidth: 140 },
    {
      field: 'roles',
      headerName: 'Role',
      flex: 1.2,
      minWidth: 170,
      sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ py: 0.5 }}>
          {params.row.roles.length === 0 ? (
            <Typography variant="caption" color="text.disabled">chưa gán</Typography>
          ) : (
            params.row.roles.map((role) => (
              <Chip
                key={role}
                size="small"
                label={ROLE_LABELS[role as Role] ?? role}
                sx={{ bgcolor: 'rgba(139,92,246,0.12)', color: '#A78BFA' }}
              />
            ))
          )}
        </Stack>
      ),
    },
    {
      field: 'status',
      headerName: 'Trạng thái',
      width: 170,
      sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ py: 0.5 }}>
          <Chip size="small" label={params.row.isActive ? 'Hoạt động' : 'Đã ngưng'} color={params.row.isActive ? 'success' : 'default'} />
          {params.row.isEmailVerified ? null : <Chip size="small" label="Chưa xác thực email" color="warning" />}
        </Stack>
      ),
    },
    { field: 'createdAt', headerName: 'Tạo lúc', width: 150, valueFormatter: (v: string) => fmtDate(v) },
    { field: 'lastLoginAt', headerName: 'Đăng nhập cuối', width: 150, valueFormatter: (v: string | null) => fmtDate(v) },
    {
      field: 'actions',
      headerName: '',
      width: 210,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        const isSelf = params.row.email === currentEmail
        return (
          <Stack direction="row" spacing={1} alignItems="center" sx={{ height: '100%' }}>
            <FormControl size="small" sx={{ minWidth: 130 }}>
              <Select
                displayEmpty
                value={params.row.roles[0] ?? ''}
                onChange={(e) => {
                  setNotice(null)
                  assignRole.mutate(
                    { id: params.row.id, role: e.target.value as Role },
                    { onSuccess: () => setNotice(`Đã đổi role của ${params.row.email}.`) },
                  )
                }}
                disabled={isSelf}
              >
                <MenuItem value="" disabled>Chọn role…</MenuItem>
                {ROLES.map((role) => (
                  <MenuItem key={role} value={role}>{ROLE_LABELS[role]}</MenuItem>
                ))}
              </Select>
            </FormControl>
            <Tooltip title={isSelf ? 'Không thể tự ngưng tài khoản đang đăng nhập' : 'Ngưng hoạt động'}>
              <span>
                <Button
                  size="small"
                  color="error"
                  startIcon={<BlockRounded fontSize="small" />}
                  disabled={isSelf || !params.row.isActive || deactivate.isPending}
                  onClick={() => deactivate.mutate(params.row.id, { onSuccess: () => setNotice(`Đã ngưng ${params.row.email}.`) })}
                >
                  Ngưng
                </Button>
              </span>
            </Tooltip>
          </Stack>
        )
      },
    },
  ]

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1200, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Người dùng &amp; phân quyền
              </Typography>
              <Typography color="text.secondary">
                Tạo tài khoản và gán role cho từng vai trong mạng lưới (FR-ADMIN-001)
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Button variant="contained" color="primary" startIcon={<PersonAddRounded />} onClick={() => { setFormError(null); setFormOpen(true) }}>
              Tạo tài khoản
            </Button>
            <Button variant="outlined" color="error" startIcon={<LogoutRounded />} onClick={() => { logout(); navigate('/', { replace: true }) }}>
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        {isError ? <Alert severity="error">Không tải được danh sách người dùng.</Alert> : null}
        {notice ? <Alert severity="success" onClose={() => setNotice(null)}>{notice}</Alert> : null}
        {assignRole.isError ? <Alert severity="error">{getApiErrorMessage(assignRole.error, 'Không đổi được role.')}</Alert> : null}
        {deactivate.isError ? <Alert severity="error">{getApiErrorMessage(deactivate.error, 'Không ngưng được tài khoản.')}</Alert> : null}

        <Box sx={{ border: '1px solid rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper' }}>
          <DataGrid
            rows={rows}
            columns={columns}
            loading={isLoading}
            autoHeight
            disableRowSelectionOnClick
            initialState={{ pagination: { paginationModel: { pageSize: 25 } } }}
            pageSizeOptions={[25, 50, 100]}
            slots={{
              loadingOverlay: () => (
                <Box sx={{ display: 'grid', placeItems: 'center', height: '100%' }}>
                  <CircularProgress size={40} />
                </Box>
              ),
              noRowsOverlay: () => (
                <Stack sx={{ height: '100%', alignItems: 'center', justifyContent: 'center' }} spacing={1}>
                  <ManageAccountsRounded sx={{ fontSize: 40, color: 'text.disabled' }} />
                  <Typography color="text.secondary">Chưa có tài khoản nào.</Typography>
                </Stack>
              ),
            }}
            sx={{
              '&.MuiDataGrid-root': { border: 'none' },
              '& .MuiDataGrid-columnHeaders': { bgcolor: 'rgba(255,255,255,0.03)' },
              '& .MuiDataGrid-row': { alignItems: 'center' },
            }}
          />
        </Box>

        <Typography variant="caption" color="text.disabled">
          Role quyết định toàn bộ quyền truy cập: ví dụ tài khoản cần vào màn duyệt báo giá phải là
          Nhân viên duyệt đơn, Điều phối mạng lưới hoặc Quản trị hệ thống.
        </Typography>
      </Stack>

      <Dialog open={formOpen} onClose={() => setFormOpen(false)} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
        <DialogTitle sx={{ fontWeight: 700 }}>Tạo tài khoản mới</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Email" type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} required fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Họ tên" value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} required fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Mật khẩu"
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))}
                  helperText="Tối thiểu 8 ký tự"
                  required
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Số điện thoại (tuỳ chọn)" value={form.phoneNumber} onChange={(e) => setForm((f) => ({ ...f, phoneNumber: e.target.value }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 12 }}>
                <FormControl fullWidth>
                  <InputLabel id="role-label">Role</InputLabel>
                  <Select
                    labelId="role-label"
                    label="Role"
                    value={form.role}
                    onChange={(e) => setForm((f) => ({ ...f, role: e.target.value as Role }))}
                  >
                    {ROLES.map((role) => (
                      <MenuItem key={role} value={role}>{ROLE_LABELS[role]}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
            </Grid>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setFormOpen(false)} color="inherit" sx={{ color: 'text.secondary' }}>Huỷ</Button>
          <Button
            variant="contained"
            onClick={submit}
            disabled={createUser.isPending || !form.email.trim() || !form.fullName.trim() || form.password.length < 8}
          >
            {createUser.isPending ? 'Đang tạo…' : 'Tạo tài khoản'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}