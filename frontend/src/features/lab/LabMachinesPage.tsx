import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  AddRounded,
  BuildCircleRounded,
  DeleteOutlineRounded,
  EditRounded,
  FactoryRounded,
  LogoutRounded,
  PrecisionManufacturingRounded,
  ScheduleRounded,
} from '@mui/icons-material'
import { useAuth } from '../../app/AuthContext'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  useAddMachine,
  useDeleteMachine,
  useMyLab,
  useScheduleMachineMaintenance,
  useSetMachineStatus,
  useUpdateMachine,
} from '../ops/useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  type Machine,
  type MachineStatus,
  type PrintTechnology,
} from '../ops/labTypes'

const emptyMachineForm = {
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
 * Lab Manager's own Machine Registry & Maintenance screen (FR-LAB-002 · BR-OPS-006):
 * - Scoped strictly to the current LabManager's own lab ("chỉ máy mình").
 * - Full CRUD (Add / Edit / Delete machine specs).
 * - Blocks deleting a machine that still has active jobs (AC02).
 * - Maintenance schedule window + immediate Offline/Maintenance status switch (AC01: excluded from filter <= 1 min).
 */
export default function LabMachinesPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const { data: myLab, isLoading, isError } = useMyLab(true)

  const addMachine = useAddMachine()
  const updateMachine = useUpdateMachine()
  const deleteMachine = useDeleteMachine()
  const setStatus = useSetMachineStatus()
  const scheduleMaintenance = useScheduleMachineMaintenance()

  const [bannerError, setBannerError] = useState<string | null>(null)
  const [bannerSuccess, setBannerSuccess] = useState<string | null>(null)

  // Add / Edit dialog state
  const [formOpen, setFormOpen] = useState(false)
  const [editingMachine, setEditingMachine] = useState<Machine | null>(null)
  const [machineForm, setMachineForm] = useState(emptyMachineForm)
  const [formError, setFormError] = useState<string | null>(null)

  // Maintenance schedule dialog state
  const [maintTarget, setMaintTarget] = useState<Machine | null>(null)
  const [maintFrom, setMaintFrom] = useState('')
  const [maintUntil, setMaintUntil] = useState('')
  const [maintReason, setMaintReason] = useState('')
  const [maintNow, setMaintNow] = useState(true)
  const [maintError, setMaintError] = useState<string | null>(null)

  function openAddDialog() {
    setEditingMachine(null)
    setMachineForm(emptyMachineForm)
    setFormError(null)
    setFormOpen(true)
  }

  function openEditDialog(m: Machine) {
    setEditingMachine(m)
    setMachineForm({
      name: m.name,
      model: m.model,
      technology: m.technology,
      buildWidthMm: m.buildWidthMm,
      buildDepthMm: m.buildDepthMm,
      buildHeightMm: m.buildHeightMm,
      minLayerHeightMm: m.minLayerHeightMm,
      achievableToleranceMm: m.achievableToleranceMm,
      materials: m.supportedMaterials.join(', '),
    })
    setFormError(null)
    setFormOpen(true)
  }

  function openMaintenanceDialog(m: Machine) {
    setMaintTarget(m)
    const nowIso = new Date().toISOString().slice(0, 16)
    const plus4hIso = new Date(Date.now() + 4 * 3600 * 1000).toISOString().slice(0, 16)
    setMaintFrom(m.maintenanceFromUtc ? new Date(m.maintenanceFromUtc).toISOString().slice(0, 16) : nowIso)
    setMaintUntil(m.maintenanceUntilUtc ? new Date(m.maintenanceUntilUtc).toISOString().slice(0, 16) : plus4hIso)
    setMaintReason(m.maintenanceReason ?? 'Bảo trì định kỳ cụm đầu phun & cân bàn')
    setMaintNow(true)
    setMaintError(null)
  }

  async function handleSaveMachine() {
    if (!myLab) return
    setFormError(null)
    setBannerError(null)
    const supportedMaterials = machineForm.materials
      .split(',')
      .map((s) => s.trim().toUpperCase())
      .filter(Boolean)

    try {
      if (editingMachine) {
        await updateMachine.mutateAsync({
          labId: myLab.id,
          machineId: editingMachine.id,
          name: machineForm.name.trim(),
          model: machineForm.model.trim(),
          technology: machineForm.technology,
          buildWidthMm: Number(machineForm.buildWidthMm),
          buildDepthMm: Number(machineForm.buildDepthMm),
          buildHeightMm: Number(machineForm.buildHeightMm),
          minLayerHeightMm: Number(machineForm.minLayerHeightMm),
          achievableToleranceMm: Number(machineForm.achievableToleranceMm),
          supportedMaterials,
        })
        setBannerSuccess(`Đã cập nhật thông số máy "${machineForm.name.trim()}".`)
      } else {
        await addMachine.mutateAsync({
          labId: myLab.id,
          name: machineForm.name.trim(),
          model: machineForm.model.trim(),
          technology: machineForm.technology,
          buildWidthMm: Number(machineForm.buildWidthMm),
          buildDepthMm: Number(machineForm.buildDepthMm),
          buildHeightMm: Number(machineForm.buildHeightMm),
          minLayerHeightMm: Number(machineForm.minLayerHeightMm),
          achievableToleranceMm: Number(machineForm.achievableToleranceMm),
          supportedMaterials,
        })
        setBannerSuccess(`Đã thêm máy "${machineForm.name.trim()}" vào xưởng.`)
      }
      setFormOpen(false)
    } catch (err) {
      setFormError(getApiErrorMessage(err, 'Không thể lưu thông số máy.'))
    }
  }

  async function handleDeleteMachine(m: Machine) {
    if (!myLab) return
    setBannerError(null)
    setBannerSuccess(null)
    try {
      await deleteMachine.mutateAsync({ labId: myLab.id, machineId: m.id })
      setBannerSuccess(`Đã xóa máy "${m.name}".`)
    } catch (err) {
      setBannerError(getApiErrorMessage(err, 'Không thể xóa máy khi đang có đơn in hoạt động.'))
    }
  }

  async function handleChangeStatus(m: Machine, status: MachineStatus) {
    if (!myLab) return
    setBannerError(null)
    setBannerSuccess(null)
    try {
      await setStatus.mutateAsync({ labId: myLab.id, machineId: m.id, status })
      setBannerSuccess(
        status === 3 || status === 2
          ? `Đã chuyển máy "${m.name}" sang ${MACHINE_STATUS_LABELS[status]} — hệ thống đã cập nhật trạng thái ngay lập tức.`
          : `Đã cập nhật trạng thái máy "${m.name}" sang ${MACHINE_STATUS_LABELS[status]}.`,
      )
    } catch (err) {
      setBannerError(getApiErrorMessage(err, 'Không thể đổi trạng thái máy.'))
    }
  }

  async function handleSaveMaintenance(clear = false) {
    if (!myLab || !maintTarget) return
    setMaintError(null)
    try {
      await scheduleMaintenance.mutateAsync({
        labId: myLab.id,
        machineId: maintTarget.id,
        maintenanceFromUtc: clear ? null : new Date(maintFrom).toISOString(),
        maintenanceUntilUtc: clear ? null : new Date(maintUntil).toISOString(),
        reason: clear ? null : maintReason.trim(),
        clear,
        setMaintenanceStatusNow: !clear && maintNow,
      })
      setMaintTarget(null)
      setBannerSuccess(
        clear
          ? `Đã xóa lịch bảo trì và đưa máy "${maintTarget.name}" về sẵn sàng.`
          : `Đã lưu lịch bảo trì cho máy "${maintTarget.name}".`,
      )
    } catch (err) {
      setMaintError(getApiErrorMessage(err, 'Không thể đặt lịch bảo trì.'))
    }
  }

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
              <Typography variant="h1" sx={{ fontSize: '1.9rem', fontWeight: 800, color: 'text.primary' }}>
                Khai báo &amp; Quản lý Máy — Lab
              </Typography>
              <Typography color="text.secondary">
                Khai báo thông số kỹ thuật máy in và thiết lập lịch bảo trì định kỳ
              </Typography>
            </Box>
          </Stack>
          <Stack direction="row" spacing={1.25} flexWrap="wrap" useFlexGap>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<FactoryRounded />}
              onClick={() => navigate('/lab/register')}
              sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
            >
              Hồ sơ xưởng
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ScheduleRounded />}
              onClick={() => navigate('/lab/timeline')}
              sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.25)' }}
            >
              Lịch máy &amp; Điểm
            </Button>
            {myLab && (
              <Button variant="contained" color="primary" startIcon={<AddRounded />} onClick={openAddDialog}>
                Thêm máy in
              </Button>
            )}
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

        {bannerError && (
          <Alert severity="error" onClose={() => setBannerError(null)}>
            {bannerError}
          </Alert>
        )}
        {bannerSuccess && (
          <Alert severity="success" onClose={() => setBannerSuccess(null)}>
            {bannerSuccess}
          </Alert>
        )}
        {isError && <Alert severity="error">Không tải được thông tin xưởng của bạn.</Alert>}

        {isLoading ? (
          <CircularProgress sx={{ alignSelf: 'center', my: 4 }} />
        ) : !myLab ? (
          <Box
            sx={{
              p: 6,
              textAlign: 'center',
              borderRadius: 3,
              border: '1px dashed rgba(255,255,255,0.18)',
              bgcolor: 'background.paper',
            }}
          >
            <FactoryRounded sx={{ fontSize: 44, color: 'text.disabled', mb: 1 }} />
            <Typography sx={{ fontWeight: 700, mb: 1 }}>Bạn chưa đăng ký hồ sơ xưởng</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5 }}>
              Hãy khai báo tên xưởng, lịch làm việc và thời gian chuyển Hub trước khi thêm máy in.
            </Typography>
            <Button variant="contained" onClick={() => navigate('/lab/register')}>
              Đăng ký xưởng ngay
            </Button>
          </Box>
        ) : (
          <Box
            sx={{
              p: { xs: 2, md: 3 },
              borderRadius: 3,
              border: '1px solid rgba(255,255,255,0.1)',
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
                  <Typography sx={{ fontWeight: 700, fontSize: '1.1rem', color: 'text.primary' }}>
                    {myLab.name}
                  </Typography>
                  <Chip
                    size="small"
                    label={
                      myLab.status === 'Active' || myLab.isActive
                        ? 'ACTIVE — Đang hoạt động'
                        : myLab.status === 'Rejected'
                          ? 'REJECTED — Bị từ chối'
                          : 'PENDING — Chờ Admin duyệt'
                    }
                    color={
                      myLab.status === 'Active' || myLab.isActive
                        ? 'success'
                        : myLab.status === 'Rejected'
                          ? 'error'
                          : 'warning'
                    }
                  />
                </Stack>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                  {myLab.city} · Lịch: {myLab.operatingHours ?? 'T2-T7 08:00-18:00'} · Chuyển về Hub:{' '}
                  {myLab.transitDaysToHub} ngày · Tổng số máy: {myLab.machines.length}
                </Typography>
              </Box>
              <Button variant="outlined" size="small" startIcon={<AddRounded />} onClick={openAddDialog}>
                Khai báo máy mới
              </Button>
            </Stack>

            <Divider sx={{ my: 2, borderColor: 'rgba(255,255,255,0.08)' }} />

            {myLab.machines.length === 0 ? (
              <Box sx={{ py: 5, textAlign: 'center' }}>
                <PrecisionManufacturingRounded sx={{ fontSize: 40, color: 'text.disabled', mb: 1 }} />
                <Typography color="text.secondary" sx={{ mb: 2 }}>
                  Xưởng chưa khai báo máy in nào. Ban quản trị chỉ có thể duyệt kích hoạt sau khi xưởng có ít nhất 1 máy in.
                </Typography>
                <Button variant="contained" startIcon={<AddRounded />} onClick={openAddDialog}>
                  Thêm máy đầu tiên
                </Button>
              </Box>
            ) : (
              <Stack spacing={1.75}>
                {myLab.machines.map((machine) => {
                  const isOfflineOrMaint = machine.status === 2 || machine.status === 3
                  return (
                    <Box
                      key={machine.id}
                      sx={{
                        p: 2,
                        borderRadius: 2.5,
                        border: '1px solid rgba(255,255,255,0.09)',
                        bgcolor: 'rgba(255,255,255,0.015)',
                      }}
                    >
                      <Stack
                        direction={{ xs: 'column', md: 'row' }}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', md: 'center' }}
                        spacing={2}
                      >
                        <Stack direction="row" spacing={1.5} alignItems="flex-start">
                          <PrecisionManufacturingRounded
                            sx={{ mt: 0.5, color: isOfflineOrMaint ? 'error.main' : 'primary.main' }}
                          />
                          <Box>
                            <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                              <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{machine.name}</Typography>
                              <Typography variant="body2" color="text.secondary">
                                · {machine.model}
                              </Typography>
                              <Chip
                                size="small"
                                label={MACHINE_STATUS_LABELS[machine.status]}
                                color={
                                  machine.status === 0
                                    ? 'success'
                                    : machine.status === 1
                                      ? 'info'
                                      : machine.status === 2
                                        ? 'warning'
                                        : 'error'
                                }
                              />
                            </Stack>
                            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.4 }}>
                              {PRINT_TECHNOLOGY_LABELS[machine.technology] ?? 'FDM'} · Khổ in {machine.buildWidthMm}×
                              {machine.buildDepthMm}×{machine.buildHeightMm} mm · Layer min {machine.minLayerHeightMm}{' '}
                              mm · Dung sai ±{machine.achievableToleranceMm} mm
                            </Typography>
                            {machine.maintenanceUntilUtc && (
                              <Typography variant="caption" sx={{ display: 'block', color: 'warning.main', mt: 0.5 }}>
                                Lịch bảo trì:{' '}
                                {machine.maintenanceFromUtc
                                  ? new Date(machine.maintenanceFromUtc).toLocaleString('vi-VN')
                                  : 'Hiện tại'}{' '}
                                → {new Date(machine.maintenanceUntilUtc).toLocaleString('vi-VN')}
                                {machine.maintenanceReason ? ` (${machine.maintenanceReason})` : ''}
                              </Typography>
                            )}
                            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.75 }}>
                              {machine.supportedMaterials.map((mat) => (
                                <Chip
                                  key={mat}
                                  size="small"
                                  label={mat}
                                  sx={{ bgcolor: 'rgba(139,92,246,0.14)', color: '#A78BFA', height: 22 }}
                                />
                              ))}
                            </Stack>
                          </Box>
                        </Stack>

                        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                          <FormControl size="small" sx={{ minWidth: 145 }}>
                            <Select
                              value={machine.status}
                              onChange={(e) => handleChangeStatus(machine, Number(e.target.value) as MachineStatus)}
                            >
                              {MACHINE_STATUS_LABELS.map((label, idx) => (
                                <MenuItem key={label} value={idx}>
                                  {label}
                                </MenuItem>
                              ))}
                            </Select>
                          </FormControl>
                          <Button
                            size="small"
                            variant="outlined"
                            color="warning"
                            startIcon={<BuildCircleRounded fontSize="small" />}
                            onClick={() => openMaintenanceDialog(machine)}
                          >
                            Lịch bảo trì
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="inherit"
                            startIcon={<EditRounded fontSize="small" />}
                            onClick={() => openEditDialog(machine)}
                            sx={{ color: 'text.primary', borderColor: 'rgba(255,255,255,0.2)' }}
                          >
                            Sửa
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            color="error"
                            startIcon={<DeleteOutlineRounded fontSize="small" />}
                            disabled={deleteMachine.isPending}
                            onClick={() => handleDeleteMachine(machine)}
                          >
                            Xóa
                          </Button>
                        </Stack>
                      </Stack>
                    </Box>
                  )
                })}
              </Stack>
            )}
          </Box>
        )}
      </Stack>

      {/* ── Add / Edit Machine Dialog ── */}
      <Dialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {editingMachine ? `Chỉnh sửa máy — ${editingMachine.name}` : `Thêm máy in mới — ${myLab?.name ?? ''}`}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
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
                  <InputLabel id="lab-machine-tech-label">Công nghệ</InputLabel>
                  <Select
                    labelId="lab-machine-tech-label"
                    label="Công nghệ"
                    value={machineForm.technology}
                    onChange={(e) =>
                      setMachineForm((f) => ({ ...f, technology: Number(e.target.value) as PrintTechnology }))
                    }
                  >
                    {PRINT_TECHNOLOGY_LABELS.map((label, idx) => (
                      <MenuItem key={label} value={idx}>
                        {label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Vật liệu hỗ trợ (phân cách dấu phẩy)"
                  value={machineForm.materials}
                  onChange={(e) => setMachineForm((f) => ({ ...f, materials: e.target.value }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in X (mm)"
                  type="number"
                  value={machineForm.buildWidthMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, buildWidthMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in Y (mm)"
                  type="number"
                  value={machineForm.buildDepthMm}
                  onChange={(e) => setMachineForm((f) => ({ ...f, buildDepthMm: Number(e.target.value) }))}
                  fullWidth
                />
              </Grid>
              <Grid size={{ xs: 4 }}>
                <TextField
                  label="Khổ in Z (mm)"
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
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button onClick={() => setFormOpen(false)} color="inherit" sx={{ color: 'text.secondary' }}>
            Huỷ
          </Button>
          <Button
            variant="contained"
            onClick={handleSaveMachine}
            disabled={
              addMachine.isPending ||
              updateMachine.isPending ||
              !machineForm.name.trim() ||
              !machineForm.model.trim()
            }
          >
            {editingMachine ? 'Lưu thay đổi' : 'Thêm máy'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Maintenance Schedule Dialog ── */}
      <Dialog
        open={maintTarget !== null}
        onClose={() => setMaintTarget(null)}
        fullWidth
        maxWidth="sm"
        PaperProps={{
          sx: { bgcolor: '#0A0A0A', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.12)' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>Khai lịch bảo trì — {maintTarget?.name}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {maintError && <Alert severity="error">{maintError}</Alert>}
            <TextField
              label="Bắt đầu bảo trì"
              type="datetime-local"
              value={maintFrom}
              onChange={(e) => setMaintFrom(e.target.value)}
              InputLabelProps={{ shrink: true }}
              fullWidth
            />
            <TextField
              label="Kết thúc bảo trì dự kiến"
              type="datetime-local"
              value={maintUntil}
              onChange={(e) => setMaintUntil(e.target.value)}
              InputLabelProps={{ shrink: true }}
              fullWidth
            />
            <TextField
              label="Ghi chú / Nội dung bảo trì"
              value={maintReason}
              onChange={(e) => setMaintReason(e.target.value)}
              fullWidth
            />
            <Typography variant="caption" color="text.secondary">
              Trong khung giờ bảo trì hoặc khi máy ở trạng thái Bảo trì / Ngoại tuyến, hệ thống sẽ tạm thời không phân bổ đơn in cho máy này.
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, justifyContent: 'space-between' }}>
          {maintTarget?.maintenanceUntilUtc ? (
            <Button color="warning" onClick={() => handleSaveMaintenance(true)}>
              Xóa lịch bảo trì
            </Button>
          ) : (
            <Box />
          )}
          <Stack direction="row" spacing={1}>
            <Button onClick={() => setMaintTarget(null)} color="inherit" sx={{ color: 'text.secondary' }}>
              Huỷ
            </Button>
            <Button
              variant="contained"
              color="warning"
              onClick={() => handleSaveMaintenance(false)}
              disabled={scheduleMaintenance.isPending || !maintFrom || !maintUntil}
            >
              Lưu lịch bảo trì
            </Button>
          </Stack>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

