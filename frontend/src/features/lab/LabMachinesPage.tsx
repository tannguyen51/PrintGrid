import { useState, type ReactNode } from 'react'
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
  PrecisionManufacturingRounded,
  ScheduleRounded,
} from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import {
  defaultPrinterConfiguration,
  PrinterConfigurationFields,
  ProfilePresetField,
  type CuraProfileSummary,
} from './PrinterConfigurationFields'
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
  PROFILE_STATUS_LABELS,
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
  printerConfiguration: defaultPrinterConfiguration,
}

function MachineFormSection({ number, title, description, children }: {
  number: number
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <Box sx={{ p: { xs: 1.75, sm: 2.25 }, border: '1px solid', borderColor: 'divider', borderRadius: 2.5 }}>
      <Stack spacing={0.5} sx={{ mb: 2 }}>
        <Typography variant="overline" color="primary.main" fontWeight={800}>Mục {number}</Typography>
        <Typography variant="subtitle1" fontWeight={750}>{title}</Typography>
        <Typography variant="body2" color="text.secondary">{description}</Typography>
      </Stack>
      {children}
    </Box>
  )
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
  const [profilePresetId, setProfilePresetId] = useState('')
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
    setProfilePresetId('')
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
      printerConfiguration: m.printerConfiguration ?? defaultPrinterConfiguration,
    })
    setProfilePresetId(m.curaProfileIds[0] ?? '')
    setFormError(null)
    setFormOpen(true)
  }

  function applyProfilePreset(profile: CuraProfileSummary) {
    setProfilePresetId(profile.id)
    setMachineForm(f => ({
      ...f,
      model: profile.machineModel,
      technology: 0,
      buildWidthMm: profile.widthMm,
      buildDepthMm: profile.depthMm,
      buildHeightMm: profile.heightMm,
      materials: profile.materialCode,
      printerConfiguration: {
        ...f.printerConfiguration,
        manufacturer: profile.manufacturer,
        extruderCount: profile.extruderCount,
        maximumColorSlots: profile.maximumColorSlots,
        nozzleDiameterMm: profile.nozzleDiameterMm,
        filamentDiameterMm: profile.filamentDiameterMm,
        heatedBed: profile.heatedBed,
        maxBedTemperatureC: profile.heatedBed
          ? f.printerConfiguration.maxBedTemperatureC || 100
          : 0,
      },
    }))
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
        const saved = await updateMachine.mutateAsync({
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
          printerConfiguration: machineForm.printerConfiguration,
        })
        setBannerSuccess(`Đã cập nhật máy "${saved.name}". ${PROFILE_STATUS_LABELS[saved.profileStatus]}.`)
      } else {
        const saved = await addMachine.mutateAsync({
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
          printerConfiguration: machineForm.printerConfiguration,
        })
        setBannerSuccess(`Đã thêm máy "${saved.name}". ${PROFILE_STATUS_LABELS[saved.profileStatus]}.`)
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
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
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
              sx={{ color: 'text.primary', borderColor: 'divider' }}
            >
              Hồ sơ xưởng
            </Button>
            <Button
              variant="outlined"
              color="inherit"
              startIcon={<ScheduleRounded />}
              onClick={() => navigate('/lab/timeline')}
              sx={{ color: 'text.primary', borderColor: 'divider' }}
            >
              Lịch máy &amp; Điểm
            </Button>
            {myLab && (
              <Button variant="contained" color="primary" startIcon={<AddRounded />} onClick={openAddDialog}>
                Thêm máy in
              </Button>
            )}
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
              border: '1px dashed #DCE0EA',
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
              border: '1px solid #E8EBF2',
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

            <Divider sx={{ my: 2, borderColor: 'divider' }} />

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
                        border: '1px solid #E8EBF2',
                        bgcolor: 'background.secondary',
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
                            <Chip size="small" sx={{ mt: 1 }}
                              color={machine.profileStatus === 'Approved' ? 'success' : 'warning'}
                              label={PROFILE_STATUS_LABELS[machine.profileStatus] ?? 'Chưa có profile'} />
                            {machine.profileReviewReason && <Typography color="error" variant="body2">{machine.profileReviewReason}</Typography>}
                            <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap sx={{ mt: 0.75 }}>
                              {machine.supportedMaterials.map((mat) => (
                                <Chip
                                  key={mat}
                                  size="small"
                                  label={mat}
                                  sx={{ bgcolor: 'rgba(111,85,255,0.10)', color: '#4933B8', height: 22 }}
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
                            sx={{ color: 'text.primary', borderColor: 'divider' }}
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
        maxWidth="md"
        PaperProps={{
          sx: { bgcolor: 'background.paper', backgroundImage: 'none', border: '1px solid #E8EBF2' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {editingMachine ? `Chỉnh sửa máy — ${editingMachine.name}` : `Thêm máy in mới — ${myLab?.name ?? ''}`}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {formError && <Alert severity="error">{formError}</Alert>}
            {editingMachine && <Alert severity="info">Lưu thông số máy sẽ tìm lại profile và chuyển hồ sơ sang chờ duyệt lại.</Alert>}
            <MachineFormSection number={1} title="Thông tin máy" description="Mã máy giúp lab nhận diện đúng thiết bị; khai đúng hãng và model thực tế.">
              <Grid container spacing={1.5}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField label="Mã máy của lab" value={machineForm.name}
                    onChange={e => setMachineForm(f => ({ ...f, name: e.target.value }))} required fullWidth />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField label="Hãng và model" value={machineForm.model}
                    onChange={e => setMachineForm(f => ({ ...f, model: e.target.value }))} required fullWidth />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <FormControl fullWidth>
                    <InputLabel id="lab-machine-tech-label">Công nghệ</InputLabel>
                    <Select labelId="lab-machine-tech-label" label="Công nghệ" value={machineForm.technology}
                      onChange={e => setMachineForm(f => ({ ...f, technology: Number(e.target.value) as PrintTechnology }))}>
                      {PRINT_TECHNOLOGY_LABELS.map((label, idx) => <MenuItem key={label} value={idx}>{label}</MenuItem>)}
                    </Select>
                  </FormControl>
                </Grid>
                <Grid size={{ xs: 12 }}>
                  <Grid container spacing={1}>
                    <PrinterConfigurationFields groups={['machine']} value={machineForm.printerConfiguration}
                      onChange={printerConfiguration => setMachineForm(f => ({ ...f, printerConfiguration }))} />
                  </Grid>
                </Grid>
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={2} title="Kích thước vùng in" description="Nhập vùng in hữu dụng theo X × Y × Z; thông số phải khớp với profile máy.">
              <Grid container spacing={1.5}>
                {([
                  ['buildWidthMm', 'Khổ in X (mm)'],
                  ['buildDepthMm', 'Khổ in Y (mm)'],
                  ['buildHeightMm', 'Khổ in Z (mm)'],
                ] as const).map(([key, label]) => <Grid key={key} size={{ xs: 12, sm: 4 }}>
                  <TextField label={label} type="number" value={machineForm[key]} required fullWidth
                    slotProps={{ htmlInput: { min: 1, step: 1 } }}
                    onChange={e => setMachineForm(f => ({ ...f, [key]: Number(e.target.value) }))} />
                </Grid>)}
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={3} title="Cấu hình đầu đùn" description="Thông số phần cứng phải khớp đầu phun và sợi nhựa lab đang sử dụng.">
              <Grid container spacing={1.5}>
                <PrinterConfigurationFields groups={['extrusion']} value={machineForm.printerConfiguration}
                  onChange={printerConfiguration => setMachineForm(f => ({ ...f, printerConfiguration }))} />
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={4} title="Vật liệu và màu sắc" description="Chỉ khai báo mã vật liệu máy thực sự hỗ trợ. Màu sắc và lượng tồn kho được ghi nhận riêng trong kho lab.">
              <Stack spacing={1.5}>
                <TextField label="Vật liệu hỗ trợ (mã, phân cách bằng dấu phẩy)"
                  value={machineForm.materials}
                  onChange={e => setMachineForm(f => ({ ...f, materials: e.target.value }))}
                  helperText="Ví dụ: PLA, PETG. Chỉ vật liệu có profile CuraEngine phù hợp mới được xét duyệt báo giá."
                  required fullWidth />
                {myLab && <Button variant="outlined" sx={{ alignSelf: 'flex-start' }}
                  onClick={() => { setFormOpen(false); navigate(`/lab/${myLab.id}/inventory`) }}>
                  Khai báo màu và tồn kho vật liệu
                </Button>}
              </Stack>
            </MachineFormSection>

            <MachineFormSection number={5} title="Cấu hình chất lượng mặc định" description="Thông số khả năng tối thiểu của máy dùng để lọc đơn; profile CuraEngine cung cấp chất lượng in tiêu chuẩn.">
              <Grid container spacing={1.5}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField label="Độ dày lớp nhỏ nhất máy hỗ trợ (mm)" type="number"
                    value={machineForm.minLayerHeightMm} required fullWidth
                    slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
                    onChange={e => setMachineForm(f => ({ ...f, minLayerHeightMm: Number(e.target.value) }))} />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField label="Dung sai đạt được (mm)" type="number"
                    value={machineForm.achievableToleranceMm} required fullWidth
                    slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
                    onChange={e => setMachineForm(f => ({ ...f, achievableToleranceMm: Number(e.target.value) }))} />
                </Grid>
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={6} title="Tốc độ máy" description="Không nhập một con số tốc độ tối đa: CuraEngine dùng tốc độ in/di chuyển và các giới hạn trong profile đã duyệt.">
              <Alert severity="info">Tốc độ theo từng loại đường in và giới hạn chuyển động được lấy từ profile CuraEngine tương ứng.</Alert>
            </MachineFormSection>

            <MachineFormSection number={7} title="Rút nhựa" description="Không cần lab tự nhập retraction, jerk hay prime tower.">
              <Alert severity="info">Các thông số rút nhựa, đổi đầu đùn và prime tower được giữ trong profile CuraEngine; không chỉnh riêng trên biểu mẫu máy.</Alert>
            </MachineFormSection>

            <MachineFormSection number={8} title="Support và bám bàn" description="Dùng thiết lập thống nhất trong profile chất lượng, không khai báo lại ở cấp máy.">
              <Alert severity="info">Support, kiểu support và kiểu bám bàn được lấy từ profile CuraEngine khi slicer tính báo giá.</Alert>
            </MachineFormSection>

            <MachineFormSection number={9} title="In nhiều màu" description="Khai đúng số đầu đùn vật lý và số màu tối đa mà máy có thể đổi trong một sản phẩm.">
              <Grid container spacing={1.5}>
                <PrinterConfigurationFields groups={['multicolor']} value={machineForm.printerConfiguration}
                  onChange={printerConfiguration => setMachineForm(f => ({ ...f, printerConfiguration }))} />
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={10} title="Thông tin tính giá" description="Đơn giá máy áp dụng theo giờ; giá vật liệu, màu, đánh bóng và dịch vụ khác theo thỏa thuận riêng.">
              <Grid container spacing={1.5}>
                <PrinterConfigurationFields groups={['pricing']} value={machineForm.printerConfiguration}
                  onChange={printerConfiguration => setMachineForm(f => ({ ...f, printerConfiguration }))} />
                <Grid size={{ xs: 12 }}>
                  <Alert severity="info">Giá vật liệu theo kg và phí dịch vụ chưa được cấu hình trong hồ sơ máy; lab tiếp tục quản lý theo hợp đồng và tồn kho đã khai báo.</Alert>
                </Grid>
              </Grid>
            </MachineFormSection>

            <MachineFormSection number={11} title="Profile CuraEngine" description="Chọn profile tham chiếu để điền nhanh model, khổ in, đầu phun, số màu và vật liệu; hệ thống sẽ đối chiếu lại khi lưu.">
              <Grid container spacing={1.5}>
                <ProfilePresetField value={profilePresetId} onSelect={applyProfilePreset} />
                <Grid size={{ xs: 12 }}>
                  <Alert severity="info">
                    Profile phải do quản trị viên nhập và duyệt trước khi máy tham gia báo giá. Việc chọn profile ở đây chỉ điền thông số tham chiếu; hồ sơ máy được tự đối chiếu theo hãng, model, khổ in, đầu phun, đầu đùn, số màu và vật liệu.
                  </Alert>
                </Grid>
              </Grid>
            </MachineFormSection>
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
              !machineForm.model.trim() ||
              !machineForm.printerConfiguration.manufacturer.trim() ||
              !machineForm.materials.trim() ||
              machineForm.buildWidthMm <= 0 ||
              machineForm.buildDepthMm <= 0 ||
              machineForm.buildHeightMm <= 0 ||
              machineForm.minLayerHeightMm <= 0 ||
              machineForm.achievableToleranceMm <= 0 ||
              machineForm.printerConfiguration.nozzleDiameterMm <= 0 ||
              machineForm.printerConfiguration.filamentDiameterMm <= 0 ||
              machineForm.printerConfiguration.extruderCount < 1 ||
              machineForm.printerConfiguration.maximumColorSlots < machineForm.printerConfiguration.extruderCount ||
              machineForm.printerConfiguration.maxNozzleTemperatureC <= 0 ||
              (machineForm.printerConfiguration.heatedBed && machineForm.printerConfiguration.maxBedTemperatureC <= 0) ||
              machineForm.printerConfiguration.pricePerHour <= 0 ||
              machineForm.printerConfiguration.failureRate < 0 ||
              machineForm.printerConfiguration.failureRate >= 1
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
          sx: { bgcolor: 'background.paper', backgroundImage: 'none', border: '1px solid #E8EBF2' },
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
