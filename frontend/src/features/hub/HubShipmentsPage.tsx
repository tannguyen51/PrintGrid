import { useMemo, useState } from 'react'
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
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  LocalShippingRounded,
  InventoryRounded,
  CheckRounded,
  CallMergeRounded,
  CurrencyExchangeRounded,
} from '@mui/icons-material'
import {
  useShipmentQueue,
  useShipOrder,
  useDeliverOrder,
  useShipConsolidatedOrders,
  useDeliverConsolidatedOrders,
  useRefundOrderWithinScope,
  type ShipmentOrder,
} from './useShipments'
import { PageBackButton } from '../../shared/components/PageBackButton'

const fmtDate = (d: string | null | undefined) => (d ? new Date(d + 'T00:00:00').toLocaleDateString('vi-VN') : '—')
const fmtDateTime = (d: string | null | undefined) =>
  d ? new Date(d).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit' }) : '—'
const fmtVnd = (amount: number | undefined) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount ?? 0)

const statusLabel: Record<ShipmentOrder['status'], string> = {
  QualityCheck: 'Sẵn sàng giao (QC Pass)',
  Shipping: 'Đang giao',
  Delivered: 'Đã giao',
}

const statusColor: Record<ShipmentOrder['status'], 'warning' | 'info' | 'success'> = {
  QualityCheck: 'warning',
  Shipping: 'info',
  Delivered: 'success',
}

interface ConsolidationCluster {
  groupKey: string
  customerId: string
  deliveryAddressSummary: string
  orders: ShipmentOrder[]
}

/**
 * Hub shipment & order completion console (FR-HUB-006 · BR-LOG-001/002):
 * - Suggests consolidating >=2 QualityCheck orders from the same customer + same address ready within <=24h into 1 waybill & 1 package (AC01).
 * - "Ghi vận đơn" ships a single order or consolidated group.
 * - "Xác nhận đã giao" records physical handover (single or consolidated).
 * - "Hoàn tiền đơn gốc" enforces original-order-scoped refund limit (AC02).
 */
export default function HubShipmentsPage() {
  const queue = useShipmentQueue()
  const ship = useShipOrder()
  const deliver = useDeliverOrder()
  const shipConsolidated = useShipConsolidatedOrders()
  const deliverConsolidated = useDeliverConsolidatedOrders()
  const refundOrder = useRefundOrderWithinScope()

  const [shipTarget, setShipTarget] = useState<ShipmentOrder | null>(null)
  const [tracking, setTracking] = useState('')

  // Consolidated shipment dialog state (FR-HUB-006 AC01)
  const [consolidateCluster, setConsolidateCluster] = useState<ConsolidationCluster | null>(null)
  const [selectedClusterOrderIds, setSelectedClusterOrderIds] = useState<string[]>([])
  const [consolidatedTracking, setConsolidatedTracking] = useState('')

  // Original-order-scoped refund dialog state (FR-HUB-006 AC02 / BR-LOG-002)
  const [refundTarget, setRefundTarget] = useState<ShipmentOrder | null>(null)
  const [refundAmount, setRefundAmount] = useState('')
  const [refundReason, setRefundReason] = useState('')

  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const orders = queue.data ?? []

  // Group SuggestedConsolidationGroupKey into clusters with >= 2 orders
  const suggestedClusters = useMemo<ConsolidationCluster[]>(() => {
    const map = new Map<string, ShipmentOrder[]>()
    for (const o of orders) {
      if (o.status === 'QualityCheck' && o.suggestedConsolidationGroupKey) {
        const list = map.get(o.suggestedConsolidationGroupKey) ?? []
        list.push(o)
        map.set(o.suggestedConsolidationGroupKey, list)
      }
    }
    const result: ConsolidationCluster[] = []
    for (const [groupKey, groupOrders] of map.entries()) {
      if (groupOrders.length >= 2) {
        result.push({
          groupKey,
          customerId: groupOrders[0].customerId ?? '',
          deliveryAddressSummary: groupOrders[0].deliveryAddressSummary ?? '—',
          orders: groupOrders,
        })
      }
    }
    return result
  }, [orders])

  function openShipDialog(order: ShipmentOrder) {
    setShipTarget(order)
    setTracking(order.trackingNumber ?? '')
    setErrorMessage(null)
  }

  function openConsolidateDialog(cluster: ConsolidationCluster) {
    setConsolidateCluster(cluster)
    setSelectedClusterOrderIds(cluster.orders.map((o) => o.id))
    setConsolidatedTracking('')
    setErrorMessage(null)
  }

  function toggleClusterOrder(orderId: string) {
    setSelectedClusterOrderIds((prev) =>
      prev.includes(orderId) ? prev.filter((id) => id !== orderId) : [...prev, orderId],
    )
  }

  function openRefundDialog(order: ShipmentOrder) {
    setRefundTarget(order)
    const remaining = Math.max(0, (order.totalAmount ?? 0) - (order.refundedAmount ?? 0))
    setRefundAmount(remaining > 0 ? String(remaining) : '')
    setRefundReason('')
    setErrorMessage(null)
  }

  async function handleShip() {
    if (!shipTarget) return
    const value = tracking.trim()
    if (!value) {
      setErrorMessage('Bắt buộc nhập mã vận đơn trước khi ghi.')
      return
    }
    try {
      await ship.mutateAsync({ id: shipTarget.id, trackingNumber: value })
      setSuccessMessage(`Đã ghi vận đơn ${value} cho đơn ${shipTarget.orderNumber}.`)
      setShipTarget(null)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể ghi vận đơn.')
    }
  }

  async function handleShipConsolidated() {
    if (!consolidateCluster) return
    const value = consolidatedTracking.trim()
    if (!value) {
      setErrorMessage('Bắt buộc nhập mã vận đơn chung cho kiện gộp.')
      return
    }
    if (selectedClusterOrderIds.length < 2) {
      setErrorMessage('Cần chọn ít nhất 2 đơn để đóng gói gộp 1 kiện.')
      return
    }
    try {
      const res = await shipConsolidated.mutateAsync({
        orderIds: selectedClusterOrderIds,
        trackingNumber: value,
      })
      setSuccessMessage(
        `Đã gộp ${res.orderNumbers.join(', ')} vào 1 kiện (${res.consolidatedShipmentId.slice(0, 8)}) với vận đơn ${res.trackingNumber}.`,
      )
      setConsolidateCluster(null)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể gộp kiện giao hàng.')
    }
  }

  async function handleDeliver(order: ShipmentOrder) {
    try {
      setErrorMessage(null)
      await deliver.mutateAsync(order.id)
      setSuccessMessage(`Đã xác nhận giao đơn ${order.orderNumber}.`)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể xác nhận đã giao.')
    }
  }

  async function handleDeliverConsolidatedPackage(consolidatedShipmentId: string) {
    try {
      setErrorMessage(null)
      const packageOrders = orders.filter(
        (o) => o.consolidatedShipmentId === consolidatedShipmentId && o.status === 'Shipping',
      )
      const orderIds = packageOrders.map((o) => o.id)
      if (orderIds.length === 0) return
      await deliverConsolidated.mutateAsync(orderIds)
      setSuccessMessage(`Đã xác nhận giao kiện gộp (${orderIds.length} đơn hàng).`)
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.error?.message || 'Không thể xác nhận giao kiện gộp.')
    }
  }

  async function handleRefund() {
    if (!refundTarget) return
    const num = Number(refundAmount)
    if (!Number.isFinite(num) || num <= 0) {
      setErrorMessage('Số tiền hoàn phải lớn hơn 0.')
      return
    }
    if (!refundReason.trim()) {
      setErrorMessage('Vui lòng nhập lý do hoàn tiền / khiếu nại.')
      return
    }
    try {
      await refundOrder.mutateAsync({
        orderId: refundTarget.id,
        amount: num,
        reason: refundReason.trim(),
      })
      const remainingAfter = Math.max(0, (refundTarget.totalAmount ?? 0) - (refundTarget.refundedAmount ?? 0) - num)
      setSuccessMessage(
        `Đã hoàn tiền ${fmtVnd(num)} trong phạm vi đơn gốc ${refundTarget.orderNumber} (Còn tối đa: ${fmtVnd(remainingAfter)}).`,
      )
      setRefundTarget(null)
    } catch (err: any) {
      setErrorMessage(
        err?.response?.data?.error?.message || 'Không thể hoàn tiền (vượt quá phạm vi giá trị đơn gốc).',
      )
    }
  }

  const busy =
    ship.isPending ||
    deliver.isPending ||
    shipConsolidated.isPending ||
    deliverConsolidated.isPending ||
    refundOrder.isPending

  return (
    <Box sx={{ minHeight: 'calc(100vh - 72px)', bgcolor: 'background.default' }}>
      <Stack sx={{ p: { xs: 2.5, md: 4 }, maxWidth: 1080, mx: 'auto' }} spacing={2.5}>
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
                Hoàn tất đơn & Giao hàng — Hub
              </Typography>
              <Typography color="text.secondary">
                Gộp nhiều đơn cùng khách vào một kiện (trong 24 giờ) · Ghi mã vận đơn · Xác nhận giao hàng · Quản lý hoàn tiền
              </Typography>
            </Box>
          </Stack>
        </Stack>

        {errorMessage && (
          <Alert severity="error" onClose={() => setErrorMessage(null)}>
            {errorMessage}
          </Alert>
        )}
        {successMessage && (
          <Alert severity="success" onClose={() => setSuccessMessage(null)}>
            {successMessage}
          </Alert>
        )}

        {/* Consolidation suggestions when >=2 eligible orders from same customer + address within <=24h */}
        {suggestedClusters.length > 0 && (
          <Box
            sx={{
              p: 2.5,
              borderRadius: 3,
              border: '1px solid rgba(56, 189, 248, 0.45)',
              bgcolor: 'rgba(56, 189, 248, 0.08)',
            }}
          >
            <Stack spacing={2}>
              <Stack direction="row" spacing={1} alignItems="center">
                <CallMergeRounded color="info" />
                <Typography sx={{ fontWeight: 800, color: 'text.primary' }}>
                  Đề xuất đóng gói chung đơn cùng khách hàng
                </Typography>
                <Chip
                  size="small"
                  color="info"
                  label={`${suggestedClusters.length} cụm đủ điều kiện`}
                />
              </Stack>
              <Typography variant="body2" color="text.secondary">
                Các đơn dưới đây cùng khách hàng, cùng địa chỉ nhận và hoàn tất kiểm định cách nhau trong vòng 24 giờ. Đóng gói chung thành 1 kiện giúp dùng chung một mã vận đơn; mọi khiếu nại và hoàn tiền vẫn được xử lý tách biệt theo từng đơn gốc.
              </Typography>

              <Stack spacing={1.5}>
                {suggestedClusters.map((cluster) => (
                  <Box
                    key={cluster.groupKey}
                    sx={{
                      p: 2,
                      borderRadius: 2,
                      bgcolor: 'background.paper',
                      border: '1px solid rgba(255,255,255,0.12)',
                    }}
                  >
                    <Stack
                      direction={{ xs: 'column', md: 'row' }}
                      justifyContent="space-between"
                      alignItems={{ xs: 'stretch', md: 'center' }}
                      spacing={1.5}
                    >
                      <Box>
                        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap sx={{ mb: 0.5 }}>
                          {cluster.orders.map((o) => (
                            <Chip key={o.id} label={o.orderNumber} size="small" color="primary" />
                          ))}
                          <Chip
                            label={`${cluster.orders.length} đơn cùng khách`}
                            size="small"
                            variant="outlined"
                            color="info"
                          />
                        </Stack>
                        <Typography variant="body2" color="text.secondary">
                          Địa chỉ nhận: <strong>{cluster.deliveryAddressSummary}</strong>
                        </Typography>
                      </Box>
                      <Button
                        variant="contained"
                        color="info"
                        startIcon={<CallMergeRounded />}
                        disabled={busy}
                        onClick={() => openConsolidateDialog(cluster)}
                      >
                        Đóng gói gộp ({cluster.orders.length} đơn → 1 kiện)
                      </Button>
                    </Stack>
                  </Box>
                ))}
              </Stack>
            </Stack>
          </Box>
        )}

        {queue.isError ? <Alert severity="error">Không tải được danh sách cần giao.</Alert> : null}
        {queue.isLoading ? <CircularProgress sx={{ alignSelf: 'center' }} /> : null}

        {!queue.isLoading && orders.length === 0 ? (
          <Box sx={{ p: 6, textAlign: 'center', borderRadius: 3, border: '1px dashed rgba(255,255,255,0.15)' }}>
            <InventoryRounded sx={{ fontSize: 42, color: 'text.disabled', mb: 1 }} />
            <Typography color="text.secondary">Chưa có đơn nào chờ ghi vận đơn hoặc xác nhận giao.</Typography>
          </Box>
        ) : (
          <Stack spacing={2}>
            {orders.map((o) => {
              const totalAmt = o.totalAmount ?? 0
              const refundedAmt = o.refundedAmount ?? 0
              const remainingRefund = Math.max(0, totalAmt - refundedAmt)
              const companions = o.suggestedCompanionOrderNumbers ?? []
              return (
                <Box
                  key={o.id}
                  sx={{
                    p: { xs: 2, md: 3 },
                    borderRadius: 3,
                    border: '1px solid rgba(255,255,255,0.1)',
                    bgcolor: 'background.paper',
                  }}
                >
                  <Stack
                    direction={{ xs: 'column', md: 'row' }}
                    justifyContent="space-between"
                    alignItems={{ xs: 'stretch', md: 'center' }}
                    spacing={2}
                  >
                    <Box>
                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap sx={{ mb: 0.75 }}>
                        <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{o.orderNumber}</Typography>
                        <Chip label={statusLabel[o.status]} size="small" color={statusColor[o.status]} />
                        {o.consolidatedShipmentId && (
                          <Chip
                            icon={<CallMergeRounded />}
                            label={`Kiện gộp #${o.consolidatedShipmentId.slice(0, 8)}`}
                            size="small"
                            color="info"
                            variant="outlined"
                          />
                        )}
                        {companions.length > 0 && o.status === 'QualityCheck' && (
                          <Chip
                            label={`Có thể gộp với: ${companions.join(', ')}`}
                            size="small"
                            color="info"
                          />
                        )}
                        {o.status === 'Delivered' && (
                          <Chip
                            label={o.receiptConfirmed ? 'Khách đã xác nhận' : 'Chờ khách xác nhận'}
                            size="small"
                            variant="outlined"
                            color={o.receiptConfirmed ? 'success' : 'default'}
                          />
                        )}
                      </Stack>

                      <Typography variant="body2" color="text.secondary">
                        Địa chỉ: {o.deliveryAddressSummary || '—'} · Sẵn sàng đóng gói: {fmtDateTime(o.readyForPackingAtUtc)}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Hạn giao: {fmtDate(o.promisedDeliveryDate)}
                        {o.trackingNumber ? ` · Mã vận đơn: ${o.trackingNumber}` : ' · Chưa có vận đơn'}
                      </Typography>
                      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                        Giá trị đơn gốc: <strong>{fmtVnd(totalAmt)}</strong>
                        {refundedAmt > 0 && (
                          <>
                            {' · '}Đã hoàn: <strong>{fmtVnd(refundedAmt)}</strong>
                          </>
                        )}
                        {' · '}Hạn mức hoàn còn lại:{' '}
                        <strong>{fmtVnd(remainingRefund)}</strong>
                      </Typography>
                    </Box>

                    <Stack
                      direction="row"
                      spacing={1}
                      flexWrap="wrap"
                      useFlexGap
                      justifyContent={{ xs: 'flex-start', md: 'flex-end' }}
                    >
                      {o.status === 'QualityCheck' && (
                        <Button
                          variant="contained"
                          color="primary"
                          startIcon={<LocalShippingRounded />}
                          disabled={busy}
                          onClick={() => openShipDialog(o)}
                        >
                          Ghi vận đơn lẻ
                        </Button>
                      )}
                      {o.status === 'Shipping' && (
                        <>
                          <Button
                            variant="contained"
                            color="success"
                            startIcon={deliver.isPending ? <CircularProgress size={16} color="inherit" /> : <CheckRounded />}
                            disabled={busy}
                            onClick={() => handleDeliver(o)}
                          >
                            Xác nhận đã giao
                          </Button>
                          {o.consolidatedShipmentId && (
                            <Button
                              variant="outlined"
                              color="info"
                              startIcon={<CallMergeRounded />}
                              disabled={busy}
                              onClick={() => handleDeliverConsolidatedPackage(o.consolidatedShipmentId!)}
                            >
                              Giao cả kiện gộp
                            </Button>
                          )}
                        </>
                      )}
                      <Button
                        variant="outlined"
                        color="warning"
                        size="small"
                        startIcon={<CurrencyExchangeRounded />}
                        disabled={busy || remainingRefund <= 0}
                        onClick={() => openRefundDialog(o)}
                      >
                        Hoàn tiền đơn gốc
                      </Button>
                    </Stack>
                  </Stack>
                </Box>
              )
            })}
          </Stack>
        )}
      </Stack>

      {/* Single Order Ship Dialog */}
      <Dialog
        open={shipTarget !== null}
        onClose={() => {
          if (!ship.isPending) setShipTarget(null)
        }}
        PaperProps={{ sx: { bgcolor: '#121212', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.15)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 800 }}>
          Ghi vận đơn cho đơn {shipTarget?.orderNumber}
        </DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Mã vận đơn là bắt buộc (BR). Đơn sẽ chuyển sang trạng thái &quot;Đang giao&quot;.
          </Typography>
          <TextField
            label="Mã vận đơn"
            value={tracking}
            onChange={(e) => setTracking(e.target.value)}
            fullWidth
            autoFocus
            required
            placeholder="VD: VNPost-123456789"
            inputProps={{ maxLength: 128 }}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, pt: 1, borderTop: '1px solid rgba(255,255,255,0.08)' }}>
          <Button onClick={() => setShipTarget(null)} color="inherit" disabled={ship.isPending} sx={{ color: 'text.secondary' }}>
            Hủy
          </Button>
          <Button
            variant="contained"
            color="primary"
            startIcon={ship.isPending ? <CircularProgress size={16} color="inherit" /> : <LocalShippingRounded />}
            disabled={ship.isPending || !tracking.trim()}
            onClick={handleShip}
          >
            {ship.isPending ? 'Đang lưu…' : 'Xác nhận ghi vận đơn'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Consolidated Package Ship Dialog (FR-HUB-006 AC01) */}
      <Dialog
        open={consolidateCluster !== null}
        onClose={() => {
          if (!shipConsolidated.isPending) setConsolidateCluster(null)
        }}
        maxWidth="sm"
        fullWidth
        PaperProps={{ sx: { bgcolor: '#121212', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.15)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 800 }}>
          Đóng gói gộp nhiều đơn cùng khách (1 kiện · 1 vận đơn)
        </DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              Địa chỉ nhận chung: <strong>{consolidateCluster?.deliveryAddressSummary}</strong>
            </Typography>
            <Divider sx={{ borderColor: 'rgba(255,255,255,0.1)' }} />
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              Chọn các đơn đưa vào kiện gộp (tối thiểu 2 đơn):
            </Typography>
            <Stack spacing={1}>
              {consolidateCluster?.orders.map((o) => (
                <FormControlLabel
                  key={o.id}
                  control={
                    <Checkbox
                      checked={selectedClusterOrderIds.includes(o.id)}
                      onChange={() => toggleClusterOrder(o.id)}
                    />
                  }
                  label={`${o.orderNumber} — Giá trị gốc: ${fmtVnd(o.totalAmount)} (QC: ${fmtDateTime(o.readyForPackingAtUtc)})`}
                />
              ))}
            </Stack>
            <TextField
              label="Mã vận đơn chung cho kiện gộp"
              value={consolidatedTracking}
              onChange={(e) => setConsolidatedTracking(e.target.value)}
              fullWidth
              required
              placeholder="VD: GHTK-CONSOL-998877"
              inputProps={{ maxLength: 128 }}
            />
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, pt: 1, borderTop: '1px solid rgba(255,255,255,0.08)' }}>
          <Button
            onClick={() => setConsolidateCluster(null)}
            color="inherit"
            disabled={shipConsolidated.isPending}
            sx={{ color: 'text.secondary' }}
          >
            Hủy
          </Button>
          <Button
            variant="contained"
            color="info"
            startIcon={shipConsolidated.isPending ? <CircularProgress size={16} color="inherit" /> : <CallMergeRounded />}
            disabled={shipConsolidated.isPending || selectedClusterOrderIds.length < 2 || !consolidatedTracking.trim()}
            onClick={handleShipConsolidated}
          >
            {shipConsolidated.isPending ? 'Đang gộp kiện…' : `Ghi 1 vận đơn cho ${selectedClusterOrderIds.length} đơn`}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Original-Order-Scoped Refund Dialog (FR-HUB-006 AC02 / BR-LOG-002) */}
      <Dialog
        open={refundTarget !== null}
        onClose={() => {
          if (!refundOrder.isPending) setRefundTarget(null)
        }}
        maxWidth="sm"
        fullWidth
        PaperProps={{ sx: { bgcolor: '#121212', backgroundImage: 'none', border: '1px solid rgba(255,255,255,0.15)' } }}
      >
        <DialogTitle sx={{ color: 'text.primary', fontWeight: 800 }}>
          Hoàn tiền theo phạm vi đơn gốc — {refundTarget?.orderNumber}
        </DialogTitle>
        <DialogContent>
          {refundTarget && (
            <Stack spacing={2} sx={{ mt: 0.5 }}>
              <Alert severity="info">
                Dù các đơn hàng được đóng gói chung trong cùng kiện gộp, mọi khiếu nại và hoàn tiền chỉ áp dụng trong phạm vi giá trị của chính đơn gốc ({refundTarget.orderNumber}), không cộng dồn hay ảnh hưởng đến các đơn khác cùng kiện.
              </Alert>
              <Typography variant="body2" color="text.secondary">
                Tổng giá trị đơn gốc: <strong>{fmtVnd(refundTarget.totalAmount)}</strong> · Đã hoàn:{' '}
                <strong>{fmtVnd(refundTarget.refundedAmount)}</strong> · Tối đa có thể hoàn thêm:{' '}
                <strong>{fmtVnd(Math.max(0, (refundTarget.totalAmount ?? 0) - (refundTarget.refundedAmount ?? 0)))}</strong>
              </Typography>
              <TextField
                label="Số tiền hoàn (VND)"
                type="number"
                value={refundAmount}
                onChange={(e) => setRefundAmount(e.target.value)}
                fullWidth
                required
              />
              <TextField
                label="Lý do hoàn tiền / khiếu nại đơn gốc"
                value={refundReason}
                onChange={(e) => setRefundReason(e.target.value)}
                fullWidth
                required
                multiline
                minRows={2}
                placeholder="VD: Lỗi bề mặt chi tiết trong đơn này sau kiểm tra"
              />
            </Stack>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2.5, pt: 1, borderTop: '1px solid rgba(255,255,255,0.08)' }}>
          <Button
            onClick={() => setRefundTarget(null)}
            color="inherit"
            disabled={refundOrder.isPending}
            sx={{ color: 'text.secondary' }}
          >
            Hủy
          </Button>
          <Button
            variant="contained"
            color="warning"
            startIcon={refundOrder.isPending ? <CircularProgress size={16} color="inherit" /> : <CurrencyExchangeRounded />}
            disabled={refundOrder.isPending || !refundAmount || !refundReason.trim()}
            onClick={handleRefund}
          >
            {refundOrder.isPending ? 'Đang xử lý…' : 'Xác nhận hoàn tiền đơn gốc'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
