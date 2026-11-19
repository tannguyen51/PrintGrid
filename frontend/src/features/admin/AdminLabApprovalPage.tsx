import { useEffect, useState } from 'react'
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
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { CheckCircleRounded, CloseRounded, FactoryRounded, SearchRounded } from '@mui/icons-material'
import { ServerPaginationBar } from '../../shared/components/ServerPaginationBar'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { useApproveLab, useLabsPaged, useRejectLab } from '../ops/useLabs'
import type { Lab, PagedLabsFilter } from '../ops/labTypes'

export default function AdminLabApprovalPage() {
  const [searchInput, setSearchInput] = useState('')
  const [filters, setFilters] = useState<PagedLabsFilter>({
    search: '',
    approvalStatus: '',
    isActive: '',
    manufacturerOrModel: '',
    material: '',
    includeInactive: true,
    sortBy: 'pendingFirst',
    page: 1,
    pageSize: 10,
  })

  useEffect(() => {
    const t = setTimeout(() => {
      setFilters((prev) => (prev.search === searchInput ? prev : { ...prev, search: searchInput, page: 1 }))
    }, 300)
    return () => clearTimeout(t)
  }, [searchInput])

  const labs = useLabsPaged(filters)
  const approve = useApproveLab()
  const reject = useRejectLab()

  const [rejectTarget, setRejectTarget] = useState<Lab | null>(null)
  const [rejectReason, setRejectReason] = useState('')
  const [notice, setNotice] = useState<string | null>(null)

  const items = labs.data?.items ?? []
  const pendingInPage = items.filter((lab) => lab.status === 'Pending').length

  function updateFilter<K extends keyof PagedLabsFilter>(key: K, value: PagedLabsFilter[K]) {
    setFilters((prev) => ({ ...prev, [key]: value, page: 1 }))
  }

  function resetFilters() {
    setSearchInput('')
    setFilters((prev) => ({
      ...prev,
      search: '',
      approvalStatus: '',
      isActive: '',
      manufacturerOrModel: '',
      material: '',
      page: 1,
    }))
  }

  async function handleApprove(lab: Lab) {
    setNotice(null)
    await approve.mutateAsync(lab.id)
    setNotice(`Đã duyệt kích hoạt xưởng "${lab.name}".`)
  }

  async function handleReject() {
    if (!rejectTarget || !rejectReason.trim()) return
    setNotice(null)
    await reject.mutateAsync({ labId: rejectTarget.id, reason: rejectReason.trim() })
    setNotice(`Đã từ chối hồ sơ xưởng "${rejectTarget.name}".`)
    setRejectTarget(null)
    setRejectReason('')
  }

  return (
    <Box className="admin-page">
      <Stack className="admin-page-body" spacing={2.5}>
        <Box>
          <Typography variant="h1">Duyệt kích hoạt xưởng</Typography>
          <Typography color="text.secondary">
            Kiểm tra hồ sơ, máy in và vật liệu trước khi cho phép xưởng nhận job từ hệ thống.
          </Typography>
        </Box>

        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
          <Chip
            color="primary"
            variant="outlined"
            label={`Tổng ${labs.data?.totalCount?.toLocaleString('vi-VN') ?? 0} xưởng`}
          />
          <Chip
            color={pendingInPage > 0 ? 'warning' : 'default'}
            label={`${pendingInPage} xưởng chờ duyệt trên trang này`}
          />
          <Typography variant="body2" color="text.secondary">
            Admin chỉ phê duyệt kích hoạt; vận hành mạng lưới thuộc OpsManager.
          </Typography>
        </Stack>

        <Paper variant="outlined" sx={{ p: 2, borderRadius: 2.5 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
            <TextField
              size="small"
              placeholder="Tìm tên xưởng, thành phố, máy in…"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              sx={{ minWidth: 240, flex: 1 }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchRounded fontSize="small" />
                    </InputAdornment>
                  ),
                },
              }}
            />
            <FormControl size="small" sx={{ minWidth: 165 }}>
              <InputLabel id="admin-lab-approval-status">Trạng thái duyệt</InputLabel>
              <Select
                labelId="admin-lab-approval-status"
                label="Trạng thái duyệt"
                value={filters.approvalStatus ?? ''}
                onChange={(e) => updateFilter('approvalStatus', e.target.value)}
              >
                <MenuItem value="">Tất cả</MenuItem>
                <MenuItem value="Pending">Chờ duyệt</MenuItem>
                <MenuItem value="Active">Đã kích hoạt</MenuItem>
                <MenuItem value="Rejected">Đã từ chối</MenuItem>
                <MenuItem value="Inactive">Tạm dừng</MenuItem>
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 165 }}>
              <InputLabel id="admin-lab-active-status">Trạng thái hoạt động</InputLabel>
              <Select
                labelId="admin-lab-active-status"
                label="Trạng thái hoạt động"
                value={filters.isActive ?? ''}
                onChange={(e) => updateFilter('isActive', e.target.value as '' | 'true' | 'false')}
              >
                <MenuItem value="">Tất cả</MenuItem>
                <MenuItem value="true">Đang bật (Active)</MenuItem>
                <MenuItem value="false">Đang tắt / Chờ duyệt</MenuItem>
              </Select>
            </FormControl>
            <TextField
              size="small"
              label="Hãng / Model máy"
              placeholder="Bambu, Prusa…"
              value={filters.manufacturerOrModel ?? ''}
              onChange={(e) => updateFilter('manufacturerOrModel', e.target.value)}
              sx={{ width: { xs: '100%', sm: 170 } }}
            />
            <TextField
              size="small"
              label="Vật liệu"
              placeholder="PLA, PETG…"
              value={filters.material ?? ''}
              onChange={(e) => updateFilter('material', e.target.value)}
              sx={{ width: { xs: '100%', sm: 130 } }}
            />
            <Button color="inherit" onClick={resetFilters}>
              Xóa lọc
            </Button>
          </Stack>
        </Paper>

        {notice && (
          <Alert severity="success" onClose={() => setNotice(null)}>
            {notice}
          </Alert>
        )}
        {labs.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
        {labs.isError && <Alert severity="error">Không tải được danh sách xưởng.</Alert>}
        {approve.isError && (
          <Alert severity="error">{getApiErrorMessage(approve.error, 'Không thể duyệt xưởng.')}</Alert>
        )}
        {reject.isError && (
          <Alert severity="error">{getApiErrorMessage(reject.error, 'Không thể từ chối xưởng.')}</Alert>
        )}

        {!labs.isLoading && !labs.isError && items.length === 0 && (
          <Paper variant="outlined" sx={{ p: 6, textAlign: 'center', borderStyle: 'dashed' }}>
            <FactoryRounded sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">
              Không có xưởng nào phù hợp với bộ lọc hiện tại.
            </Typography>
          </Paper>
        )}

        <Stack spacing={2}>
          {items.map((lab) => {
            const missing = [
              lab.machines.length === 0 ? 'máy' : null,
              lab.materialStockCount === 0 ? 'vật liệu' : null,
              !lab.operatingHours ? 'lịch hoạt động' : null,
              lab.transitDaysToHub < 0 ? 'thời gian trung chuyển' : null,
            ].filter(Boolean)
            const statusLabel =
              lab.status === 'Active'
                ? 'Đã kích hoạt'
                : lab.status === 'Rejected'
                  ? 'Đã từ chối'
                  : lab.status === 'Inactive'
                    ? 'Đang tạm dừng'
                    : 'Chờ duyệt'
            return (
              <Paper
                key={lab.id}
                variant="outlined"
                sx={{ p: 2.5, borderRadius: 3, opacity: lab.isActive ? 0.92 : 1 }}
              >
                <Stack
                  direction={{ xs: 'column', md: 'row' }}
                  justifyContent="space-between"
                  alignItems={{ md: 'center' }}
                  gap={2}
                >
                  <Box>
                    <Stack direction="row" spacing={1} alignItems="center" mb={0.5} flexWrap="wrap" useFlexGap>
                      <Typography variant="h6" fontWeight={750}>
                        {lab.name}
                      </Typography>
                      <Chip
                        size="small"
                        color={
                          lab.status === 'Active'
                            ? 'success'
                            : lab.status === 'Rejected'
                              ? 'error'
                              : lab.status === 'Pending'
                                ? 'warning'
                                : 'default'
                        }
                        label={statusLabel}
                      />
                      <Chip
                        size="small"
                        variant="outlined"
                        color={lab.isActive ? 'success' : 'default'}
                        label={lab.isActive ? 'Đang bật' : 'Chưa bật'}
                      />
                    </Stack>
                    <Typography color="text.secondary">
                      {lab.city} · {lab.machines.length} máy · {lab.materialStockCount} dòng vật liệu · vận chuyển về hub{' '}
                      {lab.transitDaysToHub} ngày
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      Lịch: {lab.operatingHours || 'Chưa khai'}
                    </Typography>
                    {lab.machines.length > 0 && (
                      <Typography variant="body2" color="text.secondary" mt={0.75}>
                        {lab.machines
                          .map(
                            (machine) =>
                              `${machine.name} (${machine.printerConfiguration?.manufacturer ? `${machine.printerConfiguration.manufacturer} ` : ''}${machine.model})`,
                          )
                          .join(' · ')}
                      </Typography>
                    )}
                    {lab.status === 'Rejected' && lab.rejectionReason && (
                      <Alert severity="error" sx={{ mt: 1 }}>
                        Lý do từ chối: {lab.rejectionReason}
                      </Alert>
                    )}
                    {missing.length > 0 && lab.status === 'Pending' && (
                      <Alert severity="warning" sx={{ mt: 1 }}>
                        Hồ sơ còn thiếu: {missing.join(', ')}.
                      </Alert>
                    )}
                  </Box>
                  {lab.status === 'Pending' && (
                    <Stack direction="row" spacing={1} flexShrink={0}>
                      <Button
                        variant="contained"
                        startIcon={<CheckCircleRounded />}
                        disabled={approve.isPending || missing.length > 0}
                        onClick={() => void handleApprove(lab)}
                      >
                        {missing.length > 0 ? 'Hồ sơ chưa đầy đủ' : 'Duyệt kích hoạt'}
                      </Button>
                      <Button
                        variant="outlined"
                        color="error"
                        startIcon={<CloseRounded />}
                        disabled={reject.isPending}
                        onClick={() => {
                          setRejectTarget(lab)
                          setRejectReason('')
                        }}
                      >
                        Từ chối
                      </Button>
                    </Stack>
                  )}
                </Stack>
              </Paper>
            )
          })}
        </Stack>

        {labs.data && (
          <ServerPaginationBar
            totalCount={labs.data.totalCount}
            page={labs.data.page}
            pageSize={labs.data.pageSize}
            totalPages={labs.data.totalPages}
            itemLabel="xưởng"
            onPageChange={(nextPage) => setFilters((prev) => ({ ...prev, page: nextPage }))}
            onPageSizeChange={(nextSize) => setFilters((prev) => ({ ...prev, pageSize: nextSize, page: 1 }))}
          />
        )}
      </Stack>

      <Dialog
        open={rejectTarget !== null}
        onClose={() => setRejectTarget(null)}
        fullWidth
        maxWidth="xs"
        PaperProps={{ sx: { bgcolor: 'background.paper', backgroundImage: 'none', border: '1px solid #E8EBF2' } }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>Từ chối duyệt xưởng — {rejectTarget?.name}</DialogTitle>
        <DialogContent>
          <TextField
            label="Lý do từ chối"
            value={rejectReason}
            onChange={(e) => setRejectReason(e.target.value)}
            required
            fullWidth
            multiline
            rows={3}
            sx={{ mt: 1 }}
            placeholder="VD: Chưa đủ máy in hoặc thông tin vận chuyển chưa hợp lệ"
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setRejectTarget(null)} color="inherit">
            Huỷ
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => void handleReject()}
            disabled={reject.isPending || !rejectReason.trim()}
          >
            Xác nhận từ chối
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
