import { Box, Divider, Stack, Typography } from '@mui/material'
import type { Quote } from './quoteTypes'

const fmt = (amount: number) => amount.toLocaleString('vi-VN')

export function QuotePriceDetails({ quote }: { quote: Quote }) {
  // Older API deployments used promisedDeliveryDate for this same hub-ready estimate.
  // Neither that legacy field nor entering an address establishes a shipping promise.
  const hubReadyDate = quote.estimatedHubReadyDate ?? quote.promisedDeliveryDate
  return <Stack spacing={3}>
    {quote.items.map(item => {
      const calculation = item.calculation
      return <Box key={item.id}>
        <Typography variant="caption" color="text.disabled">CHI TIẾT</Typography>
        <Stack spacing={0.5} sx={{ mt: 0.5 }}>
          <Row label="Vật liệu / màu" value={`${item.materialCode} · ${calculation?.colors.map(c => c.colorCode).join(', ') ?? item.colorCode}`} />
          {calculation ? <>
            <Row label="Kích thước sau điều chỉnh" value={`${fmt(calculation.widthMm)} × ${fmt(calculation.depthMm)} × ${fmt(calculation.heightMm)} mm`} />
            <Row label="Thời gian in cả đơn (ước tính)" value={`${fmt(calculation.printMinutes / 60)} giờ`} />
            {calculation.colors.map(color => <Row key={color.colorCode}
              label={color.pricePerKg != null ? `${color.colorCode}: ${fmt(color.grams)} g × ${fmt(color.pricePerKg)} đ/kg` : `Vật liệu ${color.colorCode}`}
              value={color.amount != null ? `${fmt(color.amount)} đ` : `${fmt(color.grams)} g`} />)}
            <Row label="Vật liệu cả đơn" value={`${fmt(calculation.materialAmount)} đ`} />
            <Row label={calculation.pricePerHour != null ? `Giờ máy (${fmt(calculation.pricePerHour)} đ/giờ)` : 'Giờ máy'} value={`${fmt(calculation.machineAmount)} đ`} />
            <Row label={calculation.failureRate != null ? `Dự phòng lỗi (${fmt(calculation.failureRate * 100)}%)` : 'Dự phòng lỗi'} value={`${fmt(calculation.failureAllowance)} đ`} />
            <Row label={calculation.polishingPerUnit != null ? `Đánh bóng (${fmt(calculation.polishingPerUnit)} đ/sản phẩm)` : 'Đánh bóng'} value={`${fmt(calculation.serviceAmount)} đ`} />
            <Row label="Giá trung bình / sản phẩm" value={`${fmt(calculation.totalAmount / item.quantity)} đ`} />
          </> : <>
            <Row label="Phân tích" value={`${item.estimatedPrintMinutes} phút · ${item.estimatedMaterialGrams} g vật liệu`} />
            <Row label="Đơn giá" value={`${fmt(item.unitPrice)} đ`} />
            <Row label="— vật liệu" value={`${fmt(item.materialCostAmount)} đ`} />
            <Row label="— giờ máy" value={`${fmt(item.machineTimeCostAmount)} đ`} />
          </>}
          <Row label="Số lượng" value={`${item.quantity}`} />
        </Stack>
      </Box>
    })}
    <Divider />
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="space-between"
      sx={{ bgcolor: 'rgba(111,85,255,0.07)', borderRadius: 2, p: 2 }}>
      <Box sx={{ flexShrink: 0 }}>
        <Typography variant="caption" color="text.disabled">TỔNG GIÁ IN</Typography>
        <Typography sx={{ fontWeight: 800, fontSize: '1.4rem', color: 'text.primary', whiteSpace: 'nowrap' }}>
          {fmt(quote.totalPrice)} {quote.currency}
        </Typography>
        <Typography variant="caption" color="text.secondary">Chưa bao gồm phí giao hàng</Typography>
      </Box>
      <Box textAlign={{ xs: 'left', sm: 'right' }}>
        <Typography variant="caption" color="text.disabled">DỰ KIẾN SẴN SÀNG TẠI HUB</Typography>
        <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>
          {hubReadyDate ? new Date(hubReadyDate + 'T00:00:00').toLocaleDateString('vi-VN') : 'Chưa có dự kiến'}
        </Typography>
        <Typography component="p" variant="caption" color="text.secondary">
          {quote.customerPlacementSummary ?? 'tính theo lịch máy thật của mạng lưới'}
        </Typography>
        <Typography component="p" variant="caption" color="text.secondary">
          Chưa có ngày giao cam kết tới địa chỉ nhận. Dự kiến trên chưa tính chặng giao từ hub tới khách.
        </Typography>
        <Typography variant="caption" color="text.disabled">Kỳ giá {quote.pricingVersion}</Typography>
      </Box>
    </Stack>
  </Stack>
}

function Row({ label, value }: { label: string; value: string }) {
  return <Stack direction="row" justifyContent="space-between" spacing={2}>
    <Typography variant="body2" color="text.secondary">{label}</Typography>
    <Typography variant="body2" color="text.primary" sx={{ fontWeight: 600, textAlign: 'right' }}>{value}</Typography>
  </Stack>
}
