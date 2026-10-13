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
  Divider,
  FormControl,
  FormControlLabel,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import {
  AddRounded,
  CheckCircleOutlineRounded,
  CloseRounded,
  FactoryRounded,
  LogoutRounded,
  PrecisionManufacturingRounded,
} from '@mui/icons-material'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  useAddMachine,
  useApproveLab,
  useCreateLab,
  useLabs,
  useRejectLab,
  useSetLabActive,
  useSetMachineStatus,
} from './useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  type Lab,
  type Machine,
  type PrintTechnology,
} from './labTypes'

const emptyLab = { name: '', city: '', transitDaysToHub: 1 }
const emptyMachine = {
  name: '',
  model: '',
  technology: 0 as PrintTechnology,
  buildWidthMm: 256,
  buildDepthMm: 256,
  buildHeightMm: 256,
  minLayerHeightMm: 0.1,
  achievableToleranceMm: 0.2,
  materials: 'PLA, PETG',
}

/**
 * Admin / Ops Lab Onboarding Approval & Network Registry Console (FR-LAB-001/002 · BR-OPS-005/006).
 * - Displays PENDING labs awaiting Admin approval (PENDING -> ACTIVE).
 * - AC01: Unapproved labs receive no jobs.
 * - AC02: Blocks approving a lab when it has 0 machines ("thiếu máy => không duyệt được").
 */
export default function OpsLabsPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()

  const [includeInactive, setIncludeInactive] = useState(true)
  const { data, isLoading, isError } = useLabs(includeInactive)
  const labs = useMemo(() => data ?? [], [data])

  const createLab = useCreateLab()
  const addMachine = useAddMachine()
  const setActive = useSetLabActive()
  const approveLab = useApproveLab()
  const rejectLab = useRejectLab()
  const setStatus = useSetMachineStatus()

  const [actionBannerError, setActionBannerError] = useState<string | null>(null)
  const [actionBannerSuccess, setActionBannerSuccess] = useState<string | null>(null)

  const [labOpen, setLabOpen] = useState(false)
  const [labForm, setLabForm] = useState(emptyLab)
  const [labError, setLabError] = useState<string | null>(null)

  const [machineLab, setMachineLab] = useState<Lab | null>(null)
  const [machineForm, setMachineForm] = useState(emptyMachine)
  const [machineError, setMachineError] = useState<string | null>(null)

  const [rejectTarget, setRejectTarget] = useState<Lab | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  async function submitLab() {
    setLabError(null)
    try {
      await createLab.mutateAsync({
        name: labForm.name.trim(),
        city: labForm.city.trim(),
        transitDaysToHub: Number(labForm.transitDaysToHub) || 0,
      })
      setLabOpen(false)
      setLabForm(emptyLab)
    } catch (error) {
      setLabError(getApiErrorMessage(error, 'Không tạo được xưởng.'))
    }
  }

  async function submitMachine() {
    if (!machineLab) return
    setMachineError(null)
    try {
      await addMachine.mutateAsync({
        labId: machineLab.id,
        name: machineForm.name.trim(),
        model: machineForm.model.trim(),
        technology: machineForm.technology,
        buildWidthMm: Number(machineForm.buildWidthMm),
        buildDepthMm: Number(machineForm.buildDepthMm),
        buildHeightMm: Number(machineForm.buildHeightMm),
        minLayerHeightMm: Number(machineForm.minLayerHeightMm),
        achievableToleranceMm: Number(machineForm.achievableToleranceMm),
        supportedMaterials: machineForm.materials
          .split(',')
          .map((m) => m.trim().toUpperCase())
          .filter(Boolean),
      })
      setMachineLab(null)
      setMachineForm(emptyMachine)
    } catch (error) {
      setMachineError(getApiErrorMessage(error, 'Không thêm được máy.'))
    }
  }

  async function handleApprove(lab: Lab) {
    setActionBannerError(null)
    setActionBannerSuccess(null)
    try {
      await approveLab.mutateAsync(lab.id)
      setActionBannerSuccess(`Đã duyệt xưởng "${lab.name}" — xưởng đã sẵn sàng nhận đơn in.`)
    } catch (error) {
      setActionBannerError(
        getApiErrorMessage(error, 'Không thể duyệt xưởng khi chưa khai báo ít nhất 1 máy in.'),
      )
    }
  }

  async function handleReject() {
    if (!rejectTarget || !rejectReason.trim()) return
    setActionBannerError(null)
    setActionBannerSuccess(null)
    try {
      await rejectLab.mutateAsync({ labId: rejectTarget.id, reason: rejectReason.trim() })
      setActionBannerSuccess(`Đã từ chối hồ sơ xưởng "${rejectTarget.name}".`)
      setRejectTarget(null)
      setRejectReason('')
    } catch (error) {
      setActionBannerError(getApiErrorMessage(error, 'Không thể từ chối hồ sơ xưởng.'))
    }
  }

  async function handleToggleActive(lab: Lab) {
    setActionBannerError(null)
    setActionBannerSuccess(null)
    try {
      await setActive.mutateAsync({ labId: lab.id, isActive: !lab.isActive })
    } catch (error) {
      setActionBannerError(getApiErrorMessage(error, 'Không thể thay đổi trạng thái hoạt động của xưởng.'))
    }
  }

  const pendingCount = labs.filter((l) => l.status === 'Pending' || (!l.isActive && l.status !== 'Inactive')).length

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1100, mx: 'auto' }} spacing={2.5}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'stretch', sm: 'center' }}
          spacing={2}
        >
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
                <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                  Duyệt Xưởng &amp; Sổ Đăng Ký Máy — Admin / Ops
                </Typography>
                {pendingCount > 0 && (
                  <Chip size="small" color="warning" label={`${pendingCount} xưởng chờ duyệt`} />
                )}
              </Stack>
              <Typography color="text.secondary">
                Duyệt hồ sơ xưởng tự đăng ký và quản lý danh sách máy in toàn mạng lưới
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Button
              variant="contained"
              color="primary"
              startIcon={<AddRounded />}
              onClick={() => {
                setLabError(null)
                setLabOpen(true)
              }}
            >
              Thêm xưởng
            </Button>
            <Button
              variant="outlined"
              color="error"
              startIcon={<LogoutRounded />}
              onClick={() => {
                logout()
                navigate('/', { replace: true })
              }}
            >
              Đăng xuất
            </Button>
          </Stack>
        </Stack>

        <FormControlLabel
          control={<Switch checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />}
          label={<Typography color="text.secondary">Hiện cả xưởng chờ duyệt &amp; xưởng đã tắt</Typography>}
        />

        {actionBannerError && (
          <Alert severity="error" onClose={() => setActionBannerError(null)}>
            {actionBannerError}
          </Alert>
        )}
        {actionBannerSuccess && (
          <Alert severity="success" onClose={() => setActionBannerSuccess(null)}>
            {actionBannerSuccess}
          </Alert>
        )}
        {isError ? <Alert severity="error">Không tải được danh sách xưởng.</Alert> : null}
        {setStatus.isError ? (
          <Alert severity="error">{getApiErrorMessage(setStatus.error, 'Không đổi được trạng thái máy.')}</Alert>
        ) : null}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center' }} />
        ) : labs.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <FactoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">
              Chưa có xưởng nào. Không có xưởng thì mọi yêu cầu báo giá sẽ bị từ chối vì không có máy khả thi.
            </Typography>
          </Box>
        ) : (
          <Stack spacing={2.5}>
            {labs.map((lab) => {
              const status = lab.status ?? (lab.isActive ? 'Active' : 'Pending')
              const isPending = status === 'Pending' || status === 'Rejected'
              const missingMachines = lab.machines.length === 0

              return (
                <Box
                  key={lab.id}
                  sx={{
                    p: { xs: 2, md: 3 },
                    borderRadius: 3,
                    border: isPending
                      ? '1px solid rgba(245,158,11,0.45)'
                      : '1px solid rgba(255,255,255,0.1)',
                    bgcolor: 'background.paper',
                  }}
                >
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    justifyContent="space-between"
                    alignItems={{ xs: 'flex-start', sm: 'center' }}
                    spacing={1.5}
                  >
                    <Box>
                      <Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
                        <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>
                          {lab.name}
                        </Typography>
                        <Chip
                          size="small"
                          label={
                            status === 'Active'
                              ? 'Đang hoạt động'
                              : status === 'Rejected'
                                ? 'Đã từ chối'
                                : status === 'Pending'
                                  ? 'Chờ duyệt kích hoạt (Chưa nhận đơn in)'
                                  : 'Tạm dừng hoạt động'
                          }
                          color={
                            status === 'Active'
                              ? 'success'
                              : status === 'Rejected'
                                ? 'error'
                                : status === 'Pending'
                                  ? 'warning'
                                  : 'default'
                          }
                        />
                        {missingMachines && (
                          <Chip
                            size="small"
                            color="error"
                            variant="outlined"
                            label="Chưa có máy in (Cần ít nhất 1 máy để duyệt)"
                          />
                        )}
                      </Stack>
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                        {lab.city}
                        {lab.address ? ` (${lab.address})` : ''}
                        {lab.contactPhone ? ` · SĐT: ${lab.contactPhone}` : ''} · Lịch làm việc:{' '}
                        <b>{lab.operatingHours ?? 'T2-T7 08:00-18:00'}</b> · Chuyển về Hub:{' '}
                        <b>{lab.transitDaysToHub} ngày</b> · {lab.machines.length} máy · Đúng hạn{' '}
                        {Math.round(lab.onTimeDeliveryRate * 100)}% · Đạt lần đầu{' '}
                        {Math.round(lab.firstPassYield * 100)}%
                      </Typography>
                      {lab.rejectionReason && (
                        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
                          Lý do từ chối: {lab.rejectionReason}
                        </Typography>
                      )}
                    </Box>
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      {!lab.isActive && (
                        <>
                          <Button
                            size="small"
                            variant="contained"
                            color="success"
                            startIcon={<CheckCircleOutlineRounded />}
                            onClick={() => handleApprove(lab)}
                            disabled={approveLab.isPending}
                          >
                            Duyệt xưởng
                          </Button>
                          {status === 'Pending' && (
                            <Button
                              size="small"
                              variant="outlined"
                              color="error"
                              startIcon={<CloseRounded />}
                              onClick={() => {
                                setRejectTarget(lab)
                                setRejectReason('')
                              }}
                            >
                              Từ chối
                            </Button>
                          )}
                        </>
                      )}
                      {lab.isActive && (
                        <Button
                          size="small"
                          variant="outlined"
                          color="inherit"
                          sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
                          onClick={() => handleToggleActive(lab)}
                          disabled={setActive.isPending}
                        >
                          Tắt xưởng
                        </Button>
                      )}
                      <Button
                        size="small"
                        variant="outlined"
                        color="primary"
                        startIcon={<AddRounded />}
                        onClick={() => {
                          setMachineError(null)
                          setMachineForm(emptyMachine)
                          setMachineLab(lab)
                        }}
                      >
                        Thêm máy
                      </Button>
                    </Stack>
                  </Stack>

                  <Divider sx={{ my: 2, borderColor: 'rgba(255,255,255,0.08)' }} />

                  {lab.machines.length === 0 ? (
                    <Typography variant="body2" color="text.disabled">
                      Xưởng chưa khai báo máy in nào — cần ít nhất 1 máy in để duyệt kích hoạt.
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      {lab.machines.map((machine) => (
                        <MachineRow
                          key={machine.id}
                          machine={machine}
                          onChangeStatus={(s) => setStatus.mutate({ labId: lab.id, machineId: machine.id, status: s })}
                        />
                      ))}
                    </Stack>
                  )}
                </Box>
              )
            })}
          </Stack>
        )}
      </Stack>

      {/* ── Reject Lab Dialog ── */}
      <Dialog
        open={rejectTarget !== null}
        onClose={() => setRejectTarget(null)}
        fullWidth
        maxWidth="xs"
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
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
            placeholder="VD: Chưa khai báo máy in hoặc thông tin lịch làm việc chưa hợp lệ"
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setRejectTarget(null)} color="inherit">
            Huỷ
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={handleReject}
            disabled={rejectLab.isPending || !rejectReason.trim()}
          >
            Xác nhận từ chối
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Register lab ── */}
      <Dialog
        open={labOpen}
        onClose={() => setLabOpen(false)}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>Thêm xưởng vào mạng lưới</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {labError && <Alert severity="error">{labError}</Alert>}
            <TextField
              label="Tên xưởng"
              value={labForm.name}
              onChange={(e) => setLabForm((f) => ({ ...f, name: e.target.value }))}
              required
              fullWidth
            />
            <TextField
              label="Thành phố"
              value={labForm.city}
              onChange={(e) => setLabForm((f) => ({ ...f, city: e.target.value }))}
              required
              fullWidth
            />
            <TextField
              label="Số ngày vận chuyển về hub"
              type="number"
              value={labForm.transitDaysToHub}
              onChange={(e) => setLabForm((f) => ({ ...f, transitDaysToHub: Number(e.target.value) }))}
              inputProps={{ min: 0 }}
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setLabOpen(false)} color="inherit" sx={{ color: 'text.secondary' }}>
            Huỷ
          </Button>
          <Button
            variant="contained"
            onClick={submitLab}
            disabled={createLab.isPending || !labForm.name.trim() || !labForm.city.trim()}
          >
            {createLab.isPending ? 'Đang tạo…' : 'Tạo xưởng'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Register machine ── */}
      <Dialog
        open={machineLab !== null}
        onClose={() => setMachineLab(null)}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>Thêm máy — {machineLab?.name}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {machineError && <Alert severity="error">{machineError}</Alert>}
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Tên máy"
                  value={machineForm.name}
                  onChange={(e) => setMachineForm((f) => ({ ...f, name: e.target.value }))}
                  required
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Model"
                  value={machineForm.model}
                  onChange={(e) => setMachineForm((f) => ({ ...f, model: e.target.value }))}
                  required
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <FormControl fullWidth>
                  <InputLabel id="tech-label">Công nghệ</InputLabel>
                  <Select
                    labelId="tech-label"
                    label="Công nghệ"
                    value={machineForm.technology}
                    onChange={(e) =>
                      setMachineForm((f) => ({ ...f, technology: Number(e.target.value) as PrintTechnology }))
                    }
                  >
                    {PRINT_TECHNOLOGY_LABELS.map((label, index) => (
                      <MenuItem key={label} value={index}>
                        {label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Vật liệu (phân cách bằng dấu phẩy)"
                  value={machineForm.materials}
                  onChange={(e) => setMachineForm((f) => ({ ...f, materials: e.target.value }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in R (mm)"
                  type="number"
                  value={machineForm.buildWidthMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, buildWidthMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in S (mm)"
                  type="number"
                  value={machineForm.buildDepthMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, buildDepthMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in C (mm)"
                  type="number"
                  value={machineForm.buildHeightMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, buildHeightMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Layer height nhỏ nhất (mm)"
                  type="number"
                  value={machineForm.minLayerHeightMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, minLayerHeightMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Dung sai đạt được (mm)"
                  type="number"
                  value={machineForm.achievableToleranceMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, achievableToleranceMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
            </Grid>
            <Typography variant="caption" color="text.disabled">
              Khổ in, layer height và dung sai là ba điều kiện lọc cứng của engine — nhập sai thì máy sẽ bị loại khỏi mọi
              job.
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setMachineLab(null)} color="inherit" sx={{ color: 'text.secondary' }}>
            Huỷ
          </Button>
          <Button
            variant="contained"
            onClick={submitMachine}
            disabled={addMachine.isPending || !machineForm.name.trim() || !machineForm.model.trim()}
          >
            {addMachine.isPending ? 'Đang thêm…' : 'Thêm máy'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function MachineRow({
  machine,
  onChangeStatus,
}: {
  machine: Machine
  onChangeStatus: (status: Machine['status']) => void
}) {
  const offline = machine.status === 2 || machine.status === 3
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      justifyContent="space-between"
      alignItems={{ xs: 'flex-start', sm: 'center' }}
      spacing={1.5}
      sx={{ p: 1.5, borderRadius: 2, border: '1px solid rgba(255,255,255,0.08)' }}
    >
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
        <PrecisionManufacturingRounded sx={{ color: offline ? 'error.main' : 'text.secondary' }} />
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontWeight: 600, color: 'text.primary' }}>
            {machine.name}{' '}
            <Typography component="span" variant="body2" color="text.secondary">
              · {machine.model}
            </Typography>
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {PRINT_TECHNOLOGY_LABELS[machine.technology] ?? '?'} · khổ in {machine.buildWidthMm}×{machine.buildDepthMm}×
            {machine.buildHeightMm} mm · layer ≥ {machine.minLayerHeightMm} mm · dung sai ≤{' '}
            {machine.achievableToleranceMm} mm
          </Typography>
          <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
            {machine.supportedMaterials.map((material) => (
              <Chip
                key={material}
                size="small"
                label={material}
                sx={{ bgcolor: 'rgba(139,92,246,0.12)', color: '#A78BFA', height: 20 }}
              />
            ))}
          </Stack>
        </Box>
      </Stack>
      <FormControl size="small" sx={{ minWidth: 150 }}>
        <Select value={machine.status} onChange={(e) => onChangeStatus(Number(e.target.value) as Machine['status'])}>
          {MACHINE_STATUS_LABELS.map((label, index) => (
            <MenuItem key={label} value={index}>
              {label}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
    </Stack>
  )
}