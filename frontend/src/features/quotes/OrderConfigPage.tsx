import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
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
import { ArrowBackRounded, ArrowForwardRounded, PriceCheckRounded } from '@mui/icons-material'
import type { Quote } from './quoteTypes'
import { COLORS, INFILL_OPTIONS, MATERIALS, QUALITY_GRADES } from './quoteTypes'
import { useCreateQuote, usePlaceOrder } from './useQuotes'
import { useAuth } from '../../app/AuthContext'

/**
 * G3 — Print configuration & pre-order flow.
 * Step 1: choose material/color/quality/infill/quantity → get a quote.
 * Step 2: on success, show the price breakdown + delivery date, then confirm with an address.
 */
export default function OrderConfigPage() {
  const { modelId } = useParams<{ modelId: string }>()
  const { logout } = useAuth()
  const navigate = useNavigate()
  const createQuote = useCreateQuote()
  const placeOrder = usePlaceOrder()

  // Config state
  const [material, setMaterial] = useState('PLA')
  const [color, setColor] = useState('BLACK')
  const [layerIndex, setLayerIndex] = useState(1) // Standard
  const [infill, setInfill] = useState(30)
  const [quantity, setQuantity] = useState(1)
  const [configError, setConfigError] = useState<string | null>(null)

  // Quote + address state
  const [quote, setQuote] = useState<Quote | null>(null)
  const [street, setStreet] = useState('')
  const [ward, setWard] = useState('')
  const [district, setDistrict] = useState('')
  const [city, setCity] = useState('')
  const [postalCode, setPostalCode] = useState('')
  const [orderError, setOrderError] = useState<string | null>(null)

  const layer = QUALITY_GRADES[layerIndex]

  async function handleGetQuote() {
    if (!modelId) return
    setConfigError(null)
    setQuote(null)
    try {
      const q = await createQuote.mutateAsync({
        modelId,
        materialCode: material,
        colorCode: color,
        layerHeightMm: layer.layerMm,
        infillPercent: infill,
        quantity,
        toleranceMm: 0.2,
      })
      setQuote(q)
    } catch {
      setConfigError('Không lấy được báo giá. Kiểm tra lại cấu hình.')
    }
  }

  async function handlePlaceOrder() {
    if (!quote) return
    setOrderError(null)
    try {
      await placeOrder.mutateAsync({
        quoteId: quote.id,
        street: street.trim(),
        ward: ward.trim(),
        district: district.trim(),
        city: city.trim(),
        postalCode: postalCode.trim(),
      })
      navigate('/orders', { replace: true })
    } catch {
      setOrderError('Không thể đặt hàng. Vui lòng kiểm tra lại thông tin.')
    }
  }

  const fmt = (n: number) => n.toLocaleString('vi-VN')

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 860, mx: 'auto' }} spacing={2.5}>
        {/* Header */}
        <Stack direction="row" justifyContent="space-between" alignItems="center">
          <Button onClick={() => navigate(-1)} startIcon={<ArrowBackRounded />} color="inherit" sx={{ color: 'text.primary' }}>
            Quay lại
          </Button>
          <Button onClick={() => { logout(); navigate('/login', { replace: true }) }} color="error" variant="outlined" size="small">
            Đăng xuất
          </Button>
        </Stack>

        <Box>
          <Typography variant="h1" sx={{ fontSize: '1.8rem', fontWeight: 800, color: 'text.primary' }}>
            Đặt in model
          </Typography>
          <Typography color="text.secondary">
            Bước 1 — chọn cấu hình in · Bước 2 — nhận báo giá & giao hàng
          </Typography>
        </Box>

        {configError && <Alert severity="error">{configError}</Alert>}

        {/* ── Step 1: config ── */}
        <Box sx={{ p: { xs: 2.5, md: 4 }, borderRadius: 3, border: '1px solid rgba(255,255,255,0.1)', bgcolor: 'background.paper' }}>
          <Stack spacing={3}>
            <Typography sx={{ fontWeight: 700, color: 'text.primary', fontSize: '1.05rem' }}>Cấu hình in</Typography>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5}>
              <FormControl fullWidth>
                <InputLabel>Vật liệu</InputLabel>
                <Select value={material} label="Vật liệu" onChange={(e) => setMaterial(e.target.value)}>
                  {MATERIALS.map((m) => (
                    <MenuItem key={m.code} value={m.code}>{m.label} — {m.rate} · {m.desc}</MenuItem>
                  ))}
                </Select>
              </FormControl>

              <FormControl sx={{ width: { xs: '100%', sm: 180 } }}>
                <InputLabel>Màu sắc</InputLabel>
                <Select value={color} label="Màu sắc" onChange={(e) => setColor(e.target.value)}>
                  {COLORS.map((c) => (
                    <MenuItem key={c.code} value={c.code}>{c.label}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>

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

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5} alignItems="center">
              <TextField
                label="Số lượng"
                type="number"
                inputProps={{ min: 1, max: 100 }}
                value={quantity}
                onChange={(e) => setQuantity(Math.max(1, Number(e.target.value) || 1))}
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
          <Box sx={{ p: { xs: 2.5, md: 4 }, borderRadius: 3, border: '1px solid rgba(139,92,246,0.35)', bgcolor: 'background.paper' }}>
            <Stack spacing={3}>
              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Typography sx={{ fontWeight: 800, fontSize: '1.15rem', color: 'text.primary' }}>
                  Báo giá của bạn
                </Typography>
                <Chip
                  label={quote.status === 'Ready' ? 'Hiệu lực 48 giờ' : quote.status}
                  color={quote.status === 'Ready' ? 'success' : 'warning'}
                  size="small"
                />
              </Stack>

              {quote.items.map((it) => (
                <Box key={it.id}>
                  <Typography variant="caption" color="text.disabled">CHI TIẾT</Typography>
                  <Stack spacing={0.5} sx={{ mt: 0.5 }}>
                    <Row label="Vật liệu / màu" value={`${it.materialCode} · ${it.colorCode}`} />
                    <Row label="Phân tích" value={`${it.estimatedPrintMinutes} phút · ${it.estimatedMaterialGrams} g vật liệu`} />
                    <Row label="Đơn giá" value={`${fmt(it.unitPrice)} đ`} />
                    <Row label="Số lượng" value={`${it.quantity}`} />
                  </Stack>
                </Box>
              ))}

              <Divider sx={{ borderColor: 'rgba(255,255,255,0.08)' }} />

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="space-between" sx={{ bgcolor: 'rgba(139,92,246,0.08)', borderRadius: 2, p: 2 }}>
                <Box>
                  <Typography variant="caption" color="text.disabled">TỔNG</Typography>
                  <Typography sx={{ fontWeight: 800, fontSize: '1.4rem', color: 'text.primary' }}>
                    {fmt(quote.totalPrice)} {quote.currency}
                  </Typography>
                </Box>
                <Box textAlign={{ xs: 'left', sm: 'right' }}>
                  <Typography variant="caption" color="text.disabled">NGÀY GIAO DỰ KIẾN</Typography>
                  <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>
                    {new Date(quote.promisedDeliveryDate + 'T00:00:00').toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })}
                  </Typography>
                </Box>
              </Stack>

              {orderError && <Alert severity="error">{orderError}</Alert>}

              {/* Address */}
              <Box>
                <Typography variant="body2" sx={{ color: 'text.primary', mb: 1 }}>Địa chỉ giao hàng</Typography>
                <Stack spacing={2}>
                  <TextField label="Số nhà, đường" value={street} onChange={(e) => setStreet(e.target.value)} fullWidth />
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField label="Phường/Xã" value={ward} onChange={(e) => setWard(e.target.value)} fullWidth />
                    <TextField label="Quận/Huyện" value={district} onChange={(e) => setDistrict(e.target.value)} fullWidth />
                  </Stack>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField label="Tỉnh/Thành phố" value={city} onChange={(e) => setCity(e.target.value)} fullWidth />
                    <TextField label="Mã bưu điện" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} sx={{ width: { xs: '100%', sm: 180 } }} />
                  </Stack>
                </Stack>
              </Box>

              <Button
                variant="contained"
                color="primary"
                size="large"
                onClick={handlePlaceOrder}
                disabled={placeOrder.isPending || !street.trim() || !city.trim()}
                endIcon={placeOrder.isPending ? <CircularProgress size={18} color="inherit" /> : <ArrowForwardRounded />}
              >
                {placeOrder.isPending ? 'Đang đặt hàng…' : 'Xác nhận đặt hàng'}
              </Button>
            </Stack>
          </Box>
        )}
      </Stack>
    </Box>
  )
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" justifyContent="space-between">
      <Typography variant="body2" color="text.secondary">{label}</Typography>
      <Typography variant="body2" color="text.primary" sx={{ fontWeight: 600 }}>{value}</Typography>
    </Stack>
  )
}