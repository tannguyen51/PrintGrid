import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Checkbox,
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
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import {
  AddRounded,
  AutoFixHighRounded,
  BuildCircleRounded,
  DeleteOutlineRounded,
  EditRounded,
  FactoryRounded,
  PrecisionManufacturingRounded,
  ScheduleRounded,
  SearchRounded,
  SendRounded,
  SyncRounded,
  UploadFileRounded,
} from '@mui/icons-material'
import { PageBackButton } from '../../shared/components/PageBackButton'
import { ServerPaginationBar } from '../../shared/components/ServerPaginationBar'
import { getApiErrorMessage } from '../../shared/api/apiError'
import {
  ApprovedProfilePresetField,
  defaultPrinterConfiguration,
  PrinterConfigurationFields,
  ProfilePresetField,
  type CuraProfileSummary,
} from './PrinterConfigurationFields'
import {
  useCuraProfileProposalDetail,
  useMyCuraProfileProposals,
  useResubmitCuraProfileProposal,
  useSubmitCuraProfileProposal,
} from './curaProfileApi'
import { buildSampleResolvedCuraProfileJson } from './curaProfileTemplate'
import {
  useAddMachine,
  useDeleteMachine,
  useMyLab,
  useScheduleMachineMaintenance,
  useSetMachineStatus,
  useSyncMachineProfiles,
  useUpdateMachine,
} from '../ops/useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  PROFILE_APPROVAL_STATUS_LABELS,
  PROFILE_STATUS_LABELS,
  type CuraProfileListItem,
  type Machine,
  type MachineStatus,
  type MyProposalsFilter,
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

const emptyProposalForm = {
  name: '',
  manufacturer: '',
  machineModel: '',
  materialCode: 'PLA',
  version: '1.0',
  extruderCount: 1,
  targetMachineId: '',
  submissionNotes: '',
  sourceFileName: '',
  settingsJson: '',
}

function MachineFormSection({
  number,
  title,
  description,
  children,
}: {
  number: number
  title: string
  description: string
  children: ReactNode
}) {
  return (
    <Box sx={{ p: { xs: 1.75, sm: 2.25 }, border: '1px solid', borderColor: 'divider', borderRadius: 2.5 }}>
      <Stack spacing={0.5} sx={{ mb: 1.75 }}>
        <Typography variant="overline" color="primary.main" fontWeight={800}>
          Mục {number}
        </Typography>
        <Typography variant="subtitle1" fontWeight={750}>
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {description}
        </Typography>
      </Stack>
      {children}
    </Box>
  )
}

function profileStatusColor(status: string): 'success' | 'warning' | 'error' | 'info' {
  if (status === 'Approved') return 'success'
  if (status === 'PendingReview') return 'info'
  if (status === 'Rejected') return 'error'
  return 'warning'
}

export default function LabMachinesPage() {
  const navigate = useNavigate()
  const { data: myLab, isLoading, isError } = useMyLab(false)

  const addMachine = useAddMachine()
  const updateMachine = useUpdateMachine()
  const deleteMachine = useDeleteMachine()
  const setStatus = useSetMachineStatus()
  const scheduleMaintenance = useScheduleMachineMaintenance()
  const syncProfiles = useSyncMachineProfiles()
  const submitProposal = useSubmitCuraProfileProposal()
  const resubmitProposal = useResubmitCuraProfileProposal()

  const [bannerError, setBannerError] = useState<string | null>(null)
  const [bannerSuccess, setBannerSuccess] = useState<string | null>(null)
  const [profileSyncErrors, setProfileSyncErrors] = useState<string[]>([])

  // Add / Edit machine dialog state
  const [formOpen, setFormOpen] = useState(false)
  const [editingMachine, setEditingMachine] = useState<Machine | null>(null)
  const [machineForm, setMachineForm] = useState(emptyMachineForm)
  const [profileMode, setProfileMode] = useState<'existing' | 'request_new'>('existing')
  const [selectedApprovedProfileId, setSelectedApprovedProfileId] = useState('')
  const [includeProfileProposal, setIncludeProfileProposal] = useState(true)
  const [inlineProposal, setInlineProposal] = useState({
    name: '',
    version: '1.0',
    materialCode: 'PLA',
    submissionNotes: '',
    sourceFileName: '',
    settingsJson: '',
  })
  const [formError, setFormError] = useState<string | null>(null)

  // Standalone / Resubmit profile proposal dialog state
  const [proposalDialogOpen, setProposalDialogOpen] = useState(false)
  const [editingProposalId, setEditingProposalId] = useState<string | null>(null)
  const [proposalForm, setProposalForm] = useState(emptyProposalForm)
  const [proposalError, setProposalError] = useState<string | null>(null)
  const proposalDetail = useCuraProfileProposalDetail(editingProposalId)

  useEffect(() => {
    if (editingProposalId && proposalDetail.data && proposalDetail.data.id === editingProposalId) {
      const d = proposalDetail.data
      setProposalForm({
        name: d.name,
        manufacturer: d.manufacturer,
        machineModel: d.machineModel,
        materialCode: d.materialCode,
        version: d.version,
        extruderCount: d.extruderCount,
        targetMachineId: d.targetMachineId ?? '',
        submissionNotes: d.submissionNotes ?? '',
        sourceFileName: d.sourceFileName ?? '',
        settingsJson: d.settingsJson,
      })
    }
  }, [editingProposalId, proposalDetail.data])

  // My proposals server-side filters & pagination
  const [proposalSearchInput, setProposalSearchInput] = useState('')
  const [proposalFilters, setProposalFilters] = useState<MyProposalsFilter>({
    search: '',
    approvalStatus: '',
    material: '',
    page: 1,
    pageSize: 5,
  })
  useEffect(() => {
    const t = setTimeout(() => {
      setProposalFilters((prev) =>
        prev.search === proposalSearchInput ? prev : { ...prev, search: proposalSearchInput, page: 1 },
      )
    }, 300)
    return () => clearTimeout(t)
  }, [proposalSearchInput])

  const myProposals = useMyCuraProfileProposals(proposalFilters, Boolean(myLab))

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
    setProfileMode('existing')
    setSelectedApprovedProfileId('')
    setIncludeProfileProposal(true)
    setInlineProposal({
      name: '',
      version: '1.0',
      materialCode: 'PLA',
      submissionNotes: '',
      sourceFileName: '',
      settingsJson: '',
    })
    setFormError(null)
    setFormOpen(true)
  }

  function openEditDialog(m: Machine) {
    setEditingMachine(m)
    const cfg = { ...defaultPrinterConfiguration, ...(m.printerConfiguration ?? {}) }
    setMachineForm({
      name: m.name ?? '',
      model: m.model ?? '',
      technology: m.technology,
      buildWidthMm: m.buildWidthMm,
      buildDepthMm: m.buildDepthMm,
      buildHeightMm: m.buildHeightMm,
      minLayerHeightMm: m.minLayerHeightMm,
      achievableToleranceMm: m.achievableToleranceMm,
      materials: (m.supportedMaterials ?? []).join(', '),
      printerConfiguration: cfg,
    })
    const hasExisting = Boolean(cfg.curaDefinitionId) || (m.curaProfileIds?.length ?? 0) > 0
    setProfileMode(hasExisting ? 'existing' : 'request_new')
    setSelectedApprovedProfileId(m.curaProfileIds?.[0] ?? '')
    setIncludeProfileProposal(false)
    setInlineProposal({
      name: `${cfg.manufacturer || ''} ${m.model} ${m.supportedMaterials?.[0] ?? 'PLA'}`.trim(),
      version: '1.0',
      materialCode: m.supportedMaterials?.[0] ?? 'PLA',
      submissionNotes: '',
      sourceFileName: '',
      settingsJson: '',
    })
    setFormError(null)
    setFormOpen(true)
  }

  function applyApprovedProfile(profile: CuraProfileSummary) {
    setSelectedApprovedProfileId(profile.id)
    setMachineForm((f) => ({
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
        maxNozzleTemperatureC: Math.max(
          f.printerConfiguration.maxNozzleTemperatureC || 0,
          profile.requiredNozzleTemperatureC,
        ),
        maxBedTemperatureC: profile.heatedBed
          ? Math.max(f.printerConfiguration.maxBedTemperatureC || 0, profile.requiredBedTemperatureC)
          : 0,
        curaDefinitionId: '',
        curaExtruderDefinitionId: '',
        curaVariantId: '',
        curaQualityId: '',
      },
    }))
  }

  async function readJsonFile(
    file: File | undefined,
    onLoaded: (fileName: string, jsonText: string) => void,
    onErr: (msg: string) => void,
  ) {
    if (!file) return
    if (file.size > 2 * 1024 * 1024) {
      onErr('File resolved JSON tối đa 2 MB.')
      return
    }
    try {
      const text = await file.text()
      JSON.parse(text)
      onLoaded(file.name, text)
    } catch {
      onErr('File không phải JSON hợp lệ.')
    }
  }

  function fillInlineSampleJson() {
    const firstMaterial =
      inlineProposal.materialCode.trim().toUpperCase() ||
      machineForm.materials.split(',')[0]?.trim().toUpperCase() ||
      'PLA'
    const json = buildSampleResolvedCuraProfileJson({
      machineName: `${machineForm.printerConfiguration.manufacturer || 'Custom'} ${machineForm.model || 'Printer'}`.trim(),
      widthMm: Number(machineForm.buildWidthMm),
      depthMm: Number(machineForm.buildDepthMm),
      heightMm: Number(machineForm.buildHeightMm),
      nozzleDiameterMm: Number(machineForm.printerConfiguration.nozzleDiameterMm),
      filamentDiameterMm: Number(machineForm.printerConfiguration.filamentDiameterMm),
      heatedBed: machineForm.printerConfiguration.heatedBed,
      extruderCount: Number(machineForm.printerConfiguration.extruderCount),
      maximumColorSlots: Number(machineForm.printerConfiguration.maximumColorSlots),
      materialCode: firstMaterial,
    })
    const slug = (machineForm.model || 'custom-printer').toLowerCase().replace(/[^a-z0-9]+/g, '-')
    setInlineProposal((p) => ({
      ...p,
      name: p.name || `${machineForm.model || 'Custom'} ${firstMaterial} Profile`.trim(),
      materialCode: firstMaterial,
      sourceFileName: p.sourceFileName || `${slug}-${firstMaterial.toLowerCase()}.resolved.json`,
      settingsJson: json,
    }))
    setFormError(null)
  }

  function openNewProposalDialog(machine?: Machine) {
    setEditingProposalId(null)
    const mat = machine?.supportedMaterials?.[0] ?? 'PLA'
    const cfg = machine?.printerConfiguration
    const sample = machine
      ? buildSampleResolvedCuraProfileJson({
          machineName: `${cfg?.manufacturer || ''} ${machine.model}`.trim(),
          widthMm: machine.buildWidthMm,
          depthMm: machine.buildDepthMm,
          heightMm: machine.buildHeightMm,
          nozzleDiameterMm: cfg?.nozzleDiameterMm ?? 0.4,
          filamentDiameterMm: cfg?.filamentDiameterMm ?? 1.75,
          heatedBed: cfg?.heatedBed ?? true,
          extruderCount: cfg?.extruderCount ?? 1,
          maximumColorSlots: cfg?.maximumColorSlots ?? 1,
          materialCode: mat,
        })
      : ''
    setProposalForm({
      name: machine ? `${machine.model} ${mat} Standard` : '',
      manufacturer: cfg?.manufacturer ?? '',
      machineModel: machine?.model ?? '',
      materialCode: mat,
      version: '1.0',
      extruderCount: cfg?.extruderCount ?? 1,
      targetMachineId: machine?.id ?? '',
      submissionNotes: machine ? `Đề nghị bổ sung profile cho máy ${machine.name} (${machine.model})` : '',
      sourceFileName: machine ? `${machine.model.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${mat.toLowerCase()}.resolved.json` : '',
      settingsJson: sample,
    })
    setProposalError(null)
    setProposalDialogOpen(true)
  }

  function openResubmitProposalDialog(item: CuraProfileListItem) {
    setEditingProposalId(item.id)
    setProposalForm({
      name: item.name,
      manufacturer: item.manufacturer,
      machineModel: item.machineModel,
      materialCode: item.materialCode,
      version: item.version,
      extruderCount: item.extruderCount,
      targetMachineId: item.targetMachineId ?? '',
      submissionNotes: item.submissionNotes ?? '',
      sourceFileName: item.sourceFileName ?? '',
      settingsJson: '',
    })
    setProposalError(null)
    setProposalDialogOpen(true)
  }

  async function handleSaveProposalDialog() {
    if (!myLab) return
    setProposalError(null)
    try {
      JSON.parse(proposalForm.settingsJson)
    } catch {
      setProposalError('Nội dung Settings JSON không hợp lệ.')
      return
    }

    try {
      if (editingProposalId) {
        const updated = await resubmitProposal.mutateAsync({
          id: editingProposalId,
          name: proposalForm.name.trim(),
          manufacturer: proposalForm.manufacturer.trim(),
          machineModel: proposalForm.machineModel.trim(),
          materialCode: proposalForm.materialCode.trim().toUpperCase(),
          version: proposalForm.version.trim(),
          extruderCount: Number(proposalForm.extruderCount),
          settingsJson: proposalForm.settingsJson,
          targetMachineId: proposalForm.targetMachineId || null,
          submissionNotes: proposalForm.submissionNotes.trim() || null,
          sourceFileName: proposalForm.sourceFileName.trim() || null,
        })
        setBannerSuccess(`Đã sửa và gửi lại yêu cầu profile "${updated.name}" để Admin duyệt.`)
      } else {
        const created = await submitProposal.mutateAsync({
          labId: myLab.id,
          name: proposalForm.name.trim(),
          manufacturer: proposalForm.manufacturer.trim(),
          machineModel: proposalForm.machineModel.trim(),
          materialCode: proposalForm.materialCode.trim().toUpperCase(),
          version: proposalForm.version.trim(),
          extruderCount: Number(proposalForm.extruderCount),
          settingsJson: proposalForm.settingsJson,
          targetMachineId: proposalForm.targetMachineId || null,
          submissionNotes: proposalForm.submissionNotes.trim() || null,
          sourceFileName: proposalForm.sourceFileName.trim() || null,
        })
        setBannerSuccess(
          `Đã gửi yêu cầu profile "${created.name}" (${created.materialCode}) cho Admin duyệt. Profile sẽ chỉ dùng ghép máy và báo giá sau khi được duyệt.`,
        )
      }
      setProposalDialogOpen(false)
      setEditingProposalId(null)
    } catch (err) {
      setProposalError(getApiErrorMessage(err, 'Không thể gửi yêu cầu profile.'))
    }
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
    setBannerSuccess(null)
    setProfileSyncErrors([])

    if (!machineForm.name?.trim() || !machineForm.model?.trim()) {
      setFormError('Tên máy và model không được để trống.')
      return
    }
    if (!machineForm.printerConfiguration.manufacturer?.trim()) {
      setFormError('Vui lòng nhập hãng sản xuất.')
      return
    }

    const supportedMaterials = machineForm.materials
      .split(',')
      .map((s) => s.trim().toUpperCase())
      .filter(Boolean)
    if (supportedMaterials.length === 0) {
      setFormError('Vui lòng khai báo ít nhất 1 mã vật liệu (VD: PLA, PETG).')
      return
    }

    const shouldSendProposal =
      machineForm.technology === 0 && profileMode === 'request_new' && includeProfileProposal

    if (shouldSendProposal) {
      if (!inlineProposal.name.trim() || !inlineProposal.version.trim() || !inlineProposal.materialCode.trim()) {
        setFormError('Vui lòng điền tên profile, phiên bản và mã vật liệu cho yêu cầu profile mới.')
        return
      }
      if (!inlineProposal.settingsJson.trim()) {
        setFormError('Vui lòng tải file resolved JSON hoặc tạo khung JSON mẫu cho yêu cầu profile mới.')
        return
      }
      try {
        JSON.parse(inlineProposal.settingsJson)
      } catch {
        setFormError('Nội dung profile resolved JSON không hợp lệ.')
        return
      }
    }

    if (
      machineForm.technology === 0 &&
      profileMode === 'existing' &&
      !selectedApprovedProfileId &&
      !machineForm.printerConfiguration.curaDefinitionId
    ) {
      setFormError(
        'Vui lòng chọn profile đã duyệt từ thư viện hoặc chọn model từ danh mục Cura 5.11.0; nếu chưa có profile phù hợp, hãy chuyển sang mục "Chưa có profile".',
      )
      return
    }

    let savedMachine: Machine
    try {
      const payload = {
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
      }
      if (editingMachine) {
        savedMachine = await updateMachine.mutateAsync({ ...payload, machineId: editingMachine.id })
      } else {
        savedMachine = await addMachine.mutateAsync(payload)
        // If proposal submission fails after machine creation, switch to edit mode to prevent duplicate machine creation on retry.
        setEditingMachine(savedMachine)
      }
      setProfileSyncErrors(savedMachine.profileSyncErrors ?? [])
    } catch (err) {
      setFormError(getApiErrorMessage(err, 'Không thể lưu thông số máy.'))
      return
    }

    if (shouldSendProposal) {
      try {
        const proposal = await submitProposal.mutateAsync({
          labId: myLab.id,
          name: inlineProposal.name.trim(),
          manufacturer: machineForm.printerConfiguration.manufacturer.trim(),
          machineModel: machineForm.model.trim(),
          materialCode: inlineProposal.materialCode.trim().toUpperCase(),
          version: inlineProposal.version.trim(),
          extruderCount: Number(machineForm.printerConfiguration.extruderCount),
          settingsJson: inlineProposal.settingsJson,
          targetMachineId: savedMachine.id,
          submissionNotes: inlineProposal.submissionNotes.trim() || null,
          sourceFileName: inlineProposal.sourceFileName.trim() || null,
        })
        setBannerSuccess(
          `Đã lưu máy "${savedMachine.name}" (${PROFILE_STATUS_LABELS[savedMachine.profileStatus] ?? savedMachine.profileStatus}) và gửi yêu cầu profile "${proposal.name}" chờ Admin duyệt.`,
        )
      } catch (err) {
        setFormError(
          `Đã lưu máy "${savedMachine.name}", nhưng gửi yêu cầu profile thất bại: ${getApiErrorMessage(err, 'JSON profile chưa hợp lệ.')}`,
        )
        return
      }
    } else {
      setBannerSuccess(
        `${editingMachine ? 'Đã cập nhật' : 'Đã thêm'} máy "${savedMachine.name}". ${PROFILE_STATUS_LABELS[savedMachine.profileStatus] ?? savedMachine.profileStatus}.`,
      )
    }

    setFormOpen(false)
  }

  async function handleSyncProfiles(machine: Machine) {
    setBannerError(null)
    setBannerSuccess(null)
    setProfileSyncErrors([])
    try {
      const result = await syncProfiles.mutateAsync(machine.id)
      setProfileSyncErrors(result.errors)
      setBannerSuccess(
        `Đã đồng bộ profile máy "${machine.name}". ${PROFILE_STATUS_LABELS[result.status] ?? result.status}.`,
      )
    } catch (err) {
      setBannerError(getApiErrorMessage(err, 'Không thể đồng bộ profile máy.'))
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
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1120, mx: 'auto' }} spacing={2.5}>
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
                Khai báo máy in, chọn hoặc đề nghị profile CuraEngine và theo dõi kết quả duyệt từ Admin
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
        {profileSyncErrors.length > 0 && (
          <Alert severity="warning" onClose={() => setProfileSyncErrors([])}>
            Chưa đồng bộ đủ profile Cura. {profileSyncErrors.join(' ')}
          </Alert>
        )}

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
          <>
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
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  <Button variant="outlined" size="small" startIcon={<SendRounded />} onClick={() => openNewProposalDialog()}>
                    Đề nghị profile mới
                  </Button>
                  <Button variant="outlined" size="small" startIcon={<AddRounded />} onClick={openAddDialog}>
                    Khai báo máy mới
                  </Button>
                </Stack>
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
                    const isQuotingReady =
                      (myLab.status === 'Active' || myLab.isActive) &&
                      myLab.isInGoodStanding &&
                      !isOfflineOrMaint &&
                      (machine.technology !== 0 || machine.profileStatus === 'Approved')

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
                                  · {machine.printerConfiguration?.manufacturer ? `${machine.printerConfiguration.manufacturer} ` : ''}
                                  {machine.model}
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
                                {machine.buildDepthMm}×{machine.buildHeightMm} mm · Layer min {machine.minLayerHeightMm} mm · Dung sai ±
                                {machine.achievableToleranceMm} mm
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
                              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap sx={{ mt: 1 }}>
                                <Chip
                                  size="small"
                                  color={profileStatusColor(machine.profileStatus)}
                                  label={PROFILE_STATUS_LABELS[machine.profileStatus] ?? machine.profileStatus}
                                />
                                <Chip
                                  size="small"
                                  variant="outlined"
                                  color={isQuotingReady ? 'success' : 'default'}
                                  label={
                                    isQuotingReady
                                      ? 'Được phép báo giá & gán việc'
                                      : machine.technology === 0 && machine.profileStatus === 'PendingReview'
                                        ? 'Chờ Admin duyệt máy để báo giá/gán việc'
                                        : 'Chưa tham gia báo giá/gán việc'
                                  }
                                />
                              </Stack>
                              {machine.profileReviewReason && (
                                <Alert severity="error" sx={{ mt: 1 }}>
                                  Lý do từ chối máy: {machine.profileReviewReason}
                                </Alert>
                              )}
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
                            {machine.technology === 0 && machine.profileStatus !== 'Approved' && (
                              <Button
                                size="small"
                                variant="outlined"
                                color="primary"
                                startIcon={<SendRounded fontSize="small" />}
                                onClick={() => openNewProposalDialog(machine)}
                              >
                                Đề nghị profile
                              </Button>
                            )}
                            {machine.technology === 0 &&
                              machine.printerConfiguration?.curaDefinitionId &&
                              machine.profileStatus !== 'Approved' && (
                                <Button
                                  size="small"
                                  variant="outlined"
                                  startIcon={<SyncRounded fontSize="small" />}
                                  disabled={syncProfiles.isPending}
                                  onClick={() => handleSyncProfiles(machine)}
                                >
                                  Đồng bộ profile
                                </Button>
                              )}
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

            {/* ── Lab Profile Proposals Section ── */}
            <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                justifyContent="space-between"
                alignItems={{ xs: 'flex-start', sm: 'center' }}
                spacing={1.5}
                sx={{ mb: 2 }}
              >
                <Box>
                  <Typography variant="h6" fontWeight={750}>
                    Yêu cầu đề nghị profile mới của xưởng
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Theo dõi kết quả duyệt profile từ Admin. Nếu bị từ chối, bạn có thể xem lý do, chỉnh sửa và gửi lại.
                  </Typography>
                </Box>
                <Button variant="contained" size="small" startIcon={<SendRounded />} onClick={() => openNewProposalDialog()}>
                  Gửi đề nghị profile mới
                </Button>
              </Stack>

              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ mb: 2 }} flexWrap="wrap" useFlexGap>
                <TextField
                  size="small"
                  placeholder="Tìm theo tên profile, hãng, model…"
                  value={proposalSearchInput}
                  onChange={(e) => setProposalSearchInput(e.target.value)}
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
                <FormControl size="small" sx={{ minWidth: 180 }}>
                  <InputLabel id="lab-proposal-status-label">Trạng thái duyệt</InputLabel>
                  <Select
                    labelId="lab-proposal-status-label"
                    label="Trạng thái duyệt"
                    value={proposalFilters.approvalStatus ?? ''}
                    onChange={(e) =>
                      setProposalFilters((prev) => ({ ...prev, approvalStatus: e.target.value, page: 1 }))
                    }
                  >
                    <MenuItem value="">Tất cả trạng thái</MenuItem>
                    <MenuItem value="PendingReview">Chờ Admin duyệt</MenuItem>
                    <MenuItem value="Approved">Đã duyệt</MenuItem>
                    <MenuItem value="Rejected">Bị từ chối</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  size="small"
                  label="Vật liệu"
                  placeholder="PLA, PETG…"
                  value={proposalFilters.material ?? ''}
                  onChange={(e) => setProposalFilters((prev) => ({ ...prev, material: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 140 } }}
                />
                <Button
                  color="inherit"
                  onClick={() => {
                    setProposalSearchInput('')
                    setProposalFilters((prev) => ({ ...prev, search: '', approvalStatus: '', material: '', page: 1 }))
                  }}
                >
                  Xóa lọc
                </Button>
              </Stack>

              {myProposals.isLoading ? (
                <CircularProgress size={28} sx={{ my: 2 }} />
              ) : myProposals.isError ? (
                <Alert severity="error">Không tải được danh sách yêu cầu profile của xưởng.</Alert>
              ) : (myProposals.data?.items.length ?? 0) === 0 ? (
                <Alert severity="info">
                  Chưa có yêu cầu đề nghị profile nào khớp bộ lọc hiện tại.
                </Alert>
              ) : (
                <Stack spacing={1.5}>
                  {myProposals.data?.items.map((p) => (
                    <Box key={p.id} sx={{ p: 2, borderRadius: 2, border: '1px solid', borderColor: 'divider' }}>
                      <Stack
                        direction={{ xs: 'column', sm: 'row' }}
                        justifyContent="space-between"
                        alignItems={{ xs: 'flex-start', sm: 'center' }}
                        spacing={1}
                      >
                        <Box>
                          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                            <Typography fontWeight={700}>
                              {p.name} · v{p.version}
                            </Typography>
                            <Chip
                              size="small"
                              color={
                                p.approvalStatus === 'Approved'
                                  ? 'success'
                                  : p.approvalStatus === 'Rejected'
                                    ? 'error'
                                    : 'warning'
                              }
                              label={PROFILE_APPROVAL_STATUS_LABELS[p.approvalStatus] ?? p.approvalStatus}
                            />
                            {p.targetMachineName && (
                              <Chip size="small" variant="outlined" label={`Máy: ${p.targetMachineName}`} />
                            )}
                          </Stack>
                          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                            {p.manufacturer} · {p.machineModel} · Vật liệu <b>{p.materialCode}</b> · CuraEngine {p.engineVersion} · Khổ{' '}
                            {p.widthMm}×{p.depthMm}×{p.heightMm} mm · Đầu phun {p.nozzleDiameterMm} mm · {p.extruderCount} đầu đùn /{' '}
                            {p.maximumColorSlots} màu
                          </Typography>
                          {(p.sourceFileName || p.submissionNotes) && (
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.4 }}>
                              {p.sourceFileName ? `File: ${p.sourceFileName}` : ''}
                              {p.sourceFileName && p.submissionNotes ? ' · ' : ''}
                              {p.submissionNotes ? `Ghi chú: ${p.submissionNotes}` : ''}
                            </Typography>
                          )}
                        </Box>

                        {p.approvalStatus !== 'Approved' && (
                          <Button
                            size="small"
                            variant={p.approvalStatus === 'Rejected' ? 'contained' : 'outlined'}
                            color={p.approvalStatus === 'Rejected' ? 'warning' : 'primary'}
                            startIcon={<EditRounded fontSize="small" />}
                            onClick={() => openResubmitProposalDialog(p)}
                          >
                            {p.approvalStatus === 'Rejected' ? 'Sửa & gửi lại' : 'Chỉnh sửa yêu cầu'}
                          </Button>
                        )}
                      </Stack>

                      {p.approvalStatus === 'Rejected' && p.reviewReason && (
                        <Alert severity="error" sx={{ mt: 1.25 }}>
                          <b>Lý do từ chối từ Admin:</b> {p.reviewReason}
                        </Alert>
                      )}
                      {p.approvalStatus === 'Approved' && (
                        <Alert severity="success" sx={{ mt: 1.25 }}>
                          Profile đã được Admin duyệt vào thư viện{p.reviewReason ? ` (${p.reviewReason})` : ''}. Lưu ý: máy sử dụng profile này vẫn cần được Admin duyệt riêng trong hồ sơ máy trước khi báo giá/gán việc.
                        </Alert>
                      )}
                    </Box>
                  ))}

                  {myProposals.data && (
                    <ServerPaginationBar
                      totalCount={myProposals.data.totalCount}
                      page={myProposals.data.page}
                      pageSize={myProposals.data.pageSize}
                      totalPages={myProposals.data.totalPages}
                      pageSizeOptions={[5, 10, 20]}
                      itemLabel="yêu cầu profile"
                      onPageChange={(nextPage) => setProposalFilters((prev) => ({ ...prev, page: nextPage }))}
                      onPageSizeChange={(nextSize) =>
                        setProposalFilters((prev) => ({ ...prev, pageSize: nextSize, page: 1 }))
                      }
                    />
                  )}
                </Stack>
              )}
            </Paper>
          </>
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
            {editingMachine && (
              <Alert severity="info">
                Lưu thông số máy sẽ đối chiếu lại profile và đặt máy về trạng thái chờ Admin duyệt lại.
              </Alert>
            )}

            {machineForm.technology === 0 && (
              <MachineFormSection
                number={1}
                title="Chọn nguồn profile CuraEngine"
                description="Nếu hệ thống đã có profile phù hợp, chọn profile để điền nhanh thông số và đưa máy chờ duyệt. Nếu chưa có profile phù hợp, chuyển sang luồng đề nghị profile mới."
              >
                <Stack spacing={2}>
                  <ToggleButtonGroup
                    exclusive
                    fullWidth
                    size="small"
                    value={profileMode}
                    onChange={(_, next) => {
                      if (!next) return
                      setProfileMode(next)
                      if (next === 'request_new') {
                        setSelectedApprovedProfileId('')
                        setMachineForm((f) => ({
                          ...f,
                          printerConfiguration: {
                            ...f.printerConfiguration,
                            curaDefinitionId: '',
                            curaExtruderDefinitionId: '',
                            curaVariantId: '',
                            curaQualityId: '',
                          },
                        }))
                      }
                    }}
                  >
                    <ToggleButton value="existing">Có profile phù hợp (chọn &amp; gửi máy chờ duyệt)</ToggleButton>
                    <ToggleButton value="request_new">Chưa có profile (nhập thông số &amp; đề nghị profile mới)</ToggleButton>
                  </ToggleButtonGroup>

                  {profileMode === 'existing' ? (
                    <Stack spacing={1.5}>
                      <ApprovedProfilePresetField
                        profileId={selectedApprovedProfileId}
                        onSelect={applyApprovedProfile}
                        onClear={() => setSelectedApprovedProfileId('')}
                      />
                      <ProfilePresetField
                        definitionId={machineForm.printerConfiguration.curaDefinitionId}
                        onClear={() =>
                          setMachineForm((f) => ({
                            ...f,
                            printerConfiguration: {
                              ...f.printerConfiguration,
                              curaDefinitionId: '',
                              curaExtruderDefinitionId: '',
                              curaVariantId: '',
                              curaQualityId: '',
                            },
                          }))
                        }
                        onSelect={(p) => {
                          setSelectedApprovedProfileId('')
                          setMachineForm((f) => ({
                            ...f,
                            model: p.name,
                            technology: 0,
                            buildWidthMm: p.widthMm,
                            buildDepthMm: p.depthMm,
                            buildHeightMm: p.heightMm,
                            printerConfiguration: {
                              ...f.printerConfiguration,
                              manufacturer: p.manufacturer,
                              extruderCount: p.extruderCount,
                              maximumColorSlots: Math.max(p.extruderCount, f.printerConfiguration.maximumColorSlots),
                              filamentDiameterMm: p.filamentDiameterMm,
                              heatedBed: p.heatedBed,
                              maxBedTemperatureC: p.heatedBed ? f.printerConfiguration.maxBedTemperatureC || 100 : 0,
                              curaDefinitionId: p.id,
                              curaExtruderDefinitionId: p.extruderDefinitionId,
                              curaVariantId: '',
                              curaQualityId: '',
                            },
                          }))
                        }}
                      />
                    </Stack>
                  ) : (
                    <Alert severity="info">
                      Hãy điền đầy đủ thông số phần cứng &amp; vật liệu bên dưới, sau đó tải hoặc tạo thông tin profile ở Mục 4 để gửi Admin duyệt.
                    </Alert>
                  )}
                </Stack>
              </MachineFormSection>
            )}

            <MachineFormSection
              number={2}
              title="Thông tin máy &amp; vùng in"
              description="Khai báo tên gọi nội bộ của máy, model, công nghệ, vật liệu và kích thước khổ in thực tế."
            >
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Tên máy (mã nhận diện của xưởng)"
                    value={machineForm.name ?? ''}
                    onChange={(e) => setMachineForm((f) => ({ ...f, name: e.target.value }))}
                    required
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Model máy"
                    value={machineForm.model ?? ''}
                    onChange={(e) => setMachineForm((f) => ({ ...f, model: e.target.value }))}
                    required
                    disabled={Boolean(machineForm.printerConfiguration.curaDefinitionId)}
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
                      disabled={Boolean(machineForm.printerConfiguration.curaDefinitionId)}
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
                    onChange={(e) => {
                      const val = e.target.value
                      setMachineForm((f) => ({ ...f, materials: val }))
                      const first = val.split(',')[0]?.trim().toUpperCase()
                      if (first) setInlineProposal((p) => ({ ...p, materialCode: first }))
                    }}
                    required
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Khổ in X (mm)"
                    type="number"
                    value={machineForm.buildWidthMm}
                    disabled={Boolean(machineForm.printerConfiguration.curaDefinitionId)}
                    onChange={(e) => setMachineForm((f) => ({ ...f, buildWidthMm: Number(e.target.value) }))}
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Khổ in Y (mm)"
                    type="number"
                    value={machineForm.buildDepthMm}
                    disabled={Boolean(machineForm.printerConfiguration.curaDefinitionId)}
                    onChange={(e) => setMachineForm((f) => ({ ...f, buildDepthMm: Number(e.target.value) }))}
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 4 }}>
                  <TextField
                    label="Khổ in Z (mm)"
                    type="number"
                    value={machineForm.buildHeightMm}
                    disabled={Boolean(machineForm.printerConfiguration.curaDefinitionId)}
                    onChange={(e) => setMachineForm((f) => ({ ...f, buildHeightMm: Number(e.target.value) }))}
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Layer height nhỏ nhất (mm)"
                    type="number"
                    value={machineForm.minLayerHeightMm}
                    slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
                    onChange={(e) => setMachineForm((f) => ({ ...f, minLayerHeightMm: Number(e.target.value) }))}
                    fullWidth
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <TextField
                    label="Dung sai đạt được (mm)"
                    type="number"
                    value={machineForm.achievableToleranceMm}
                    slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
                    onChange={(e) => setMachineForm((f) => ({ ...f, achievableToleranceMm: Number(e.target.value) }))}
                    fullWidth
                  />
                </Grid>
              </Grid>
            </MachineFormSection>

            <MachineFormSection
              number={3}
              title="Cấu hình đầu in, nhiệt độ &amp; đơn giá"
              description="Các thông số phần cứng phải khớp với profile CuraEngine; đơn giá chạy máy dùng để tính báo giá."
            >
              <Grid container spacing={2}>
                <PrinterConfigurationFields
                  value={machineForm.printerConfiguration}
                  onChange={(printerConfiguration) => setMachineForm((f) => ({ ...f, printerConfiguration }))}
                />
              </Grid>
            </MachineFormSection>

            {machineForm.technology === 0 && profileMode === 'request_new' && (
              <MachineFormSection
                number={4}
                title="Thông tin đề nghị profile mới"
                description="Điền thông tin profile và tải file resolved JSON (gồm global và extruder.0..N) để gửi Admin xem xét duyệt vào thư viện."
              >
                <Stack spacing={1.5}>
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={includeProfileProposal}
                        onChange={(_, checked) => setIncludeProfileProposal(checked)}
                      />
                    }
                    label="Gửi kèm yêu cầu đề nghị profile mới ngay khi lưu máy"
                  />
                  {includeProfileProposal && (
                    <Grid container spacing={1.5}>
                      <Grid size={{ xs: 12, sm: 5 }}>
                        <TextField
                          fullWidth
                          required
                          label="Tên profile đề nghị"
                          value={inlineProposal.name}
                          onChange={(e) => setInlineProposal((p) => ({ ...p, name: e.target.value }))}
                        />
                      </Grid>
                      <Grid size={{ xs: 6, sm: 3 }}>
                        <TextField
                          fullWidth
                          required
                          label="Phiên bản"
                          value={inlineProposal.version}
                          onChange={(e) => setInlineProposal((p) => ({ ...p, version: e.target.value }))}
                        />
                      </Grid>
                      <Grid size={{ xs: 6, sm: 4 }}>
                        <TextField
                          fullWidth
                          required
                          label="Mã vật liệu profile"
                          value={inlineProposal.materialCode}
                          onChange={(e) => setInlineProposal((p) => ({ ...p, materialCode: e.target.value }))}
                        />
                      </Grid>
                      <Grid size={{ xs: 12 }}>
                        <TextField
                          fullWidth
                          label="Ghi chú gửi Admin (nguồn profile, tốc độ, lưu ý phần cứng)"
                          value={inlineProposal.submissionNotes}
                          onChange={(e) => setInlineProposal((p) => ({ ...p, submissionNotes: e.target.value }))}
                        />
                      </Grid>
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Button component="label" variant="outlined" fullWidth startIcon={<UploadFileRounded />}>
                          {inlineProposal.sourceFileName || 'Chọn file resolved JSON (.json)'}
                          <input
                            hidden
                            type="file"
                            accept=".json,application/json"
                            onChange={(e) => {
                              void readJsonFile(
                                e.target.files?.[0],
                                (fileName, jsonText) => {
                                  setInlineProposal((p) => ({
                                    ...p,
                                    name: p.name || fileName.replace(/\.resolved\.json$|\.json$/i, ''),
                                    sourceFileName: fileName,
                                    settingsJson: jsonText,
                                  }))
                                  setFormError(null)
                                },
                                (msg) => setFormError(msg),
                              )
                              e.target.value = ''
                            }}
                          />
                        </Button>
                      </Grid>
                      <Grid size={{ xs: 12, sm: 6 }}>
                        <Button
                          variant="outlined"
                          color="secondary"
                          fullWidth
                          startIcon={<AutoFixHighRounded />}
                          onClick={fillInlineSampleJson}
                        >
                          Tạo khung JSON mẫu từ thông số máy
                        </Button>
                      </Grid>
                      <Grid size={{ xs: 12 }}>
                        <TextField
                          fullWidth
                          required
                          multiline
                          minRows={5}
                          maxRows={12}
                          label="Nội dung profile resolved JSON"
                          value={inlineProposal.settingsJson}
                          onChange={(e) => setInlineProposal((p) => ({ ...p, settingsJson: e.target.value }))}
                        />
                      </Grid>
                    </Grid>
                  )}
                </Stack>
              </MachineFormSection>
            )}
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
              submitProposal.isPending ||
              !machineForm.name?.trim() ||
              !machineForm.model?.trim() ||
              !machineForm.printerConfiguration.manufacturer?.trim() ||
              !machineForm.materials?.trim()
            }
          >
            {addMachine.isPending || updateMachine.isPending || submitProposal.isPending
              ? 'Đang xử lý…'
              : editingMachine
                ? 'Lưu thay đổi'
                : profileMode === 'request_new' && includeProfileProposal
                  ? 'Lưu máy & gửi yêu cầu profile'
                  : 'Thêm máy & gửi chờ duyệt'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Standalone / Resubmit Profile Proposal Dialog ── */}
      <Dialog
        open={proposalDialogOpen}
        onClose={() => {
          setProposalDialogOpen(false)
          setEditingProposalId(null)
        }}
        fullWidth
        maxWidth="md"
        PaperProps={{
          sx: { bgcolor: 'background.paper', backgroundImage: 'none', border: '1px solid #E8EBF2' },
        }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>
          {editingProposalId ? 'Chỉnh sửa & gửi lại yêu cầu profile' : 'Đề nghị profile CuraEngine mới'}
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {proposalError && <Alert severity="error">{proposalError}</Alert>}
            {editingProposalId && proposalDetail.isLoading && <CircularProgress size={24} />}
            {editingProposalId && proposalDetail.data?.reviewReason && (
              <Alert severity="warning">
                <b>Lý do Admin từ chối trước đó:</b> {proposalDetail.data.reviewReason}
              </Alert>
            )}
            <Grid container spacing={1.5}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  fullWidth
                  required
                  label="Tên profile"
                  value={proposalForm.name}
                  onChange={(e) => setProposalForm((f) => ({ ...f, name: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <FormControl fullWidth>
                  <InputLabel id="proposal-machine-select">Liên kết máy trong xưởng (tùy chọn)</InputLabel>
                  <Select
                    labelId="proposal-machine-select"
                    label="Liên kết máy trong xưởng (tùy chọn)"
                    value={proposalForm.targetMachineId}
                    onChange={(e) => {
                      const machineId = e.target.value
                      const selectedMachine = myLab?.machines.find((x) => x.id === machineId)
                      setProposalForm((f) => ({
                        ...f,
                        targetMachineId: machineId,
                        manufacturer: selectedMachine?.printerConfiguration?.manufacturer ?? f.manufacturer,
                        machineModel: selectedMachine?.model ?? f.machineModel,
                        extruderCount: selectedMachine?.printerConfiguration?.extruderCount ?? f.extruderCount,
                      }))
                    }}
                  >
                    <MenuItem value="">Không gắn máy cụ thể</MenuItem>
                    {(myLab?.machines ?? []).map((m) => (
                      <MenuItem key={m.id} value={m.id}>
                        {m.name} ({m.model})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  fullWidth
                  required
                  label="Hãng sản xuất"
                  value={proposalForm.manufacturer}
                  onChange={(e) => setProposalForm((f) => ({ ...f, manufacturer: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  fullWidth
                  required
                  label="Model máy"
                  value={proposalForm.machineModel}
                  onChange={(e) => setProposalForm((f) => ({ ...f, machineModel: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 6, sm: 2 }}>
                <TextField
                  fullWidth
                  required
                  label="Vật liệu"
                  value={proposalForm.materialCode}
                  onChange={(e) => setProposalForm((f) => ({ ...f, materialCode: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 6, sm: 2 }}>
                <TextField
                  fullWidth
                  required
                  label="Phiên bản"
                  value={proposalForm.version}
                  onChange={(e) => setProposalForm((f) => ({ ...f, version: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField
                  fullWidth
                  required
                  type="number"
                  label="Số đầu đùn vật lý"
                  value={proposalForm.extruderCount}
                  slotProps={{ htmlInput: { min: 1, max: 8 } }}
                  onChange={(e) => setProposalForm((f) => ({ ...f, extruderCount: Number(e.target.value) }))}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 8 }}>
                <TextField
                  fullWidth
                  label="Ghi chú gửi Admin"
                  value={proposalForm.submissionNotes}
                  onChange={(e) => setProposalForm((f) => ({ ...f, submissionNotes: e.target.value }))}
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Button component="label" variant="outlined" fullWidth startIcon={<UploadFileRounded />}>
                  {proposalForm.sourceFileName || 'Chọn file resolved JSON (.json)'}
                  <input
                    hidden
                    type="file"
                    accept=".json,application/json"
                    onChange={(e) => {
                      void readJsonFile(
                        e.target.files?.[0],
                        (fileName, jsonText) => {
                          setProposalForm((f) => ({
                            ...f,
                            name: f.name || fileName.replace(/\.resolved\.json$|\.json$/i, ''),
                            sourceFileName: fileName,
                            settingsJson: jsonText,
                          }))
                          setProposalError(null)
                        },
                        (msg) => setProposalError(msg),
                      )
                      e.target.value = ''
                    }}
                  />
                </Button>
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <Button
                  variant="outlined"
                  color="secondary"
                  fullWidth
                  startIcon={<AutoFixHighRounded />}
                  onClick={() => {
                    const targetMachine = myLab?.machines.find((m) => m.id === proposalForm.targetMachineId)
                    const json = buildSampleResolvedCuraProfileJson({
                      machineName: `${proposalForm.manufacturer} ${proposalForm.machineModel}`.trim(),
                      widthMm: targetMachine?.buildWidthMm ?? 220,
                      depthMm: targetMachine?.buildDepthMm ?? 220,
                      heightMm: targetMachine?.buildHeightMm ?? 250,
                      nozzleDiameterMm: targetMachine?.printerConfiguration?.nozzleDiameterMm ?? 0.4,
                      filamentDiameterMm: targetMachine?.printerConfiguration?.filamentDiameterMm ?? 1.75,
                      heatedBed: targetMachine?.printerConfiguration?.heatedBed ?? true,
                      extruderCount: Number(proposalForm.extruderCount) || 1,
                      maximumColorSlots:
                        targetMachine?.printerConfiguration?.maximumColorSlots ??
                        (Number(proposalForm.extruderCount) || 1),
                      materialCode: proposalForm.materialCode || 'PLA',
                    })
                    setProposalForm((f) => ({
                      ...f,
                      settingsJson: json,
                      sourceFileName:
                        f.sourceFileName ||
                        `${(f.machineModel || 'printer').toLowerCase().replace(/[^a-z0-9]+/g, '-')}.resolved.json`,
                    }))
                  }}
                >
                  Tạo khung JSON mẫu
                </Button>
              </Grid>
              <Grid size={{ xs: 12 }}>
                <TextField
                  fullWidth
                  required
                  multiline
                  minRows={7}
                  maxRows={14}
                  label="Nội dung profile resolved JSON"
                  value={proposalForm.settingsJson}
                  onChange={(e) => setProposalForm((f) => ({ ...f, settingsJson: e.target.value }))}
                />
              </Grid>
            </Grid>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5 }}>
          <Button
            onClick={() => {
              setProposalDialogOpen(false)
              setEditingProposalId(null)
            }}
            color="inherit"
          >
            Huỷ
          </Button>
          <Button
            variant="contained"
            disabled={
              submitProposal.isPending ||
              resubmitProposal.isPending ||
              !proposalForm.name.trim() ||
              !proposalForm.manufacturer.trim() ||
              !proposalForm.machineModel.trim() ||
              !proposalForm.materialCode.trim() ||
              !proposalForm.version.trim() ||
              !proposalForm.settingsJson.trim()
            }
            onClick={() => void handleSaveProposalDialog()}
          >
            {submitProposal.isPending || resubmitProposal.isPending
              ? 'Đang gửi…'
              : editingProposalId
                ? 'Gửi lại cho Admin duyệt'
                : 'Gửi yêu cầu profile'}
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
