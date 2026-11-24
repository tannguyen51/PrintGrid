import { useCallback, useEffect, useEffectEvent, useRef, useState } from 'react'
import axios from 'axios'
import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  CircularProgress,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { ArrowBackRounded, ArrowDownwardRounded, ArrowForwardRounded, PriceCheckRounded } from '@mui/icons-material'
import type { Quote } from './quoteTypes'
import { INFILL_OPTIONS, MATERIALS, QUALITY_GRADES, QUOTE_STATUS_LABELS } from './quoteTypes'
import { assignedColors, type ColorGroupAssignment } from '../../shared/threeMf/colorGroups'
import { OrderColorConfigurator } from './OrderColorConfigurator'
import { useModelFileUrl } from '../models/useModelFile'
import type { ThreeDModel } from '../models/modelTypes'
import { useCreateQuote, usePlaceOrder, useQuote } from './useQuotes'
import { QuotePriceDetails } from './QuotePriceDetails'
import { QuoteHistoryPanel } from './QuoteHistoryPanel'
import { useAuth } from '../../app/AuthContext'
import type { TechConstraintsValue } from './TechConstraintsSelector'
import MachinePinningSelector from './MachinePinningSelector'
import { OrderShippingAddress } from './OrderShippingAddress'
import type { CustomerAddress } from '../account/accountTypes'

function readColorDraft(modelId: string | undefined): ColorGroupAssignment[] {
  try {
    const saved = JSON.parse(localStorage.getItem(`printgrid:order-colors:${modelId}`) ?? '[]')
    return Array.isArray(saved) ? saved.filter(a => a && typeof a.groupId === 'string' && typeof a.colorCode === 'string') : []
  } catch { return [] }
}

/**
 * G3 — Print configuration & pre-order flow.
 * Step 1: choose material/color/quality/infill/quantity → get a quote.
 * Step 2: show print pricing + hub-ready estimate, then collect the delivery address.
 * Quote approval does not establish a delivery promise to that address.
 */
export default function OrderConfigPage() {
  const { modelId } = useParams<{ modelId: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const requestedQuoteId = searchParams.get('quoteId')
  const createQuote = useCreateQuote()
  const placeOrder = usePlaceOrder()
  const quoteResult = useRef<HTMLDivElement>(null)
  const deliveryForm = useRef<HTMLDivElement>(null)
  const configurationFrame = useRef<HTMLDivElement>(null)
  const quoteColumn = useRef<HTMLDivElement>(null)
  const quoteAnchor = useRef<HTMLSpanElement>(null)
  const [quotePinned, setQuotePinned] = useState(false)

  useEffect(() => {
    const configuration = configurationFrame.current
    const column = quoteColumn.current
    const anchor = quoteAnchor.current
    if (!configuration || !column || !anchor) return

    const syncHeight = () => column.style.setProperty('--configuration-height', `${configuration.getBoundingClientRect().height}px`)
    const resize = new ResizeObserver(syncHeight)
    resize.observe(configuration)
    syncHeight()

    // Keep the initial frames equal, then fit the sticky quote inside the viewport.
    const intersection = new IntersectionObserver(([entry]) => {
      setQuotePinned(!entry.isIntersecting && entry.boundingClientRect.top < 88)
    }, { rootMargin: '-88px 0px 0px 0px', threshold: 0 })
    intersection.observe(anchor)
    return () => { resize.disconnect(); intersection.disconnect() }
  }, [])

  // Config state
  const [material, setMaterial] = useState('PLA')
  const [fallbackColors, setFallbackColors] = useState<string[]>(['BLACK'])
  const color = fallbackColors[0] ?? 'BLACK'
  const [colorDraft, setColorDraft] = useState(() => ({ modelId, assignments: readColorDraft(modelId) }))
  const colorAssignments = colorDraft.modelId === modelId ? colorDraft.assignments : readColorDraft(modelId)
  const colors = colorAssignments.length ? assignedColors(colorAssignments) : fallbackColors
  const [colorMappingValid, setColorMappingValid] = useState(false)
  const restoredColorConfig = useRef<string | null>(null)
  const modelQuery = useQuery({ queryKey: ['model', modelId], enabled: !!modelId,
    queryFn: () => apiClient.get<ThreeDModel>(`/models/${modelId}`).then(r => r.data) })
  const model = modelQuery.data
  const modelFile = useModelFileUrl(modelId ?? null, !!model?.hasFile)
  const [sizeCode, setSizeCode] = useState('ORIGINAL')
  const [polishing, setPolishing] = useState(false)
  const [layerIndex, setLayerIndex] = useState(1) // Standard
  const [infill, setInfill] = useState(30)
  const [quantity, setQuantity] = useState(1)
  const [configError, setConfigError] = useState<string | null>(null)

  // Section 3: tech constraints (doc §3, BR-03)
  const [techConstraints, setTechConstraints] = useState<TechConstraintsValue>({})
  // Section 4: machine pinning (doc §4, BR-04/BR-05)
  const [pinnedModelId, setPinnedModelId] = useState<string | null>(null)
  const [machineSelectionValid, setMachineSelectionValid] = useState(true)

  // Quote + address state
  const [quote, setQuote] = useState<Quote | null>(null)
  const changeColorAssignments = useCallback((assignments: ColorGroupAssignment[]) => {
    setColorDraft({ modelId, assignments })
    try { localStorage.setItem(`printgrid:order-colors:${modelId}`, JSON.stringify(assignments)) } catch { /* storage is optional */ }
    setQuote(null)
  }, [modelId, setQuote])
  const quoteStatus = useQuote(quote?.id ?? requestedQuoteId)
  const restoreColorAssignments = useEffectEvent((restored: Quote) => {
    const assignments = restored.items.find(i => i.modelId === modelId)?.calculation?.colorAssignments
    if (!assignments?.length) return
    restoredColorConfig.current = JSON.stringify([modelId, material, assignedColors(assignments), assignments, sizeCode, polishing,
      layerIndex, infill, quantity, techConstraints ?? {}, pinnedModelId])
    setColorDraft({ modelId, assignments })
    try { localStorage.setItem(`printgrid:order-colors:${modelId}`, JSON.stringify(assignments)) } catch { /* storage is optional */ }
  })
  useEffect(() => {
    const restored = quoteStatus.data
    if (!restored) return
    // The server is the source of truth for a quote reopened from another page.
    // oxlint-disable-next-line react/set-state-in-effect
    setQuote(restored)
    if (requestedQuoteId === restored.id) {
      restoreColorAssignments(restored)
      const next = new URLSearchParams(searchParams)
      next.delete('quoteId')
      setSearchParams(next, { replace: true })
    }
  }, [quoteStatus.data, requestedQuoteId, searchParams, setSearchParams])
  const [shippingAddress, setShippingAddress] = useState<CustomerAddress | null>(null)
  const { street = '', ward = '', district = '', city = '', postalCode = '' } = shippingAddress ?? {}
  const [requestedDeliveryDate, setRequestedDeliveryDate] = useState('')
  const addressValid = !!street.trim() && street.trim().length <= 200 && !!ward.trim() && ward.trim().length <= 100
    && !!district.trim() && district.trim().length <= 100 && !!city.trim() && city.trim().length <= 100 && postalCode.trim().length <= 20
  const today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())
  const delivery = useQuery({
    queryKey: ['delivery-preview', quote?.id, requestedDeliveryDate],
    enabled: (quote?.status === 'Ready' || quote?.status === 'Draft') && requestedDeliveryDate > today,
    queryFn: () => apiClient.post<{ earliestDeliveryDate: string; promisedDeliveryDate: string; canMeetRequestedDate: boolean }>('/orders/delivery-preview',
      { quoteId: quote!.id, requestedDeliveryDate }).then(r => r.data),
    staleTime: 0,
  })
  const [orderError, setOrderError] = useState<string | null>(null)
  const [acceptTerms, setAcceptTerms] = useState(false)
  const configKey = JSON.stringify([modelId, material, colors, colorAssignments, sizeCode, polishing, layerIndex, infill, quantity, techConstraints ?? {}, pinnedModelId])
  const currentConfig = useRef(configKey)
  // The ref guards an in-flight calculation from being applied after its form changed.
  // oxlint-disable-next-line react/refs
  currentConfig.current = configKey
  const previousConfig = useRef(configKey)

  // A result belongs to exactly one configuration. Never allow placing a stale quote.
  useEffect(() => {
    const restoring = restoredColorConfig.current === configKey
    restoredColorConfig.current = null
    if (previousConfig.current === configKey) return
    previousConfig.current = configKey
    if (restoring) return
    // A quote calculated for the previous form must never be submitted with new settings.
    // oxlint-disable-next-line react/set-state-in-effect
    setQuote(null)
    setConfigError(null)
  }, [configKey])

  useEffect(() => {
    if (quote?.id && window.matchMedia('(max-width: 1199px)').matches)
      quoteResult.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [quote?.id])

  // Live expiry countdown for the quote (BR-QUOTE-003) — refreshed every 30 s.
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 30_000)
    return () => clearInterval(t)
  }, [])

  const [resendCooldown, setResendCooldown] = useState(0)
  const { user } = useAuth()

  useEffect(() => {
    let timer: any
    if (resendCooldown > 0) {
      timer = setInterval(() => setResendCooldown(c => c - 1), 1000)
    }
    return () => clearInterval(timer)
  }, [resendCooldown])

  async function handleResendVerification() {
    if (!user) return
    setOrderError(null)
    try {
      const res = await apiClient.post<{ waitTimeSeconds?: number }>('/auth/resend-verification', { email: user.email })
      setResendCooldown(res.data.waitTimeSeconds || 60)
    } catch (err: any) {
      setOrderError(err.response?.data?.error?.message || 'Không thể gửi lại email.')
    }
  }

  const layer = QUALITY_GRADES[layerIndex]

  function serverMessage(e: unknown, fallback: string): string {
    const msg = (e as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message
    return msg || fallback
  }

  async function handleGetQuote() {
    if (!modelId || !machineSelectionValid || !colorMappingValid) return
    setConfigError(null)
    setQuote(null)
    const submittedConfig = configKey
    try {
      const saved = await createQuote.mutateAsync({
        modelId,
        materialCode: material,
        colorCode: colors[0] ?? color,
        layerHeightMm: layer.layerMm,
        infillPercent: infill,
        quantity,
        toleranceMm: 0.2,
        sizeCode, colorCodes: colors, polishing,
        colorAssignments: colorAssignments.length ? colorAssignments : undefined,
        // Section 3: hard tech constraints (BR-03)
        maxColors: techConstraints?.maxColors,
        enclosedChamber: techConstraints?.enclosedChamber,
        nozzleDiameterMm: techConstraints?.nozzleDiameterMm,
        maxToleranceMm: techConstraints.maxToleranceMm,
        minLayerHeightMm: techConstraints.minLayerHeightMm,
        // Section 4: machine pinning (BR-04/BR-05)
        bindingType: pinnedModelId ? 1 : undefined,
        pinnedModelId: pinnedModelId ? pinnedModelId : undefined,
      })
      if (currentConfig.current === submittedConfig) {
        setQuote(saved)
        setOrderError(null)
      }
    } catch (error) {
      const apiMessage = axios.isAxiosError(error)
        ? (error.response?.data as { error?: { message?: string } } | undefined)?.error?.message
        : undefined
      setConfigError(apiMessage ?? 'Không lấy được báo giá. Kiểm tra lại cấu hình.')
    }
  }

  async function handlePlaceOrder() {
    if (!quote) return
    setOrderError(null)
    try {
      await placeOrder.mutateAsync({
        quoteId: quote.id,
        requestedDeliveryDate,
        street: street.trim(),
        ward: ward.trim(),
        district: district.trim(),
        city: city.trim(),
        postalCode: postalCode.trim(),
        acceptTerms,
      })
      navigate('/orders', { replace: true })
    } catch (e) {
      setOrderError(serverMessage(e, 'Không thể đặt hàng. Vui lòng kiểm tra lại thông tin.'))
    }
  }

  // BR-QUOTE-003 live countdown instead of a hard-coded "48 giờ" label.
  const expiry = quote?.expiresAt
    ? (() => {
        const ms = new Date(quote.expiresAt!).getTime() - now
        if (ms <= 0) return { label: 'Hết hiệu lực', expired: true }
        const h = Math.floor(ms / 3_600_000)
        const d = Math.floor(h / 24)
        return { label: d > 0 ? `Còn ${d} ngày ${h % 24} giờ` : `Còn ${h} giờ ${Math.floor((ms % 3_600_000) / 60_000)} phút`, expired: false }
      })()
    : null

  const orderBlockReason = (() => {
    if (!quote) return null
    if (quote.status === 'Pending') return 'Hệ thống đang tính báo giá.'
    if (quote.status === 'PendingReview') return 'Báo giá đã được gửi duyệt. Bạn có thể theo dõi trong Đơn hàng.'
    if (quote.status === 'Expired' || expiry?.expired) return 'Báo giá đã hết hạn. Vui lòng lấy báo giá mới.'
    if (quote.status === 'Converted') return 'Báo giá này đã được đặt hàng. Bạn có thể xem đơn trong danh sách đơn hàng.'
    if (quote.status !== 'Ready' && quote.status !== 'Draft') return 'Báo giá chưa sẵn sàng để đặt hàng.'
    if (user && !user.isEmailVerified) return 'Vui lòng xác thực email trước khi đặt hàng.'
    if (!shippingAddress) return 'Vui lòng chọn hoặc thêm địa chỉ giao hàng.'
    if (!addressValid) return 'Địa chỉ đã chọn chưa đầy đủ hoặc vượt giới hạn. Bấm “Sửa địa chỉ” để cập nhật số nhà/đường, phường/xã, quận/huyện và tỉnh/thành phố.'
    if (!requestedDeliveryDate) return 'Vui lòng chọn ngày giao hàng mong muốn.'
    if (requestedDeliveryDate <= today) return 'Ngày giao hàng mong muốn phải sau ngày hiện tại.'
    if (delivery.isFetching) return 'Đang kiểm tra khả năng giao hàng cho ngày bạn chọn.'
    if (delivery.isError) return 'Chưa kiểm tra được khả năng giao hàng. Vui lòng bấm thử lại.'
    if (!delivery.data?.canMeetRequestedDate) return 'Vui lòng chọn ngày mong muốn từ ngày giao sớm nhất dự kiến trở đi.'
    if (!acceptTerms) return 'Vui lòng đồng ý điều khoản dịch vụ để đặt hàng.'
    return null
  })()

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default', overflowAnchor: 'none' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1400, mx: 'auto' }} spacing={2.5}>
        {/* Header */}
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Button onClick={() => navigate(-1)} startIcon={<ArrowBackRounded />} color="inherit" sx={{ color: 'text.primary' }}>
            Quay lại
          </Button>
        </Stack>

        <Box>
          <Typography variant="h1" sx={{ fontSize: '1.8rem', fontWeight: 800, color: 'text.primary' }}>
            Đặt in model
          </Typography>
          <Typography color="text.secondary">
            Chọn cấu hình, so sánh báo giá và hoàn tất thông tin nhận hàng.
          </Typography>
        </Box>

        <Box sx={{ display: 'grid', gap: 3, alignItems: 'start',
          gridTemplateColumns: { xs: 'minmax(0, 1fr)', lg: 'repeat(2, minmax(0, 1fr))' },
          gridTemplateAreas: { xs: quote ? '"configuration" "quote" "delivery"' : '"configuration" "quote"',
            lg: quote ? '"configuration quote" "delivery quote"' : '"configuration quote"' } }}>
        <Box ref={configurationFrame} component="section" aria-label="Cấu hình in" sx={{ gridArea: 'configuration', minWidth: 0,
          p: { xs: 2.5, md: 3.5 }, borderRadius: 3, border: '1px solid #E8EBF2', bgcolor: 'background.paper' }}>
          <Stack spacing={3}>
            <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>Cấu hình in</Typography>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5}>
              <FormControl fullWidth>
                <InputLabel>Vật liệu</InputLabel>
                <Select value={material} label="Vật liệu" onChange={(e) => setMaterial(e.target.value)}>
                  {MATERIALS.map((m) => (
                    <MenuItem key={m.code} value={m.code}>{m.label} — {m.desc}</MenuItem>
                  ))}
                </Select>
              </FormControl>

            </Stack>

            <OrderColorConfigurator key={modelId} url={modelFile.url} format={(model?.fileFormat ?? '').toUpperCase()}
              failed={modelFile.failed || modelQuery.isError || !!model && !model.hasFile}
              assignments={colorAssignments} singleColor={color} onChange={changeColorAssignments}
              fallbackColors={fallbackColors} onFallbackColorsChange={setFallbackColors}
              onSingleColorChange={code => setFallbackColors([code])} onValidityChange={setColorMappingValid} />

            <FormControl fullWidth>
              <InputLabel>Kích thước</InputLabel>
              <Select value={sizeCode} label="Kích thước" onChange={e => setSizeCode(e.target.value)}>
                <MenuItem value="ORIGINAL">Giữ kích thước file</MenuItem>
                <MenuItem value="S">S — cạnh lớn nhất 100 mm</MenuItem>
                <MenuItem value="M">M — cạnh lớn nhất 200 mm</MenuItem>
                <MenuItem value="L">L — cạnh lớn nhất 300 mm</MenuItem>
              </Select>
            </FormControl>
            <Typography variant="body2" color="text.secondary">Các cạnh được thay đổi cùng tỷ lệ. Lựa chọn màu in được gắn với từng nhóm màu gốc trong file.</Typography>
            <FormControlLabel control={<Checkbox checked={polishing} onChange={e => setPolishing(e.target.checked)} />} label="Thêm dịch vụ đánh bóng" />

            <Box>
              <Typography variant="body2" sx={{ color: 'text.primary', mb: 0.75 }}>Chất lượng (độ cao lớp)</Typography>
              <RadioGroup value={layerIndex} onChange={(e) => setLayerIndex(+e.target.value)}
                sx={{ display: 'grid', gridTemplateColumns: { xs: 'minmax(0, 1fr)', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 1 }}>
                {QUALITY_GRADES.map((g, i) => (
                  <FormControlLabel
                    key={g.layerMm}
                    value={i}
                    control={<Radio size="small" />}
                    label={<Box>
                      <Typography sx={{ fontSize: '0.85rem', color: 'text.primary' }}>{g.label} — {g.layerMm}mm</Typography>
                      <Typography sx={{ fontSize: '0.72rem', color: 'text.secondary' }}>{g.note}</Typography>
                    </Box>}
                  />
                ))}
              </RadioGroup>
            </Box>

            <Box>
              <Typography variant="body2" sx={{ color: 'text.primary', mb: 0.75 }}>Mật độ infill</Typography>
              <Stack direction="row" spacing={1}>
                {INFILL_OPTIONS.map((v) => (
                  <Chip
                    key={v}
                    label={`${v}%`}
                    clickable
                    color={infill === v ? 'primary' : 'default'}
                    variant={infill === v ? 'filled' : 'outlined'}
                    onClick={() => setInfill(v)}
                  />
                ))}
              </Stack>
            </Box>

            <MachinePinningSelector modelId={modelId ?? ''} materialCode={material} colorCodes={colors}
              sizeCode={sizeCode} layerHeightMm={layer.layerMm} constraints={techConstraints}
              onConstraintsChange={setTechConstraints} pinnedModelId={pinnedModelId}
              onPinnedModelChange={setPinnedModelId} onValidityChange={setMachineSelectionValid} />

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5} alignItems="center">
              <TextField
                label="Số lượng"
                type="number"
                inputProps={{ min: 1, max: 100 }}
                value={quantity}
                onChange={(e) => setQuantity(Math.min(100, Math.max(1, Math.floor(Number(e.target.value) || 1))))}
                sx={{ width: { xs: '100%', sm: 160 } }}
              />
              <Box sx={{ flex: 1 }} />
              <Button
                variant="contained"
                color="primary"
                size="large"
                onClick={handleGetQuote}
                disabled={createQuote.isPending || !machineSelectionValid || !colorMappingValid}
                endIcon={createQuote.isPending ? <CircularProgress size={18} color="inherit" /> : <PriceCheckRounded />}
              >
                {createQuote.isPending ? 'Đang phân tích…' : 'Nhận báo giá'}
              </Button>
            </Stack>
          </Stack>
        </Box>

        <Box ref={quoteColumn} component="aside" aria-label="Báo giá in" sx={{ gridArea: 'quote', minWidth: 0, alignSelf: 'stretch', overflowAnchor: 'none' }}>
          <Box ref={quoteAnchor} component="span" aria-hidden sx={{ display: 'block', height: '1px', mb: '-1px', pointerEvents: 'none' }} />
          <Box ref={quoteResult} sx={{ position: { lg: 'sticky' }, top: { lg: 88 },
            height: { lg: quotePinned ? 'min(var(--configuration-height, 100dvh), calc(100dvh - 112px))' : 'var(--configuration-height, auto)' },
            display: 'flex', flexDirection: 'column', minWidth: 0, scrollMarginTop: 88,
            borderRadius: 3, border: '1px solid', borderColor: quote ? 'rgba(111,85,255,0.32)' : 'divider',
            bgcolor: 'background.paper', p: { xs: 2.5, md: 3.5 } }}>
            <Stack spacing={2} sx={{ flex: 1, minHeight: 0, overflowY: { lg: 'auto' }, scrollbarWidth: 'thin' }}>
              <Stack direction="row" gap={1.5} justifyContent="space-between" alignItems="center" flexWrap="wrap">
                <Typography component="h2" sx={{ fontWeight: 800, fontSize: '1.15rem' }}>Báo giá của bạn</Typography>
                {quote && <Chip label={`${QUOTE_STATUS_LABELS[quote.status]}${expiry ? ` · ${expiry.label}` : ''}`}
                  color={expiry?.expired ? 'error' : quote.status === 'Ready' ? 'success' : 'info'} size="small" />}
              </Stack>
              {quote ? <>
                <QuotePriceDetails quote={quote} collapsibleDetails />
                {quote.status === 'PendingReview' && <Alert severity="info">Báo giá đã gửi đến Ops. Đơn chuyển sang bước thanh toán sau khi được duyệt.</Alert>}
                {quoteStatus.isError && <Alert severity="warning">Không cập nhật được trạng thái báo giá. Vui lòng thử cập nhật lại.</Alert>}
                {['Draft', 'Ready'].includes(quote.status) && !expiry?.expired && <Button variant="outlined"
                  onClick={() => deliveryForm.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })}
                  endIcon={<ArrowDownwardRounded />}>Nhập thông tin nhận hàng</Button>}
              </> : <Stack alignItems="center" justifyContent="center" textAlign="center" spacing={2} sx={{ py: 4, flex: 1 }} aria-live="polite">
                {createQuote.isPending ? <CircularProgress size={36} /> : <PriceCheckRounded sx={{ fontSize: 44, color: 'primary.main' }} />}
                <Typography fontWeight={650}>{createQuote.isPending ? 'Đang tính báo giá…' : 'Giá in sẽ hiển thị tại đây'}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 340 }}>
                  {createQuote.isPending ? 'Đang phân tích sản phẩm và kiểm tra lịch máy phù hợp.'
                    : 'Chọn cấu hình và bấm “Nhận báo giá” để xem giá in, thời gian dự kiến và so sánh các phương án.'}
                </Typography>
                {configError && <Alert severity="error" sx={{ textAlign: 'left', width: '100%' }}>{configError}</Alert>}
              </Stack>}
            </Stack>
          </Box>
        </Box>

        {quote && (
          <Box sx={{ gridArea: 'delivery', minWidth: 0, minHeight: { lg: 'calc(100dvh - 112px)' } }}>
          <Box ref={deliveryForm} component="section" aria-label="Thông tin nhận hàng" sx={{
            p: { xs: 2.5, md: 3.5 }, scrollMarginTop: 88, borderRadius: 3, border: '1px solid #E8EBF2', bgcolor: 'background.paper' }}>
            <Stack spacing={3}>
              <Typography component="h2" sx={{ fontWeight: 800, fontSize: '1.15rem' }}>Thông tin nhận hàng</Typography>

              {orderError && <Alert severity="error">{orderError}</Alert>}

              {quote.status === 'Draft' && (
                <Alert severity="info">
                  Đây là bản nháp tính giá, chưa gửi đến Ops. Chọn địa chỉ nhận hàng, ngày mong muốn và bấm “Gửi duyệt báo giá” để tạo đơn chờ duyệt.
                </Alert>
              )}

              {user && !user.isEmailVerified && (
                <Alert
                  severity="warning"
                  sx={{ display: 'flex', alignItems: 'center' }}
                  action={
                    <Button color="inherit" size="small" onClick={handleResendVerification} disabled={resendCooldown > 0}>
                      {resendCooldown > 0 ? `Chờ ${resendCooldown}s` : 'Gửi lại email'}
                    </Button>
                  }
                >
                  Tài khoản của bạn chưa xác thực email. Bạn không thể đặt hàng.
                </Alert>
              )}

              {/* Address */}
              <Box>
                <Stack spacing={2}>
                  <OrderShippingAddress value={shippingAddress} onChange={setShippingAddress} />
                  <TextField required type="date" label="Ngày giao hàng mong muốn" value={requestedDeliveryDate}
                    onChange={e => setRequestedDeliveryDate(e.target.value)} InputLabelProps={{ shrink: true }}
                    inputProps={{ min: today }} error={!!requestedDeliveryDate && requestedDeliveryDate <= today}
                    helperText="Hệ thống kiểm tra lại lịch sản xuất và thời gian vận chuyển trước khi cam kết." />
                  {delivery.isFetching && <Typography role="status">Đang kiểm tra khả năng giao hàng…</Typography>}
                  {delivery.isError && <Alert severity="error">{serverMessage(delivery.error, 'Không kiểm tra được lịch giao hàng.')} <Button onClick={() => void delivery.refetch()}>Thử lại</Button></Alert>}
                  {delivery.data && <Alert severity={delivery.data.canMeetRequestedDate ? 'success' : 'warning'}>
                    Ngày giao sớm nhất dự kiến: {new Date(`${delivery.data.earliestDeliveryDate}T00:00:00`).toLocaleDateString('vi-VN')}.
                    {delivery.data.canMeetRequestedDate ? ` Ngày cam kết: ${new Date(`${delivery.data.promisedDeliveryDate}T00:00:00`).toLocaleDateString('vi-VN')}.` : ' Vui lòng chọn ngày mong muốn từ ngày này trở đi.'}
                  </Alert>}
                  <Typography variant="body2" color="text.secondary">Đơn từ 5 sản phẩm do điều phối viên phân công. Đơn nhỏ chỉ được tự gán sau khi đủ cọc và có lịch phù hợp.</Typography>
                </Stack>
              </Box>

              <FormControlLabel
                control={
                  <Checkbox
                    checked={acceptTerms}
                    onChange={(e) => setAcceptTerms(e.target.checked)}
                    size="small"
                  />
                }
                label={
                  <Typography variant="body2" color="text.secondary">
                    Tôi đồng ý <Box component="span" sx={{ textDecoration: 'underline' }}>điều khoản dịch vụ</Box> của PrintGrid
                    (chất lượng kiểm tại hub, bảo hành 30 ngày, hoàn tiền theo quy định)
                  </Typography>
                }
              />

              {orderBlockReason && <Alert severity="info">{orderBlockReason}</Alert>}
              <Button
                variant="contained"
                color="primary"
                size="large"
                onClick={handlePlaceOrder}
                disabled={!!orderBlockReason || placeOrder.isPending}
                endIcon={placeOrder.isPending ? <CircularProgress size={18} color="inherit" /> : <ArrowForwardRounded />}
              >
                {placeOrder.isPending ? (quote.status === 'Draft' ? 'Đang gửi duyệt…' : 'Đang đặt hàng…')
                  : quote.status === 'Draft' ? 'Gửi duyệt báo giá'
                  : quote.status === 'PendingReview' ? 'Đã gửi duyệt'
                  : quote.status === 'Expired' || expiry?.expired ? 'Báo giá đã hết hạn' : 'Xác nhận đặt hàng'}
              </Button>
            </Stack>
          </Box>
          </Box>
        )}
        </Box>
        {modelId && <QuoteHistoryPanel modelId={modelId} selectedQuoteId={quote?.id} onSelect={saved => {
          setQuote(null); setOrderError(null)
          const next = new URLSearchParams(searchParams)
          next.set('quoteId', saved.id)
          setSearchParams(next, { replace: true })
          requestAnimationFrame(() => quoteResult.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }))
        }} />}
      </Stack>
    </Box>
  )
}
