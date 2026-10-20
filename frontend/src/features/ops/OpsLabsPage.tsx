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
  DeleteOutlineRounded,
  EditRounded,
  FactoryRounded,
  PrecisionManufacturingRounded,
} from '@mui/icons-material'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  useAddMachine,
  useApproveLab,
  useCreateLab,
  useDeleteMachine,
  useLabs,
  useRejectLab,
  useSetLabActive,
  useSetLabCapacityLimit,
  useSetLabStanding,
  useSetMachineStatus,
  useUpdateMachine,
} from './useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  type Lab,
  type Machine,
  type PrintTechnology,
} from './labTypes'

/**
 * Shared lab registry surface (FR-LAB-001/002/006/007 · BR-OPS-005/006).
 * - LabManager declares its own lab/machines and tracks approval state.
 * - Admin/Ops approve or reject pending labs, suspend/restore standing, cap concurrency
 *   and maintain the network-wide machine registry.
 */

const emptyLab = {
  name: '',
  city: '',
  transitDaysToHub: 1,
  workingDays: 'Thứ 2, Thứ 3, Thứ 4, Thứ 5, Thứ 6',
  openingTime: '08:00',
  closingTime: '17:00',
}
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

const labStatusChip = (lab: Lab) =>
  lab.status === 'Active'
    ? { label: 'Đang hoạt động', color: 'success' as const }
    : lab.status === 'Rejected'
      ? { label: 'Đã từ chối', color: 'error' as const }
      : lab.status === 'Inactive'
        ? { label: 'Tạm dừng hoạt động', color: 'default' as const }
        : { label: 'Chờ duyệt kích hoạt', color: 'warning' as const }

export default function OpsLabsPage() {
  const { user } = useAuth()
  const isLabManager = user?.roles.includes('LabManager') ?? false

  const [includeInactive, setIncludeInactive] = useState(true)
  const { data, isLoading, isError } = useLabs(includeInactive)
  const labs = useMemo(() => data ?? [], [data])

  const createLab = useCreateLab()
  const addMachine = useAddMachine()
  const updateMachine = useUpdateMachine()
  const deleteMachine = useDeleteMachine()
  const setStatus = useSetMachineStatus()
  const approveLab = useApproveLab()
  const rejectLab = useRejectLab()
  const setActive = useSetLabActive()
  const setStanding = useSetLabStanding()
  const setCapacity = useSetLabCapacityLimit()

  const [actionBannerError, setActionBannerError] = useState<string | null>(null)
  const [actionBannerSuccess, setActionBannerSuccess] = useState<string | null>(null)

  const [labOpen, setLabOpen] = useState(false)
  const [labForm, setLabForm] = useState(emptyLab)
  const [labError, setLabError] = useState<string | null>(null)

  const [machineLab, setMachineLab] = useState<Lab | null>(null)
  const [editingMachine, setEditingMachine] = useState<Machine | null>(null)
  const [machineForm, setMachineForm] = useState(emptyMachine)
  const [machineError, setMachineError] = useState<string | null>(null)

  const [rejectTarget, setRejectTarget] = useState<Lab | null>(null)
  const [rejectReason, setRejectReason] = useState('')

  const [capacityLab, setCapacityLab] = useState<Lab | null>(null)
  const [capacityValue, setCapacityValue] = useState('')

  async function submitLab() {
    setLabError(null)
    try {
      await createLab.mutateAsync({
        name: labForm.name.trim(),
        city: labForm.city.trim(),
        transitDaysToHub: Number(labForm.transitDaysToHub) || 0,
        workingDays: labForm.workingDays.trim(),
        openingTime: labForm.openingTime,
        closingTime: labForm.closingTime,
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
      const input = {
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
      }
      if (editingMachine) await updateMachine.mutateAsync({ ...input, machineId: editingMachine.id })
      else await addMachine.mutateAsync(input)
      setMachineLab(null)
      setEditingMachine(null)
      setMachineForm(emptyMachine)
    } catch (error) {
      setMachineError(getApiErrorMessage(error, 'Không lưu được thông số máy.'))
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
      setActionBannerSuccess(
        lab.isActive ? `Đã tạm dừng xưởng "${lab.name}".` : `Đã kích hoạt lại xưởng "${lab.name}".`,
      )
    } catch (error) {
      setActionBannerError(getApiErrorMessage(error, 'Không thể thay đổi trạng thái hoạt động của xưởng.'))
    }
  }

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1100, mx: 'auto' }} spacing={2.5}>
        <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }} spacing={2}>
          <Stack direction="row" spacing={1.5} alignItems="flex-start">
            <PageBackButton />
            <Box>
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                {isLabManager ? 'Xưởng & máy của tôi' : 'Duyệt xưởng & mạng lưới máy — Ops'}
              </Typography>
              <Typography color="text.secondary">
                {isLabManager
                  ? 'Khai báo năng lực xưởng và máy; xưởng chỉ nhận việc sau khi được Admin duyệt.'
                  : 'Duyệt hồ sơ xưởng tự đăng ký, đình chỉ hoặc giới hạn số job đồng thời của từng xưởng.'}
              </Typography>
            </Box>
          </Stack>
          {isLabManager && labs.length === 0 && (
            <Stack direction="row" spacing={1.5} alignItems="center">
              <Button variant="contained" color="primary" startIcon={<AddRounded />} onClick={() => { setLabError(null); setLabOpen(true) }}>
                Đăng ký xưởng
              </Button>
            </Stack>
          )}
          {!isLabManager && (
            <Stack direction="row" spacing={1.5} alignItems="center">
              <Button variant="contained" color="primary" startIcon={<AddRounded />} onClick={() => { setLabError(null); setLabOpen(true) }}>
                Thêm xưởng
              </Button>
            </Stack>
          )}
        </Stack>

        <FormControlLabel
          control={<Switch checked={includeInactive} onChange={(e) => setIncludeInactive(e.target.checked)} />}
          label={<Typography color="text.secondary">Hiện cả xưởng chờ duyệt &amp; xưởng đã tắt</Typography>}
        />

        {actionBannerError && (
          <Alert severity="error" onClose={() => setActionBannerError(null)}>{actionBannerError}</Alert>
        )}
        {actionBannerSuccess && (
          <Alert severity="success" onClose={() => setActionBannerSuccess(null)}>{actionBannerSuccess}</Alert>
        )}
        {isError ? <Alert severity="error">Không tải được danh sách xưởng.</Alert> : null}
        {setStatus.isError ? <Alert severity="error">{getApiErrorMessage(setStatus.error, 'Không đổi được trạng thái máy.')}</Alert> : null}
        {setStanding.isError ? <Alert severity="error">{getApiErrorMessage(setStanding.error, 'Không đổi được trạng thái phân công của xưởng.')}</Alert> : null}
        {setCapacity.isError ? <Alert severity="error">{getApiErrorMessage(setCapacity.error, 'Không lưu được giới hạn năng lực.')}</Alert> : null}

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
              const statusChip = labStatusChip(lab)
              const missingMachines = lab.machines.length === 0
              return (
                <Box key={lab.id} sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
                  <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ xs: 'flex-start', sm: 'center' }} spacing={1.5}>
                    <Box>
                      <Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
                        <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>{lab.name}</Typography>
                        <Chip size="small" label={statusChip.label} color={statusChip.color} />
                        {!lab.isInGoodStanding && <Chip size="small" variant="outlined" color="error" label="Đã đình chỉ phân công" />}
                        {lab.maxConcurrentJobs && <Chip size="small" variant="outlined" label={`Tối đa ${lab.maxConcurrentJobs} job`} />}
                        {missingMachines && <Chip size="small" variant="outlined" color="error" label="Chưa có máy in — cần 1 máy để duyệt" />}
                      </Stack>
                      <Typography variant="body2" color="text.secondary">
                        {lab.city}{lab.address ? ` (${lab.address})` : ''}{lab.contactPhone ? ` · SĐT: ${lab.contactPhone}` : ''} · {lab.machines.length} máy · lịch {lab.workingDays || 'chưa khai'} {lab.openingTime}–{lab.closingTime} · vận chuyển về hub {lab.transitDaysToHub} ngày · đúng hạn {Math.round(lab.onTimeDeliveryRate * 100)}% · đạt lần đầu {Math.round(lab.firstPassYield * 100)}%
                      </Typography>
                      {lab.rejectionReason && (
                        <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5 }}>
                          Lý do từ chối: {lab.rejectionReason}
                        </Typography>
                      )}
                    </Box>
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      {isLabManager ? (
                        <Button
                          size="small"
                          variant="outlined"
                          color="primary"
                          startIcon={<AddRounded />}
                          onClick={() => { setMachineError(null); setEditingMachine(null); setMachineForm(emptyMachine); setMachineLab(lab) }}
                        >
                          Thêm máy
                        </Button>
                      ) : (
                        <>
                          {!lab.isActive && (
                            <>
                              <Button
                                size="small"
                                variant="contained"
                                color="success"
                                startIcon={<CheckCircleOutlineRounded />}
                                onClick={() => handleApprove(lab)}
                                disabled={approveLab.isPending || missingMachines}
                              >
                                {missingMachines ? 'Thiếu máy in' : 'Duyệt xưởng'}
                              </Button>
                              {lab.status !== 'Rejected' && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  color="error"
                                  startIcon={<CloseRounded />}
                                  onClick={() => { setRejectTarget(lab); setRejectReason('') }}
                                >
                                  Từ chối
                                </Button>
                              )}
                            </>
                          )}
                          <Button
                            size="small"
                            variant="outlined"
                            color={lab.isInGoodStanding ? 'error' : 'success'}
                            disabled={!lab.isActive || setStanding.isPending}
                            onClick={() => setStanding.mutate({ labId: lab.id, isInGoodStanding: !lab.isInGoodStanding })}
                          >
                            {lab.isInGoodStanding ? 'Đình chỉ' : 'Khôi phục'}
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            disabled={!lab.isActive}
                            onClick={() => { setCapacityLab(lab); setCapacityValue(lab.maxConcurrentJobs?.toString() ?? '') }}
                          >
                            Giới hạn năng lực
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="inherit"
                            sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
                            onClick={() => handleToggleActive(lab)}
                            disabled={setActive.isPending}
                          >
                            {lab.isActive ? 'Tắt xưởng' : 'Bật xưởng'}
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="primary"
                            startIcon={<AddRounded />}
                            onClick={() => { setMachineError(null); setEditingMachine(null); setMachineForm(emptyMachine); setMachineLab(lab) }}
                          >
                            Thêm máy
                          </Button>
                        </>
                      )}
                    </Stack>
                  </Stack>

                  {isLabManager && (
                    <Stack direction="row" spacing={1} mt={1.5} flexWrap="wrap" useFlexGap>
                      <Button size="small" component={RouterLink} to={`/lab/${lab.id}/inventory`}>
                        Khai tồn kho vật liệu ({lab.materialStockCount})
                      </Button>
                      <Typography variant="caption" color="text.secondary" alignSelf="center">
                        Lịch: {lab.workingDays}, {lab.openingTime}–{lab.closingTime}
                      </Typography>
                    </Stack>
                  )}

                  <Divider sx={{ my: 2, borderColor: 'rgba(255,255,255,0.08)' }} />

                  {lab.machines.length === 0 ? (
                    <Typography variant="body2" color="text.disabled">
                      Xưởng chưa khai báo máy in nào — engine sẽ không bao giờ chọn được xưởng này.
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      {lab.machines.map((machine) => (
                        <MachineRow
                          key={machine.id}
                          machine={machine}
                          editable
                          onChangeStatus={(status) => setStatus.mutate({ labId: lab.id, machineId: machine.id, status })}
                          onEdit={() => {
                            setMachineError(null)
                            setEditingMachine(machine)
                            setMachineLab(lab)
                            setMachineForm({
                              name: machine.name, model: machine.model, technology: machine.technology,
                              buildWidthMm: machine.buildWidthMm, buildDepthMm: machine.buildDepthMm,
                              buildHeightMm: machine.buildHeightMm, minLayerHeightMm: machine.minLayerHeightMm,
                              achievableToleranceMm: machine.achievableToleranceMm,
                              materials: machine.supportedMaterials.join(', '),
                            })
                          }}
                          onDelete={async () => {
                            if (!window.confirm(`Xóa máy ${machine.name}? Máy đang có job sẽ không thể xóa.`)) return
                            try {
                              await deleteMachine.mutateAsync({ labId: lab.id, machineId: machine.id })
                              setActionBannerSuccess(`Đã xóa máy "${machine.name}".`)
                            } catch (error) {
                              setActionBannerError(getApiErrorMessage(error, 'Không thể xóa máy khi đang có đơn in hoạt động.'))
                            }
                          }}
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
      <Dialog open={rejectTarget !== null} onClose={() => setRejectTarget(null)} fullWidth maxWidth="xs"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
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
          <Button onClick={() => setRejectTarget(null)} color="inherit">Huỷ</Button>
          <Button variant="contained" color="error" onClick={handleReject} disabled={rejectLab.isPending || !rejectReason.trim()}>
            Xác nhận từ chối
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Register lab ── */}
      <Dialog open={labOpen} onClose={() => setLabOpen(false)} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
        <DialogTitle sx={{ fontWeight: 700 }}>Thêm xưởng vào mạng lưới</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {labError && <Alert severity="error">{labError}</Alert>}
            <TextField label="Tên xưởng" value={labForm.name} onChange={(e) => setLabForm((f) => ({ ...f, name: e.target.value }))} required fullWidth />
            <TextField label="Thành phố" value={labForm.city} onChange={(e) => setLabForm((f) => ({ ...f, city: e.target.value }))} required fullWidth />
            <TextField
              label="Số ngày vận chuyển về hub"
              type="number"
              value={labForm.transitDaysToHub}
              onChange={(e) => setLabForm((f) => ({ ...f, transitDaysToHub: Number(e.target.value) }))}
              inputProps={{ min: 1 }}
              fullWidth
            />
            <TextField label="Ngày làm việc" value={labForm.workingDays} onChange={(e) => setLabForm((f) => ({ ...f, workingDays: e.target.value }))} helperText="Ví dụ: Thứ 2, Thứ 3, Thứ 4, Thứ 5, Thứ 6" required fullWidth />
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <TextField label="Giờ mở cửa" type="time" value={labForm.openingTime} onChange={(e) => setLabForm((f) => ({ ...f, openingTime: e.target.value }))} InputLabelProps={{ shrink: true }} required fullWidth />
              <TextField label="Giờ đóng cửa" type="time" value={labForm.closingTime} onChange={(e) => setLabForm((f) => ({ ...f, closingTime: e.target.value }))} InputLabelProps={{ shrink: true }} required fullWidth />
            </Stack>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setLabOpen(false)} color="inherit" sx={{ color: 'text.secondary' }}>Huỷ</Button>
          <Button variant="contained" onClick={submitLab} disabled={createLab.isPending || !labForm.name.trim() || !labForm.city.trim() || !labForm.workingDays.trim() || labForm.transitDaysToHub < 1 || labForm.closingTime <= labForm.openingTime}>
            {createLab.isPending ? 'Đang tạo…' : 'Tạo xưởng'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Register / update machine ── */}
      <Dialog open={machineLab !== null} onClose={() => { setMachineLab(null); setEditingMachine(null) }} fullWidth maxWidth="sm"
        PaperProps={{ sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' } }}>
        <DialogTitle sx={{ fontWeight: 700 }}>{editingMachine ? 'Cập nhật máy' : 'Thêm máy'} — {machineLab?.name}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {machineError && <Alert severity="error">{machineError}</Alert>}
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Tên máy" value={machineForm.name} onChange={(e) => setMachineForm((f) => ({ ...f, name: e.target.value }))} required fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Model" value={machineForm.model} onChange={(e) => setMachineForm((f) => ({ ...f, model: e.target.value }))} required fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <FormControl fullWidth>
                  <InputLabel id="tech-label">Công nghệ</InputLabel>
                  <Select
                    labelId="tech-label"
                    label="Công nghệ"
                    value={machineForm.technology}
                    onChange={(e) => setMachineForm((f) => ({ ...f, technology: Number(e.target.value) as PrintTechnology }))}
                  >
                    {PRINT_TECHNOLOGY_LABELS.map((label, index) => (
                      <MenuItem key={label} value={index}>{label}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Vật liệu (phân cách bằng dấu phẩy)" value={machineForm.materials} onChange={(e) => setMachineForm((f) => ({ ...f, materials: e.target.value }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField label="Khổ in R (mm)" type="number" value={machineForm.buildWidthMm} onChange={(e) => setMachineForm((f) => ({ ...f, buildWidthMm: Number(e.target.value) }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField label="Khổ in S (mm)" type="number" value={machineForm.buildDepthMm} onChange={(e) => setMachineForm((f) => ({ ...f, buildDepthMm: Number(e.target.value) }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField label="Khổ in C (mm)" type="number" value={machineForm.buildHeightMm} onChange={(e) => setMachineForm((f) => ({ ...f, buildHeightMm: Number(e.target.value) }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Layer height nhỏ nhất (mm)" type="number" value={machineForm.minLayerHeightMm} onChange={(e) => setMachineForm((f) => ({ ...f, minLayerHeightMm: Number(e.target.value) }))} fullWidth />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField label="Dung sai đạt được (mm)" type="number" value={machineForm.achievableToleranceMm} onChange={(e) => setMachineForm((f) => ({ ...f, achievableToleranceMm: Number(e.target.value) }))} fullWidth />
              </Grid>
            </Grid>
            <Typography variant="caption" color="text.disabled">
              Khổ in, layer height và dung sai là ba điều kiện lọc cứng của engine — nhập sai thì máy sẽ bị loại khỏi mọi job.
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => { setMachineLab(null); setEditingMachine(null) }} color="inherit" sx={{ color: 'text.secondary' }}>Huỷ</Button>
          <Button
            variant="contained"
            onClick={submitMachine}
            disabled={addMachine.isPending || updateMachine.isPending || !machineForm.name.trim() || !machineForm.model.trim() || !machineForm.materials.trim()}
          >
            {addMachine.isPending || updateMachine.isPending ? 'Đang lưu…' : editingMachine ? 'Lưu thay đổi' : 'Thêm máy'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Capacity limit ── */}
      <Dialog open={capacityLab !== null} onClose={() => setCapacityLab(null)} fullWidth maxWidth="xs">
        <DialogTitle>Giới hạn năng lực — {capacityLab?.name}</DialogTitle>
        <DialogContent>
          <TextField autoFocus fullWidth sx={{ mt: 1 }} type="number" label="Số job đồng thời tối đa" value={capacityValue} onChange={(event) => setCapacityValue(event.target.value)} helperText="Để trống nếu không giới hạn." inputProps={{ min: 1 }} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCapacityLab(null)}>Hủy</Button>
          <Button variant="contained" disabled={setCapacity.isPending || (capacityValue !== '' && Number(capacityValue) <= 0)} onClick={async () => {
            if (!capacityLab) return
            await setCapacity.mutateAsync({ labId: capacityLab.id, maxConcurrentJobs: capacityValue === '' ? null : Number(capacityValue) })
            setCapacityLab(null)
          }}>Lưu</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function MachineRow({ machine, editable, onChangeStatus, onEdit, onDelete }: { machine: Machine; editable: boolean; onChangeStatus: (status: Machine['status']) => void; onEdit: () => void; onDelete: () => void }) {
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
            {machine.name} <Typography component="span" variant="body2" color="text.secondary">· {machine.model}</Typography>
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {PRINT_TECHNOLOGY_LABELS[machine.technology] ?? '?'} · khổ in {machine.buildWidthMm}×{machine.buildDepthMm}×{machine.buildHeightMm} mm · layer ≥ {machine.minLayerHeightMm} mm · dung sai ≤ {machine.achievableToleranceMm} mm
          </Typography>
          <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.5 }}>
            {machine.supportedMaterials.map((material) => (
              <Chip key={material} size="small" label={material} sx={{ bgcolor: 'rgba(139,92,246,0.12)', color: '#A78BFA', height: 20 }} />
            ))}
          </Stack>
        </Box>
      </Stack>
      <Stack direction="row" spacing={0.75} alignItems="center">
        <FormControl size="small" sx={{ minWidth: 150 }} disabled={!editable}>
          <Select value={machine.status} onChange={(e) => onChangeStatus(Number(e.target.value) as Machine['status'])}>
            {MACHINE_STATUS_LABELS.map((label, index) => (
              <MenuItem key={label} value={index}>{label}</MenuItem>
            ))}
          </Select>
        </FormControl>
        {editable && <>
          <Button size="small" color="inherit" onClick={onEdit} aria-label="Sửa máy"><EditRounded fontSize="small" /></Button>
          <Button size="small" color="error" onClick={onDelete} aria-label="Xóa máy"><DeleteOutlineRounded fontSize="small" /></Button>
        </>}
      </Stack>
    </Stack>
  )
}
