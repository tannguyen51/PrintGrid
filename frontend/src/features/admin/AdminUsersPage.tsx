import { useMemo, useState } from 'react'
import {
  Alert,
  Avatar,
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
  InputAdornment,
  IconButton,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { DataGrid, type GridColDef } from '@mui/x-data-grid'
import { BlockRounded, ManageAccountsRounded, PersonAddRounded, SearchRounded } from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
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
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const filteredRows = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('vi-VN')
    return rows.filter((row) =>
      (!term || `${row.fullName} ${row.email}`.toLocaleLowerCase('vi-VN').includes(term)) &&
      (!roleFilter || row.roles.includes(roleFilter)) &&
      (!statusFilter || row.isActive === (statusFilter === 'active')),
    )
  }, [rows, search, roleFilter, statusFilter])
  const hasFilters = Boolean(search || roleFilter || statusFilter)

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
    {
      field: 'fullName', headerName: 'Người dùng', flex: 1.4, minWidth: 250,
      renderCell: ({ row }) => <Stack direction="row" spacing={1.25} alignItems="center" sx={{ height: '100%', py: 1 }}>
        <Avatar sx={{ width: 34, height: 34, fontSize: 12, bgcolor: '#F0EDFA', color: '#6550CE' }}>{(row.fullName || row.email).charAt(0).toUpperCase()}</Avatar>
        <Box sx={{ minWidth: 0 }}><Typography noWrap sx={{ fontSize: 12, fontWeight: 650 }}>{row.fullName || 'Chưa có tên'}</Typography><Typography noWrap sx={{ fontSize: 11, color: 'text.secondary' }}>{row.email}</Typography></Box>
      </Stack>,
    },
    {
      field: 'roles',
      headerName: 'Vai trò',
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
                sx={{ bgcolor: 'rgba(111,85,255,0.10)', color: '#4933B8' }}
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
      headerName: 'Thao tác',
      width: 210,
      sortable: false,
      filterable: false,
      renderCell: (params) => {
        const isSelf = params.row.email === currentEmail
        return (
          <Stack direction="row" spacing={1} alignItems="center" sx={{ height: '100%' }}>
            <FormControl size="small" sx={{ width: 145, flexShrink: 0 }}>
              <Select
                inputProps={{ 'aria-label': `Vai trò của ${params.row.email}` }}
                displayEmpty
                value={params.row.roles[0] ?? ''}
                onChange={(e) => {
                  setNotice(null)
                  assignRole.mutate(
                    { id: params.row.id, role: e.target.value as Role },
                    { onSuccess: () => setNotice(`Đã đổi role của ${params.row.email}.`) },
                  )
                }}
                disabled={isSelf || assignRole.isPending}
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
                  aria-label={`Ngưng hoạt động ${params.row.email}`}
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
    <Box className="admin-page">
      <Stack className="admin-page-body" spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">

            <Box>
              <Typography variant="h1" sx={{ color: 'text.primary' }}>
                Người dùng &amp; phân quyền
              </Typography>
              <Typography color="text.secondary">
                Tạo tài khoản và phân quyền vai trò cho người dùng trong hệ thống
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Button variant="contained" color="primary" startIcon={<PersonAddRounded />} onClick={() => { setFormError(null); setFormOpen(true) }}>
              Tạo tài khoản
            </Button>
          </Stack>
        </Stack>

        {isError ? <Alert severity="error">Không tải được danh sách người dùng.</Alert> : null}
        {notice ? <Alert severity="success" onClose={() => setNotice(null)}>{notice}</Alert> : null}
        {assignRole.isError ? <Alert severity="error">{getApiErrorMessage(assignRole.error, 'Không đổi được role.')}</Alert> : null}
        {deactivate.isError ? <Alert severity="error">{getApiErrorMessage(deactivate.error, 'Không ngưng được tài khoản.')}</Alert> : null}

        <Grid container spacing={2}>
          {[
            ['Tổng tài khoản', rows.length],
            ['Đang hoạt động', rows.filter((row) => row.isActive).length],
            ['Đã ngưng hoạt động', rows.filter((row) => !row.isActive).length],
          ].map(([label, count]) => <Grid key={label} size={{ xs: 12, sm: 4 }}><Paper variant="outlined" sx={{ px: 2.5, py: 2 }}>
            <Typography variant="body2" color="text.secondary">{label}</Typography><Typography sx={{ mt: 0.75, fontSize: 25, fontWeight: 700 }}>{isLoading ? '…' : isError && !data ? '—' : Number(count).toLocaleString('vi-VN')}</Typography>
          </Paper></Grid>)}
        </Grid>

        <Box sx={{ border: '1px solid #E8EBF2', borderRadius: 3, overflow: 'hidden', bgcolor: 'background.paper' }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} gap={1.5} useFlexGap flexWrap="wrap" sx={{ p: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
            <TextField size="small" placeholder="Tìm theo tên hoặc email…" value={search} onChange={(event) => setSearch(event.target.value)}
              sx={{ flex: 1, minWidth: { sm: 220 } }} inputProps={{ 'aria-label': 'Tìm người dùng theo tên hoặc email' }}
              InputProps={{ startAdornment: <InputAdornment position="start"><SearchRounded sx={{ fontSize: 19 }} /></InputAdornment> }} />
            <TextField select size="small" label="Vai trò" value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)} sx={{ minWidth: 170 }}>
              <MenuItem value="">Tất cả vai trò</MenuItem>{ROLES.map((role) => <MenuItem key={role} value={role}>{ROLE_LABELS[role]}</MenuItem>)}
            </TextField>
            <TextField select size="small" label="Trạng thái" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} sx={{ minWidth: 160 }}>
              <MenuItem value="">Tất cả trạng thái</MenuItem><MenuItem value="active">Đang hoạt động</MenuItem><MenuItem value="inactive">Đã ngưng hoạt động</MenuItem>
            </TextField>
            {hasFilters && <Button onClick={() => { setSearch(''); setRoleFilter(''); setStatusFilter('') }}>Xóa bộ lọc</Button>}
          </Stack>
          <Typography role="status" sx={{ px: 2, py: 1.25, fontSize: 11, color: 'text.secondary' }}>{isLoading ? 'Đang tải danh sách…' : isError && !data ? 'Không tải được danh sách' : `${filteredRows.length} tài khoản${hasFilters ? ` phù hợp / ${rows.length} tài khoản` : ' trong hệ thống'}`}</Typography>
          <DataGrid
            rows={filteredRows}
            columns={columns}
            loading={isLoading}
            autoHeight
            getRowHeight={() => 'auto'}
            getEstimatedRowHeight={() => 76}
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
                  <Typography color="text.secondary">{hasFilters ? 'Không có tài khoản phù hợp với bộ lọc.' : isError ? 'Chưa tải được danh sách tài khoản.' : 'Chưa có tài khoản nào.'}</Typography>
                </Stack>
              ),
            }}
            sx={{
              '&.MuiDataGrid-root': { border: 'none' },
              '& .MuiDataGrid-columnHeaders': { bgcolor: 'action.hover' },
              '& .MuiDataGrid-cell': { display: 'flex', alignItems: 'center', minHeight: 72, py: 1.25 },
            }}
          />
        </Box>

        <Typography variant="caption" color="text.disabled">
          Vai trò quyết định toàn bộ quyền truy cập: ví dụ tài khoản cần vào màn duyệt báo giá phải là
          Nhân viên duyệt đơn, Điều phối mạng lưới hoặc Quản trị hệ thống.
        </Typography>
      </Stack>

      <Dialog open={formOpen} onClose={() => setFormOpen(false)} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: 'background.paper', backgroundImage: 'none', border: '1px solid #E8EBF2' } }}>
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
                  <InputLabel id="role-label">Vai trò</InputLabel>
                  <Select
                    labelId="role-label"
                    label="Vai trò"
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
