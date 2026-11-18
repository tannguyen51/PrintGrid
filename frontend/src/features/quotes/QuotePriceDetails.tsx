import { Box, Divider, Stack, Typography } from '@mui/material'
import type { Quote } from './quoteTypes'

const fmt = (amount: number) => amount.toLocaleString('vi-VN')

export function QuotePriceDetails({ quote }: { quote: Quote }) {
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
        <Typography variant="caption" color="text.disabled">TỔNG</Typography>
        <Typography sx={{ fontWeight: 800, fontSize: '1.4rem', color: 'text.primary', whiteSpace: 'nowrap' }}>
          {fmt(quote.totalPrice)} {quote.currency}
        </Typography>
      </Box>
      <Box textAlign={{ xs: 'left', sm: 'right' }}>
        <Typography variant="caption" color="text.disabled">NGÀY GIAO CAM KẾT</Typography>
        <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>
          {new Date(quote.promisedDeliveryDate + 'T00:00:00').toLocaleDateString('vi-VN')}
        </Typography>
        <Typography variant="caption" color="text.secondary">
          {quote.customerPlacementSummary ?? 'tính theo lịch máy thật của mạng lưới'}
        </Typography>
        <Typography variant="caption" color="text.disabled"> · kỳ giá {quote.pricingVersion}</Typography>
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
