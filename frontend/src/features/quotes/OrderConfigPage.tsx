import { useEffect, useRef, useState } from 'react'
import axios from 'axios'
import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { apiClient } from '../../shared/api/apiClient'
import {
  Alert,
  Accordion,
  AccordionDetails,
  AccordionSummary,
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
import { ArrowBackRounded, ArrowForwardRounded, ExpandMoreRounded, PriceCheckRounded } from '@mui/icons-material'
import type { Quote } from './quoteTypes'
import { COLORS, INFILL_OPTIONS, MATERIALS, QUALITY_GRADES } from './quoteTypes'
import { useCreateQuote, usePlaceOrder, useQuotes } from './useQuotes'
import { QuotePriceDetails } from './QuotePriceDetails'
import { useAuth } from '../../app/AuthContext'
import TechConstraintsSelector from './TechConstraintsSelector'
import type { TechConstraintsValue } from './TechConstraintsSelector'
import MachinePinningSelector from './MachinePinningSelector'
import type { PinModelValue } from './MachinePinningSelector'

/**
 * G3 — Print configuration & pre-order flow.
 * Step 1: choose material/color/quality/infill/quantity → get a quote.
 * Step 2: show print pricing + hub-ready estimate, then collect the delivery address.
 * Quote approval does not establish a delivery promise to that address.
 */
export default function OrderConfigPage() {
  const { modelId } = useParams<{ modelId: string }>()
  const navigate = useNavigate()
  const createQuote = useCreateQuote()
  const placeOrder = usePlaceOrder()
  const savedQuotes = useQuotes()
  const quoteResult = useRef<HTMLDivElement>(null)

  // Config state
  const [material, setMaterial] = useState('PLA')
  const [color, setColor] = useState('BLACK')
  const [colors, setColors] = useState<string[]>(['BLACK'])
  const [sizeCode, setSizeCode] = useState('ORIGINAL')
  const [polishing, setPolishing] = useState(false)
  const [layerIndex, setLayerIndex] = useState(1) // Standard
  const [infill, setInfill] = useState(30)
  const [quantity, setQuantity] = useState(1)
  const [configError, setConfigError] = useState<string | null>(null)

  // Section 3: tech constraints (doc §3, BR-03)
  const [techConstraints, setTechConstraints] = useState<TechConstraintsValue | null>(null)
  // Section 4: machine pinning (doc §4, BR-04/BR-05)
  const [pinnedModelId, setPinnedModelId] = useState<string | null>(null)

  // Quote + address state
  const [quote, setQuote] = useState<Quote | null>(null)
  const [street, setStreet] = useState('')
  const [ward, setWard] = useState('')
  const [district, setDistrict] = useState('')
  const [city, setCity] = useState('')
  const [postalCode, setPostalCode] = useState('')
  const [requestedDeliveryDate, setRequestedDeliveryDate] = useState('')
  const addressValid = !!street.trim() && street.trim().length <= 200 && !!ward.trim() && ward.trim().length <= 100
    && !!district.trim() && district.trim().length <= 100 && !!city.trim() && city.trim().length <= 100 && postalCode.trim().length <= 20
  const today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())
  const delivery = useQuery({
    queryKey: ['delivery-preview', quote?.id, requestedDeliveryDate],
    enabled: quote?.status === 'Ready' && requestedDeliveryDate > today,
    queryFn: () => apiClient.post<{ earliestDeliveryDate: string; promisedDeliveryDate: string; canMeetRequestedDate: boolean }>('/orders/delivery-preview',
      { quoteId: quote!.id, requestedDeliveryDate }).then(r => r.data),
    staleTime: 0,
  })
  const [orderError, setOrderError] = useState<string | null>(null)
  const [acceptTerms, setAcceptTerms] = useState(false)
  const configKey = JSON.stringify([modelId, material, colors, sizeCode, polishing, layerIndex, infill, quantity, techConstraints ?? {}, pinnedModelId])
  const currentConfig = useRef(configKey)
  currentConfig.current = configKey

  // A result belongs to exactly one configuration. Never allow placing a stale quote.
  useEffect(() => { setQuote(null) }, [configKey])

  useEffect(() => {
    if (quote) quoteResult.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [quote])

  // Live expiry countdown for the quote (BR-QUOTE-003) — refreshed every 30 s.
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const t = setInterval(() => setNow(Date.now()), 30_000)
    return () => clearInterval(t)
  }, [])

  const [resendCooldown, setResendCooldown] = useState(0)
  const { user } = useAuth()

  // Listen to pin-model-selector changes (custom event pattern)
  useEffect(() => {
    const handler = (e: Event) => {
      const detail = (e as CustomEvent).detail as PinModelValue | null
      if (detail) setPinnedModelId(detail.pinnedModelId)
      else setPinnedModelId(null)
    }
    window.addEventListener('pin-model-change', handler)
    return () => window.removeEventListener('pin-model-change', handler)
  }, [])

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
    if (!modelId) return
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
        // Section 3: hard tech constraints (BR-03)
        maxColors: techConstraints?.maxColors,
        enclosedChamber: techConstraints?.enclosedChamber,
        nozzleDiameterMm: techConstraints?.nozzleDiameterMm,
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
        // Doc §6.1 / §6.2 — pass machine selection context to backend (BR-03/04/05/09)
        techConstraintsJson: techConstraints ? JSON.stringify(techConstraints) : null,
        modelBindingKind: pinnedModelId ? 'theo_model' : 'không',
        pinnedModelId: pinnedModelId,
      })
      navigate('/orders', { replace: true })
    } catch (e) {
      setOrderError(serverMessage(e, 'Không thể đặt hàng. Vui lòng kiểm tra lại thông tin.'))
    }
  }

  const history = (savedQuotes.data ?? [])
    .filter(saved => saved.id !== quote?.id && saved.items.some(item => item.modelId === modelId))
    .sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt))

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

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 860, mx: 'auto' }} spacing={2.5}>
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
            Bước 1 — chọn cấu hình in · Bước 2 — xem giá in & nhập địa chỉ nhận
          </Typography>
        </Box>

        {configError && <Alert severity="error">{configError}</Alert>}

        {/* ── Step 1: config ── */}
        <Box sx={{ p: { xs: 2.5, md: 4 }, borderRadius: 3, border: '1px solid #E8EBF2', bgcolor: 'background.paper' }}>
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

              <FormControl sx={{ width: { xs: '100%', sm: 180 } }}>
                <InputLabel>Màu sắc</InputLabel>
                <Select multiple value={colors} label="Màu sắc" onChange={(e) => {
                  const values = typeof e.target.value === 'string' ? e.target.value.split(',') : e.target.value
                  if (values.length) { setColors(values); setColor(values[0]) }
                }}>
                  {COLORS.map((c) => (
                    <MenuItem key={c.code} value={c.code}>{c.label}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>

            <FormControl fullWidth>
              <InputLabel>Kích thước</InputLabel>
              <Select value={sizeCode} label="Kích thước" onChange={e => setSizeCode(e.target.value)}>
                <MenuItem value="ORIGINAL">Giữ kích thước file</MenuItem>
                <MenuItem value="S">S — cạnh lớn nhất 100 mm</MenuItem>
                <MenuItem value="M">M — cạnh lớn nhất 200 mm</MenuItem>
                <MenuItem value="L">L — cạnh lớn nhất 300 mm</MenuItem>
              </Select>
            </FormControl>
            <Typography variant="body2" color="text.secondary">Các cạnh được thay đổi cùng tỷ lệ. Chọn nhiều màu trên một mô hình cần file 3MF có vùng vật liệu riêng; thứ tự màu chọn tương ứng thứ tự vùng vật liệu trong file.</Typography>
            {colors.length > 1 && <Stack direction="row" gap={1} flexWrap="wrap">{colors.map((c, i) => <Chip key={c} label={`Vùng ${i + 1}: ${COLORS.find(x => x.code === c)?.label ?? c}`} />)}</Stack>}
            <FormControlLabel control={<Checkbox checked={polishing} onChange={e => setPolishing(e.target.checked)} />} label="Thêm dịch vụ đánh bóng" />

            <Box>
              <Typography variant="body2" sx={{ color: 'text.primary', mb: 0.75 }}>Chất lượng (độ cao lớp)</Typography>
              <RadioGroup row value={layerIndex} onChange={(e) => setLayerIndex(+e.target.value)}>
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

            {/* Section 3: Tech constraints */}
            <TechConstraintsSelector defaultQualityLayerMm={layer.layerMm} onValueChange={setTechConstraints} />

            {/* Section 4: Machine pinning (BR-04/BR-05) */}
            <MachinePinningSelector />

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
                disabled={createQuote.isPending}
                endIcon={createQuote.isPending ? <CircularProgress size={18} color="inherit" /> : <PriceCheckRounded />}
              >
                {createQuote.isPending ? 'Đang phân tích…' : 'Nhận báo giá'}
              </Button>
            </Stack>
          </Stack>
        </Box>

        {/* ── Step 2: quote result + address ── */}
        {quote && (
          <Box ref={quoteResult} sx={{ p: { xs: 2.5, md: 4 }, scrollMarginTop: 100, borderRadius: 3, border: '1px solid rgba(111,85,255,0.32)', bgcolor: 'background.paper' }}>
            <Stack spacing={3}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography sx={{ fontWeight: 800, fontSize: '1.15rem', color: 'text.primary' }}>
                  Báo giá của bạn
                </Typography>
                <Chip
                  label={`${quoteStatusLabels[quote.status]}${expiry ? ` · ${expiry.label}` : ''}`}
                  color={expiry?.expired ? 'error' : quote.status === 'Ready' ? 'success' : 'info'}
                  size="small"
                />
              </Stack>

              <QuotePriceDetails quote={quote} />

              {orderError && <Alert severity="error">{orderError}</Alert>}

              {quote.status === 'Draft' && (
                <Alert severity="info">
                  Giá in đang chờ nhân viên thẩm định; thời hạn chỉ bắt đầu sau khi giá được duyệt. Đây chưa phải xác nhận ngày giao tới địa chỉ nhận.
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
                <Typography variant="body2" sx={{ color: 'text.primary', mb: 1 }}>Địa chỉ giao hàng</Typography>
                <Stack spacing={2}>
                  <TextField required label="Số nhà, đường" value={street} onChange={(e) => setStreet(e.target.value)} inputProps={{ maxLength: 200 }} fullWidth />
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField required label="Phường/Xã" value={ward} onChange={(e) => setWard(e.target.value)} inputProps={{ maxLength: 100 }} fullWidth />
                    <TextField required label="Quận/Huyện" value={district} onChange={(e) => setDistrict(e.target.value)} inputProps={{ maxLength: 100 }} fullWidth />
                  </Stack>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField required label="Tỉnh/Thành phố" value={city} onChange={(e) => setCity(e.target.value)} inputProps={{ maxLength: 100 }} fullWidth />
                    <TextField label="Mã bưu điện" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} inputProps={{ maxLength: 20 }} sx={{ width: { xs: '100%', sm: 180 } }} />
                  </Stack>
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

              <Button
                variant="contained"
                color="primary"
                size="large"
                onClick={handlePlaceOrder}
                disabled={quote.status !== 'Ready' || Boolean(expiry?.expired) || placeOrder.isPending || !addressValid || !requestedDeliveryDate || requestedDeliveryDate <= today || delivery.isFetching || delivery.isError || !delivery.data?.canMeetRequestedDate || !acceptTerms || Boolean(user && !user.isEmailVerified)}
                endIcon={placeOrder.isPending ? <CircularProgress size={18} color="inherit" /> : <ArrowForwardRounded />}
              >
                {placeOrder.isPending ? 'Đang đặt hàng…' : expiry?.expired ? 'Báo giá đã hết hạn' : 'Xác nhận đặt hàng'}
              </Button>
            </Stack>
          </Box>
        )}
        {savedQuotes.isError && <Alert severity="warning">Không tải được các báo giá đã lưu.</Alert>}
        {history.length > 0 && (
          <Box>
            <Typography sx={{ fontWeight: 700, mb: 1 }}>Báo giá đã tạo cho model này</Typography>
            {history.map(saved => (
              <Accordion key={saved.id}>
                <AccordionSummary expandIcon={<ExpandMoreRounded />}>
                  <Stack spacing={0.5}>
                    <Typography sx={{ fontWeight: 600 }}>{saved.totalPrice.toLocaleString('vi-VN')} {saved.currency}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {new Date(saved.createdAt).toLocaleString('vi-VN')} · {quoteStatusLabels[saved.status]}
                    </Typography>
                  </Stack>
                </AccordionSummary>
                <AccordionDetails><QuotePriceDetails quote={saved} /></AccordionDetails>
              </Accordion>
            ))}
          </Box>
        )}
      </Stack>
    </Box>
  )
}

const quoteStatusLabels: Record<Quote['status'], string> = {
  Pending: 'Đang tính giá in', Draft: 'Giá in chờ duyệt', Ready: 'Giá in đã duyệt', Expired: 'Hết hiệu lực',
  Converted: 'Đã đặt hàng', Failed: 'Không thể báo giá',
}
