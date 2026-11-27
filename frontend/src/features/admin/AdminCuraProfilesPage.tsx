import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  FormControl,
  FormControlLabel,
  Grid,
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Switch,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { AddRounded, CheckCircleRounded, PrecisionManufacturingRounded, SearchRounded } from '@mui/icons-material'
import { apiClient } from '../../shared/api/apiClient'
import { getApiErrorMessage } from '../../shared/api/apiError'
import { ServerPaginationBar } from '../../shared/components/ServerPaginationBar'
import {
  useCuraProfileProposalDetail,
  useCuraProfilesPaged,
  useImportCuraProfile,
  useMatchMachineProfiles,
  useReviewCuraProfileProposal,
  useReviewMachineProfile,
  useSetCuraProfileActive,
} from '../lab/curaProfileApi'
import { useLabOptions, useMachinesPaged } from '../ops/useLabs'
import {
  MACHINE_STATUS_LABELS,
  PRINT_TECHNOLOGY_LABELS,
  PROFILE_APPROVAL_STATUS_LABELS,
  PROFILE_STATUS_LABELS,
  type PagedMachinesFilter,
  type PagedProfilesFilter,
} from '../ops/labTypes'

const emptyImport = {
  name: '',
  manufacturer: '',
  machineModel: '',
  materialCode: 'PLA',
  version: '1',
  engineVersion: '5.11.0',
  extruderCount: 1,
  settingsJson: '',
}

export default function AdminCuraProfilesPage() {
  const [tab, setTab] = useState(0)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const client = useQueryClient()
  const models = useQuery({ queryKey: ['admin-machine-models'], queryFn: () => apiClient.get<{
    id: string; name: string; allowsCustomerPinning: boolean
  }[]>('/cura-profiles/models').then(r => r.data) })
  const pinning = useMutation({ mutationFn: ({ id, allowed }: { id: string; allowed: boolean }) =>
    apiClient.put(`/cura-profiles/models/${id}/pinning`, { allowed }),
    onSuccess: () => { void client.invalidateQueries({ queryKey: ['admin-machine-models'] }); void client.invalidateQueries({ queryKey: ['eligible-models'] }) },
    onError: e => setError(getApiErrorMessage(e, 'Không cập nhật được quyền chỉ định model.')),
  })
  const [importOpen, setImportOpen] = useState(false)
  const [form, setForm] = useState(emptyImport)

  // Proposal inspection & rejection reasons
  const [previewId, setPreviewId] = useState<string | null>(null)
  const [proposalReasons, setProposalReasons] = useState<Record<string, string>>({})
  const [machineReasons, setMachineReasons] = useState<Record<string, string>>({})

  const previewDetail = useCuraProfileProposalDetail(previewId)
  const labOptions = useLabOptions(true)

  // Tab 0: Lab Proposals filters
  const [propSearchInput, setPropSearchInput] = useState('')
  const [propFilters, setPropFilters] = useState<PagedProfilesFilter>({
    scope: 'proposals',
    search: '',
    labId: '',
    approvalStatus: '',
    manufacturerOrModel: '',
    material: '',
    page: 1,
    pageSize: 10,
  })
  useEffect(() => {
    const t = setTimeout(() => {
      setPropFilters((prev) => (prev.search === propSearchInput ? prev : { ...prev, search: propSearchInput, page: 1 }))
    }, 300)
    return () => clearTimeout(t)
  }, [propSearchInput])
  const proposals = useCuraProfilesPaged(propFilters, tab === 0)

  // Tab 1: Profile Library filters
  const [libSearchInput, setLibSearchInput] = useState('')
  const [libFilters, setLibFilters] = useState<PagedProfilesFilter>({
    scope: 'library',
    search: '',
    isActive: '',
    manufacturerOrModel: '',
    material: '',
    page: 1,
    pageSize: 10,
  })
  useEffect(() => {
    const t = setTimeout(() => {
      setLibFilters((prev) => (prev.search === libSearchInput ? prev : { ...prev, search: libSearchInput, page: 1 }))
    }, 300)
    return () => clearTimeout(t)
  }, [libSearchInput])
  const library = useCuraProfilesPaged(libFilters, tab === 1)

  // Tab 2: Machines Approval filters
  const [machSearchInput, setMachSearchInput] = useState('')
  const [machFilters, setMachFilters] = useState<PagedMachinesFilter>({
    search: '',
    labId: '',
    profileStatus: '',
    operationalStatus: '',
    manufacturerOrModel: '',
    material: '',
    sortBy: 'pendingFirst',
    page: 1,
    pageSize: 10,
  })
  useEffect(() => {
    const t = setTimeout(() => {
      setMachFilters((prev) => (prev.search === machSearchInput ? prev : { ...prev, search: machSearchInput, page: 1 }))
    }, 300)
    return () => clearTimeout(t)
  }, [machSearchInput])
  const machines = useMachinesPaged(machFilters, tab === 2)

  // Mutations
  const importProfile = useImportCuraProfile()
  const reviewProposal = useReviewCuraProfileProposal()
  const setProfileActive = useSetCuraProfileActive()
  const matchMachine = useMatchMachineProfiles()
  const reviewMachine = useReviewMachineProfile()

  async function readFile(file?: File) {
    if (!file) return
    if (file.size > 2 * 1024 * 1024) {
      setError('File profile tối đa 2 MB.')
      return
    }
    try {
      const json = await file.text()
      JSON.parse(json)
      setForm((f) => ({ ...f, settingsJson: json }))
      setError('')
    } catch {
      setError('File JSON không hợp lệ.')
    }
  }

  async function copyProfile(id: string) {
    try {
      const { data } = await apiClient.get<{
        name: string
        manufacturer: string
        machineModel: string
        materialCode: string
        version: string
        engineVersion: string
        extruderCount: number
        settingsJson: string
      }>(`/cura-profiles/${id}`)
      setForm({
        name: data.name,
        manufacturer: data.manufacturer,
        machineModel: data.machineModel,
        materialCode: data.materialCode,
        version: `${data.version}-new`,
        engineVersion: data.engineVersion,
        extruderCount: data.extruderCount,
        settingsJson: data.settingsJson,
      })
      setImportOpen(true)
      setMessage('Đã mở cấu hình profile. Bạn có thể chỉnh sửa và lưu thành phiên bản mới.')
      setError('')
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không tải được profile.'))
    }
  }

  async function handleImport() {
    setError('')
    setMessage('')
    try {
      await importProfile.mutateAsync(form)
      setForm(emptyImport)
      setImportOpen(false)
      setMessage('Đã lưu profile vào thư viện và đối chiếu lại cho các máy chưa duyệt.')
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không nhập được profile.'))
    }
  }

  async function handleProposalReview(id: string, approve: boolean) {
    setError('')
    setMessage('')
    try {
      const result = await reviewProposal.mutateAsync({
        id,
        approve,
        reason: proposalReasons[id]?.trim() || null,
      })
      setMessage(
        approve
          ? `Đã duyệt profile "${result.name}". Các máy khớp cấu hình đã được đối chiếu sang trạng thái Chờ duyệt máy (vẫn cần duyệt máy riêng ở tab "Duyệt máy sử dụng profile").`
          : `Đã từ chối yêu cầu profile "${result.name}" kèm lý do để xưởng chỉnh sửa và gửi lại.`,
      )
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không thể xử lý yêu cầu profile.'))
    }
  }

  async function handleMachineMatch(id: string) {
    setError('')
    setMessage('')
    try {
      const updated = await matchMachine.mutateAsync(id)
      setMessage(`Đã đối chiếu lại profile cho máy "${updated.name}": ${PROFILE_STATUS_LABELS[updated.profileStatus] ?? updated.profileStatus}.`)
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không tìm lại được profile cho máy.'))
    }
  }

  async function handleMachineReview(id: string, approve: boolean) {
    setError('')
    setMessage('')
    try {
      const updated = await reviewMachine.mutateAsync({
        machineId: id,
        approve,
        reason: machineReasons[id]?.trim() || null,
      })
      setMessage(
        approve
          ? `Đã duyệt máy "${updated.name}" sử dụng profile — máy đủ điều kiện tham gia báo giá & gán việc (khi xưởng hoạt động và máy Sẵn sàng).`
          : `Đã từ chối / thu hồi quyền sử dụng profile của máy "${updated.name}".`,
      )
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không cập nhật được trạng thái duyệt máy.'))
    }
  }

  return (
    <Box className="admin-page">
      <Stack className="admin-page-body" spacing={2.5}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          spacing={2}
        >
          <Box>
            <Typography variant="h1">Profile CuraEngine &amp; Duyệt máy</Typography>
            <Typography color="text.secondary">
              Phân tách rõ quy trình duyệt profile từ xưởng, quản lý thư viện profile và duyệt máy sử dụng profile trước khi báo giá/gán việc.
            </Typography>
          </Box>
          <Button
            variant="outlined"
            startIcon={<AddRounded />}
            onClick={() => {
              setTab(1)
              setImportOpen((open) => !open)
            }}
          >
            {importOpen ? 'Đóng form nhập profile' : 'Nhập profile mới'}
          </Button>
        </Stack>

        {error && (
          <Alert severity="error" onClose={() => setError('')}>
            {error}
          </Alert>
        )}
        {message && (
          <Alert severity="success" onClose={() => setMessage('')}>
            {message}
          </Alert>
        )}

        <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 }, borderRadius: 3, overflow: 'hidden', boxShadow: '0 12px 32px rgba(35, 29, 85, 0.05)' }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2} sx={{ mb: 2.5 }}>
            <Stack direction="row" spacing={1.5} alignItems="flex-start">
              <Box sx={{ width: 42, height: 42, borderRadius: 2.5, display: 'grid', placeItems: 'center', bgcolor: '#F0ECFF', color: 'primary.main', flexShrink: 0 }}>
                <PrecisionManufacturingRounded />
              </Box>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 800 }}>Model khách được phép chỉ định</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 0.35, maxWidth: 760 }}>
                  Chọn các model khách hàng có thể yêu cầu khi đặt in. Model vẫn phải đáp ứng đủ năng lực máy, số xưởng và yêu cầu sản phẩm.
                </Typography>
              </Box>
            </Stack>
            {!models.isLoading && !models.isError && (
              <Chip icon={<CheckCircleRounded />} label={`${models.data?.filter((model) => model.allowsCustomerPinning).length ?? 0}/${models.data?.length ?? 0} đang cho phép`} color="primary" variant="outlined" sx={{ alignSelf: { xs: 'flex-start', sm: 'center' }, fontWeight: 700, bgcolor: '#FAF9FF' }} />
            )}
          </Stack>
          {models.isLoading && <Box sx={{ py: 3, display: 'grid', placeItems: 'center' }}><CircularProgress size={28} /></Box>}
          {models.isError && <Alert severity="error" action={<Button onClick={() => void models.refetch()}>Thử lại</Button>}>Không tải được danh sách model.</Alert>}
          {!!models.data?.length && (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))', xl: 'repeat(3, minmax(0, 1fr))' }, gap: 1.25 }}>
              {models.data.map((model) => (
                <Box key={model.id} sx={{ px: 1.75, py: 1.25, border: '1px solid', borderColor: model.allowsCustomerPinning ? 'primary.light' : 'divider', borderRadius: 2.25, bgcolor: model.allowsCustomerPinning ? '#FAF9FF' : '#fff', transition: 'border-color 160ms ease, background-color 160ms ease, box-shadow 160ms ease', '&:hover': { borderColor: 'primary.main', boxShadow: '0 6px 18px rgba(111, 85, 255, 0.08)' } }}>
                  <FormControlLabel
                    sx={{ m: 0, width: '100%', justifyContent: 'space-between', gap: 1, '& .MuiFormControlLabel-label': { minWidth: 0 } }}
                    labelPlacement="start"
                    label={<Box sx={{ minWidth: 0 }}><Typography sx={{ fontWeight: 700, fontSize: '0.9rem', overflow: 'hidden', textOverflow: 'ellipsis' }}>{model.name}</Typography><Typography variant="caption" color={model.allowsCustomerPinning ? 'primary.main' : 'text.secondary'}>{model.allowsCustomerPinning ? 'Khách hàng có thể chỉ định' : 'Chỉ phân bổ tự động'}</Typography></Box>}
                    control={<Switch size="small" checked={model.allowsCustomerPinning} disabled={pinning.isPending} onChange={(_, allowed) => pinning.mutate({ id: model.id, allowed })} />}
                  />
                </Box>
              ))}
            </Box>
          )}
          {!models.isLoading && !models.isError && !models.data?.length && <Box sx={{ py: 4, textAlign: 'center', color: 'text.secondary' }}>Chưa có model máy nào trong hệ thống.</Box>}
        </Paper>

        <Paper variant="outlined" sx={{ borderRadius: 2.5, overflow: 'hidden' }}>
          <Tabs
            value={tab}
            onChange={(_, next) => setTab(next)}
            variant="scrollable"
            scrollButtons="auto"
            sx={{ borderBottom: 1, borderColor: 'divider', px: 1 }}
          >
            <Tab label="Yêu cầu profile từ lab" />
            <Tab label="Thư viện profile" />
            <Tab label="Duyệt máy sử dụng profile" />
          </Tabs>
        </Paper>

        {/* ── TAB 0: LAB PROFILE PROPOSALS ── */}
        {tab === 0 && (
          <Stack spacing={2}>
            <Alert severity="info">
              Duyệt profile tại đây chỉ xác nhận file profile hợp lệ để đưa vào thư viện và ghép tự động cho máy khớp cấu hình. Để máy được tham gia báo giá/gán việc, Admin cần duyệt riêng máy ở tab <b>Duyệt máy sử dụng profile</b>.
            </Alert>

            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2.5 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
                <TextField
                  size="small"
                  placeholder="Tìm tên profile, xưởng, ghi chú, tên file…"
                  value={propSearchInput}
                  onChange={(e) => setPropSearchInput(e.target.value)}
                  sx={{ minWidth: 230, flex: 1 }}
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
                <FormControl size="small" sx={{ minWidth: 190 }}>
                  <InputLabel id="prop-lab-filter">Xưởng gửi</InputLabel>
                  <Select
                    labelId="prop-lab-filter"
                    label="Xưởng gửi"
                    value={propFilters.labId ?? ''}
                    onChange={(e) => setPropFilters((p) => ({ ...p, labId: e.target.value, page: 1 }))}
                  >
                    <MenuItem value="">Tất cả xưởng</MenuItem>
                    {(labOptions.data ?? []).map((lab) => (
                      <MenuItem key={lab.id} value={lab.id}>
                        {lab.name} ({lab.city})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 195 }}>
                  <InputLabel id="prop-status-filter">Trạng thái duyệt</InputLabel>
                  <Select
                    labelId="prop-status-filter"
                    label="Trạng thái duyệt"
                    value={propFilters.approvalStatus ?? ''}
                    onChange={(e) => setPropFilters((p) => ({ ...p, approvalStatus: e.target.value, page: 1 }))}
                  >
                    <MenuItem value="">Tất cả</MenuItem>
                    <MenuItem value="PendingReview">Chờ Admin duyệt</MenuItem>
                    <MenuItem value="Approved">Đã duyệt</MenuItem>
                    <MenuItem value="Rejected">Bị từ chối</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  size="small"
                  label="Hãng / Model"
                  value={propFilters.manufacturerOrModel ?? ''}
                  onChange={(e) =>
                    setPropFilters((p) => ({ ...p, manufacturerOrModel: e.target.value, page: 1 }))
                  }
                  sx={{ width: { xs: '100%', sm: 160 } }}
                />
                <TextField
                  size="small"
                  label="Vật liệu"
                  value={propFilters.material ?? ''}
                  onChange={(e) => setPropFilters((p) => ({ ...p, material: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 120 } }}
                />
                <Button
                  color="inherit"
                  onClick={() => {
                    setPropSearchInput('')
                    setPropFilters((p) => ({
                      ...p,
                      search: '',
                      labId: '',
                      approvalStatus: '',
                      manufacturerOrModel: '',
                      material: '',
                      page: 1,
                    }))
                  }}
                >
                  Xóa lọc
                </Button>
              </Stack>
            </Paper>

            {proposals.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
            {proposals.isError && <Alert severity="error">Không tải được danh sách yêu cầu profile.</Alert>}
            {!proposals.isLoading && !proposals.isError && (proposals.data?.items.length ?? 0) === 0 && (
              <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', borderStyle: 'dashed' }}>
                <Typography color="text.secondary">Không có yêu cầu profile nào khớp bộ lọc.</Typography>
              </Paper>
            )}

            <Stack spacing={2}>
              {proposals.data?.items.map((p) => (
                <Paper key={p.id} variant="outlined" sx={{ p: 2.5, borderRadius: 2.5 }}>
                  <Stack spacing={1.25}>
                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                      <Typography fontWeight={750}>
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
                      {p.submittedByLabName && (
                        <Chip size="small" variant="outlined" label={`Xưởng: ${p.submittedByLabName}`} />
                      )}
                      {p.targetMachineName && (
                        <Chip size="small" variant="outlined" color="primary" label={`Máy gắn: ${p.targetMachineName}`} />
                      )}
                    </Stack>

                    <Typography variant="body2" color="text.secondary">
                      {p.manufacturer} · {p.machineModel} · Vật liệu <b>{p.materialCode}</b> · CuraEngine {p.engineVersion} · Khổ{' '}
                      {p.widthMm}×{p.depthMm}×{p.heightMm} mm · Đầu phun {p.nozzleDiameterMm} mm · Sợi {p.filamentDiameterMm} mm ·{' '}
                      {p.extruderCount} đầu đùn / {p.maximumColorSlots} màu · Nhiệt vật liệu {p.requiredNozzleTemperatureC}/
                      {p.requiredBedTemperatureC} °C
                    </Typography>

                    {(p.sourceFileName || p.submissionNotes) && (
                      <Typography variant="body2" color="text.secondary">
                        {p.sourceFileName ? `File nguồn: ${p.sourceFileName}` : ''}
                        {p.sourceFileName && p.submissionNotes ? ' · ' : ''}
                        {p.submissionNotes ? `Ghi chú xưởng: ${p.submissionNotes}` : ''}
                      </Typography>
                    )}

                    {p.reviewReason && (
                      <Alert severity={p.approvalStatus === 'Rejected' ? 'error' : 'info'}>
                        {p.approvalStatus === 'Rejected' ? 'Lý do từ chối: ' : 'Ghi chú duyệt: '}
                        {p.reviewReason}
                      </Alert>
                    )}

                    <Box>
                      <Button
                        size="small"
                        onClick={() => setPreviewId((curr) => (curr === p.id ? null : p.id))}
                      >
                        {previewId === p.id ? 'Ẩn nội dung resolved JSON' : 'Kiểm tra nội dung resolved JSON'}
                      </Button>
                    </Box>

                    {previewId === p.id && (
                      previewDetail.isLoading ? (
                        <CircularProgress size={20} />
                      ) : previewDetail.isError ? (
                        <Alert severity="error">Không tải được nội dung resolved JSON.</Alert>
                      ) : (
                        <TextField
                          fullWidth
                          multiline
                          minRows={5}
                          maxRows={12}
                          label="Resolved JSON"
                          value={previewDetail.data?.settingsJson ?? ''}
                          slotProps={{ input: { readOnly: true } }}
                        />
                      )
                    )}

                    {p.approvalStatus === 'PendingReview' && (
                      <>
                        <TextField
                          size="small"
                          label="Lý do từ chối (bắt buộc khi từ chối) hoặc ghi chú duyệt"
                          value={proposalReasons[p.id] ?? ''}
                          onChange={(e) => setProposalReasons((r) => ({ ...r, [p.id]: e.target.value }))}
                        />
                        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                          <Button
                            variant="contained"
                            size="small"
                            disabled={reviewProposal.isPending}
                            onClick={() => void handleProposalReview(p.id, true)}
                          >
                            Duyệt profile vào thư viện
                          </Button>
                          <Button
                            variant="outlined"
                            color="error"
                            size="small"
                            disabled={reviewProposal.isPending || !(proposalReasons[p.id] ?? '').trim()}
                            onClick={() => void handleProposalReview(p.id, false)}
                          >
                            Từ chối kèm lý do
                          </Button>
                        </Stack>
                      </>
                    )}
                  </Stack>
                </Paper>
              ))}
            </Stack>

            {proposals.data && (
              <ServerPaginationBar
                totalCount={proposals.data.totalCount}
                page={proposals.data.page}
                pageSize={proposals.data.pageSize}
                totalPages={proposals.data.totalPages}
                itemLabel="yêu cầu profile"
                onPageChange={(nextPage) => setPropFilters((p) => ({ ...p, page: nextPage }))}
                onPageSizeChange={(nextSize) => setPropFilters((p) => ({ ...p, pageSize: nextSize, page: 1 }))}
              />
            )}
          </Stack>
        )}

        {/* ── TAB 1: PROFILE LIBRARY ── */}
        {tab === 1 && (
          <Stack spacing={2}>
            <Collapse in={importOpen}>
              <Paper variant="outlined" sx={{ p: 3, borderRadius: 2.5 }}>
                <Stack spacing={2}>
                  <Typography variant="h6">Tạo / nhập profile mới vào thư viện</Typography>
                  <Typography variant="body2" color="text.secondary">
                    Mỗi profile dành cho một model máy, vật liệu và cấu hình đầu phun. Nhập resolved JSON gồm global và extruder.0, extruder.1…
                  </Typography>
                  <Grid container spacing={2}>
                    {(
                      [
                        ['name', 'Tên profile'],
                        ['manufacturer', 'Hãng sản xuất'],
                        ['machineModel', 'Model máy'],
                        ['materialCode', 'Mã vật liệu'],
                        ['version', 'Phiên bản profile'],
                        ['engineVersion', 'Phiên bản CuraEngine'],
                      ] as const
                    ).map(([key, label]) => (
                      <Grid key={key} size={{ xs: 12, sm: 6 }}>
                        <TextField
                          fullWidth
                          required
                          label={label}
                          value={form[key]}
                          onChange={(e) => setForm((f) => ({ ...f, [key]: e.target.value }))}
                        />
                      </Grid>
                    ))}
                    <Grid size={{ xs: 12, sm: 6 }}>
                      <TextField
                        fullWidth
                        required
                        type="number"
                        label="Số đầu đùn vật lý"
                        value={form.extruderCount}
                        onChange={(e) => setForm((f) => ({ ...f, extruderCount: Number(e.target.value) }))}
                      />
                    </Grid>
                  </Grid>
                  <Button component="label" variant="outlined">
                    Chọn file resolved JSON
                    <input
                      hidden
                      type="file"
                      accept=".json,application/json"
                      onChange={(e) => {
                        void readFile(e.target.files?.[0])
                        e.target.value = ''
                      }}
                    />
                  </Button>
                  <TextField
                    multiline
                    minRows={5}
                    maxRows={14}
                    label="Nội dung profile JSON"
                    value={form.settingsJson}
                    onChange={(e) => setForm((f) => ({ ...f, settingsJson: e.target.value }))}
                  />
                  <Button
                    variant="contained"
                    disabled={
                      importProfile.isPending ||
                      !form.settingsJson ||
                      !form.name ||
                      !form.manufacturer ||
                      !form.machineModel
                    }
                    onClick={() => void handleImport()}
                  >
                    {importProfile.isPending ? 'Đang nhập…' : 'Lưu vào thư viện & đối chiếu máy'}
                  </Button>
                </Stack>
              </Paper>
            </Collapse>

            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2.5 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
                <TextField
                  size="small"
                  placeholder="Tìm theo tên profile, hãng, model, phiên bản…"
                  value={libSearchInput}
                  onChange={(e) => setLibSearchInput(e.target.value)}
                  sx={{ minWidth: 230, flex: 1 }}
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
                <FormControl size="small" sx={{ minWidth: 195 }}>
                  <InputLabel id="lib-active-filter">Trạng thái hoạt động</InputLabel>
                  <Select
                    labelId="lib-active-filter"
                    label="Trạng thái hoạt động"
                    value={libFilters.isActive ?? ''}
                    onChange={(e) =>
                      setLibFilters((p) => ({ ...p, isActive: e.target.value as '' | 'true' | 'false', page: 1 }))
                    }
                  >
                    <MenuItem value="">Tất cả</MenuItem>
                    <MenuItem value="true">Đang kích hoạt</MenuItem>
                    <MenuItem value="false">Đã tắt</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  size="small"
                  label="Hãng / Model"
                  value={libFilters.manufacturerOrModel ?? ''}
                  onChange={(e) => setLibFilters((p) => ({ ...p, manufacturerOrModel: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 170 } }}
                />
                <TextField
                  size="small"
                  label="Vật liệu"
                  value={libFilters.material ?? ''}
                  onChange={(e) => setLibFilters((p) => ({ ...p, material: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 130 } }}
                />
                <Button
                  color="inherit"
                  onClick={() => {
                    setLibSearchInput('')
                    setLibFilters((p) => ({
                      ...p,
                      search: '',
                      isActive: '',
                      manufacturerOrModel: '',
                      material: '',
                      page: 1,
                    }))
                  }}
                >
                  Xóa lọc
                </Button>
              </Stack>
            </Paper>

            {library.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
            {library.isError && <Alert severity="error">Không tải được thư viện profile.</Alert>}
            {!library.isLoading && !library.isError && (library.data?.items.length ?? 0) === 0 && (
              <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', borderStyle: 'dashed' }}>
                <Typography color="text.secondary">Chưa có profile nào phù hợp với bộ lọc.</Typography>
              </Paper>
            )}

            <Stack spacing={1.5}>
              {library.data?.items.map((p) => (
                <Paper key={p.id} variant="outlined" sx={{ p: 2.25, borderRadius: 2.5 }}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    justifyContent="space-between"
                    alignItems={{ xs: 'flex-start', sm: 'center' }}
                    gap={1.5}
                  >
                    <Box>
                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                        <Typography fontWeight={750}>
                          {p.name} · v{p.version}
                        </Typography>
                        <Chip
                          size="small"
                          color={p.isActive ? 'success' : 'default'}
                          label={p.isActive ? 'Đang kích hoạt' : 'Đã tắt'}
                        />
                        {p.isVerified && <Chip size="small" color="info" variant="outlined" label="Cura Verified" />}
                        {p.submittedByLabName && (
                          <Chip size="small" variant="outlined" label={`Đề xuất từ ${p.submittedByLabName}`} />
                        )}
                      </Stack>
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                        {p.manufacturer} · {p.machineModel} · {p.materialCode} · CuraEngine {p.engineVersion} · Đầu phun{' '}
                        {p.nozzleDiameterMm} mm · {p.extruderCount} đầu đùn / {p.maximumColorSlots} màu · {p.widthMm}×{p.depthMm}×
                        {p.heightMm} mm · Nhiệt {p.requiredNozzleTemperatureC}/{p.requiredBedTemperatureC} °C
                      </Typography>
                    </Box>
                    <Stack direction="row" spacing={1} flexShrink={0}>
                      <Button size="small" variant="outlined" onClick={() => void copyProfile(p.id)}>
                        Xem / tạo bản mới
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color={p.isActive ? 'warning' : 'success'}
                        disabled={setProfileActive.isPending}
                        onClick={() => void setProfileActive.mutateAsync({ id: p.id, isActive: !p.isActive })}
                      >
                        {p.isActive ? 'Tắt' : 'Kích hoạt'}
                      </Button>
                    </Stack>
                  </Stack>
                </Paper>
              ))}
            </Stack>

            {library.data && (
              <ServerPaginationBar
                totalCount={library.data.totalCount}
                page={library.data.page}
                pageSize={library.data.pageSize}
                totalPages={library.data.totalPages}
                itemLabel="profile"
                onPageChange={(nextPage) => setLibFilters((p) => ({ ...p, page: nextPage }))}
                onPageSizeChange={(nextSize) => setLibFilters((p) => ({ ...p, pageSize: nextSize, page: 1 }))}
              />
            )}
          </Stack>
        )}

        {/* ── TAB 2: MACHINE APPROVAL ── */}
        {tab === 2 && (
          <Stack spacing={2}>
            <Alert severity="info">
              <b>Phân biệt duyệt profile và duyệt máy:</b> Máy in FDM chỉ được tham gia báo giá &amp; gán việc khi (1) có đủ profile đã duyệt cho mọi vật liệu hỗ trợ, (2) được Admin bấm <b>Duyệt máy sử dụng profile</b>, (3) xưởng chủ quản đang kích hoạt &amp; không bị đình chỉ, và (4) máy ở trạng thái Sẵn sàng/Đang in.
            </Alert>

            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2.5 }}>
              <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} flexWrap="wrap" useFlexGap>
                <TextField
                  size="small"
                  placeholder="Tìm tên máy, model, hãng, tên xưởng…"
                  value={machSearchInput}
                  onChange={(e) => setMachSearchInput(e.target.value)}
                  sx={{ minWidth: 220, flex: 1 }}
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
                <FormControl size="small" sx={{ minWidth: 185 }}>
                  <InputLabel id="mach-lab-filter">Xưởng</InputLabel>
                  <Select
                    labelId="mach-lab-filter"
                    label="Xưởng"
                    value={machFilters.labId ?? ''}
                    onChange={(e) => setMachFilters((p) => ({ ...p, labId: e.target.value, page: 1 }))}
                  >
                    <MenuItem value="">Tất cả xưởng</MenuItem>
                    {(labOptions.data ?? []).map((lab) => (
                      <MenuItem key={lab.id} value={lab.id}>
                        {lab.name} ({lab.city})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 195 }}>
                  <InputLabel id="mach-profile-status-filter">Trạng thái duyệt máy</InputLabel>
                  <Select
                    labelId="mach-profile-status-filter"
                    label="Trạng thái duyệt máy"
                    value={machFilters.profileStatus ?? ''}
                    onChange={(e) => setMachFilters((p) => ({ ...p, profileStatus: e.target.value, page: 1 }))}
                  >
                    <MenuItem value="">Tất cả</MenuItem>
                    <MenuItem value="PendingReview">Chờ duyệt máy</MenuItem>
                    <MenuItem value="MissingProfile">Chưa có đủ profile</MenuItem>
                    <MenuItem value="Approved">Đã duyệt máy</MenuItem>
                    <MenuItem value="Rejected">Bị từ chối</MenuItem>
                  </Select>
                </FormControl>
                <FormControl size="small" sx={{ minWidth: 195 }}>
                  <InputLabel id="mach-op-status-filter">Trạng thái hoạt động</InputLabel>
                  <Select
                    labelId="mach-op-status-filter"
                    label="Trạng thái hoạt động"
                    value={machFilters.operationalStatus ?? ''}
                    onChange={(e) => setMachFilters((p) => ({ ...p, operationalStatus: e.target.value, page: 1 }))}
                  >
                    <MenuItem value="">Tất cả</MenuItem>
                    <MenuItem value="Idle">Sẵn sàng</MenuItem>
                    <MenuItem value="Printing">Đang in</MenuItem>
                    <MenuItem value="Maintenance">Bảo trì</MenuItem>
                    <MenuItem value="Offline">Ngoại tuyến</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  size="small"
                  label="Hãng / Model"
                  value={machFilters.manufacturerOrModel ?? ''}
                  onChange={(e) => setMachFilters((p) => ({ ...p, manufacturerOrModel: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 155 } }}
                />
                <TextField
                  size="small"
                  label="Vật liệu"
                  value={machFilters.material ?? ''}
                  onChange={(e) => setMachFilters((p) => ({ ...p, material: e.target.value, page: 1 }))}
                  sx={{ width: { xs: '100%', sm: 120 } }}
                />
                <Button
                  color="inherit"
                  onClick={() => {
                    setMachSearchInput('')
                    setMachFilters((p) => ({
                      ...p,
                      search: '',
                      labId: '',
                      profileStatus: '',
                      operationalStatus: '',
                      manufacturerOrModel: '',
                      material: '',
                      page: 1,
                    }))
                  }}
                >
                  Xóa lọc
                </Button>
              </Stack>
            </Paper>

            {machines.isLoading && <CircularProgress sx={{ alignSelf: 'center' }} />}
            {machines.isError && <Alert severity="error">Không tải được danh sách máy in.</Alert>}
            {!machines.isLoading && !machines.isError && (machines.data?.items.length ?? 0) === 0 && (
              <Paper variant="outlined" sx={{ p: 4, textAlign: 'center', borderStyle: 'dashed' }}>
                <Typography color="text.secondary">Không có máy in nào khớp bộ lọc hiện tại.</Typography>
              </Paper>
            )}

            <Stack spacing={2}>
              {machines.data?.items.map((m) => (
                <Paper key={m.id} variant="outlined" sx={{ p: 2.5, borderRadius: 2.5 }}>
                  <Stack spacing={1.5}>
                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                      <Typography fontWeight={750}>
                        {m.labName} · {m.name}
                      </Typography>
                      <Chip
                        size="small"
                        color={
                          m.profileStatus === 'Approved'
                            ? 'success'
                            : m.profileStatus === 'Rejected'
                              ? 'error'
                              : m.profileStatus === 'PendingReview'
                                ? 'info'
                                : 'warning'
                        }
                        label={PROFILE_STATUS_LABELS[m.profileStatus] ?? m.profileStatus}
                      />
                      <Chip
                        size="small"
                        variant="outlined"
                        color={
                          m.status === 0 ? 'success' : m.status === 1 ? 'info' : m.status === 2 ? 'warning' : 'error'
                        }
                        label={MACHINE_STATUS_LABELS[m.status] ?? 'Không rõ'}
                      />
                      <Chip
                        size="small"
                        variant="outlined"
                        color={m.isEligibleForQuotingAndScheduling ? 'success' : 'default'}
                        label={
                          m.isEligibleForQuotingAndScheduling
                            ? 'Đủ điều kiện báo giá & gán việc'
                            : 'Chưa đủ điều kiện báo giá & gán việc'
                        }
                      />
                      {!m.labIsActive && <Chip size="small" color="warning" variant="outlined" label={`Xưởng: ${m.labStatus}`} />}
                    </Stack>

                    <Typography variant="body2">
                      {PRINT_TECHNOLOGY_LABELS[m.technology] ?? 'FDM'} · {m.printerConfiguration?.manufacturer ?? 'Chưa khai hãng'} ·{' '}
                      {m.model} · Khổ {m.buildWidthMm}×{m.buildDepthMm}×{m.buildHeightMm} mm · Vật liệu:{' '}
                      {m.supportedMaterials.join(', ')}
                    </Typography>

                    {m.printerConfiguration && (
                      <Typography variant="body2" color="text.secondary">
                        Đầu phun {m.printerConfiguration.nozzleDiameterMm} mm · Sợi {m.printerConfiguration.filamentDiameterMm} mm ·{' '}
                        {m.printerConfiguration.extruderCount} đầu đùn / {m.printerConfiguration.maximumColorSlots} màu · Nhiệt tối đa{' '}
                        {m.printerConfiguration.maxNozzleTemperatureC}/{m.printerConfiguration.maxBedTemperatureC} °C ·{' '}
                        {m.printerConfiguration.pricePerHour.toLocaleString('vi-VN')} đ/giờ · Lỗi{' '}
                        {Number((m.printerConfiguration.failureRate * 100).toFixed(2))}%
                      </Typography>
                    )}

                    <Typography variant="body2" color="text.secondary">
                      Profile đã gắn:{' '}
                      {m.matchedProfiles.length > 0
                        ? m.matchedProfiles.map((p) => `${p.name} (${p.materialCode} v${p.version})`).join(', ')
                        : m.curaProfileIds.length > 0
                          ? m.curaProfileIds.join(', ')
                          : 'Chưa có'}
                    </Typography>

                    {m.profileReviewReason && <Alert severity="warning">{m.profileReviewReason}</Alert>}
                    {!m.printerConfiguration && (
                      <Alert severity="info">Lab cần mở form sửa máy để bổ sung cấu hình đầu in, hãng và đơn giá.</Alert>
                    )}

                    <TextField
                      size="small"
                      label="Lý do từ chối / thu hồi duyệt máy"
                      value={machineReasons[m.id] ?? ''}
                      onChange={(e) => setMachineReasons((r) => ({ ...r, [m.id]: e.target.value }))}
                    />

                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      <Button
                        size="small"
                        variant="outlined"
                        disabled={matchMachine.isPending || !m.printerConfiguration}
                        onClick={() => void handleMachineMatch(m.id)}
                      >
                        Tìm lại profile
                      </Button>
                      <Button
                        size="small"
                        variant="contained"
                        disabled={reviewMachine.isPending || m.profileStatus !== 'PendingReview'}
                        onClick={() => void handleMachineReview(m.id, true)}
                      >
                        Duyệt máy sử dụng profile
                      </Button>
                      <Button
                        size="small"
                        color="error"
                        variant="outlined"
                        disabled={reviewMachine.isPending || !(machineReasons[m.id] ?? '').trim()}
                        onClick={() => void handleMachineReview(m.id, false)}
                      >
                        Từ chối / thu hồi duyệt
                      </Button>
                    </Stack>
                  </Stack>
                </Paper>
              ))}
            </Stack>

            {machines.data && (
              <ServerPaginationBar
                totalCount={machines.data.totalCount}
                page={machines.data.page}
                pageSize={machines.data.pageSize}
                totalPages={machines.data.totalPages}
                itemLabel="máy in"
                onPageChange={(nextPage) => setMachFilters((p) => ({ ...p, page: nextPage }))}
                onPageSizeChange={(nextSize) => setMachFilters((p) => ({ ...p, pageSize: nextSize, page: 1 }))}
              />
            )}
          </Stack>
        )}
      </Stack>
    </Box>
  )
}
