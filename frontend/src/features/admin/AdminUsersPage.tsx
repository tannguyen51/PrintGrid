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
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { BlockRounded, ManageAccountsRounded, PersonAddRounded } from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { StaffNavbar } from '../home/components/StaffNavbar'
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
  const { user: currentUser } = useAuth()

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
  const activeCount = rows.filter((user) => user.isActive).length
  const unverifiedCount = rows.filter((user) => !user.isEmailVerified).length

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
    { field: 'email', headerName: 'Email', flex: 1.4, minWidth: 190 },
    { field: 'fullName', headerName: 'Họ tên', flex: 1, minWidth: 135 },
    {
      field: 'roles',
      headerName: 'Role',
      flex: 1,
      minWidth: 145,
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
      width: 185,
      sortable: false,
      renderCell: (params) => (
        <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ py: 0.5 }}>
          <Chip size="small" label={params.row.isActive ? 'Hoạt động' : 'Đã ngưng'} color={params.row.isActive ? 'success' : 'default'} />
          {params.row.isEmailVerified ? null : <Chip size="small" label="Chưa xác thực email" color="warning" />}
        </Stack>
      ),
    },
    { field: 'createdAt', headerName: 'Tạo lúc', width: 145, valueFormatter: (v: string) => fmtDate(v) },
    { field: 'lastLoginAt', headerName: 'Đăng nhập cuối', width: 155, valueFormatter: (v: string | null) => fmtDate(v) },
    {
      field: 'actions',
      headerName: '',
      width: 200,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        const isSelf = params.row.email === currentEmail
        return (
          <Stack direction="row" spacing={0.75} alignItems="center" sx={{ height: '100%', width: '100%' }}>
            <FormControl size="small" sx={{ minWidth: 0, flex: 1 }}>
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
                <IconButton
                  size="small"
                  color="error"
                  aria-label={`Ngưng tài khoản ${params.row.email}`}
                  disabled={isSelf || !params.row.isActive || deactivate.isPending}
                  onClick={() => deactivate.mutate(params.row.id, { onSuccess: () => setNotice(`Đã ngưng ${params.row.email}.`) })}
                >
                  <BlockRounded fontSize="small" />
                </IconButton>
              </span>
            </Tooltip>
          </Stack>
        )
      },
    },
  ]

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <StaffNavbar />
      <Stack sx={{ px: { xs: 2.5, md: 4 }, py: { xs: 3, md: 4.5 }, maxWidth: 1440, mx: 'auto' }} spacing={3}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Người dùng &amp; phân quyền
              </Typography>
              <Typography color="text.secondary">
                Tạo tài khoản, phân vai và kiểm soát quyền truy cập trong mạng lưới
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.5} alignItems="center" sx={{ pl: { sm: 6 } }}>
            <Button size="large" variant="contained" color="primary" startIcon={<PersonAddRounded />} onClick={() => { setFormError(null); setFormOpen(true) }}>
              Tạo tài khoản
            </Button>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          <Chip label={`${rows.length} tài khoản`} sx={{ bgcolor: 'rgba(255,255,255,0.06)', color: 'text.primary' }} />
          <Chip label={`${activeCount} đang hoạt động`} color="success" variant="outlined" />
          {unverifiedCount > 0 && <Chip label={`${unverifiedCount} chưa xác thực`} color="warning" variant="outlined" />}
        </Stack>

        {isError ? <Alert severity="error">Không tải được danh sách người dùng.</Alert> : null}
        {notice ? <Alert severity="success" onClose={() => setNotice(null)}>{notice}</Alert> : null}
        {assignRole.isError ? <Alert severity="error">{getApiErrorMessage(assignRole.error, 'Không đổi được role.')}</Alert> : null}
        {deactivate.isError ? <Alert severity="error">{getApiErrorMessage(deactivate.error, 'Không ngưng được tài khoản.')}</Alert> : null}

        <Box sx={{ border: '1px solid rgba(255,255,255,0.1)', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper', boxShadow: '0 18px 50px rgba(0,0,0,0.28)' }}>
          <DataGrid
            rows={rows}
            columns={columns}
            loading={isLoading}
            autoHeight
            rowHeight={64}
            columnHeaderHeight={52}
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
              '& .MuiDataGrid-columnHeaders': { bgcolor: 'rgba(255,255,255,0.04)', borderBottomColor: 'rgba(255,255,255,0.12)' },
              '& .MuiDataGrid-columnHeaderTitle': { fontWeight: 700 },
              '& .MuiDataGrid-row': { alignItems: 'center' },
              '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(139,92,246,0.05)' },
              '& .MuiDataGrid-cell': { borderBottomColor: 'rgba(255,255,255,0.07)' },
              '& .MuiDataGrid-footerContainer': { minHeight: 58, bgcolor: 'rgba(255,255,255,0.02)' },
            }}
          />
        </Box>

        <Box sx={{ px: 2, py: 1.5, borderRadius: 2, bgcolor: 'rgba(139,92,246,0.06)', border: '1px solid rgba(139,92,246,0.14)' }}>
          <Typography variant="body2" color="text.secondary">
            Vai trò quyết định các khu vực và thao tác mà tài khoản được phép truy cập. Bạn không thể tự đổi vai trò hoặc ngưng tài khoản đang đăng nhập.
          </Typography>
        </Box>
      </Stack>

      <Dialog open={formOpen} onClose={() => setFormOpen(false)} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)', borderRadius: 3 } }}>
        <DialogTitle sx={{ px: 3, pt: 3, fontWeight: 800 }}>Tạo tài khoản mới</DialogTitle>
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
