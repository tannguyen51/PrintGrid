import { Accordion, AccordionDetails, AccordionSummary, Box, Divider, Stack, Table, TableBody,
  TableCell, TableHead, TableRow, Typography } from '@mui/material'
import ExpandMoreRounded from '@mui/icons-material/ExpandMoreRounded'
import type { Quote } from './quoteTypes'

const fmt = (amount: number) => amount.toLocaleString('vi-VN')

export function QuotePriceDetails({ quote, collapsibleDetails = false }: { quote: Quote; collapsibleDetails?: boolean }) {
  const hubReadyDate = quote.estimatedHubReadyDate || quote.promisedDeliveryDate
  const parsedHubDate = hubReadyDate ? new Date(hubReadyDate.includes('T') ? hubReadyDate : `${hubReadyDate}T00:00:00`) : null
  const hubDateLabel = parsedHubDate && Number.isFinite(parsedHubDate.getTime())
    ? parsedHubDate.toLocaleDateString('vi-VN') : 'Chưa có dự kiến'
  const pinnedName = quote.items.find(item => item.calculation?.requirements?.pinnedModelName)?.calculation?.requirements?.pinnedModelName

  const summary = <Box sx={{ bgcolor: 'rgba(111,85,255,0.06)', borderRadius: 2.5, p: 2 }}>
    <Stack spacing={.75}>
      <Typography variant="body2" color="text.secondary">Tổng giá in</Typography>
      <Typography component="p" sx={{ fontWeight: 800, fontSize: { xs: '1.9rem', sm: '2.25rem' }, lineHeight: 1.2, letterSpacing: '-0.035em', overflowWrap: 'anywhere' }}>
        {fmt(quote.totalPrice)} <Typography component="span" sx={{ fontSize: '1rem', fontWeight: 650, letterSpacing: 0 }}>{quote.currency}</Typography>
      </Typography>
      <Typography variant="caption" color="text.secondary">Chưa bao gồm VAT và phí giao hàng</Typography>
      {collapsibleDetails && <Box sx={{ pt: .5 }}>
        <Typography variant="body2" fontWeight={650}>{pinnedName ?? 'Hệ thống tự chọn máy phù hợp'}</Typography>
        <Typography variant="body2" color="text.secondary">
          {[...new Set(quote.items.map(item => item.materialCode))].join(', ')} · {quote.items.reduce((sum, item) => sum + item.quantity, 0)} sản phẩm
        </Typography>
      </Box>}
      <Divider sx={{ my: '8px !important' }} />
      <Stack direction="row" gap={2} justifyContent="space-between" alignItems="baseline">
        <Typography variant="body2" color="text.secondary">Dự kiến sẵn sàng tại Hub</Typography>
        <Typography fontWeight={750} sx={{ whiteSpace: 'nowrap' }}>{hubDateLabel}</Typography>
      </Stack>
      <Typography variant="caption" color="text.secondary">Ngày giao đến địa chỉ nhận được kiểm tra ở bước thông tin nhận hàng.</Typography>
    </Stack>
  </Box>

  const details = <Stack spacing={2.5}>
    {quote.items.map(item => {
      const calculation = item.calculation
      return <Box key={item.id}>
        <Stack spacing={1}>
          <Row label="Vật liệu / màu" value={`${item.materialCode} · ${calculation?.colors.map(c => c.colorCode).join(', ') ?? item.colorCode}`} />
          {calculation?.requirements?.pinnedModelName && <Row label="Model máy bắt buộc" value={calculation.requirements.pinnedModelName} />}
          {calculation ? <>
            <Row label="Kích thước sau điều chỉnh" value={`${fmt(calculation.widthMm)} × ${fmt(calculation.depthMm)} × ${fmt(calculation.heightMm)} mm`} />
            <Row label="Thời gian in cả đơn (ước tính)" value={`${fmt(calculation.printMinutes / 60)} giờ`} />
            {calculation.colors.map(color => <Row key={color.colorCode}
              label={color.pricePerKg != null ? `${color.colorCode}: ${fmt(color.grams)} g × ${fmt(color.pricePerKg)} đ/kg` : `Vật liệu ${color.colorCode}`}
              value={color.amount != null ? `${fmt(color.amount)} đ` : `${fmt(color.grams)} g`} />)}
            <Row label="Vật liệu cả đơn" value={`${fmt(calculation.materialAmount)} đ`} />
            <Row label={calculation.pricePerHour != null ? `Giờ máy (${fmt(calculation.pricePerHour)} đ/giờ)` : 'Giờ máy'} value={`${fmt(calculation.machineAmount)} đ`} />
            {!!calculation.setupAmount && <Row label={`Chuẩn bị ${calculation.plateCount ?? 1} bàn in`} value={`${fmt(calculation.setupAmount)} đ`} />}
            <Row label={calculation.failureRate != null ? `Dự phòng lỗi (${fmt(calculation.failureRate * 100)}%)` : 'Dự phòng lỗi'} value={`${fmt(calculation.failureAllowance)} đ`} />
            <Row label={calculation.polishingPerUnit != null ? `Đánh bóng (${fmt(calculation.polishingPerUnit)} đ/sản phẩm)` : 'Đánh bóng'} value={`${fmt(calculation.serviceAmount)} đ`} />
            {!!calculation.hubAmount && <Row label="Xử lý tại Hub" value={`${fmt(calculation.hubAmount)} đ`} />}
            {(calculation.pinSurchargeCoefficient ?? 1) > 1 && <Row label={`Phụ phí chỉ định model (×${calculation.pinSurchargeCoefficient})`}
              value={`${fmt(calculation.pinSurchargeAmount ?? 0)} đ`} />}
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
    <Typography variant="caption" color="text.secondary">Kỳ giá {quote.pricingVersion}</Typography>
  </Stack>

  const comparison = quote.isPinnedModelQuote && quote.unpinnedPriceVnd != null && <Box>
    <Typography variant="body2" fontWeight={750} mb={1}>So sánh phương án in</Typography>
    <Table size="small" aria-label="So sánh báo giá theo model máy" sx={{ tableLayout: 'fixed',
      '& .MuiTableCell-root': { px: { xs: .75, sm: 1.5 }, whiteSpace: 'normal', overflowWrap: 'anywhere' } }}>
      <TableHead><TableRow><TableCell sx={{ width: '40%' }}>Phương án</TableCell><TableCell align="right" sx={{ width: '30%' }}>Giá in</TableCell><TableCell sx={{ width: '30%' }}>Dự kiến tại Hub</TableCell></TableRow></TableHead>
      <TableBody>
        <TableRow><TableCell>Hệ thống tự chọn máy</TableCell><TableCell align="right">{fmt(quote.unpinnedPriceVnd)} đ</TableCell>
          <TableCell>{quote.unpinnedDeliveryDay ? new Date(`${quote.unpinnedDeliveryDay}T00:00:00`).toLocaleDateString('vi-VN') : 'Chưa có dự kiến'}</TableCell></TableRow>
        <TableRow selected><TableCell>Model đã chỉ định · Đang chọn</TableCell><TableCell align="right">{fmt(quote.totalPrice)} đ</TableCell><TableCell>{hubDateLabel}</TableCell></TableRow>
      </TableBody>
    </Table>
    <Typography component="p" variant="caption" color="text.secondary" sx={{ mt: 1 }}>
      Để đổi sang phương án tự động, tắt “Bắt buộc model / dòng máy” và lấy báo giá mới.
    </Typography>
  </Box>

  return <Stack spacing={collapsibleDetails ? 2 : 2.5}>
    {collapsibleDetails ? <>
      {summary}
      {comparison}
      <Accordion key={quote.id} disableGutters elevation={0} sx={{ border: '1px solid', borderColor: 'divider',
        borderRadius: '12px !important', '&::before': { display: 'none' } }}>
        <AccordionSummary expandIcon={<ExpandMoreRounded />} aria-controls={`quote-cost-${quote.id}`}>
          <Typography variant="body2" fontWeight={650}>Chi tiết chi phí</Typography>
        </AccordionSummary>
        <AccordionDetails id={`quote-cost-${quote.id}`}>{details}</AccordionDetails>
      </Accordion>
    </> : <>{details}{comparison}<Divider />{summary}</>}
  </Stack>
}

function Row({ label, value }: { label: string; value: string }) {
  return <Stack direction="row" justifyContent="space-between" spacing={2}>
    <Typography variant="body2" color="text.secondary" sx={{ flex: 1, minWidth: 0 }}>{label}</Typography>
    <Typography variant="body2" sx={{ flex: 1, minWidth: 0, fontWeight: 600, textAlign: 'right', overflowWrap: 'anywhere' }}>{value}</Typography>
  </Stack>
}
